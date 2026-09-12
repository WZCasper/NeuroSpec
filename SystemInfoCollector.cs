using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Management;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuroSpec
{
    /// <summary>
    /// Собирает информацию об оборудовании и операционной системе
    /// через WMI (Windows Management Instrumentation) и системные API.
    /// Каждый метод защищён от сбоев: если конкретный класс WMI недоступен
    /// (например, отключена служба WMI или недостаточно прав), возвращается
    /// понятное сообщение вместо аварийного завершения приложения.
    /// </summary>
    public static class SystemInfoCollector
    {
        private const string NotDetected = "Не удалось определить";

        public static Task<SystemSpecs> CollectAsync()
        {
            return Task.Run(() => Collect());
        }

        public static SystemSpecs Collect()
        {
            return new SystemSpecs
            {
                OperatingSystem = GetOperatingSystemInfo(),
                WindowsInstallDate = GetWindowsInstallDate(),
                Processor = GetProcessorInfo(),
                RandomAccessMemory = GetRamInfo(),
                VideoController = GetVideoControllerInfo(),
                Motherboard = GetMotherboardInfo(),
                Bios = GetBiosInfo(),
                SerialNumber = GetSerialNumber(),
                Storage = GetStorageInfo(),
                DiskSpace = GetDiskSpaceInfo(),
                NetworkAdapters = GetNetworkAdaptersInfo(),
                Monitors = GetMonitorsInfo()
            };
        }

        private static string GetOperatingSystemInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Caption, OSArchitecture, Version FROM Win32_OperatingSystem");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string caption = obj["Caption"]?.ToString()?.Trim() ?? "Windows";
                    string architecture = obj["OSArchitecture"]?.ToString()?.Trim() ?? "";
                    string version = obj["Version"]?.ToString()?.Trim() ?? "";

                    caption = string.Join(" ", caption.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

                    string result = caption;
                    if (!string.IsNullOrWhiteSpace(architecture))
                    {
                        result += $", {architecture}";
                    }
                    if (!string.IsNullOrWhiteSpace(version))
                    {
                        result += $" (сборка {version})";
                    }

                    return result;
                }
            }
            catch
            {
                // WMI недоступен — вернём запасное значение ниже.
            }

            return NotDetected;
        }

        private static string GetWindowsInstallDate()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT InstallDate FROM Win32_OperatingSystem");
                foreach (ManagementObject obj in searcher.Get())
                {
                    string raw = obj["InstallDate"]?.ToString();
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        continue;
                    }

                    DateTime date = ManagementDateTimeConverter.ToDateTime(raw);
                    return date.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetProcessorInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");

                var names = new List<string>();

                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString()?.Trim() ?? "";
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    name = string.Join(" ", name.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

                    string cores = obj["NumberOfCores"]?.ToString() ?? "";
                    string threads = obj["NumberOfLogicalProcessors"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(cores) && !string.IsNullOrEmpty(threads))
                    {
                        name += $" ({cores} ядер, {threads} потоков)";
                    }

                    names.Add(name);
                }

                if (names.Count > 0)
                {
                    return string.Join("; ", names.Distinct());
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetRamInfo()
        {
            try
            {
                ulong totalBytes = 0;
                int moduleCount = 0;

                using (var searcher = new ManagementObjectSearcher("SELECT Capacity FROM Win32_PhysicalMemory"))
                {
                    foreach (ManagementObject obj in searcher.Get())
                    {
                        if (obj["Capacity"] != null && ulong.TryParse(obj["Capacity"].ToString(), out ulong capacity))
                        {
                            totalBytes += capacity;
                            moduleCount++;
                        }
                    }
                }

                if (totalBytes == 0)
                {
                    using var fallbackSearcher = new ManagementObjectSearcher(
                        "SELECT TotalPhysicalMemory FROM Win32_ComputerSystem");

                    foreach (ManagementObject obj in fallbackSearcher.Get())
                    {
                        if (obj["TotalPhysicalMemory"] != null &&
                            ulong.TryParse(obj["TotalPhysicalMemory"].ToString(), out ulong total))
                        {
                            totalBytes = total;
                        }
                    }
                }

                if (totalBytes > 0)
                {
                    double gb = totalBytes / 1024.0 / 1024.0 / 1024.0;
                    string result = $"{Math.Round(gb)} ГБ";

                    if (moduleCount > 0)
                    {
                        result += $" ({moduleCount} {PluralizeRu(moduleCount, "модуль", "модуля", "модулей")})";
                    }

                    return result;
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetVideoControllerInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController");

                var cards = new List<string>();

                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    // Пропускаем виртуальные адаптеры удалённого рабочего стола -
                    // это не настоящая видеокарта пользователя.
                    if (name.ToLowerInvariant().Contains("remote"))
                    {
                        continue;
                    }

                    cards.Add(name);
                }

                if (cards.Count > 0)
                {
                    return string.Join("; ", cards.Distinct());
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetMotherboardInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Manufacturer, Product FROM Win32_BaseBoard");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                    string product = obj["Product"]?.ToString()?.Trim() ?? "";

                    if (string.IsNullOrWhiteSpace(manufacturer) && string.IsNullOrWhiteSpace(product))
                    {
                        continue;
                    }

                    return string.IsNullOrWhiteSpace(manufacturer)
                        ? product
                        : $"{manufacturer} {product}".Trim();
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetBiosInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Manufacturer, SMBIOSBIOSVersion, ReleaseDate FROM Win32_BIOS");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string manufacturer = obj["Manufacturer"]?.ToString()?.Trim() ?? "";
                    string version = obj["SMBIOSBIOSVersion"]?.ToString()?.Trim() ?? "";
                    string releaseDateRaw = obj["ReleaseDate"]?.ToString();

                    string result = string.Join(" ", new[] { manufacturer, version }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));

                    if (!string.IsNullOrWhiteSpace(releaseDateRaw))
                    {
                        try
                        {
                            DateTime releaseDate = ManagementDateTimeConverter.ToDateTime(releaseDateRaw);
                            result += $" (от {releaseDate:dd.MM.yyyy})";
                        }
                        catch
                        {
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(result))
                    {
                        return result;
                    }
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetSerialNumber()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT SerialNumber FROM Win32_BIOS");

                foreach (ManagementObject obj in searcher.Get())
                {
                    string serial = obj["SerialNumber"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(serial))
                    {
                        continue;
                    }

                    // На многих домашних/самосборных ПК производитель платы пишет сюда
                    // служебную заглушку вместо настоящего номера - не показываем её.
                    string lower = serial.ToLowerInvariant();
                    if (lower.Contains("to be filled") || lower == "default string" ||
                        lower == "system serial number" || lower == "none")
                    {
                        continue;
                    }

                    return serial;
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        /// <summary>
        /// Сначала пробуем современный класс MSFT_PhysicalDisk - он умеет
        /// определять тип накопителя (SSD/HDD). Если он недоступен (старые
        /// системы, Server Core без модуля хранения) - используем
        /// классический Win32_DiskDrive без указания типа.
        /// </summary>
        private static string GetStorageInfo()
        {
            string modern = GetStorageInfoModern();
            if (modern != null)
            {
                return modern;
            }

            return GetStorageInfoLegacy();
        }

        private static string GetStorageInfoModern()
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                scope.Connect();

                var query = new ObjectQuery("SELECT FriendlyName, MediaType, Size FROM MSFT_PhysicalDisk");
                using var searcher = new ManagementObjectSearcher(scope, query);

                var disks = new List<string>();

                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["FriendlyName"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    string typeLabel = "";
                    if (obj["MediaType"] != null && int.TryParse(obj["MediaType"].ToString(), out int mediaType))
                    {
                        typeLabel = mediaType switch
                        {
                            4 => "SSD",
                            3 => "HDD",
                            5 => "SCM",
                            _ => ""
                        };
                    }

                    string sizeInfo = "";
                    if (obj["Size"] != null && ulong.TryParse(obj["Size"].ToString(), out ulong sizeBytes) && sizeBytes > 0)
                    {
                        double sizeGb = sizeBytes / 1024.0 / 1024.0 / 1024.0;
                        sizeInfo = $", {Math.Round(sizeGb)} ГБ";
                    }

                    string typePart = string.IsNullOrEmpty(typeLabel) ? "" : $" [{typeLabel}]";
                    disks.Add($"{name}{typePart}{sizeInfo}");
                }

                if (disks.Count > 0)
                {
                    return string.Join("; ", disks);
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        private static string GetStorageInfoLegacy()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive");

                var disks = new List<string>();

                foreach (ManagementObject obj in searcher.Get())
                {
                    string model = obj["Model"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(model))
                    {
                        continue;
                    }

                    string sizeInfo = "";
                    if (obj["Size"] != null && ulong.TryParse(obj["Size"].ToString(), out ulong sizeBytes) && sizeBytes > 0)
                    {
                        double sizeGb = sizeBytes / 1024.0 / 1024.0 / 1024.0;
                        sizeInfo = $" ({Math.Round(sizeGb)} ГБ)";
                    }

                    disks.Add($"{model}{sizeInfo}");
                }

                if (disks.Count > 0)
                {
                    return string.Join("; ", disks);
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetDiskSpaceInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT DeviceID, Size, FreeSpace FROM Win32_LogicalDisk WHERE DriveType=3");

                var drives = new List<string>();

                foreach (ManagementObject obj in searcher.Get())
                {
                    string deviceId = obj["DeviceID"]?.ToString();
                    if (string.IsNullOrWhiteSpace(deviceId))
                    {
                        continue;
                    }

                    if (obj["Size"] == null || obj["FreeSpace"] == null)
                    {
                        continue;
                    }
                    if (!ulong.TryParse(obj["Size"].ToString(), out ulong size) || size == 0)
                    {
                        continue;
                    }
                    if (!ulong.TryParse(obj["FreeSpace"].ToString(), out ulong free))
                    {
                        continue;
                    }

                    double sizeGb = size / 1024.0 / 1024.0 / 1024.0;
                    double freeGb = free / 1024.0 / 1024.0 / 1024.0;

                    drives.Add($"{deviceId} {Math.Round(freeGb)} ГБ свободно из {Math.Round(sizeGb)} ГБ");
                }

                if (drives.Count > 0)
                {
                    return string.Join("; ", drives);
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetNetworkAdaptersInfo()
        {
            try
            {
                using var searcher = new ManagementObjectSearcher(
                    "SELECT Name, MACAddress FROM Win32_NetworkAdapter WHERE PhysicalAdapter=true AND NetEnabled=true");

                var adapters = new List<string>();

                foreach (ManagementObject obj in searcher.Get())
                {
                    string name = obj["Name"]?.ToString()?.Trim();
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    string mac = obj["MACAddress"]?.ToString()?.Trim();
                    string entry = !string.IsNullOrWhiteSpace(mac) ? $"{name} ({mac})" : name;
                    adapters.Add(entry);
                }

                if (adapters.Count > 0)
                {
                    return string.Join("; ", adapters);
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        private static string GetMonitorsInfo()
        {
            try
            {
                Screen[] screens = Screen.AllScreens;
                if (screens != null && screens.Length > 0)
                {
                    var descriptions = screens
                        .Select(s => $"{s.Bounds.Width}x{s.Bounds.Height}{(s.Primary ? " (основной)" : "")}")
                        .ToList();

                    string countText = $"{screens.Length} {PluralizeRu(screens.Length, "монитор", "монитора", "мониторов")}";
                    return $"{countText}: {string.Join("; ", descriptions)}";
                }
            }
            catch
            {
            }

            return NotDetected;
        }

        /// <summary>
        /// Корректное склонение русских существительных по числу
        /// (например: 1 модуль, 2 модуля, 5 модулей).
        /// </summary>
        private static string PluralizeRu(int number, string one, string few, string many)
        {
            int n = Math.Abs(number) % 100;
            int n1 = n % 10;

            if (n > 10 && n < 20)
            {
                return many;
            }
            if (n1 > 1 && n1 < 5)
            {
                return few;
            }
            if (n1 == 1)
            {
                return one;
            }
            return many;
        }
    }
}
