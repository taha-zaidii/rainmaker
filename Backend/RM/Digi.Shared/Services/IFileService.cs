using Microsoft.AspNetCore.Http;

namespace Digi.Shared.Services
{
    public interface IFileService
    {
    
        Task<string> SaveFileAsync(IFormFile file, string companyId, string module, string controller, string fileType = null);
        bool FileExists(string fileName, string companyId, string module, string controller, string fileType = "documents");
        string GetFilePath(string fileName, string companyId, string module, string controller, string fileType = "documents");
        string GetFileUrl(string fileName, string companyId, string module, string controller, string fileType = "documents");
        bool DeleteFile(string fileName, string companyId, string module, string controller, string fileType = "documents");
        IEnumerable<string> GetFileList(string companyId, string module, string controller, string fileType = "documents");
        bool IsImageFile(IFormFile file);
        Task<Stream> GetFileStreamAsync(string fileName, string companyId, string module, string controller, string fileType = "documents");
        string GetFullUrl(string relativePath);
        Task<string> SaveFilePurchaseRequestAsync(byte[] fileBytes, string originalFileName, string CompanyName, string module, string controller, string fileType = "pdf");
    }
}
