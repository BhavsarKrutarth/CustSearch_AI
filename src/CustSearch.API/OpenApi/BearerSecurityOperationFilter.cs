using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CustSearch.API.OpenApi;

/// <summary>Adds JWT requirements only to endpoints that are actually authorized.</summary>
public sealed class BearerSecurityOperationFilter:IOperationFilter
{
    public void Apply(OpenApiOperation operation,OperationFilterContext context)
    {
        var metadata=context.ApiDescription.ActionDescriptor.EndpointMetadata;
        if(metadata.OfType<IAllowAnonymous>().Any()||!metadata.OfType<IAuthorizeData>().Any())return;
        operation.Responses.TryAdd("401",new OpenApiResponse{Description="Unauthenticated"});
        operation.Responses.TryAdd("403",new OpenApiResponse{Description="Authenticated but outside the required scope/permission"});
        operation.Security=[new OpenApiSecurityRequirement{{new OpenApiSecurityScheme{Reference=new OpenApiReference{Type=ReferenceType.SecurityScheme,Id="Bearer"}},Array.Empty<string>()}}];
    }
}
