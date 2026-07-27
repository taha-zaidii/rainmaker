namespace Digi.Shared.Services
{
    /// <summary>
    /// Pushes instant logout to connected web (SignalR) and mobile (FCM) before DB session revoke completes.
    /// </summary>
    public interface ISessionRevocationNotifier
    {
        Task NotifySessionsRevokedAsync(int userId, int? companyId, string reason, CancellationToken cancellationToken = default);
    }
}
