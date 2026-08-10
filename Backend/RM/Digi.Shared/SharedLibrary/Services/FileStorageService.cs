using Digi.Shared.SharedLibrary.Interfaces;
using Digi.Shared.SharedLibrary.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Digi.Shared.SharedLibrary.Services
{
    /// <summary>
    /// IFormFile-oriented upload service for the Recruitment AI pipeline.
    /// Handles upload validation, entity-scoped path organisation, and byte-array
    /// retrieval for resume parsing. Registered as <see cref="IFileStorageService"/>.
    /// <para>
    /// Intentional split: this service owns the <em>upload contract</em> (entity name,
    /// document name, metadata). <see cref="Digi.Shared.Services.FileService"/> owns
    /// low-level disk I/O, deduplication, and path resolution.
    /// </para>
    /// </summary>
    public class FileStorageService : IFileStorageService
    {
        private readonly ILogger<FileStorageService> _logger;
        private readonly IConfiguration _configuration;
        private readonly FileStorageSettings _settings;

        public FileStorageService(
            ILogger<FileStorageService> logger,
            IConfiguration configuration,
            IOptions<FileStorageSettings> options)
        {
            _logger = logger;
            _configuration = configuration;
            _settings = options.Value;
        }

        public async Task<string> SaveFileAsync(IFormFile file, string entityName, string documentName)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var extension = Path.GetExtension(file.FileName).ToLower().TrimStart('.');
            var typeFolder = GetFileTypeFolder(extension);
            var safeEntityName = MakeSafeFolderName(entityName);
            var safeDocumentName = MakeSafeFolderName(documentName);

            var uploadsFolder = Path.Combine(GetUploadsRootFolder(), typeFolder, safeEntityName, safeDocumentName);
            Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = $"{Guid.NewGuid()}.{extension}";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var relativePath = Path.Combine(typeFolder, safeEntityName, safeDocumentName, uniqueFileName).Replace("\\", "/");
            _logger.LogInformation("File saved at {Path}", relativePath);
            return relativePath;
        }

        public async Task<string> MoveToPermanentStorage(string tempPath, string containerName)
        {
            var sourcePath = Path.Combine(GetUploadsRootFolder(), tempPath);
            if (!File.Exists(sourcePath))
                throw new FileNotFoundException("Temporary file not found", tempPath);

            var extension = Path.GetExtension(tempPath).ToLower().TrimStart('.');
            var typeFolder = GetFileTypeFolder(extension);

            var destinationFolder = Path.Combine(GetUploadsRootFolder(), typeFolder, containerName);
            Directory.CreateDirectory(destinationFolder);

            var fileName = Path.GetFileName(tempPath);
            var destinationPath = Path.Combine(destinationFolder, fileName);

            if (File.Exists(destinationPath))
            {
                fileName = $"{Path.GetFileNameWithoutExtension(fileName)}_{Guid.NewGuid():N}{Path.GetExtension(fileName)}";
                destinationPath = Path.Combine(destinationFolder, fileName);
            }

            File.Move(sourcePath, destinationPath);
            var relativePath = Path.Combine(typeFolder, containerName, fileName).Replace("\\", "/");
            _logger.LogInformation("File moved to {Path}", relativePath);
            return relativePath;
        }

        public async Task DeleteTempFilesAsync(List<string> tempFileIds)
        {
            foreach (var tempFileId in tempFileIds)
            {
                if (string.IsNullOrWhiteSpace(tempFileId))
                {
                    continue;
                }

                var tempFilePath = Path.Combine(GetUploadsRootFolder(), "TempFiles", tempFileId);
                if (File.Exists(tempFilePath))
                {
                    File.Delete(tempFilePath);
                    _logger.LogInformation("Temp file deleted: {Path}", tempFilePath);
                }
            }

            await Task.CompletedTask;
        }

        public async Task DeleteAttachmentByIdAsync(string? attachmentReference, int attachmentId)
        {
            if (string.IsNullOrWhiteSpace(attachmentReference))
            {
                _logger.LogWarning("No attachment reference supplied for attachmentId {AttachmentId}", attachmentId);
                return;
            }

            await DeleteFileAsync(attachmentReference);
        }

        public async Task DeleteFileAsync(string relativeFilePath)
        {
            var fullPath = Path.Combine(GetUploadsRootFolder(), relativeFilePath);
            if (!File.Exists(fullPath))
            {
                _logger.LogWarning("File not found when attempting delete: {Path}", relativeFilePath);
                return;
            }

            File.Delete(fullPath);
            _logger.LogInformation("File deleted: {Path}", relativeFilePath);
            await Task.CompletedTask;
        }

        public async Task<byte[]> GetFileAsync(string relativeFilePath)
        {
            var fullPath = Path.Combine(GetUploadsRootFolder(), relativeFilePath);
            if (!File.Exists(fullPath))
                throw new FileNotFoundException("File not found", relativeFilePath);

            return await File.ReadAllBytesAsync(fullPath);
        }

        public async Task<bool> VerifyTempFile(string tempPath)
        {
            var fullPath = Path.Combine(GetUploadsRootFolder(), tempPath);
            return await Task.FromResult(File.Exists(fullPath));
        }

        public string GetFullUrl(string relativePath)
        {
            if (string.IsNullOrWhiteSpace(relativePath))
                return string.Empty;

            var profileImagePath = Path.Combine("storage", relativePath).Replace("\\", "/");
            var baseUrl = _settings.BaseUrl ?? _configuration["AppSettings:BaseUrl"] ?? "http://localhost";
            return $"{baseUrl.TrimEnd('/')}/{profileImagePath.TrimStart('/')}";
        }

        public async Task<string> SaveFileAsync(IFormFile file, string subPath = "")
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            var extension = Path.GetExtension(file.FileName).ToLower().TrimStart('.');
            var fileName = $"{Guid.NewGuid()}.{extension}";

            var uploadsFolder = Path.Combine(GetUploadsRootFolder(), subPath);
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await file.CopyToAsync(stream);

            var relativePath = Path.Combine(subPath, fileName).Replace("\\", "/");
            _logger.LogInformation("File saved at {Path}", relativePath);
            return relativePath;
        }

        public bool FileExists(string fileName, string subPath = "")
        {
            var filePath = GetFilePath(fileName, subPath);
            return File.Exists(filePath);
        }

        public string GetFilePath(string fileName, string subPath = "")
        {
            var uploadsFolder = Path.Combine(GetUploadsRootFolder(), subPath);
            return Path.Combine(uploadsFolder, fileName);
        }

        public string GetDirectoryPath(string subPath = "")
        {
            return Path.Combine(GetUploadsRootFolder(), subPath);
        }

        public string GetFileUrl(string fileName, string subPath = "")
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return string.Empty;

            var relativePath = Path.Combine(subPath, fileName).Replace("\\", "/");
            var baseUrl = _settings.BaseUrl ?? _configuration["AppSettings:BaseUrl"] ?? "http://localhost";
            return $"{baseUrl.TrimEnd('/')}/storage/{relativePath.TrimStart('/')}";
        }

        public async Task<string> SaveByteArrayAsync(byte[] fileBytes, string fileName, string subPath = "")
        {
            if (fileBytes == null || fileBytes.Length == 0)
                throw new ArgumentException("File bytes are empty");

            var uploadsFolder = Path.Combine(GetUploadsRootFolder(), subPath);
            Directory.CreateDirectory(uploadsFolder);

            var filePath = Path.Combine(uploadsFolder, fileName);

            await File.WriteAllBytesAsync(filePath, fileBytes);

            var relativePath = Path.Combine(subPath, fileName).Replace("\\", "/");
            _logger.LogInformation("File saved at {Path}", relativePath);
            return relativePath;
        }

        private string GetUploadsRootFolder()
        {
            return _settings.RootPath
                ?? _configuration["FileStorage:RootPath"]
                ?? Path.Combine(Directory.GetCurrentDirectory(), "Files");
        }

        private static string MakeSafeFolderName(string name)
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }

            return name.Trim();
        }

        private static string GetFileTypeFolder(string extension)
        {
            return extension switch
            {
                "jpg" or "jpeg" or "png" or "gif" => "images",
                "pdf" => "pdfs",
                "doc" or "docx" or "txt" => "documents",
                _ => "others"
            };
        }
    }
}

