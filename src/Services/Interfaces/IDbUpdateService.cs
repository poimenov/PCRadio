namespace PCRadio.Services.Interfaces;

public interface IDbUpdateService
{
    Task<bool> UpdateDatabaseAsync();
}
