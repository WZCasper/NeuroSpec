using System;
using System.IO;
using System.Text.Json;

namespace NeuroSpec
{
    /// <summary>
    /// Настройки пользователя (сейчас - только выбранный язык интерфейса).
    /// Хранится в %APPDATA%\NeuroSpec\settings.json - это правильное место
    /// для пользовательских настроек портативного приложения, так как
    /// не требует прав на запись рядом с самим .exe (он может лежать
    /// на флешке или в папке "только для чтения").
    /// </summary>
    public class AppSettings
    {
        public string Language { get; set; } = "Ru";

        private static string GetSettingsPath()
        {
            string dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "NeuroSpec");
            Directory.CreateDirectory(dir);
            return Path.Combine(dir, "settings.json");
        }

        public static AppSettings Load()
        {
            try
            {
                string path = GetSettingsPath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // Не удалось прочитать настройки - используем значения по умолчанию.
            }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string path = GetSettingsPath();
                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(path, json);
            }
            catch
            {
                // Не удалось сохранить настройки - не критично, при следующем
                // запуске просто снова будет использован язык по умолчанию.
            }
        }
    }
}
