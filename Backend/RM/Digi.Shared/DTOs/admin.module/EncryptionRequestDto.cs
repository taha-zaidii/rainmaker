using System.ComponentModel.DataAnnotations;

namespace Digi.Shared.DTOs.admin.module
{
    /// <summary>
    /// DTO for encryption/decryption requests
    /// </summary>
    public class EncryptionRequestDto
    {
        [Required(ErrorMessage = "Text is required")]
        public string Text { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for encryption/decryption response
    /// </summary>
    public class EncryptionResponseDto
    {
        public string OriginalText { get; set; } = string.Empty;
        public string Result { get; set; } = string.Empty;
        public string Operation { get; set; } = string.Empty; // "Encrypt" or "Decrypt"
    }
}

