using System.Text.Encodings.Web;
using System.Text.Json;

namespace NeuroSpec
{
    /// <summary>
    /// Модель данных с характеристиками компьютера. Значения - как их
    /// сообщает сама Windows/WMI (не переводятся). Свойство остаётся
    /// пустым (null), если соответствующие данные собрать не удалось -
    /// подходящее сообщение "не удалось определить" на выбранном языке
    /// подставляется при отображении или экспорте.
    /// </summary>
    public class SystemSpecs
    {
        public string OperatingSystem { get; set; }
        public string WindowsInstallDate { get; set; }
        public string Processor { get; set; }
        public string RandomAccessMemory { get; set; }
        public string VideoController { get; set; }
        public string Motherboard { get; set; }
        public string Bios { get; set; }
        public string SerialNumber { get; set; }
        public string Storage { get; set; }
        public string DiskSpace { get; set; }
        public string NetworkAdapters { get; set; }
        public string Monitors { get; set; }

        /// <summary>
        /// JSON всегда использует английские (camelCase) ключи независимо
        /// от языка интерфейса - так удобнее для автоматической обработки.
        /// Отсутствующие данные сохраняются как null, а не текстовая
        /// заглушка - это корректнее для структурированного формата.
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
