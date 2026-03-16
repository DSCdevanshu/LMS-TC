using TCBackend.Services.IServices;

namespace TCBackend.Services
{
    public class StorageService : IStorageService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public StorageService(IWebHostEnvironment env, IHttpContextAccessor httpContextAccessor)
        {
            _env = env;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string folderName)
        {
            string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            string folderPath = Path.Combine(rootPath, folderName);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);

            string fullPath = Path.Combine(folderPath, uniqueFileName);

            using (var stream = new FileStream(fullPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return $"/{folderName}/{uniqueFileName}";
        }

        public Task<string> GetSecureFileUrlAsync(string dbFilePath)
        {
            if (string.IsNullOrEmpty(dbFilePath)) return Task.FromResult<string>(null);

            var request = _httpContextAccessor.HttpContext?.Request;
            if (request != null)
            {
                var baseUrl = $"{request.Scheme}://{request.Host}{request.PathBase}";

                return Task.FromResult($"{baseUrl}{dbFilePath}");
            }

            return Task.FromResult(dbFilePath);
        }
        public Task DeleteFileAsync(string dbFilePath)
        {
            if (string.IsNullOrEmpty(dbFilePath)) return Task.CompletedTask;
            string rootPath = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            string fullPath = Path.Combine(rootPath, dbFilePath.TrimStart('/').Replace('/', '\\'));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
            }

            return Task.CompletedTask;
        }
    }
}
