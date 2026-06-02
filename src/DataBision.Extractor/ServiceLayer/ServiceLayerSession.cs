namespace DataBision.Extractor.ServiceLayer;

public sealed class ServiceLayerSession
{
    public string SessionId { get; }
    public string Cookie { get; }
    public int TimeoutMinutes { get; }
    public DateTime ExpiresAt { get; }

    // Renew 2 minutes before actual expiry
    private static readonly TimeSpan RenewBuffer = TimeSpan.FromMinutes(2);

    public ServiceLayerSession(string sessionId, int timeoutMinutes)
    {
        SessionId = sessionId;
        Cookie = $"B1SESSION={sessionId}";
        TimeoutMinutes = timeoutMinutes;
        ExpiresAt = DateTime.UtcNow.AddMinutes(timeoutMinutes);
    }

    public bool IsNearExpiry => DateTime.UtcNow >= ExpiresAt - RenewBuffer;
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
}
