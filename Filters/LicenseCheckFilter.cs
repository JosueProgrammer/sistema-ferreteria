using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Sistema_Ferreteria.Filters
{
    /// <summary>
    /// Action filter that checks if the application has a valid license before 
    /// allowing access to any controller action.
    /// Pages that need to be excluded (like the license activation page itself)
    /// should use the [SkipLicenseCheck] attribute.
    /// </summary>
    public class LicenseCheckFilter : IActionFilter
    {
        private readonly Services.LicenseValidatorService _licenseValidator;

        public LicenseCheckFilter(Services.LicenseValidatorService licenseValidator)
        {
            _licenseValidator = licenseValidator;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var hasSkipAttribute = context.ActionDescriptor.EndpointMetadata
                .Any(em => em.GetType() == typeof(SkipLicenseCheckAttribute));

            if (hasSkipAttribute)
            {
                return;
            }

            var licenseInfo = _licenseValidator.GetLicenseInfo();

            if (!licenseInfo.IsLicensed)
            {
                var isAjax = context.HttpContext.Request.Headers["X-Requested-With"] == "XMLHttpRequest";

                if (isAjax)
                {
                    context.Result = new JsonResult(new { error = "Licencia requerida", redirectTo = "/Licencia/Index" })
                    {
                        StatusCode = 403
                    };
                }
                else
                {
                    var controller = context.Controller as Microsoft.AspNetCore.Mvc.Controller;
                    if (controller != null)
                    {
                        controller.ViewBag.LicenseBlocked = true;
                    }
                    
                    context.Result = new ViewResult
                    {
                        ViewName = "~/Views/Shared/NoLicense.cshtml"
                    };
                }
            }
        }

        public void OnActionExecuted(ActionExecutedContext context) { }
    }

    /// <summary>
    /// Apply this attribute to controllers or actions that should bypass the license check.
    /// For example: the License activation page, static files, login page, etc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class SkipLicenseCheckAttribute : Attribute { }
}
