using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public static class StoredProcedureExtensions
    {
        public static string ToProcedureName(this StoredProcedureNames sp)
        {
            return sp switch
            {
                #region Admin StoredProcedureNames

                StoredProcedureNames.Sp_Adm_Branch => "sp_adm_branch",
                StoredProcedureNames.Sp_Adm_CompanyRegistration => "sp_CompanyRegistration_Assign",
                StoredProcedureNames.Sp_Adm_VerifyCompanyOTP => "sp_Adm_VerifyCompanyOTP",
                StoredProcedureNames.Sp_Adm_ResendCompanyOTP => "sp_Adm_ResendCompanyOTP",

                StoredProcedureNames.Sp_Adm_FieldLevelPermissions => "sp_adm_fieldlevelpermissions",
                StoredProcedureNames.Sp_Adm_Nav => "sp_adm_nav",
                StoredProcedureNames.Sp_Adm_Nav_Role_Permissions => "sp_adm_nav_role_permissions",
                StoredProcedureNames.Sp_Adm_NavPermissions => "sp_adm_navpermissions",
                StoredProcedureNames.Sp_Adm_PackageFeatures => "sp_adm_packagefeatures",
                StoredProcedureNames.Sp_Adm_Permissions => "sp_adm_permissions",
                StoredProcedureNames.Sp_Adm_RolePermissions => "sp_adm_rolepermissions",
                StoredProcedureNames.sp_Adm_Roles_CURD => "sp_Adm_Roles_CURD",
                StoredProcedureNames.Sp_Adm_RolesPermissions => "sp_adm_rolespermissions",
                StoredProcedureNames.Sp_Adm_SubscriptionFeatures => "sp_adm_subscriptionfeatures",
                StoredProcedureNames.Sp_Adm_SubscriptionPackages => "sp_adm_subscriptionpackages",
                StoredProcedureNames.Sp_Adm_Subscriptions => "sp_adm_subscriptions",
                StoredProcedureNames.Sp_Adm_UserAtModule => "sp_adm_useratmodule",
                StoredProcedureNames.Sp_Adm_UserRoles => "sp_adm_userroles",
                StoredProcedureNames.Sp_Adm_Users => "sp_Admin_Users",

                #endregion

                #region General StoredProcedureNames

                StoredProcedureNames.Sp_Gen_City => "sp_gen_city",
                StoredProcedureNames.Sp_Gen_Country => "sp_gen_country",
                StoredProcedureNames.Sp_Gen_Currency => "sp_gen_currency",
                StoredProcedureNames.Sp_Gen_Departments => "sp_gen_departments",
                StoredProcedureNames.Sp_Gen_Designation => "sp_gen_designation",
                StoredProcedureNames.Sp_Gen_Module => "sp_gen_module",
                StoredProcedureNames.Sp_Gen_State => "sp_gen_state",

                #endregion

                #region HRM StoredProcedureNames

                StoredProcedureNames.Sp_Hr_Employee => "sp_hr_employee",
                StoredProcedureNames.Sp_Hr_EmploymentType => "sp_hr_employmenttype",


                #endregion

                #region Ruc StoredProcedureNames
                StoredProcedureNames.Sp_Ruc_ApplicationStatus => "sp_ruc_applicationstatus",
                StoredProcedureNames.Sp_Ruc_Candidates => "sp_ruc_candidates",
                StoredProcedureNames.Sp_Ruc_Interviews => "sp_ruc_interviews",
                StoredProcedureNames.Sp_Ruc_JobApplications => "sp_ruc_jobapplications",
                StoredProcedureNames.Sp_Ruc_Jobs => "sp_ruc_jobs",
                StoredProcedureNames.Sp_Ruc_Recruiters => "sp_ruc_recruiters",
                StoredProcedureNames.Sp_Ruc_RecruitmentRequest => "sp_ruc_recruitmentrequest",
                #endregion

                #region Sale StoreProcedureNames

                StoredProcedureNames.Sp_Sales_CustomerPoc => "sp_Sales_CustomerPoc",
                StoredProcedureNames.Sp_Sales_CustomerPOC_TypeLevel => "sp_Sales_CustomerPOC_TypeLevel",
                StoredProcedureNames.Sp_Sales_CustomerPOC_Type => "sp_Sales_CustomerPOC_Type",
                StoredProcedureNames.Sp_Sales_CustomerPOC_Category => "sp_Sales_CustomerPOC_Category",
                StoredProcedureNames.Sp_Sales_ServiceProduct => "sp_Sales_ServiceProduct",
                StoredProcedureNames.sp_Sales_Service => "sp_Sales_Service",
                StoredProcedureNames.sp_Sales_CustomerProject_Managers => "sp_Sales_CustomerProject_Managers",
                StoredProcedureNames.sp_Sales_CustomerProject_Basic => "sp_Sales_CustomerProject_Basic",

                #endregion

                #region Generic StoredProcedureNames
                StoredProcedureNames.sp_Db_Generic_CRUD => "sp_Db_Generic_CRUD",
                _ => throw new ArgumentOutOfRangeException(nameof(sp), sp, null)
                #endregion

            };
        }
    }

}
