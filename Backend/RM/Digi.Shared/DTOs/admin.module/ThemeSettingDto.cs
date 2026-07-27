using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.DTOs.admin.module
{
    // Models/Theme.cs
    public class ThemeSettingDto
    {
        public int ThemeId { get; set; }
        public int UserId { get; set; }
        public int CompanyId { get; set; }
        public string? theme_name { get; set; }
        public string? Primary_color { get; set; }
        public string? Secondary_color { get; set; }
        public string? font_color { get; set; }
        public string? btn_color { get; set; }        // ✅ always hex
        public bool dark_mode { get; set; }
        public string? font_family { get; set; }
        public string? font_size { get; set; }
        public string? logo_url { get; set; }
        public string? favicon_url { get; set; }
        public string layout_type { get; set; } = "vertical";  // ✅ NEW
        public bool IsDefault { get; set; }
        public bool isActive { get; set; }
        public string? CreatedBy { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime createdOn { get; set; }
        public DateTime updatedOn { get; set; }
    }

    // Models/ThemeLevel.cs
    public enum ThemeLevel
    {
        System,
        Company,
        User
    }

    // Models/ThemeResponse.cs
    public class ThemeResponse : ThemeSettingDto
    {
        public int ThemeID { get; set; }
        public int? CompanyID { get; set; }
        public int? UserID { get; set; }
        public string? Theme_name { get; set; }
        public string? Primary_color { get; set; }
        public string? Secondary_color { get; set; }
        public string? font_color { get; set; }
        public string? btn_color { get; set; }        // ✅ always hex
        public bool dark_mode { get; set; }
        public string? font_family { get; set; }
        public string? font_size { get; set; }
        public string? logo_url { get; set; }
        public string? favicon_url { get; set; }
        public string layout_type { get; set; } = "vertical";  // ✅ NEW
        public bool IsDefault { get; set; }
        public bool IsActive { get; set; }
    }


    public class UpdateThemeResponse
    {
        public int ThemeID { get; set; }
        public int CompanyID { get; set; }
        public int UserID { get; set; }
        public string? Theme_name { get; set; }
        public string? Primary_color { get; set; }
        public string? Secondary_color { get; set; }
        public string? font_color { get; set; }
        public string? btn_color { get; set; }
        public bool dark_mode { get; set; }
        public string? font_family { get; set; }
        public string? font_size { get; set; }
        public string? logo_url { get; set; }
        public string? favicon_url { get; set; }
        public bool allow_user_overrides { get; set; }
        public bool IsDefault { get; set; }
        public bool IsCompanyDefault { get; set; }
        public bool InheritSystemTheme { get; set; }
        public bool IsActive { get; set; }
        public string layout_type { get; set; } = "vertical";
        public DateTime CreatedOn { get; set; }
        public string? CreatedBy { get; set; }
        public DateTime UpdatedOn { get; set; }
        public string? UpdatedBy { get; set; }
    }
}
