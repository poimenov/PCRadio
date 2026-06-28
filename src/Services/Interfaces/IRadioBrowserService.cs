using System.Diagnostics.CodeAnalysis;
using PCRadio.Common;
using PCRadio.DataAccess.Models;
using PSC.CSharp.Library.CountryData;
using RadioBrowser.Models;
namespace PCRadio.Services.Interfaces;

public class CountryAndCount : NameAndCount
{
    private static CountryHelper countryHelper = new CountryHelper();
    public int CountryId { get; set; }
    public string? CountryName { get; set; }
}

public class AdvancedSearchOptionsComparer : IEqualityComparer<AdvancedSearchOptions>
{
    public bool Equals(AdvancedSearchOptions? x, AdvancedSearchOptions? y)
    {
        if (ReferenceEquals(x, y))
        {
            return true;
        }

        if (x is null || y is null)
            return false;

        return x.BitrateMax == y.BitrateMax
            && x.BitrateMin == y.BitrateMin
            && x.Codec == y.Codec
            && x.Country == y.Country
            && x.Countrycode == y.Countrycode
            && x.CountryExact == y.CountryExact
            && x.Language == y.Language
            && x.LanguageExact == y.LanguageExact
            && x.Limit == y.Limit
            && x.Name == y.Name
            && x.NameExact == y.NameExact
            && x.Offset == y.Offset
            && x.Order == y.Order
            && x.Reverse == y.Reverse
            && x.State == y.State
            && x.StateExact == y.StateExact
            && x.TagExact == y.TagExact
            && x.TagList == y.TagList;
    }

    public int GetHashCode([DisallowNull] AdvancedSearchOptions obj)
    {
        return HashCode.Combine(
            obj.Codec.GetHashCode(),
            obj.Countrycode.GetHashCode(),
            obj.Language.GetHashCode(),
            obj.Limit.GetHashCode(),
            obj.Offset.GetHashCode(),
            obj.Order.GetHashCode(),
            obj.Reverse.GetHashCode(),
            obj.TagList.GetHashCode());
    }
}

public interface IRadioBrowserService
{
    Task<OperationResult<IEnumerable<StationInfo>>> GetStationsAsync(AdvancedSearchOptions options);
    Task<OperationResult<IEnumerable<CountryAndCount>>> GetCountriesAsync(string? filter = null);
    Task<OperationResult<IEnumerable<NameAndCount>>> GetTagsAsync(string? filter = null);
    Task<OperationResult<Station>> GetDataAccessStation(StationInfo? stationInfo);
}