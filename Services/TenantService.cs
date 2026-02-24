using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace Sistema_Ferreteria.Services
{
    public interface ITenantService
    {
        string? GetTenantId();
    }

    public class TenantService : ITenantService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public TenantService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public string? GetTenantId()
        {
            // Resolve tenant from claims. We'll need to make sure the login 
            // process adds a "TenantId" claim to the user's identity.
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId");
            
            // For development or if not authenticated, we could return a default tenant
            // or handle it according to requirements.
            return tenantId;
        }
    }
}
