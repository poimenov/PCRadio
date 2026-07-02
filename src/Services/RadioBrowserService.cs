using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PCRadio.Common;
using PCRadio.DataAccess.Interfaces;
using PCRadio.DataAccess.Models;
using PCRadio.Services.Interfaces;
using RadioBrowser;
using RadioBrowser.Models;

namespace PCRadio.Services;

public class RadioBrowserService : IRadioBrowserService
{
    private readonly IRadioBrowserClient _client;
    private readonly AppSettings _appSettings;
    private readonly ICountries _countries;
    private readonly IGenres _genres;
    private readonly ILogger<IRadioBrowserService> _logger;
    private static IEnumerable<CountryAndCount>? _countryAndCounts;

    public RadioBrowserService(IRadioBrowserClient client, ICountries countries,
        IGenres genres, IOptions<AppSettings> options, ILogger<IRadioBrowserService> logger)
    {
        _client = client;
        _countries = countries;
        _genres = genres;
        _appSettings = options.Value;
        _logger = logger;
    }

    public async Task<OperationResult<IEnumerable<RadioBrowser.Models.StationInfo>>> GetStationsAsync(AdvancedSearchOptions options)
    {
        options.Order = "votes";
        options.Reverse = true;
        options.Limit = Convert.ToUInt32(_appSettings.PageSize);
        var retVal = new OperationResult<IEnumerable<RadioBrowser.Models.StationInfo>>() { Success = false };

        try
        {
            retVal.Result = await _client.Search.AdvancedAsync(options);
            retVal.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            retVal.Exception = ex;
            retVal.Message = ex.Message;
        }

        return retVal;
    }

    public async Task<OperationResult<IEnumerable<CountryAndCount>>> GetCountriesAsync()
    {
        var retVal = new OperationResult<IEnumerable<CountryAndCount>>() { Success = false };

        try
        {
            if (_countryAndCounts is null)
            {
                var countries = await _client.Lists.GetCountriesCodesAsync();
                var dbCountries = _countries.GetAll().Select(c => new CountryAndCount
                {
                    Name = AppCultures.CountryCodes[c.Id],
                    Stationcount = 0,
                    CountryId = c.Id,
                    CountryName = c.Name
                });
                var countryAndCountList = dbCountries.Where(dc => countries.Any(c => dc.Name == c.Name.ToLower())).ToList();
                countryAndCountList.ForEach(dc => dc.Stationcount =
                    countries.First(c => c.Name.ToLower() == dc.Name).Stationcount);
                _countryAndCounts = countryAndCountList.OrderByDescending(c => c.Stationcount);
            }

            retVal.Result = _countryAndCounts;
            retVal.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            retVal.Exception = ex;
            retVal.Message = ex.Message;
        }

        return retVal;
    }

    public async Task<OperationResult<IEnumerable<NameAndCount>>> GetTagsAsync(string? filter = null)
    {
        var retVal = new OperationResult<IEnumerable<NameAndCount>>() { Success = false };

        try
        {
            var tags = await _client.Lists.GetTagsAsync(filter);
            retVal.Result = tags.Select(t => new NameAndCount() { Name = t.Name, Stationcount = t.Stationcount })
                .OrderByDescending(t => t.Stationcount);
            retVal.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            retVal.Exception = ex;
            retVal.Message = ex.Message;
        }

        return retVal;
    }

    public async Task<OperationResult<Station>> GetDataAccessStation(RadioBrowser.Models.StationInfo? stationInfo)
    {
        var retVal = new OperationResult<Station>() { Success = false };
        if (stationInfo is null)
        {
            retVal.Message = "StationInfo is null";
            return retVal;
        }

        try
        {
            var sb = new StringBuilder();
            sb.Append($"Codec: {stationInfo.Codec}");
            sb.Append($"; Bitrate: {stationInfo.Bitrate}kbps");
            if (stationInfo.Language.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var strLanguages = string.Join(", ", stationInfo.Language);
                sb.Append($"; Languages: {strLanguages}");
            }
            sb.Append($"; Station homepage: {stationInfo.Homepage}");
            if (stationInfo.Tags.Any(s => !string.IsNullOrWhiteSpace(s)))
            {
                var strTags = string.Join(", ", stationInfo.Tags);
                sb.Append($"; Tags: {strTags}");
            }

            var station = new Station()
            {
                Name = stationInfo.Name,
                Logo = stationInfo.Favicon?.ToString() ?? string.Empty,
                Stream = stationInfo.UrlResolved.ToString(),
                Description = sb.ToString()
            };

            var countriesResult = await GetCountriesAsync();
            if (!countriesResult.Success)
            {
                retVal.Message = countriesResult.Message;
                retVal.Exception = countriesResult.Exception;
                return retVal;
            }

            var countries = countriesResult.Result;
            if (countries is not null && countries.Any(c => c.Name == stationInfo.CountryCode.ToLower()))
            {
                station.CountryId = countries.First(c => c.Name == stationInfo.CountryCode.ToLower()).CountryId;
            }

            var subGenres = _genres.GetAllSubGenres();
            var genres = _genres.GetAll();
            var subGenresIds = stationInfo.Tags.Where(t => AppCultures.SubGenresEn.ContainsKey(t)).Select(t => AppCultures.SubGenresEn[t]).Distinct();
            station.StationSubGenres = subGenres.Where(sg => subGenresIds.Contains(sg.Id)).Select(sg => GetStationSubGenre(sg, station)).ToList();

            var genresIds = stationInfo.Tags.Where(t => AppCultures.GenresEn.ContainsKey(t)).Select(t => AppCultures.GenresEn[t]).Distinct();
            var genresBySubgenresIds = subGenres.Where(sg => subGenresIds.Contains(sg.Id)).Select(sg => sg.GenreId);
            var unionGenresIds = genresIds.Union(genresBySubgenresIds).Distinct();
            station.StationGenres = genres.Where(g => unionGenresIds.Contains(g.Id)).Select(g => GetStationGenre(g, station)).ToList();

            retVal.Result = station;
            retVal.Success = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, ex.Message);
            retVal.Exception = ex;
            retVal.Message = ex.Message;
        }

        return retVal;
    }

    private static StationSubGenre GetStationSubGenre(SubGenre subGenre, Station station) => new StationSubGenre() { Station = station, SubGenre = subGenre, SubGenreId = subGenre.Id };
    private static StationGenre GetStationGenre(Genre genre, Station station) => new StationGenre() { Station = station, Genre = genre, GenreId = genre.Id };
}