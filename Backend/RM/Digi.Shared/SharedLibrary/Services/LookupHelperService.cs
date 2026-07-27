using Digi.Shared.DTOs.admin.module;
using Digi.Shared.DTOs.gen.module;
using Digi.Shared.DTOs.hrm.module;
using Digi.Shared.Helper;
using Digi.Shared.SharedLibrary.Interfaces;
using static Digi.Shared.DTOs.ExtensionsDto;


namespace Digi.Shared.SharedLibrary.Services
{
    public class LookupHelperService : ILookupHelperService
    {
        #region Fields

        private readonly IDapperServices _dapperService;

        private string spExecute = StoredProcedureNames.sp_Db_Generic_CRUD.ToProcedureName();
        private readonly string ModuleTable = TableNames.Tbl_Gen_Module;
        private readonly string CompanyTable = TableNames.Tbl_Adm_Company;
        private readonly string RoleTable = TableNames.Tbl_Adm_Roles;
        private readonly string PackageTable = TableNames.Tbl_Adm_SubscriptionPackages;

        private readonly string CountryTable = TableNames.Tbl_Gen_Country;

        private readonly string StateTable = TableNames.Tbl_Gen_State;
        private readonly string CityTable = TableNames.Tbl_Gen_City;
        private readonly string DepartmentsTable = TableNames.Tbl_Hrm_Departments;
        private readonly string NavTable = TableNames.Tbl_Adm_Nav;
        private readonly string EmployeeTable = TableNames.Tbl_Hr_Employee;


        #endregion

        #region Constructor

        public LookupHelperService(IDapperServices dapperService)
        {
            _dapperService = dapperService;
        }
        #endregion

        #region Generic Lookup Method

        public async Task<Dictionary<int, string>> GetLookupDictionaryAsync<TDto>(
            string tableName,
            string idColumnName,
            string nameColumnName,
            IEnumerable<int> ids,
            bool isActiveOnly = true)
        {
            if (ids == null || !ids.Any()) return new Dictionary<int, string>();

            var whereClause = $"{idColumnName} IN ({string.Join(",", ids.Distinct())})";

            var result = await _dapperService.QueryListAsync<TDto>(
                spExecute: spExecute,
                tableName: tableName,
                whereClause: whereClause,
                isActiveFilter: isActiveOnly);

            if (!result.IsSuccess || result.Data == null)
                return new Dictionary<int, string>();

            return result.Data.ToDictionary(
                item => (int)item!.GetType().GetProperty(idColumnName)!.GetValue(item)!,
                item => item!.GetType().GetProperty(nameColumnName)!.GetValue(item)?.ToString() ?? string.Empty
            );
        }
        public async Task<string> GetLookupValueAsync<TDto>(
        string tableName,
        string idColumnName,
        string nameColumnName,
        int id,
        bool isActiveOnly = true)
        {
            // Reuse the IEnumerable-based method, then pull the one value (or empty)
            var dict = await GetLookupDictionaryAsync<TDto>(
                tableName, idColumnName, nameColumnName,
                new[] { id },
                isActiveOnly);

            return dict.TryGetValue(id, out var name)
                ? name
                : string.Empty;
        }

        #endregion

        #region Specific single ID

        public Task<string> GetModuleAsync(int? moduleId) => GetLookupValueAsync<object>(
        ModuleTable, "ModuleID", "ModuleName", (int)moduleId!);

        public Task<string> GetRoleAsync(int? roleId) => GetLookupValueAsync<object>(
       RoleTable, "RoleID", "RoleName", (int)roleId!);


        public Task<string> GetPackageAsync(int? packageId) => GetLookupValueAsync<object>(
       PackageTable, "PackageID", "PackageName", (int)packageId!);

        public Task<string> GetCompanyAsync(int? companyId) => GetLookupValueAsync<object>(
       CompanyTable, "CompanyID", "CompanyName", (int)companyId!);

        public Task<string> GetEmployeeNameAsync(int? employeeId) => GetLookupValueAsync<object>(EmployeeTable, "EmployeeID", "EmployeeName", (int)employeeId!);


        public Task<string> GetDepartmentAsync(int? departmentId) => GetLookupValueAsync<object>(
       DepartmentsTable, "DepartmentID", "DepartmentName", (int)departmentId!);


        public Task<string> GetCountryAsync(int? countryId) => GetLookupValueAsync<object>(
                CountryTable, "CountryID", "CountryName", (int)countryId!);
        public Task<string> GetStateAsync(int? stateId) => GetLookupValueAsync<object>(
               StateTable, "StateID", "StateName", (int)stateId!);
        public Task<string> GetCityAsync(int? cityId) => GetLookupValueAsync<object>(
               CityTable, "CityID", "CityName", (int)cityId!);

        public Task<string> GetNavAsync(int? parentIds) => GetLookupValueAsync<object>(
             NavTable, "NavId", "DisplayName", (int)parentIds!);


        #endregion

        #region Specific Helpers

        public async Task<Dictionary<int, string>> GetModuleDictionaryAsync(IEnumerable<int> moduleIds)
        {
            return await GetLookupDictionaryAsync<ModuleDto>(
                ModuleTable, "ModuleID", "ModuleName", moduleIds);
        }

        public async Task<Dictionary<int, string>> GetRoleDictionaryAsync(IEnumerable<int> roleIds)
        {
            return await GetLookupDictionaryAsync<RoleDto>(
                RoleTable, "RoleID", "RoleName", roleIds);
        }
        public async Task<Dictionary<int, string>> GetPackageDictionaryAsync(IEnumerable<int> packageIds)
        {
            return await GetLookupDictionaryAsync<SubscriptionPackageResponseDto>(
                PackageTable, "PackageID", "PackageName", packageIds);
        }
        public async Task<Dictionary<int, string>> GetCompanyDictionaryAsync(IEnumerable<int> companyIds)
        {
            return await GetLookupDictionaryAsync<CompanyDto>(
                CompanyTable, "CompanyID", "CompanyName", companyIds);
        }
        public async Task<Dictionary<int, string>> GetEmployeeNameDictionaryAsync(IEnumerable<int> employeeIds)
        {
            return await GetLookupDictionaryAsync<EmployeeListDto>(
                EmployeeTable, "EmployeeID", "EmployeeName", employeeIds);
        }


        public async Task<Dictionary<int, string>> GetDepartmentDictionaryAsync(IEnumerable<int> deparmentIds)
        {
            return await GetLookupDictionaryAsync<DepartmentDto>(
                DepartmentsTable, "DepartmentID", "DepartmentName", deparmentIds);
        }

        public async Task<Dictionary<int, string>> GetCountryDictionaryAsync(IEnumerable<int> countryIds)
        {
            return await GetLookupDictionaryAsync<CountryDto>(
                CountryTable, "CountryID", "CountryName", countryIds);
        }

        public async Task<Dictionary<int, string>> GetStateDictionaryAsync(IEnumerable<int> stateIds)
        {
            return await GetLookupDictionaryAsync<StateDto>(
                StateTable, "StateID", "StateName", stateIds);
        }

        public async Task<Dictionary<int, string>> GetCityDictionaryAsync(IEnumerable<int> cityIds)
        {
            return await GetLookupDictionaryAsync<CityDto>(
                CityTable, "CityID", "CityName", cityIds);
        }

        public async Task<Dictionary<int, string>> GetNavDictionaryAsync(IEnumerable<int> parentIds)
        {
            return await GetLookupDictionaryAsync<NavDto>(
                NavTable, "NavId", "DisplayName", parentIds);
        }

        #endregion

    }
}
