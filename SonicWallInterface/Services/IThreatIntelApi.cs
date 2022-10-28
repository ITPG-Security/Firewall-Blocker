namespace SonicWallInterface.Services
{
    public interface IThreatIntelApi
    {
        Task<List<string>> GetCurrentTIIPs();
    }
}
