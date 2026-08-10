using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Digi.Shared.Services
{
    /// <summary>
    /// Low-level disk I/O service. Handles path resolution, directory creation, hash-based
    /// deduplication, and secure file deletion. Used by any feature that stores arbitrary
    /// binary data to the configured <c>FileStorage:RootPath</c>.
    /// <para>
    /// Intentional split: this service owns <em>how</em> files land on disk.
    /// <see cref="Digi.Shared.SharedLibrary.Services.FileStorageService"/> owns the
    /// IFormFile upload contract used by the AI recruitment pipeline.
    /// </para>
    /// </summary>
    public class FileService : IFileService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileService> _logger;
        private readonly string _rootPath;

        public FileService(IConfiguration configuration, ILogger<FileService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            
            // Get RootPath from configuration, fallback to local wwwroot if access denied
            var configuredPath = _configuration["FileStorage:RootPath"];
            
            if (!string.IsNullOrEmpty(configuredPath))
            {
                try
                {
                    // Try to create the configured path
                    EnsureDirectoryExists(configuredPath);
                    _rootPath = configuredPath;
                    _logger.LogInformation("Using configured storage path: {Path}", _rootPath);
                }
                catch (UnauthorizedAccessException ex)
                {
                    _logger.LogWarning(ex, "Access denied to configured path: {Path}. Falling back to local wwwroot.", configuredPath);
                    _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage");
                    // Only create wwwroot in API Gateway, not in other modules
                    if (IsApiGateway())
                    {
                        EnsureDirectoryExists(_rootPath);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error accessing configured path: {Path}. Falling back to local wwwroot.", configuredPath);
                    _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage");
                    // Only create wwwroot in API Gateway, not in other modules
                    if (IsApiGateway())
                    {
                        EnsureDirectoryExists(_rootPath);
                    }
                }
            }
            else
            {
                // Fallback to local wwwroot only for API Gateway
                if (IsApiGateway())
                {
                    _rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "storage");
                    EnsureDirectoryExists(_rootPath);
                    _logger.LogInformation("Using local wwwroot storage path: {Path}", _rootPath);
                }
                else
                {
                    // For other modules, use API Gateway's storage path as default
                    var apiGatewayPath = Path.Combine(Directory.GetParent(Directory.GetCurrentDirectory())?.FullName ?? "", "Digi.APIGateways", "wwwroot", "storage");
                    _rootPath = apiGatewayPath;
                    _logger.LogInformation("Using API Gateway storage path: {Path}", _rootPath);
                }
            }
        }

        public async Task<string> SaveFileAsync(IFormFile file, string companyId, string module, string controller, string fileType = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                    throw new ArgumentException("File is empty");

                // Auto-detect file type if not specified
                if (string.IsNullOrEmpty(fileType))
                {
                    fileType = DetectFileType(file);
                }

                // Create advanced ERP directory structure: company/module/controller/fileType/
                var directoryPath = GetAdvancedDirectoryPath(companyId, module, controller, fileType);
                
                // Always create directories for centralized storage
                EnsureDirectoryExists(directoryPath);

                // Generate unique filename
                var fileName = GenerateUniqueFileName(file.FileName);
                var filePath = Path.Combine(directoryPath, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Return relative path
                var relativePath = Path.Combine(companyId, module, controller, fileType, fileName).Replace("\\", "/");
                _logger.LogInformation($"File saved with advanced ERP structure: {relativePath}");
                
                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error saving file {file?.FileName} to company {companyId}, module {module}, controller {controller}");
                throw;
            }
        }

        public bool FileExists(string fileName, string companyId, string module, string controller, string fileType = "documents")
        {
            var filePath = GetFilePath(fileName, companyId, module, controller, fileType);
            return File.Exists(filePath);
        }

        public string GetFilePath(string fileName, string companyId, string module, string controller, string fileType = "documents")
        {
            var directoryPath = GetAdvancedDirectoryPath(companyId, module, controller, fileType);
            return Path.Combine(directoryPath, fileName);
        }


        public bool DeleteFile(string fileName, string companyId, string module, string controller, string fileType = "documents")
        {
            try
            {
                var filePath = GetFilePath(fileName, companyId, module, controller, fileType);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation($"File deleted: {filePath}");
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting file {fileName} from company {companyId}, module {module}, controller {controller}");
                return false;
            }
        }

        public IEnumerable<string> GetFileList(string companyId, string module, string controller, string fileType = "documents")
        {
            try
            {
                var directoryPath = GetAdvancedDirectoryPath(companyId, module, controller, fileType);
                if (Directory.Exists(directoryPath))
                {
                    return Directory.GetFiles(directoryPath).Select(Path.GetFileName);
                }
                return Enumerable.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting file list for company {companyId}, module {module}, controller {controller}");
                return Enumerable.Empty<string>();
            }
        }

        private string DetectFileType(IFormFile file)
        {
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            // Image files
            var imageTypes = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".svg", ".ico", ".avif" };
            if (imageTypes.Contains(extension))
                return "images";
            
            // Document files
            var documentTypes = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".rtf" };
            if (documentTypes.Contains(extension))
                return "documents";
            
            // Video files
            var videoTypes = new[] { ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv" };
            if (videoTypes.Contains(extension))
                return "videos";
            
            // Audio files
            var audioTypes = new[] { ".mp3", ".wav", ".flac", ".aac", ".ogg", ".m4a" };
            if (audioTypes.Contains(extension))
                return "audio";
            
            // Archive files
            var archiveTypes = new[] { ".zip", ".rar", ".7z", ".tar", ".gz" };
            if (archiveTypes.Contains(extension))
                return "archives";
            
            // Default to documents for unknown types
            return "documents";
        }

        public bool IsImageFile(IFormFile file)
        {
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            return allowedExtensions.Contains(fileExtension);
        }

        public async Task<Stream> GetFileStreamAsync(string fileName, string companyId, string module, string controller, string fileType = "documents")
        {
            var filePath = GetFilePath(fileName, companyId, module, controller, fileType);
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {fileName}");

            return new FileStream(filePath, FileMode.Open, FileAccess.Read);
        }

        public string GetFileUrl(string fileName, string companyId, string module, string controller, string fileType = "documents")
        {
            // Get base URL from configuration
            var baseUrl = _configuration["AppSettings:BaseUrl"] ?? "https://localhost:7777";
            
            // Return full web-accessible URL for wwwroot storage
            return $"{baseUrl.TrimEnd('/')}/storage/{companyId}/{module}/{controller}/{fileType}/{fileName}";
        }
        public string GetFullUrl(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return null;
            var profileImagePath = Path.Combine("storage", relativePath);

            var baseUrl = _configuration["AppSettings:BaseUrl"];
            return $"{baseUrl.TrimEnd('/')}/{profileImagePath.TrimStart('/').Replace("\\", "/")}";
        }
        private string GetAdvancedDirectoryPath(string companyId, string module, string controller, string fileType)
        {
            // Advanced ERP structure: company/module/controller/fileType/
            return Path.Combine(_rootPath, companyId, module, controller, fileType);
        }

        private void EnsureDirectoryExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _logger.LogInformation("Created directory: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create directory: {Path}", path);
                throw;
            }
        }

        private bool IsApiGateway()
        {
            // Check if this is running in API Gateway by looking for specific assembly or configuration
            var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name;
            return assemblyName?.Contains("APIGateways") == true || 
                   _configuration["AppSettings:BaseUrl"]?.Contains("7777") == true;
        }

        private string GenerateUniqueFileName(string originalFileName)
        {
            var extension = Path.GetExtension(originalFileName);
           // var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName)
                   .Replace(" ", "_")
                   .Replace("(", "")
                   .Replace(")", "");
            
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
           

            var random = Guid.NewGuid().ToString("N")[..8];
            return $"{nameWithoutExtension}_{timestamp}_{random}{extension}";
        }

        private string ComputeFileHash(IFormFile file)
        {
            using var stream = file.OpenReadStream();
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(stream);
            return Convert.ToBase64String(hash);
        }


        public async Task<string> SaveFilePurchaseRequestAsync(byte[] fileBytes,string originalFileName,string CompanyName, string module, string controller, string fileType = "pdf")
        {
            try
            {
                if (fileBytes == null || fileBytes.Length == 0)
                    throw new ArgumentException("File bytes are empty");

                var directoryPath = GetAdvancedDirectoryPath(
                    CompanyName,
                    module,
                    controller,
                    fileType
                );

                EnsureDirectoryExists(directoryPath);

                var fileName = GenerateUniqueFileName(originalFileName);
                var filePath = Path.Combine(directoryPath, fileName);

                await File.WriteAllBytesAsync(filePath, fileBytes);

                var relativePath = Path.Combine(
                    CompanyName,
                    module,
                    controller,
                    fileType,
                    fileName
                ).Replace("\\", "/");

                _logger.LogInformation($"PDF saved: {relativePath}");

                return relativePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving PDF file");
                throw;
            }
        }

    }
}
