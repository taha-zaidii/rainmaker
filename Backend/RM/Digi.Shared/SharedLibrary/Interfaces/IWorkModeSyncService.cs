namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface IWorkModeSyncService
    {
        Task SyncWorkModeOnLoginAsync(int? companyId, int? employeeId);
    }
}
