using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public enum StoredProcedureNames
    {
        #region Admin StoredProcedureNames

        Sp_Adm_Branch = 1,
        Sp_Adm_CompanyRegistration = 2,
        Sp_Adm_FieldLevelPermissions = 3,
        Sp_Adm_ResendCompanyOTP = 4,
        Sp_Adm_Nav = 5,
        Sp_Adm_VerifyCompanyOTP = 6,
        Sp_Adm_Nav_Role_Permissions = 7,
        Sp_Adm_NavPermissions = 8,
        Sp_Adm_PackageFeatures = 9,
        Sp_Adm_Permissions = 10,
        Sp_Adm_RolePermissions = 11,
        sp_Adm_Roles_CURD = 12,
        Sp_Adm_RolesPermissions = 13,
        Sp_Adm_SubscriptionFeatures = 14,
        Sp_Adm_SubscriptionPackages = 15,
        Sp_Adm_Subscriptions = 16,
        Sp_Adm_UserAtModule = 17,
        Sp_Adm_UserRoles = 18,
        Sp_Adm_Users = 19,

        #endregion

        #region General StoredProcedureNames

        Sp_Gen_AuditLog = 20,
        Sp_Gen_City = 21,
        Sp_Gen_Country = 22,
        Sp_Gen_Currency = 23,
        Sp_Gen_Departments = 24,
        Sp_Gen_Designation = 25,
        Sp_Gen_Module = 26,
        Sp_Gen_State = 27,

        #endregion

        #region HRM StoredProcedureNames

        Sp_Hr_Employee = 28,
        Sp_Hr_EmploymentType = 29,

        #endregion

        #region Ruc StoredProcedureNames

        Sp_Ruc_ApplicationStatus = 30,
        Sp_Ruc_Candidates = 31,
        Sp_Ruc_Interviews = 32,
        Sp_Ruc_JobApplications = 33,
        Sp_Ruc_Jobs = 34,
        Sp_Ruc_Recruiters = 35,
        Sp_Ruc_RecruitmentRequest = 36,
        #endregion

        #region Generic StoredProcedureNames
        sp_Db_Generic_CRUD = 37,
        #endregion

        #region Sales StoredProcedureNames
        
        Sp_Sales_CustomerPoc = 38,
        Sp_Sales_CustomerPOC_TypeLevel = 39,
        Sp_Sales_CustomerPOC_Type = 40,
        Sp_Sales_CustomerPOC_Category = 41,
        Sp_Sales_ServiceProduct = 42,
        sp_Sales_Service = 43,
        sp_Sales_CustomerProject_Managers = 44,
        sp_Sales_CustomerProject_Basic = 45,
        #endregion
    }

}
