using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace Digi.Recruitment.Module.Middleware
{
    public class SwaggerFileUploadFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            if (context.MethodInfo.GetCustomAttribute<SwaggerFileUploadAttribute>() != null)
            {
                operation.RequestBody = new OpenApiRequestBody
                {
                    Content =
                {
                    ["multipart/form-data"] = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = "object",
                            Properties =
                            {
                                ["recruitmentData"] = new OpenApiSchema { Type = "string" },
                                ["attachments"] = new OpenApiSchema { Type = "array", Items = new OpenApiSchema { Type = "string", Format = "binary" } }
                            }
                        }
                    }
                }
                };
            }
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class SwaggerFileUploadAttribute : Attribute { }
}
