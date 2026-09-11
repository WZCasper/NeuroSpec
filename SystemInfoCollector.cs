using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Threading.Tasks;

namespace NeuroSpec
{
    /// <summary>
    /// Собирает информацию об оборудовании и операционной системе
    /// через WMI (Windows Management Instrumentation).
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
                Processor = GetProcessorInfo(),
                RandomAccessMemory = GetRamInfo(),
                VideoController = GetVideoControllerInfo(),
                Motherboard = GetMotherboardInfo(),
                Storage = GetStorageInfo()
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

        private static string GetStorageInfo()
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
