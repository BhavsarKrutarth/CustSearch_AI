using System.Reflection;
using System.Text.Json;
using CustSearch.API.Controllers;
using CustSearch.API.Security;
using CustSearch.Application.Authentication;
using CustSearch.Application.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace CustSearch.IntegrationTests;

public sealed class PhaseSixteenApiContractTests
{
    // Reuse the strict web contract options so the test exercises the API's unknown-field
    // rejection boundary without allocating serializer metadata for every test invocation.
    private static readonly JsonSerializerOptions StrictWebJson = new(JsonSerializerDefaults.Web);

    [Fact]public void OperationsControllerIsPlatformOnly(){var auth=typeof(OperationalPlatformController).GetCustomAttribute<AuthorizeAttribute>();Assert.Equal(AuthorizationPolicyNames.PlatformScope,auth?.Policy);}
    [Theory][InlineData(nameof(OperationalPlatformController.Health),PermissionCatalog.Platform.OperationsView)][InlineData(nameof(OperationalPlatformController.Pause),PermissionCatalog.Platform.OperationsManage)][InlineData(nameof(OperationalPlatformController.Retry),PermissionCatalog.Platform.OperationsManage)]public void EndpointsRequireOperationalPermission(string method,string permission){var attr=typeof(OperationalPlatformController).GetMethod(method)!.GetCustomAttribute<HasPermissionAttribute>();Assert.Equal(AuthorizationPolicyNames.ForPermission(permission),attr?.Policy);}
    [Fact]public void UnknownClientFieldsAreRejected(){var json="""{"scope":1,"tenantId":null,"storeId":null,"key":"Reports.PageSize","valueJson":"100","secretValue":"unsafe"}""";Assert.Throws<JsonException>(()=>JsonSerializer.Deserialize<SaveOperationalSettingRequest>(json,StrictWebJson));}
}

