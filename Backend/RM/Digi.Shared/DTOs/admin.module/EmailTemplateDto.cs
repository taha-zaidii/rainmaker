namespace Digi.Shared.DTOs.admin.module
{
    public class EmailTemplateDto
    {
        public int EmailTemplateID { get; set; }
        public int CompanyID { get; set; }
        public string TemplateCode { get; set; } = string.Empty;
        public string? TemplateName { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
        public bool IsActive { get; set; } = true;
    }

    public class RenderedEmailTemplateDto
    {
        public string Subject { get; set; } = string.Empty;
        public string BodyHtml { get; set; } = string.Empty;
        public bool IsHtml { get; set; } = true;
    }
}
