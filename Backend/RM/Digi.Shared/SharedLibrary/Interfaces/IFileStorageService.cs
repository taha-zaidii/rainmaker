using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface IFileStorageService
    {
        Task<string> SaveFileAsync(IFormFile file, string entityName, string documentName);
        Task<string> MoveToPermanentStorage(string tempPath, string containerName);
        Task DeleteTempFilesAsync(List<string> tempFileIds);
        Task DeleteAttachmentByIdAsync(string? attachmentReference, int attachmentId);
        Task DeleteFileAsync(string relativeFilePath);
        Task<byte[]> GetFileAsync(string relativeFilePath);
        Task<bool> VerifyTempFile(string tempPath);
        string GetFullUrl(string relativePath);

        Task<string> SaveFileAsync(IFormFile file, string subPath = "");
        Task<string> SaveByteArrayAsync(byte[] fileBytes, string fileName, string subPath = "");
        bool FileExists(string fileName, string subPath = "");
        string GetFilePath(string fileName, string subPath = "");
        string GetDirectoryPath(string subPath = "");
        string GetFileUrl(string fileName, string subPath = "");
    }
}

