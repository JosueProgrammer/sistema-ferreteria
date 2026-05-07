using Microsoft.AspNetCore.Http;
using System.Linq;
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
            // Prioridad 1: claim en usuarios autenticados.
            var tenantId = _httpContextAccessor.HttpContext?.User?.FindFirstValue("TenantId");
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }

            // Prioridad 2: header explícito para APIs/middleware.
            tenantId = _httpContextAccessor.HttpContext?.Request.Headers["X-Tenant-Id"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }

            // Prioridad 3: querystring para flujos web sin sesión.
            tenantId = _httpContextAccessor.HttpContext?.Request.Query["tenantId"].FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(tenantId))
            {
                return tenantId;
            }

            // Prioridad 4: formulario de login (pre-autenticación).
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request?.HasFormContentType == true)
            {
                tenantId = request.Form["TenantId"].FirstOrDefault()
                    ?? request.Form["tenantId"].FirstOrDefault();
                if (!string.IsNullOrWhiteSpace(tenantId))
                {
                    return tenantId;
                }
            }

            return tenantId;
        }
    }
}
