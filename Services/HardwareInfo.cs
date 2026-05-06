using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;

namespace Sistema_Ferreteria.Services
{
    /// <summary>
    /// Generates a unique hardware fingerprint for the current machine.
    /// Used for node-locking licenses to specific hardware.
    /// </summary>
    public static class HardwareInfo
    {
        private static string? _cachedMachineId;

        public static string GetMachineId()
        {
            if (_cachedMachineId != null) return _cachedMachineId;

            string rawId;

            if (OperatingSystem.IsWindows())
            {
                rawId = GetWindowsMachineId();
            }
            else
            {
                // Fallback for non-Windows
                rawId = Environment.MachineName + "-" + Environment.OSVersion.ToString();
            }

            // Hash the raw ID to produce a clean, fixed-length identifier
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawId));
            _cachedMachineId = BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16).ToUpper();

            return _cachedMachineId;
        }

        [SupportedOSPlatform("windows")]
        private static string GetWindowsMachineId()
        {
            string cpuId = "";
            string diskId = "";

            try
            {
                // Use WMI to get CPU ProcessorId
                using var cpuSearcher = new System.Management.ManagementObjectSearcher("SELECT ProcessorId FROM Win32_Processor");
                foreach (var obj in cpuSearcher.Get())
                {
                    cpuId = obj["ProcessorId"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(cpuId)) break;
                }
            }
            catch { /* WMI not available */ }

            try
            {
                // Use WMI to get Disk SerialNumber
                using var diskSearcher = new System.Management.ManagementObjectSearcher("SELECT SerialNumber FROM Win32_DiskDrive");
                foreach (var obj in diskSearcher.Get())
                {
                    diskId = obj["SerialNumber"]?.ToString()?.Trim() ?? "";
                    if (!string.IsNullOrEmpty(diskId)) break;
                }
            }
            catch { /* WMI not available */ }

            // Fallback if WMI fails
            if (string.IsNullOrEmpty(cpuId) && string.IsNullOrEmpty(diskId))
            {
                return Environment.MachineName + "-" + Environment.UserName;
            }

            return $"{cpuId}-{diskId}";
        }
    }
}
