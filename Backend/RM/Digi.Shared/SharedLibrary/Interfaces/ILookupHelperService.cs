using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.SharedLibrary.Interfaces
{
    public interface ILookupHelperService
    {
        #region Entity-Specific Methods
        Task<Dictionary<int, string>> GetModuleDictionaryAsync(IEnumerable<int> moduleIds);
        Task<Dictionary<int, string>> GetRoleDictionaryAsync(IEnumerable<int> roleIds);
        Task<Dictionary<int, string>> GetPackageDictionaryAsync(IEnumerable<int> packageIds);
        Task<Dictionary<int, string>> GetCompanyDictionaryAsync(IEnumerable<int> companyIds);
        Task<Dictionary<int, string>> GetNavDictionaryAsync(IEnumerable<int> parentIds);
        Task<Dictionary<int, string>> GetEmployeeNameDictionaryAsync(IEnumerable<int> employeeIds);

        #endregion

        #region Entity-Specific Methods
        Task<string> GetModuleAsync(int? moduleId);
        Task<string> GetRoleAsync(int? roleId);
        Task<string> GetPackageAsync(int? packageId);
        Task<string> GetCompanyAsync(int? comapnyId);
        Task<string> GetNavAsync(int? parentIds);
        Task<string> GetEmployeeNameAsync(int? departmentHeadID);

        #endregion

        #region Entity-Specific Methods

        Task<Dictionary<int, string>> GetDepartmentDictionaryAsync(IEnumerable<int> companyIds);
        Task<Dictionary<int, string>> GetCountryDictionaryAsync(IEnumerable<int> countryIds);
        Task<Dictionary<int, string>> GetStateDictionaryAsync(IEnumerable<int> stateIds);
        Task<Dictionary<int, string>> GetCityDictionaryAsync(IEnumerable<int> cityIds);

        #endregion

        #region Entity-Specific Methods

        Task<string> GetDepartmentAsync(int? companyId);
        Task<string> GetCountryAsync(int? countryId);
        Task<string> GetStateAsync(int? stateId);
        Task<string> GetCityAsync(int? cityId);

        #endregion
    }


}
