namespace FirewallBlocker.Services
{
    public interface IThreatIntelApi
    {
        Task<List<string>> GetCurrentTIIPs();
    }
}
