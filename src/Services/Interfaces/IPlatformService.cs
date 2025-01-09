namespace PCRadio.Services.Interfaces;

public enum Platform : byte
{
    Linux,
    MacOs,
    Windows,
    Unknown
}

public interface IPlatformService
{
    Platform GetPlatform();
}
