using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace NeuroSpec
{
    /// <summary>
    /// Профиль требований конкретной игры для приблизительной оценки производительности.
    /// Значения условные (баллы по внутренней шкале 0-100), подобраны вручную по опыту,
    /// а не взяты из официальных системных требований.
    /// </summary>
    public class GameProfile
    {
        public string Name { get; set; }
        public int MinCpuScore { get; set; }
        public int RecCpuScore { get; set; }
        public int MinRamGb { get; set; }
        public int RecRamGb { get; set; }
        public int MinGpuScore { get; set; }
        public int RecGpuScore { get; set; }
    }

    /// <summary>
    /// Результат оценки: общий балл ПК и, если выбрана игра, вывод по ней.
    /// </summary>
    public class PerformanceResult
    {
        public int CpuScore { get; set; }
        public int RamScore { get; set; }
        public int GpuScore { get; set; }
        public int StorageScore { get; set; }
        public int OverallScore { get; set; }
        public string OverallRating { get; set; }
        public string GameName { get; set; }
        public string GameVerdict { get; set; }
        public string GameQualityTier { get; set; }
    }

    /// <summary>
    /// Оценивает производительность ПК по уже собранным текстовым характеристикам
    /// (SystemSpecs). Это эвристическая оценка на основе модели/объёма железа,
    /// а не результат реального бенчмарка.
    /// </summary>
    public static class PerformanceEvaluator
    {
        // Известные видеокарты с условным относительным баллом (0-100).
        // Порядок важен: более специфичные названия должны идти раньше общих.
        private static readonly (string Keyword, int Score)[] GpuTiers =
        {
            ("RTX 4090", 100), ("RTX 4080", 92), ("RTX 4070 TI", 85), ("RTX 4070", 80),
            ("RTX 4060 TI", 72), ("RTX 4060", 65), ("RTX 3090", 90), ("RTX 3080", 85),
            ("RTX 3070 TI", 78), ("RTX 3070", 75), ("RTX 3060 TI", 68), ("RTX 3060", 60),
            ("RTX 3050", 48), ("RTX 2080 TI", 78), ("RTX 2080", 70), ("RTX 2070", 62),
            ("RTX 2060", 55), ("GTX 1660 TI", 45), ("GTX 1660", 42), ("GTX 1650", 33),
            ("GTX 1080 TI", 65), ("GTX 1080", 58), ("GTX 1070", 50), ("GTX 1060", 40),
            ("GTX 1050 TI", 28), ("GTX 1050", 24),
            ("RX 7900 XTX", 95), ("RX 7900 XT", 88), ("RX 7800 XT", 78), ("RX 7700 XT", 70),
            ("RX 7600", 55), ("RX 6950 XT", 87), ("RX 6900 XT", 84), ("RX 6800 XT", 80),
            ("RX 6800", 74), ("RX 6700 XT", 66), ("RX 6600", 52), ("RX 6500", 32),
            ("RX 580", 33), ("RX 570", 30), ("VEGA 64", 45),
            ("ARC A770", 60), ("ARC A750", 55),
            ("IRIS XE", 15), ("IRIS", 12), ("UHD GRAPHICS", 8), ("HD GRAPHICS", 5),
            ("RADEON(TM) GRAPHICS", 15), ("RADEON GRAPHICS", 15)
        };

        public static PerformanceResult Evaluate(SystemSpecs specs, GameProfile game)
        {
            int cpuScore = EstimateCpuScore(specs?.Processor);
            int ramScore = EstimateRamScore(specs?.RandomAccessMemory);
            int gpuScore = EstimateGpuScore(specs?.VideoController);
            int storageScore = EstimateStorageScore(specs?.Storage);

            int overall = (int)Math.Round(
                (cpuScore * 0.32) + (gpuScore * 0.40) + (ramScore * 0.18) + (storageScore * 0.10));
            overall = Math.Clamp(overall, 0, 100);

            var result = new PerformanceResult
            {
                CpuScore = cpuScore,
                RamScore = ramScore,
                GpuScore = gpuScore,
                StorageScore = storageScore,
                OverallScore = overall,
                OverallRating = RatingLabel(overall)
            };

            if (game != null)
            {
                result.GameName = game.Name;
                EvaluateForGame(result, game, cpuScore, ramScore, gpuScore);
            }

            return result;
        }

        public static List<GameProfile> GetGameCatalog()
        {
            return new List<GameProfile>
            {
                new GameProfile { Name = "Cyberpunk 2077", MinCpuScore = 35, RecCpuScore = 55, MinRamGb = 8, RecRamGb = 16, MinGpuScore = 40, RecGpuScore = 65 },
                new GameProfile { Name = "GTA V", MinCpuScore = 20, RecCpuScore = 35, MinRamGb = 4, RecRamGb = 8, MinGpuScore = 20, RecGpuScore = 40 },
                new GameProfile { Name = "Fortnite", MinCpuScore = 18, RecCpuScore = 30, MinRamGb = 8, RecRamGb = 16, MinGpuScore = 20, RecGpuScore = 40 },
                new GameProfile { Name = "Valorant", MinCpuScore = 12, RecCpuScore = 22, MinRamGb = 4, RecRamGb = 8, MinGpuScore = 10, RecGpuScore = 25 },
                new GameProfile { Name = "CS2", MinCpuScore = 15, RecCpuScore = 28, MinRamGb = 8, RecRamGb = 16, MinGpuScore = 15, RecGpuScore = 35 },
                new GameProfile { Name = "Dota 2", MinCpuScore = 12, RecCpuScore = 22, MinRamGb = 4, RecRamGb = 8, MinGpuScore = 12, RecGpuScore = 25 },
                new GameProfile { Name = "The Witcher 3", MinCpuScore = 22, RecCpuScore = 35, MinRamGb = 6, RecRamGb = 8, MinGpuScore = 25, RecGpuScore = 45 },
                new GameProfile { Name = "Red Dead Redemption 2", MinCpuScore = 35, RecCpuScore = 55, MinRamGb = 8, RecRamGb = 12, MinGpuScore = 40, RecGpuScore = 65 },
                new GameProfile { Name = "Elden Ring", MinCpuScore = 25, RecCpuScore = 40, MinRamGb = 8, RecRamGb = 12, MinGpuScore = 30, RecGpuScore = 50 },
                new GameProfile { Name = "Minecraft", MinCpuScore = 10, RecCpuScore = 20, MinRamGb = 4, RecRamGb = 8, MinGpuScore = 5, RecGpuScore = 15 },
                new GameProfile { Name = "Baldur's Gate 3", MinCpuScore = 30, RecCpuScore = 45, MinRamGb = 8, RecRamGb = 16, MinGpuScore = 35, RecGpuScore = 55 },
                new GameProfile { Name = "Call of Duty: Warzone", MinCpuScore = 35, RecCpuScore = 55, MinRamGb = 8, RecRamGb = 16, MinGpuScore = 40, RecGpuScore = 65 },
                new GameProfile { Name = "Apex Legends", MinCpuScore = 25, RecCpuScore = 40, MinRamGb = 6, RecRamGb = 8, MinGpuScore = 30, RecGpuScore = 50 },
                new GameProfile { Name = "Starfield", MinCpuScore = 40, RecCpuScore = 60, MinRamGb = 16, RecRamGb = 16, MinGpuScore = 45, RecGpuScore = 70 }
            };
        }

        private static void EvaluateForGame(PerformanceResult result, GameProfile game, int cpuScore, int ramScore, int gpuScore)
        {
            bool meetsMin = cpuScore >= game.MinCpuScore
                && gpuScore >= game.MinGpuScore
                && ramScore >= EstimateRamScoreFromValue(game.MinRamGb);

            bool meetsRec = cpuScore >= game.RecCpuScore
                && gpuScore >= game.RecGpuScore
                && ramScore >= EstimateRamScoreFromValue(game.RecRamGb);

            if (!meetsMin)
            {
                result.GameQualityTier = "Не рекомендуется";
                result.GameVerdict = "Компьютер не дотягивает до минимальных требований — игра может не запуститься или будет сильно тормозить.";
            }
            else if (!meetsRec)
            {
                result.GameQualityTier = "Средние настройки";
                result.GameVerdict = "Минимальным требованиям компьютер соответствует. Ожидаемо комфортная игра на средних настройках графики.";
            }
            else
            {
                int margin = Math.Min(cpuScore - game.RecCpuScore, gpuScore - game.RecGpuScore);
                if (margin > 20)
                {
                    result.GameQualityTier = "Высокие / ультра";
                    result.GameVerdict = "Запас производительности выше рекомендованных требований — можно играть на высоких или максимальных настройках.";
                }
                else
                {
                    result.GameQualityTier = "Высокие настройки";
                    result.GameVerdict = "Компьютер соответствует рекомендованным требованиям — комфортная игра на высоких настройках.";
                }
            }
        }

        private static string RatingLabel(int score)
        {
            if (score >= 85) return "Топовый игровой ПК";
            if (score >= 65) return "Мощный ПК";
            if (score >= 45) return "Средний ПК";
            if (score >= 25) return "Начальный уровень";
            return "Слабая конфигурация";
        }

        private static int EstimateCpuScore(string processorText)
        {
            if (string.IsNullOrWhiteSpace(processorText))
            {
                return 30;
            }

            string text = processorText.ToUpperInvariant();

            int cores = 4;
            Match coresMatch = Regex.Match(text, @"(\d+)[\s-]*(CORES|CORE|ЯДЕР|ЯДРА)");
            if (coresMatch.Success && int.TryParse(coresMatch.Groups[1].Value, out int parsedCores))
            {
                cores = parsedCores;
            }

            double ghz = 3.0;
            Match ghzMatch = Regex.Match(text, @"(\d+(?:[.,]\d+)?)\s*(GHZ|ГГЦ)");
            if (ghzMatch.Success)
            {
                string num = ghzMatch.Groups[1].Value.Replace(',', '.');
                if (double.TryParse(num, NumberStyles.Any, CultureInfo.InvariantCulture, out double parsedGhz))
                {
                    ghz = parsedGhz;
                }
            }

            double familyBonus = 0;
            if (text.Contains("I9") || text.Contains("RYZEN 9")) familyBonus = 12;
            else if (text.Contains("I7") || text.Contains("RYZEN 7")) familyBonus = 8;
            else if (text.Contains("I5") || text.Contains("RYZEN 5")) familyBonus = 4;
            else if (text.Contains("I3") || text.Contains("RYZEN 3")) familyBonus = 0;
            else if (text.Contains("CELERON") || text.Contains("PENTIUM") || text.Contains("ATHLON")) familyBonus = -10;

            double raw = (cores * ghz * 3.0) + familyBonus;
            int rounded = (int)Math.Round(raw);
            return Math.Clamp(rounded, 5, 100);
        }

        private static int EstimateRamScore(string ramText)
        {
            if (string.IsNullOrWhiteSpace(ramText))
            {
                return 30;
            }

            Match match = Regex.Match(ramText, @"(\d+)\s*(GB|ГБ)", RegexOptions.IgnoreCase);
            int gb = match.Success && int.TryParse(match.Groups[1].Value, out int parsedGb) ? parsedGb : 8;

            return EstimateRamScoreFromValue(gb);
        }

        private static int EstimateRamScoreFromValue(int gb)
        {
            if (gb <= 4) return 15;
            if (gb <= 8) return 35;
            if (gb <= 12) return 50;
            if (gb <= 16) return 65;
            if (gb <= 24) return 78;
            if (gb <= 32) return 88;
            return 97;
        }

        private static int EstimateGpuScore(string gpuText)
        {
            if (string.IsNullOrWhiteSpace(gpuText))
            {
                return 20;
            }

            string text = gpuText.ToUpperInvariant();

            foreach ((string keyword, int score) in GpuTiers)
            {
                if (text.Contains(keyword))
                {
                    return score;
                }
            }

            // Модель не распознана - пробуем прикинуть по объёму видеопамяти.
            Match vramMatch = Regex.Match(text, @"(\d+)\s*(MB|МБ|GB|ГБ)");
            if (vramMatch.Success && int.TryParse(vramMatch.Groups[1].Value, out int vram))
            {
                bool isGb = vramMatch.Groups[2].Value is "GB" or "ГБ";
                int vramGb = isGb ? vram : vram / 1024;

                if (vramGb >= 16) return 75;
                if (vramGb >= 12) return 65;
                if (vramGb >= 8) return 55;
                if (vramGb >= 6) return 45;
                if (vramGb >= 4) return 35;
                if (vramGb >= 2) return 20;
            }

            return 20; // Неизвестная или встроенная графика - осторожная базовая оценка.
        }

        private static int EstimateStorageScore(string storageText)
        {
            if (string.IsNullOrWhiteSpace(storageText))
            {
                return 40;
            }

            string text = storageText.ToUpperInvariant();
            if (text.Contains("NVME")) return 100;
            if (text.Contains("SSD")) return 85;
            if (text.Contains("HDD")) return 35;
            return 50;
        }
    }
}
