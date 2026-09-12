using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace NeuroSpec
{
    /// <summary>
    /// Модель данных с характеристиками компьютера,
    /// собранными приложением NeuroSpec.
    /// </summary>
    public class SystemSpecs
    {
        public string OperatingSystem { get; set; } = string.Empty;
        public string WindowsInstallDate { get; set; } = string.Empty;
        public string Processor { get; set; } = string.Empty;
        public string RandomAccessMemory { get; set; } = string.Empty;
        public string VideoController { get; set; } = string.Empty;
        public string Motherboard { get; set; } = string.Empty;
        public string Bios { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;
        public string DiskSpace { get; set; } = string.Empty;
        public string NetworkAdapters { get; set; } = string.Empty;
        public string Monitors { get; set; } = string.Empty;

        /// <summary>
        /// Формирует текстовое представление всех характеристик
        /// для копирования в буфер обмена или сохранения в .txt.
        /// </summary>
        public string ToClipboardText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Характеристики компьютера (собрано в NeuroSpec) ===");
            sb.AppendLine();
            sb.AppendLine($"Операционная система: {OperatingSystem}");
            sb.AppendLine($"Дата установки Windows: {WindowsInstallDate}");
            sb.AppendLine($"Процессор: {Processor}");
            sb.AppendLine($"Оперативная память: {RandomAccessMemory}");
            sb.AppendLine($"Видеокарта: {VideoController}");
            sb.AppendLine($"Материнская плата: {Motherboard}");
            sb.AppendLine($"BIOS/UEFI: {Bios}");
            sb.AppendLine($"Серийный номер: {SerialNumber}");
            sb.AppendLine($"Накопители: {Storage}");
            sb.AppendLine($"Свободное место: {DiskSpace}");
            sb.AppendLine($"Сеть: {NetworkAdapters}");
            sb.AppendLine($"Мониторы: {Monitors}");
            return sb.ToString();
        }

        /// <summary>
        /// Формирует JSON-представление характеристик для сохранения в .json.
        /// Кириллица не экранируется, чтобы файл оставался читаемым.
        /// </summary>
        public string ToJson()
        {
            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };
            return JsonSerializer.Serialize(this, options);
        }
    }
}
