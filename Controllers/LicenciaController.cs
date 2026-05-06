using Microsoft.AspNetCore.Mvc;
using Sistema_Ferreteria.Services;
using Sistema_Ferreteria.Filters;
using Sistema_Ferreteria.Models;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using System;
using Microsoft.AspNetCore.Authorization;

namespace Sistema_Ferreteria.Controllers
{
    [Authorize]
    [SkipLicenseCheck]
    public class LicenciaController : Controller
    {
        private readonly LicenseValidatorService _licenseValidator;
        private readonly IHttpClientFactory _httpClientFactory;
        private const string ApiBaseUrl = "http://localhost:3000/api";

        public LicenciaController(LicenseValidatorService licenseValidator, IHttpClientFactory httpClientFactory)
        {
            _licenseValidator = licenseValidator;
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Index(bool required = false)
        {
            var info = _licenseValidator.GetLicenseInfo();
            if (required && !info.IsLicensed)
            {
                ViewBag.Error = "Debe activar o renovar su licencia para acceder a este módulo.";
            }

            ViewBag.MachineId = HardwareInfo.GetMachineId();
            
            // Pass both LicenseInfo and ActivationViewModel to the view
            ViewBag.ActivationModel = new ActivationViewModel();
            return View(info);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ActivationViewModel model)
        {
            var info = _licenseValidator.GetLicenseInfo();
            ViewBag.MachineId = HardwareInfo.GetMachineId();
            ViewBag.ActivationModel = model;

            if (!ModelState.IsValid)
            {
                return View(info);
            }

            var machineId = HardwareInfo.GetMachineId();
            var client = _httpClientFactory.CreateClient();
            string apiUrl = "";
            object requestBody = null;

            if (model.ActivationType == "Code")
            {
                if (string.IsNullOrWhiteSpace(model.Code))
                {
                    ViewBag.Error = "Por favor, ingrese el código de activación.";
                    return View(info);
                }
                apiUrl = $"{ApiBaseUrl}/activation-codes/activate";
                requestBody = new { code = model.Code.Trim(), machineId = machineId };
            }
            else if (model.ActivationType == "Login")
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    ViewBag.Error = "Por favor, ingrese su correo y contraseña.";
                    return View(info);
                }
                apiUrl = $"{ApiBaseUrl}/subscriptions/login-activate";
                requestBody = new { 
                    email = model.Email.Trim(), 
                    pass = model.Password, 
                    productId = "ferreteria-erp",
                    machineId = machineId 
                };
            }
            else
            {
                ViewBag.Error = "Método de activación inválido.";
                return View(info);
            }

            try
            {
                var content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
                var response = await client.PostAsync(apiUrl, content);

                var responseString = await response.Content.ReadAsStringAsync();
                
                if (!response.IsSuccessStatusCode)
                {
                    // Try to parse NestJS error
                    try {
                        var errorObj = JsonSerializer.Deserialize<JsonElement>(responseString);
                        if (errorObj.TryGetProperty("message", out var msgProp))
                        {
                            ViewBag.Error = msgProp.GetString();
                            return View(info);
                        }
                    } catch { }
                    
                    ViewBag.Error = "Error al comunicarse con el servidor de licencias.";
                    return View(info);
                }

                // Parse successful response
                var successObj = JsonSerializer.Deserialize<JsonElement>(responseString);
                string licenseKey = successObj.GetProperty("licenseKey").GetString();

                // Validate locally to be absolutely sure
                var result = _licenseValidator.ValidateLicense(licenseKey, machineId);
                if (!result.IsValid)
                {
                    ViewBag.Error = "El servidor devolvió una licencia, pero no pudo ser validada localmente: " + result.Message;
                    return View(info);
                }

                // Save it
                if (!_licenseValidator.SaveLicense(licenseKey))
                {
                    ViewBag.Error = "Error al guardar la licencia en el disco duro. Verifique permisos de administrador.";
                    return View(info);
                }

                TempData["LicenseSuccess"] = "¡Software activado exitosamente!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                ViewBag.Error = "Error de conexión: " + ex.Message;
                return View(info);
            }
        }

        [HttpGet]
        public IActionResult GetMachineId()
        {
            return Json(new { machineId = HardwareInfo.GetMachineId() });
        }

        [HttpGet]
        public IActionResult Status()
        {
            return Json(_licenseValidator.GetLicenseInfo());
        }
    }
}
