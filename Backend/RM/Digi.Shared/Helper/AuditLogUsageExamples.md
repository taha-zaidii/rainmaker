# Audit Log Usage Examples

Yeh generic audit logging system har module mein use kar sakte hain. Yeh automatically sab actions ko database mein log karta hai.

## Setup

### 1. Service Registration

Har module ke `ServiceRegistration.cs` ya `Program.cs` mein add karein:

```csharp
// Digi.Shared se extension method use karein
services.AddAuditLogService();
```

### 2. Database Table aur Stored Procedure

Migration run karein:
- `Migrations/015_AuditLog_Module/015_AuditLog_Table_Creation.sql`
- `Migrations/011_StoredProcedures/011_Admin_Module_SPs/020_sp_Gen_AuditLog_Insert.sql`

## Usage Methods

### Method 1: Using Attribute (Recommended)

Controller action pe attribute lagayein:

```csharp
using Digi.Shared.Attributes;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : BaseController
{
    [HttpPost]
    [AuditLog("HRM", "Create", "Employee")] // Module, ActionType, EntityName
    public async Task<IActionResult> CreateEmployee(CreateEmployeeDto dto)
    {
        // Your code here
        return Ok();
    }

    [HttpPut("{id}")]
    [AuditLog("HRM", "Update", "Employee")]
    public async Task<IActionResult> UpdateEmployee(int id, UpdateEmployeeDto dto)
    {
        // Your code here
        return Ok();
    }

    [HttpDelete("{id}")]
    [AuditLog("HRM", "Delete", "Employee")]
    public async Task<IActionResult> DeleteEmployee(int id)
    {
        // Your code here
        return Ok();
    }
}
```

### Method 2: Manual Logging in Service/Controller

Service ya Controller mein manually log karein:

```csharp
public class EmployeeService : IEmployeeService
{
    private readonly IAuditLogService _auditLogService;
    private readonly IDapperService _dapper;

    public EmployeeService(IAuditLogService auditLogService, IDapperService dapper)
    {
        _auditLogService = auditLogService;
        _dapper = dapper;
    }

    public async Task<DbOperationResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeDto dto, ClaimsPrincipal user)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        try
        {
            // Your business logic
            var result = await _dapper.ExecuteAsync("SP_HR_Employee_Create", dto);
            
            stopwatch.Stop();
            
            // Log success
            await _auditLogService.LogSuccessAsync(
                module: "HRM",
                controller: "Employee",
                action: "CreateEmployee",
                httpMethod: "POST",
                user: user,
                actionType: "Create",
                entityName: "Employee",
                entityId: result.ToString(),
                description: "Employee created successfully",
                durationMs: stopwatch.ElapsedMilliseconds
            );
            
            return DbOperationResultHelpers.Success(result);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            // Log failure
            await _auditLogService.LogFailureAsync(
                module: "HRM",
                controller: "Employee",
                action: "CreateEmployee",
                httpMethod: "POST",
                user: user,
                actionType: "Create",
                entityName: "Employee",
                errorMessage: ex.Message,
                description: "Failed to create employee"
            );
            
            throw;
        }
    }
}
```

### Method 3: Using Helper Class

```csharp
using Digi.Shared.Helper;

// Simple logging
await AuditLogHelper.LogSimpleAsync(
    _auditLogService,
    module: "HRM",
    actionType: "Create",
    entityName: "Employee",
    entityId: employeeId.ToString(),
    user: User,
    description: "Employee created"
);

// Serialize objects for audit
var oldValues = AuditLogHelper.SerializeForAudit(oldEmployee);
var newValues = AuditLogHelper.SerializeForAudit(newEmployee);

await _auditLogService.LogActionAsync(
    module: "HRM",
    actionType: "Update",
    entityName: "Employee",
    entityId: employeeId.ToString(),
    oldValues: oldValues,
    newValues: newValues,
    user: User
);
```

## Module Names

Har module ke liye consistent naming use karein:
- `"Admin"` - Admin Module
- `"HRM"` - HRM Module
- `"Sales"` - Sales Module
- `"Inventory"` - Inventory Module
- `"Finance"` - Finance Module
- `"Gen"` - General Module
- etc.

## Action Types

Common action types:
- `"Create"` - New record creation
- `"Read"` - Data retrieval
- `"Update"` - Record update
- `"Delete"` - Record deletion
- `"Login"` - User login
- `"Logout"` - User logout
- `"Export"` - Data export
- `"Import"` - Data import
- `"Approve"` - Approval action
- `"Reject"` - Rejection action

## Notes

1. **Non-blocking**: Audit logging failures application ko break nahi karte
2. **Automatic**: Attribute use karne se automatically sab kuch log ho jata hai
3. **Flexible**: Manual logging bhi kar sakte hain for more control
4. **Performance**: Async operations use kiye gaye hain, blocking nahi karte
5. **Dapper**: Dynamic Dapper use kiya gaya hai stored procedures ke liye

