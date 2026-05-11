using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Sistema_Ferreteria.Extensions;

namespace Sistema_Ferreteria.Filters;

public class PermisoAttribute : TypeFilterAttribute
{
    public PermisoAttribute(string codigo) : base(typeof(PermisoFilter))
    {
        Arguments = new object[] { codigo };
    }
}

public class PermisoFilter : IAuthorizationFilter
{
    private readonly string _codigo;

    public PermisoFilter(string codigo)
    {
        _codigo = codigo;
    }

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        if (!user.Identity?.IsAuthenticated ?? true)
        {
            context.Result = new ChallengeResult();
            return;
        }

        // Si es Admin, tiene acceso total siempre
        if (user.IsInRole("Administrador")) return;

        // Verificar si tiene el claim de permiso específico
        var tienePermiso = user.Claims.Any(c => c.Type == "Permiso" && c.Value == _codigo);

        if (!tienePermiso)
        {
            // NOTA DE SEGURIDAD (KAN-21): 
            // La validación de permisos real (claims y roles) se realiza de forma independiente.
            // Las cabeceras HTTP ('X-Requested-With', 'Accept') pueden ser falsificadas fácilmente 
            // por el cliente, por lo que NUNCA forman parte del mecanismo de seguridad.
            // Aquí se utilizan EXCLUSIVAMENTE para mejorar la experiencia de usuario (UX),
            // determinando el formato de respuesta apropiado (JSON vs Redirect) tras haber DENEGADO el acceso.
            if (context.HttpContext.Request.IsAjaxRequest())
            {
                context.Result = new JsonResult(new { success = false, message = "No tienes permisos para realizar esta acción" }) { StatusCode = 403 };
            }
            else
            {
                context.Result = new RedirectToActionResult("AccesoDenegado", "Cuentas", null);
            }
        }
    }
}
