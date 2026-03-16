namespace TCBackend.Services.IServices
{
    public interface IStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string folderName);
        Task<string> GetSecureFileUrlAsync(string dbFilePath);
        Task DeleteFileAsync(string dbFilePath);
    }
}
