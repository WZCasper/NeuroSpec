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
    /// Каждый метод защищён от сбоев: если конкретный класс WMI недоступен,
    /// возвращается null (что при отображении заменяется на локализованное
    /// сообщение "не удалось определить") - без аварийного завершения.
    /// Единицы измерения и служебные слова (ГБ, ядер/потоков и т.п.)
    /// подставляются на языке, переданном параметром lang.
    /// </summary>
    public static class SystemInfoCollector
    {
        public static Task<SystemSpecs> CollectAsync(AppLanguage lang)
        {
            return Task.Run(() => Collect(lang));
        }

        public static SystemSpecs Collect(AppLanguage lang)
        {
            return new SystemSpecs
            {
                OperatingSystem = GetOperatingSystemInfo(lang),
                WindowsInstallDate = GetWindowsInstallDate(),
                Processor = GetProcessorInfo(lang),
                RandomAccessMemory = GetRamInfo(lang),
                VideoController = GetVideoControllerInfo(),
                Motherboard = GetMotherboardInfo(),
                Bios = GetBiosInfo(lang),
                SerialNumber = GetSerialNumber(),
                Storage = GetStorageInfo(lang),
                DiskSpace = GetDiskSpaceInfo(lang),
                NetworkAdapters = GetNetworkAdaptersInfo(),
                Monitors = GetMonitorsInfo(lang)
            };
        }

        private static string GetOperatingSystemInfo(AppLanguage lang)
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
                        result += Localization.GetFormat(lang, "BuildSuffixFormat", version);
                    }

                    return result;
                }
            }
            catch
            {
                // WMI недоступен — вернём null, отобразится как "не удалось определить".
            }

            return null;
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

            return null;
        }

        private static string GetProcessorInfo(AppLanguage lang)
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
                        name += Localization.GetFormat(lang, "CoresThreadsFormat", cores, threads);
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

            return null;
        }

        private static string GetRamInfo(AppLanguage lang)
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
                    string gbUnit = Localization.Get(lang, "GbUnit");
                    string result = $"{Math.Round(gb)} {gbUnit}";

                    if (moduleCount > 0)
                    {
                        string moduleWord = Localization.Pluralize(
                            lang, moduleCount,
                            Localization.Get(lang, "ModuleOne"),
                            Localization.Get(lang, "ModuleFew"),
                            Localization.Get(lang, "ModuleMany"));
                        result += $" ({moduleCount} {moduleWord})";
                    }

                    return result;
                }
            }
            catch
            {
            }

            return null;
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

            return null;
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

            return null;
        }

        private static string GetBiosInfo(AppLanguage lang)
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
                            result += Localization.GetFormat(lang, "BiosDateFormat", releaseDate);
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

            return null;
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

            return null;
        }

        /// <summary>
        /// Сначала пробуем современный класс MSFT_PhysicalDisk - он умеет
        /// определять тип накопителя (SSD/HDD). Если он недоступен (старые
        /// системы, Server Core без модуля хранения) - используем
        /// классический Win32_DiskDrive без указания типа.
        /// </summary>
        private static string GetStorageInfo(AppLanguage lang)
        {
            string modern = GetStorageInfoModern(lang);
            if (modern != null)
            {
                return modern;
            }

            return GetStorageInfoLegacy(lang);
        }

        private static string GetStorageInfoModern(AppLanguage lang)
        {
            try
            {
                var scope = new ManagementScope(@"\\.\root\Microsoft\Windows\Storage");
                scope.Connect();

                var query = new ObjectQuery("SELECT FriendlyName, MediaType, Size FROM MSFT_PhysicalDisk");
                using var searcher = new ManagementObjectSearcher(scope, query);

                string gbUnit = Localization.Get(lang, "GbUnit");
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
                        sizeInfo = $", {Math.Round(sizeGb)} {gbUnit}";
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

        private static string GetStorageInfoLegacy(AppLanguage lang)
        {
            try
            {
                using var searcher = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive");

                string gbUnit = Localization.Get(lang, "GbUnit");
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
                        sizeInfo = $" ({Math.Round(sizeGb)} {gbUnit})";
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

            return null;
        }

        private static string GetDiskSpaceInfo(AppLanguage lang)
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

                    drives.Add(Localization.GetFormat(
                        lang, "DiskSpaceFormat", deviceId, Math.Round(freeGb), Math.Round(sizeGb)));
                }

                if (drives.Count > 0)
                {
                    return string.Join("; ", drives);
                }
            }
            catch
            {
            }

            return null;
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

            return null;
        }

        private static string GetMonitorsInfo(AppLanguage lang)
        {
            try
            {
                Screen[] screens = Screen.AllScreens;
                if (screens != null && screens.Length > 0)
                {
                    string primarySuffix = Localization.Get(lang, "PrimaryMonitorSuffix");

                    var descriptions = screens
                        .Select(s => $"{s.Bounds.Width}x{s.Bounds.Height}{(s.Primary ? primarySuffix : "")}")
                        .ToList();

                    string monitorWord = Localization.Pluralize(
                        lang, screens.Length,
                        Localization.Get(lang, "MonitorOne"),
                        Localization.Get(lang, "MonitorFew"),
                        Localization.Get(lang, "MonitorMany"));

                    return $"{screens.Length} {monitorWord}: {string.Join("; ", descriptions)}";
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
