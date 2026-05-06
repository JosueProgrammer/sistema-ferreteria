using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sistema_Ferreteria.Services
{
    public class LicensePayload
    {
        [JsonPropertyName("cid")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("pid")]
        public string? ProductId { get; set; }

        [JsonPropertyName("mid")]
        public string? MachineId { get; set; }

        [JsonPropertyName("exp")]
        public string? ExpirationDate { get; set; }

        [JsonPropertyName("ver")]
        public string? Version { get; set; }
    }

    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public LicensePayload? Payload { get; set; }
        public int DaysRemaining { get; set; }
    }

    public class LicenseInfo
    {
        public bool IsLicensed { get; set; }
        public string Status { get; set; } = "Sin Licencia";
        public string? ExpirationDate { get; set; }
        public int DaysRemaining { get; set; }
        public string? MachineId { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class LicenseValidatorService
    {
        private readonly ILogger<LicenseValidatorService> _logger;
        private readonly string _publicKeyPem;
        private readonly string _licenseFilePath;
        private LicenseInfo? _cachedLicenseInfo;
        private DateTime _lastCheck = DateTime.MinValue;

        public LicenseValidatorService(ILogger<LicenseValidatorService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _publicKeyPem = configuration["License:PublicKey"] ?? "";
            _licenseFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "license.lic");

            if (string.IsNullOrWhiteSpace(_publicKeyPem))
            {
                _logger.LogWarning("License:PublicKey is not configured in appsettings. License validation will fail.");
            }
        }

        /// <summary>
        /// Gets the current license information, using a cache that refreshes every 5 minutes.
        /// </summary>
        public LicenseInfo GetLicenseInfo()
        {
            if (_cachedLicenseInfo != null && (DateTime.UtcNow - _lastCheck).TotalMinutes < 5)
            {
                return _cachedLicenseInfo;
            }

            var machineId = HardwareInfo.GetMachineId();
            var serial = ReadLicenseFile();

            if (string.IsNullOrWhiteSpace(serial))
            {
                _cachedLicenseInfo = new LicenseInfo
                {
                    IsLicensed = false,
                    Status = "Sin Licencia",
                    MachineId = machineId,
                    Message = "No se encontró un archivo de licencia. Por favor, active su software."
                };
                _lastCheck = DateTime.UtcNow;
                return _cachedLicenseInfo;
            }

            var result = ValidateLicense(serial, machineId);

            _cachedLicenseInfo = new LicenseInfo
            {
                IsLicensed = result.IsValid,
                Status = result.IsValid ? "Activa" : "Inválida",
                ExpirationDate = result.Payload?.ExpirationDate,
                DaysRemaining = result.DaysRemaining,
                MachineId = machineId,
                Message = result.Message
            };
            _lastCheck = DateTime.UtcNow;
            return _cachedLicenseInfo;
        }

        /// <summary>
        /// Validates a license serial against the public key and current machine.
        /// </summary>
        public LicenseValidationResult ValidateLicense(string serial, string currentMachineId)
        {
            if (string.IsNullOrWhiteSpace(_publicKeyPem))
            {
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Message = "Configuración de licencia incompleta. Contacte soporte técnico."
                };
            }

            try
            {
                var parts = serial.Trim().Split('.');
                if (parts.Length != 2)
                    return new LicenseValidationResult { IsValid = false, Message = "Formato de licencia inválido." };

                var payloadBase64 = parts[0];
                var signatureBase64 = parts[1];

                byte[] payloadBytes;
                byte[] signatureBytes;
                try
                {
                    payloadBytes = Convert.FromBase64String(payloadBase64);
                    signatureBytes = Convert.FromBase64String(signatureBase64);
                }
                catch (FormatException)
                {
                    return new LicenseValidationResult { IsValid = false, Message = "Licencia corrupta o con formato incorrecto." };
                }

                var payloadJson = Encoding.UTF8.GetString(payloadBytes);
                var payload = JsonSerializer.Deserialize<LicensePayload>(payloadJson);

                if (payload == null)
                    return new LicenseValidationResult { IsValid = false, Message = "No se pudo leer los datos de la licencia." };

                // 1. Cryptographic Verification (RSA-PSS SHA256)
                using var rsa = RSA.Create();
                rsa.ImportFromPem(_publicKeyPem.Trim());

                bool isSignatureValid = rsa.VerifyData(
                    payloadBytes,
                    signatureBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pss
                );

                if (!isSignatureValid)
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        Message = "Firma de licencia inválida. La licencia fue alterada o no es auténtica."
                    };

                // 2. Expiration check
                if (DateTime.TryParseExact(payload.ExpirationDate, "yyyy-MM-dd", null,
                    System.Globalization.DateTimeStyles.None, out DateTime expDate))
                {
                    int daysRemaining = (expDate.Date - DateTime.UtcNow.Date).Days;
                    if (daysRemaining < 0)
                    {
                        return new LicenseValidationResult
                        {
                            IsValid = false,
                            Message = $"La licencia expiró el {payload.ExpirationDate}.",
                            Payload = payload,
                            DaysRemaining = 0
                        };
                    }

                    // 3. Hardware ID (Node-locking) verification
                    if (!string.IsNullOrEmpty(payload.MachineId) && payload.MachineId != currentMachineId)
                    {
                        return new LicenseValidationResult
                        {
                            IsValid = false,
                            Message = "Esta licencia pertenece a otro equipo (Hardware ID no coincide).",
                            Payload = payload
                        };
                    }

                    return new LicenseValidationResult
                    {
                        IsValid = true,
                        Message = "Licencia válida",
                        Payload = payload,
                        DaysRemaining = daysRemaining
                    };
                }
                else
                {
                    return new LicenseValidationResult
                    {
                        IsValid = false,
                        Message = "Formato de fecha de expiración inválido en la licencia."
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al validar la licencia");
                return new LicenseValidationResult
                {
                    IsValid = false,
                    Message = "Error interno al validar la licencia."
                };
            }
        }

        /// <summary>
        /// Saves a license key to the license file.
        /// </summary>
        public bool SaveLicense(string serial)
        {
            try
            {
                File.WriteAllText(_licenseFilePath, serial.Trim());
                // Invalidate cache
                _cachedLicenseInfo = null;
                _lastCheck = DateTime.MinValue;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al guardar la licencia");
                return false;
            }
        }

        /// <summary>
        /// Reads the license file.
        /// </summary>
        public string? ReadLicenseFile()
        {
            try
            {
                if (File.Exists(_licenseFilePath))
                {
                    return File.ReadAllText(_licenseFilePath).Trim();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al leer el archivo de licencia");
            }
            return null;
        }

        /// <summary>
        /// Forces a recheck of the license.
        /// </summary>
        public void InvalidateCache()
        {
            _cachedLicenseInfo = null;
            _lastCheck = DateTime.MinValue;
        }
    }
}
