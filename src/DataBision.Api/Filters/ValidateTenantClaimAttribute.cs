using System.Security.Claims;
using DataBision.Api.Middleware;
using DataBision.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace DataBision.Api.Filters;

// Defense-in-depth: the JWT carries `company_id`; the host/?tenant resolves a tenant
// company. They must match for any authenticated, non-SuperAdmin call. Without this,
// a CompanyAdmin of A could send `?tenant=B` and operate against B's data because
// controllers read the tenant from the middleware, not the JWT.
public class ValidateTenantClaimAttribute : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;

        // [Authorize] handles unauthenticated requests — don't double-handle.
        if (user.Identity?.IsAuthenticated != true)
            return;

        // SuperAdmin operates without a tenant context.
        var role = user.FindFirstValue("role");
        if (role == nameof(UserRole.SuperAdmin))
            return;

        var tenantCompanyId = context.HttpContext.GetTenantCompanyId();
        var jwtCompanyIdClaim = user.FindFirstValue("company_id");

        if (tenantCompanyId is null || string.IsNullOrEmpty(jwtCompanyIdClaim))
        {
            context.Result = new ObjectResult(new
            {
                error = "tenant_mismatch",
                message = "Tenant context missing or invalid for this request."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }

        if (!int.TryParse(jwtCompanyIdClaim, out var jwtCompanyId) || jwtCompanyId != tenantCompanyId.Value)
        {
            context.Result = new ObjectResult(new
            {
                error = "tenant_mismatch",
                message = "JWT company does not match the resolved tenant."
            })
            { StatusCode = StatusCodes.Status403Forbidden };
            return;
        }
    }
}
