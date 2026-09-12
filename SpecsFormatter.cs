using System.Text;

namespace NeuroSpec
{
    /// <summary>
    /// Формирует текстовое представление характеристик на выбранном языке
    /// интерфейса - для копирования в буфер обмена и сохранения в .txt.
    /// Сами собранные значения (модели оборудования и т.п.) не переводятся,
    /// переводятся только названия категорий и заглушка для отсутствующих
    /// данных.
    /// </summary>
    public static class SpecsFormatter
    {
        public static string ToClipboardText(SystemSpecs specs, AppLanguage lang)
        {
            string notDetected = Localization.Get(lang, "NotDetected");
            string V(string value) => string.IsNullOrWhiteSpace(value) ? notDetected : value;

            var sb = new StringBuilder();
            sb.AppendLine("=== " + Localization.Get(lang, "ExportHeader") + " ===");
            sb.AppendLine();
            sb.AppendLine($"{Localization.Get(lang, "CategoryOs")}: {V(specs.OperatingSystem)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryInstallDate")}: {V(specs.WindowsInstallDate)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryCpu")}: {V(specs.Processor)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryRam")}: {V(specs.RandomAccessMemory)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryGpu")}: {V(specs.VideoController)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryMotherboard")}: {V(specs.Motherboard)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryBios")}: {V(specs.Bios)}");
            sb.AppendLine($"{Localization.Get(lang, "CategorySerial")}: {V(specs.SerialNumber)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryStorage")}: {V(specs.Storage)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryDiskSpace")}: {V(specs.DiskSpace)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryNetwork")}: {V(specs.NetworkAdapters)}");
            sb.AppendLine($"{Localization.Get(lang, "CategoryMonitors")}: {V(specs.Monitors)}");
            return sb.ToString();
        }
    }
}
