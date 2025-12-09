namespace PCRadio.DataAccess.Interfaces;

public interface IDatabaseMigrator
{
    Task MigrateDatabaseAsync();
}
