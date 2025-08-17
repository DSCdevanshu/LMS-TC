namespace TCBackend.Services.IServices
{
    public interface IPermissionService
    {
        Task<bool> HasPermissionAsync(int userId, string permission);
    }
}
