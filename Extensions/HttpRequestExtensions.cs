using System;
using Microsoft.AspNetCore.Http;

namespace Sistema_Ferreteria.Extensions;

public static class HttpRequestExtensions
{
    public static bool IsAjaxRequest(this HttpRequest request)
    {
        if (request == null)
            throw new ArgumentNullException(nameof(request));

        return request.Headers["X-Requested-With"] == "XMLHttpRequest" || 
               request.Headers["Accept"].ToString().Contains("application/json");
    }
}
