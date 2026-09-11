using System.Text;

namespace NeuroSpec
{
    /// <summary>
    /// Модель данных с характеристиками компьютера,
    /// собранными приложением NeuroSpec.
    /// </summary>
    public class SystemSpecs
    {
        public string OperatingSystem { get; set; } = string.Empty;
        public string Processor { get; set; } = string.Empty;
        public string RandomAccessMemory { get; set; } = string.Empty;
        public string VideoController { get; set; } = string.Empty;
        public string Motherboard { get; set; } = string.Empty;
        public string Storage { get; set; } = string.Empty;

        /// <summary>
        /// Формирует текстовое представление всех характеристик
        /// для копирования в буфер обмена.
        /// </summary>
        public string ToClipboardText()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== Характеристики компьютера (собрано в NeuroSpec) ===");
            sb.AppendLine();
            sb.AppendLine($"Операционная система: {OperatingSystem}");
            sb.AppendLine($"Процессор: {Processor}");
            sb.AppendLine($"Оперативная память: {RandomAccessMemory}");
            sb.AppendLine($"Видеокарта: {VideoController}");
            sb.AppendLine($"Материнская плата: {Motherboard}");
            sb.AppendLine($"Накопители: {Storage}");
            return sb.ToString();
        }
    }
}
