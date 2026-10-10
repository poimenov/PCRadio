namespace PCRadio.Services.Interfaces;

public interface IDbUpdateService
{
    Task<bool> UpdateDatabaseAsync();
    Task<bool> UpdateDatabaseFromFileAsync(string localFilePath);
}
