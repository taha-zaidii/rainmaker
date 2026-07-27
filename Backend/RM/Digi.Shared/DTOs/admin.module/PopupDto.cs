namespace Digi.Shared.DTOs.admin.module
{
    public class PopupDto
    {
        public int PID { get; set; }
        public string? PTitle { get; set; }
        public string? PDesc { get; set; }
        public string? PImage { get; set; }
        public string? PBtnA { get; set; }
        public string? PBtnB { get; set; }
        public bool? Status { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class PopupCreateDto
    {
        public string? PTitle { get; set; }
        public string? PDesc { get; set; }
        public string? PImage { get; set; }
        public string? PBtnA { get; set; }
        public string? PBtnB { get; set; }
        public bool? Status { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class PopupUpdateDto
    {
        public int PID { get; set; }
        public string? PTitle { get; set; }
        public string? PDesc { get; set; }
        public string? PImage { get; set; }
        public string? PBtnA { get; set; }
        public string? PBtnB { get; set; }
        public bool? Status { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }

    public class PopupResponseDto
    {
        public int PID { get; set; }
        public string? PTitle { get; set; }
        public string? PDesc { get; set; }
        public string? PImage { get; set; }
        public string? PBtnB { get; set; }
        public string? PBtnA { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? PResponseID { get; set; }
        public bool? Status { get; set; }
        public string? PResponse { get; set; }
        public bool? PRStatus { get; set; }
        public string? PResponseBy { get; set; }
        public DateTime? PResponseOn { get; set; }
        public string? FullName { get; set; }
        public string? FilePath { get; set; }
        public string? DepartmentName { get; set; }
    }

    public class PopupResponseCreateDto
    {
        public string? PResponse { get; set; }
        public string? PResponseBy { get; set; }
        public DateTime? PResponseOn { get; set; }
        public bool? PRStatus { get; set; }
        public int? PopupID { get; set; }
    }

    public class PopupResponseResultDto
    {
        public int PResponseID { get; set; }
        public int PopupID { get; set; }
        public string? Message { get; set; }
    }
}
