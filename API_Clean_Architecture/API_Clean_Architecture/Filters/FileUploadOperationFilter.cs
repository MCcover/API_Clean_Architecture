using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Reflection;

namespace API.API_Clean_Architecture.Filters {
	public class FileUploadOperationFilter : IOperationFilter {
		public void Apply(OpenApiOperation operation, OperationFilterContext context) {
			ParameterInfo[] fileParams = context.MethodInfo.GetParameters()
				.Where(p => p.ParameterType == typeof(IFormFile)).ToArray();

			if (fileParams.Length <= 0) {
				return;
			}

			operation.RequestBody = new OpenApiRequestBody {
				Content = {
					["multipart/form-data"] = new OpenApiMediaType {
						Schema = new OpenApiSchema {
							Type = "object",
							Properties = {
								["imagen"] = new OpenApiSchema {
									Type = "string", Format = "binary"
								},
								["fileName"] = new OpenApiSchema {
									Type = "string", Description = "Name"
								}
							}
						}
					}
				}
			};
		}
	}
}