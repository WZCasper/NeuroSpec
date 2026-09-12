using System.Collections.Generic;

namespace NeuroSpec
{
    /// <summary>
    /// Поддерживаемые языки интерфейса приложения.
    /// Порядок соответствует порядку в выпадающем списке.
    /// </summary>
    public enum AppLanguage
    {
        Ru,
        Be,
        Uk,
        En,
        Ar,
        Kk,
        Tr,
        De,
        Hy
    }

    /// <summary>
    /// Переводы интерфейса. Значения, реально собранные с компьютера
    /// (модели оборудования, названия из Windows), не переводятся -
    /// они всегда выводятся так, как их сообщает сама система.
    /// </summary>
    public static class Localization
    {
        public static readonly Dictionary<AppLanguage, string> LanguageNames = new Dictionary<AppLanguage, string>
        {
            [AppLanguage.Ru] = "Русский",
            [AppLanguage.Be] = "Беларуская",
            [AppLanguage.Uk] = "Українська",
            [AppLanguage.En] = "English",
            [AppLanguage.Ar] = "العربية",
            [AppLanguage.Kk] = "Қазақша",
            [AppLanguage.Tr] = "Türkçe",
            [AppLanguage.De] = "Deutsch",
            [AppLanguage.Hy] = "Հայերեն"
        };

        private static readonly Dictionary<AppLanguage, Dictionary<string, string>> All = Build();

        public static string Get(AppLanguage lang, string key)
        {
            if (All.TryGetValue(lang, out var dict) && dict.TryGetValue(key, out var value))
            {
                return value;
            }
            if (All.TryGetValue(AppLanguage.En, out var en) && en.TryGetValue(key, out var enValue))
            {
                return enValue;
            }
            return key;
        }

        public static string GetFormat(AppLanguage lang, string key, params object[] args)
        {
            return string.Format(Get(lang, key), args);
        }

        /// <summary>
        /// Склонение существительного по числу. Для восточнославянских языков
        /// используется трёхформенное правило (1/2-4/5+), для остальных -
        /// упрощённая форма единственное/множественное, соответствующая
        /// грамматике конкретного языка (тюркские и армянский после
        /// числительного используют форму единственного числа).
        /// </summary>
        public static string Pluralize(AppLanguage lang, int count, string one, string few, string many)
        {
            switch (lang)
            {
                case AppLanguage.Ru:
                case AppLanguage.Uk:
                case AppLanguage.Be:
                    return PluralizeSlavic(count, one, few, many);
                case AppLanguage.Tr:
                case AppLanguage.Kk:
                case AppLanguage.Hy:
                    return one;
                default:
                    return count == 1 ? one : few;
            }
        }

        private static string PluralizeSlavic(int number, string one, string few, string many)
        {
            int n = System.Math.Abs(number) % 100;
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

        private static Dictionary<AppLanguage, Dictionary<string, string>> Build()
        {
            return new Dictionary<AppLanguage, Dictionary<string, string>>
            {
                [AppLanguage.Ru] = Ru(),
                [AppLanguage.Be] = Be(),
                [AppLanguage.Uk] = Uk(),
                [AppLanguage.En] = En(),
                [AppLanguage.Ar] = Ar(),
                [AppLanguage.Kk] = Kk(),
                [AppLanguage.Tr] = Tr(),
                [AppLanguage.De] = De(),
                [AppLanguage.Hy] = Hy()
            };
        }

        private static Dictionary<string, string> Ru()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — характеристики компьютера",
                ["Subtitle"] = "Характеристики вашего компьютера",
                ["Loading"] = "Сбор данных о системе...",
                ["BtnRefresh"] = "Обновить",
                ["BtnMore"] = "Ещё ▾",
                ["BtnCopy"] = "Копировать",
                ["MenuCopyImage"] = "Скопировать как картинку",
                ["MenuSaveTxt"] = "Сохранить как .txt",
                ["MenuSaveJson"] = "Сохранить как .json",
                ["StatusCopied"] = "Скопировано в буфер обмена!",
                ["StatusImageCopied"] = "Картинка скопирована в буфер обмена!",
                ["StatusSaved"] = "Файл сохранён!",
                ["StatusErrorCopy"] = "Ошибка копирования: ",
                ["StatusErrorSave"] = "Ошибка сохранения: ",
                ["StatusErrorGeneric"] = "Ошибка: ",
                ["VersionPrefix"] = "Версия ",
                ["UpdateAvailableFormat"] = "Доступна новая версия (сборка {0}) — нажмите, чтобы скачать",
                ["DialogTxtFilter"] = "Текстовый файл (*.txt)|*.txt",
                ["DialogJsonFilter"] = "JSON файл (*.json)|*.json",
                ["NotDetected"] = "Не удалось определить",
                ["ExportHeader"] = "Характеристики компьютера (собрано в NeuroSpec)",
                ["CategoryOs"] = "🖥️ Операционная система",
                ["CategoryInstallDate"] = "📅 Дата установки Windows",
                ["CategoryCpu"] = "⚙️ Процессор",
                ["CategoryRam"] = "💾 Оперативная память",
                ["CategoryGpu"] = "🎮 Видеокарта",
                ["CategoryMotherboard"] = "🔧 Материнская плата",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Серийный номер",
                ["CategoryStorage"] = "💽 Накопители",
                ["CategoryDiskSpace"] = "📦 Свободное место",
                ["CategoryNetwork"] = "🌐 Сеть",
                ["CategoryMonitors"] = "📺 Мониторы",
                ["GbUnit"] = "ГБ",
                ["BuildSuffixFormat"] = " (сборка {0})",
                ["CoresThreadsFormat"] = " ({0} ядер, {1} потоков)",
                ["ModuleOne"] = "модуль",
                ["ModuleFew"] = "модуля",
                ["ModuleMany"] = "модулей",
                ["MonitorOne"] = "монитор",
                ["MonitorFew"] = "монитора",
                ["MonitorMany"] = "мониторов",
                ["PrimaryMonitorSuffix"] = " (основной)",
                ["BiosDateFormat"] = " (от {0:dd.MM.yyyy})",
                ["DiskSpaceFormat"] = "{0}: {1} ГБ свободно из {2} ГБ"
            };
        }

        private static Dictionary<string, string> Uk()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — характеристики комп'ютера",
                ["Subtitle"] = "Характеристики вашого комп'ютера",
                ["Loading"] = "Збір даних про систему...",
                ["BtnRefresh"] = "Оновити",
                ["BtnMore"] = "Ще ▾",
                ["BtnCopy"] = "Копіювати",
                ["MenuCopyImage"] = "Скопіювати як картинку",
                ["MenuSaveTxt"] = "Зберегти як .txt",
                ["MenuSaveJson"] = "Зберегти як .json",
                ["StatusCopied"] = "Скопійовано в буфер обміну!",
                ["StatusImageCopied"] = "Картинку скопійовано в буфер обміну!",
                ["StatusSaved"] = "Файл збережено!",
                ["StatusErrorCopy"] = "Помилка копіювання: ",
                ["StatusErrorSave"] = "Помилка збереження: ",
                ["StatusErrorGeneric"] = "Помилка: ",
                ["VersionPrefix"] = "Версія ",
                ["UpdateAvailableFormat"] = "Доступна нова версія (збірка {0}) — натисніть, щоб завантажити",
                ["DialogTxtFilter"] = "Текстовий файл (*.txt)|*.txt",
                ["DialogJsonFilter"] = "Файл JSON (*.json)|*.json",
                ["NotDetected"] = "Не вдалося визначити",
                ["ExportHeader"] = "Характеристики комп'ютера (зібрано в NeuroSpec)",
                ["CategoryOs"] = "🖥️ Операційна система",
                ["CategoryInstallDate"] = "📅 Дата встановлення Windows",
                ["CategoryCpu"] = "⚙️ Процесор",
                ["CategoryRam"] = "💾 Оперативна пам'ять",
                ["CategoryGpu"] = "🎮 Відеокарта",
                ["CategoryMotherboard"] = "🔧 Материнська плата",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Серійний номер",
                ["CategoryStorage"] = "💽 Накопичувачі",
                ["CategoryDiskSpace"] = "📦 Вільне місце",
                ["CategoryNetwork"] = "🌐 Мережа",
                ["CategoryMonitors"] = "📺 Монітори",
                ["GbUnit"] = "ГБ",
                ["BuildSuffixFormat"] = " (збірка {0})",
                ["CoresThreadsFormat"] = " ({0} ядер, {1} потоків)",
                ["ModuleOne"] = "модуль",
                ["ModuleFew"] = "модулі",
                ["ModuleMany"] = "модулів",
                ["MonitorOne"] = "монітор",
                ["MonitorFew"] = "монітори",
                ["MonitorMany"] = "моніторів",
                ["PrimaryMonitorSuffix"] = " (основний)",
                ["BiosDateFormat"] = " (від {0:dd.MM.yyyy})",
                ["DiskSpaceFormat"] = "{0}: вільно {1} ГБ з {2} ГБ"
            };
        }

        private static Dictionary<string, string> Be()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — характарыстыкі камп'ютара",
                ["Subtitle"] = "Характарыстыкі вашага камп'ютара",
                ["Loading"] = "Збор звестак пра сістэму...",
                ["BtnRefresh"] = "Абнавіць",
                ["BtnMore"] = "Яшчэ ▾",
                ["BtnCopy"] = "Капіяваць",
                ["MenuCopyImage"] = "Скапіяваць як малюнак",
                ["MenuSaveTxt"] = "Захаваць як .txt",
                ["MenuSaveJson"] = "Захаваць як .json",
                ["StatusCopied"] = "Скапіявана ў буфер абмену!",
                ["StatusImageCopied"] = "Малюнак скапіяваны ў буфер абмену!",
                ["StatusSaved"] = "Файл захаваны!",
                ["StatusErrorCopy"] = "Памылка капіявання: ",
                ["StatusErrorSave"] = "Памылка захавання: ",
                ["StatusErrorGeneric"] = "Памылка: ",
                ["VersionPrefix"] = "Версія ",
                ["UpdateAvailableFormat"] = "Даступная новая версія (зборка {0}) — націсніце, каб спампаваць",
                ["DialogTxtFilter"] = "Тэкставы файл (*.txt)|*.txt",
                ["DialogJsonFilter"] = "Файл JSON (*.json)|*.json",
                ["NotDetected"] = "Не ўдалося вызначыць",
                ["ExportHeader"] = "Характарыстыкі камп'ютара (сабрана ў NeuroSpec)",
                ["CategoryOs"] = "🖥️ Аперацыйная сістэма",
                ["CategoryInstallDate"] = "📅 Дата ўстаноўкі Windows",
                ["CategoryCpu"] = "⚙️ Працэсар",
                ["CategoryRam"] = "💾 Аператыўная памяць",
                ["CategoryGpu"] = "🎮 Відэакарта",
                ["CategoryMotherboard"] = "🔧 Мацярынская плата",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Серыйны нумар",
                ["CategoryStorage"] = "💽 Назапашвальнікі",
                ["CategoryDiskSpace"] = "📦 Свабоднае месца",
                ["CategoryNetwork"] = "🌐 Сетка",
                ["CategoryMonitors"] = "📺 Маніторы",
                ["GbUnit"] = "ГБ",
                ["BuildSuffixFormat"] = " (зборка {0})",
                ["CoresThreadsFormat"] = " ({0} ядраў, {1} патокаў)",
                ["ModuleOne"] = "модуль",
                ["ModuleFew"] = "модулі",
                ["ModuleMany"] = "модуляў",
                ["MonitorOne"] = "манітор",
                ["MonitorFew"] = "манітары",
                ["MonitorMany"] = "манітораў",
                ["PrimaryMonitorSuffix"] = " (асноўны)",
                ["BiosDateFormat"] = " (ад {0:dd.MM.yyyy})",
                ["DiskSpaceFormat"] = "{0}: вольна {1} ГБ з {2} ГБ"
            };
        }

        private static Dictionary<string, string> En()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — Computer Specifications",
                ["Subtitle"] = "Your computer's specifications",
                ["Loading"] = "Collecting system information...",
                ["BtnRefresh"] = "Refresh",
                ["BtnMore"] = "More ▾",
                ["BtnCopy"] = "Copy",
                ["MenuCopyImage"] = "Copy as image",
                ["MenuSaveTxt"] = "Save as .txt",
                ["MenuSaveJson"] = "Save as .json",
                ["StatusCopied"] = "Copied to clipboard!",
                ["StatusImageCopied"] = "Image copied to clipboard!",
                ["StatusSaved"] = "File saved!",
                ["StatusErrorCopy"] = "Copy error: ",
                ["StatusErrorSave"] = "Save error: ",
                ["StatusErrorGeneric"] = "Error: ",
                ["VersionPrefix"] = "Version ",
                ["UpdateAvailableFormat"] = "A new version is available (build {0}) — click to download",
                ["DialogTxtFilter"] = "Text file (*.txt)|*.txt",
                ["DialogJsonFilter"] = "JSON file (*.json)|*.json",
                ["NotDetected"] = "Could not be determined",
                ["ExportHeader"] = "Computer specifications (collected with NeuroSpec)",
                ["CategoryOs"] = "🖥️ Operating system",
                ["CategoryInstallDate"] = "📅 Windows install date",
                ["CategoryCpu"] = "⚙️ Processor",
                ["CategoryRam"] = "💾 RAM",
                ["CategoryGpu"] = "🎮 Graphics card",
                ["CategoryMotherboard"] = "🔧 Motherboard",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Serial number",
                ["CategoryStorage"] = "💽 Storage drives",
                ["CategoryDiskSpace"] = "📦 Free disk space",
                ["CategoryNetwork"] = "🌐 Network",
                ["CategoryMonitors"] = "📺 Monitors",
                ["GbUnit"] = "GB",
                ["BuildSuffixFormat"] = " (build {0})",
                ["CoresThreadsFormat"] = " ({0} cores, {1} threads)",
                ["ModuleOne"] = "module",
                ["ModuleFew"] = "modules",
                ["ModuleMany"] = "modules",
                ["MonitorOne"] = "monitor",
                ["MonitorFew"] = "monitors",
                ["MonitorMany"] = "monitors",
                ["PrimaryMonitorSuffix"] = " (primary)",
                ["BiosDateFormat"] = " (dated {0:dd.MM.yyyy})",
                ["DiskSpaceFormat"] = "{0}: {1} GB free of {2} GB"
            };
        }

        private static Dictionary<string, string> De()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — Computerdaten",
                ["Subtitle"] = "Die Spezifikationen Ihres Computers",
                ["Loading"] = "Systemdaten werden erfasst...",
                ["BtnRefresh"] = "Aktualisieren",
                ["BtnMore"] = "Mehr ▾",
                ["BtnCopy"] = "Kopieren",
                ["MenuCopyImage"] = "Als Bild kopieren",
                ["MenuSaveTxt"] = "Als .txt speichern",
                ["MenuSaveJson"] = "Als .json speichern",
                ["StatusCopied"] = "In die Zwischenablage kopiert!",
                ["StatusImageCopied"] = "Bild in die Zwischenablage kopiert!",
                ["StatusSaved"] = "Datei gespeichert!",
                ["StatusErrorCopy"] = "Fehler beim Kopieren: ",
                ["StatusErrorSave"] = "Fehler beim Speichern: ",
                ["StatusErrorGeneric"] = "Fehler: ",
                ["VersionPrefix"] = "Version ",
                ["UpdateAvailableFormat"] = "Eine neue Version ist verfügbar (Build {0}) — zum Herunterladen klicken",
                ["DialogTxtFilter"] = "Textdatei (*.txt)|*.txt",
                ["DialogJsonFilter"] = "JSON-Datei (*.json)|*.json",
                ["NotDetected"] = "Konnte nicht ermittelt werden",
                ["ExportHeader"] = "Computerdaten (erfasst mit NeuroSpec)",
                ["CategoryOs"] = "🖥️ Betriebssystem",
                ["CategoryInstallDate"] = "📅 Installationsdatum von Windows",
                ["CategoryCpu"] = "⚙️ Prozessor",
                ["CategoryRam"] = "💾 Arbeitsspeicher",
                ["CategoryGpu"] = "🎮 Grafikkarte",
                ["CategoryMotherboard"] = "🔧 Hauptplatine",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Seriennummer",
                ["CategoryStorage"] = "💽 Speicherlaufwerke",
                ["CategoryDiskSpace"] = "📦 Freier Speicherplatz",
                ["CategoryNetwork"] = "🌐 Netzwerk",
                ["CategoryMonitors"] = "📺 Monitore",
                ["GbUnit"] = "GB",
                ["BuildSuffixFormat"] = " (Build {0})",
                ["CoresThreadsFormat"] = " ({0} Kerne, {1} Threads)",
                ["ModuleOne"] = "Modul",
                ["ModuleFew"] = "Module",
                ["ModuleMany"] = "Module",
                ["MonitorOne"] = "Monitor",
                ["MonitorFew"] = "Monitore",
                ["MonitorMany"] = "Monitore",
                ["PrimaryMonitorSuffix"] = " (primär)",
                ["BiosDateFormat"] = " (vom {0:dd.MM.yyyy})",
                ["DiskSpaceFormat"] = "{0}: {1} GB frei von {2} GB"
            };
        }

        private static Dictionary<string, string> Tr()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — Bilgisayar Özellikleri",
                ["Subtitle"] = "Bilgisayarınızın özellikleri",
                ["Loading"] = "Sistem bilgileri toplanıyor...",
                ["BtnRefresh"] = "Yenile",
                ["BtnMore"] = "Diğer ▾",
                ["BtnCopy"] = "Kopyala",
                ["MenuCopyImage"] = "Görüntü olarak kopyala",
                ["MenuSaveTxt"] = ".txt olarak kaydet",
                ["MenuSaveJson"] = ".json olarak kaydet",
                ["StatusCopied"] = "Panoya kopyalandı!",
                ["StatusImageCopied"] = "Görüntü panoya kopyalandı!",
                ["StatusSaved"] = "Dosya kaydedildi!",
                ["StatusErrorCopy"] = "Kopyalama hatası: ",
                ["StatusErrorSave"] = "Kaydetme hatası: ",
                ["StatusErrorGeneric"] = "Hata: ",
                ["VersionPrefix"] = "Sürüm ",
                ["UpdateAvailableFormat"] = "Yeni bir sürüm mevcut (derleme {0}) — indirmek için tıklayın",
                ["DialogTxtFilter"] = "Metin dosyası (*.txt)|*.txt",
                ["DialogJsonFilter"] = "JSON dosyası (*.json)|*.json",
                ["NotDetected"] = "Belirlenemedi",
                ["ExportHeader"] = "Bilgisayar özellikleri (NeuroSpec ile toplandı)",
                ["CategoryOs"] = "🖥️ İşletim sistemi",
                ["CategoryInstallDate"] = "📅 Windows kurulum tarihi",
                ["CategoryCpu"] = "⚙️ İşlemci",
                ["CategoryRam"] = "💾 Bellek (RAM)",
                ["CategoryGpu"] = "🎮 Ekran kartı",
                ["CategoryMotherboard"] = "🔧 Anakart",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Seri numarası",
                ["CategoryStorage"] = "💽 Depolama sürücüleri",
                ["CategoryDiskSpace"] = "📦 Boş disk alanı",
                ["CategoryNetwork"] = "🌐 Ağ",
                ["CategoryMonitors"] = "📺 Monitörler",
                ["GbUnit"] = "GB",
                ["BuildSuffixFormat"] = " (derleme {0})",
                ["CoresThreadsFormat"] = " ({0} çekirdek, {1} iş parçacığı)",
                ["ModuleOne"] = "modül",
                ["ModuleFew"] = "modül",
                ["ModuleMany"] = "modül",
                ["MonitorOne"] = "monitör",
                ["MonitorFew"] = "monitör",
                ["MonitorMany"] = "monitör",
                ["PrimaryMonitorSuffix"] = " (birincil)",
                ["BiosDateFormat"] = " ({0:dd.MM.yyyy} tarihli)",
                ["DiskSpaceFormat"] = "{0}: {2} GB üzerinden {1} GB boş"
            };
        }

        private static Dictionary<string, string> Ar()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — مواصفات الحاسوب",
                ["Subtitle"] = "مواصفات جهاز الحاسوب الخاص بك",
                ["Loading"] = "جارٍ جمع معلومات النظام...",
                ["BtnRefresh"] = "تحديث",
                ["BtnMore"] = "المزيد ▾",
                ["BtnCopy"] = "نسخ",
                ["MenuCopyImage"] = "نسخ كصورة",
                ["MenuSaveTxt"] = "حفظ كملف txt.",
                ["MenuSaveJson"] = "حفظ كملف json.",
                ["StatusCopied"] = "تم النسخ إلى الحافظة!",
                ["StatusImageCopied"] = "تم نسخ الصورة إلى الحافظة!",
                ["StatusSaved"] = "تم حفظ الملف!",
                ["StatusErrorCopy"] = "خطأ في النسخ: ",
                ["StatusErrorSave"] = "خطأ في الحفظ: ",
                ["StatusErrorGeneric"] = "خطأ: ",
                ["VersionPrefix"] = "الإصدار ",
                ["UpdateAvailableFormat"] = "يتوفر إصدار جديد (الإصدار {0}) — انقر للتنزيل",
                ["DialogTxtFilter"] = "ملف نصي (*.txt)|*.txt",
                ["DialogJsonFilter"] = "ملف JSON (*.json)|*.json",
                ["NotDetected"] = "تعذّر التحديد",
                ["ExportHeader"] = "مواصفات الحاسوب (تم جمعها بواسطة NeuroSpec)",
                ["CategoryOs"] = "🖥️ نظام التشغيل",
                ["CategoryInstallDate"] = "📅 تاريخ تثبيت Windows",
                ["CategoryCpu"] = "⚙️ المعالج",
                ["CategoryRam"] = "💾 الذاكرة العشوائية",
                ["CategoryGpu"] = "🎮 بطاقة الرسومات",
                ["CategoryMotherboard"] = "🔧 اللوحة الأم",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ الرقم التسلسلي",
                ["CategoryStorage"] = "💽 محركات التخزين",
                ["CategoryDiskSpace"] = "📦 المساحة الحرة",
                ["CategoryNetwork"] = "🌐 الشبكة",
                ["CategoryMonitors"] = "📺 الشاشات",
                ["GbUnit"] = "غيغابايت",
                ["BuildSuffixFormat"] = " (الإصدار {0})",
                ["CoresThreadsFormat"] = " ({0} أنوية، {1} خيوط معالجة)",
                ["ModuleOne"] = "وحدة ذاكرة",
                ["ModuleFew"] = "وحدات ذاكرة",
                ["ModuleMany"] = "وحدات ذاكرة",
                ["MonitorOne"] = "شاشة",
                ["MonitorFew"] = "شاشات",
                ["MonitorMany"] = "شاشات",
                ["PrimaryMonitorSuffix"] = " (رئيسية)",
                ["BiosDateFormat"] = " (بتاريخ {0:dd.MM.yyyy})",
                ["DiskSpaceFormat"] = "{0}: {1} غيغابايت متاحة من أصل {2} غيغابايت"
            };
        }

        private static Dictionary<string, string> Kk()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — компьютер сипаттамалары",
                ["Subtitle"] = "Сіздің компьютеріңіздің сипаттамалары",
                ["Loading"] = "Жүйе туралы деректер жиналуда...",
                ["BtnRefresh"] = "Жаңарту",
                ["BtnMore"] = "Тағы ▾",
                ["BtnCopy"] = "Көшіру",
                ["MenuCopyImage"] = "Сурет ретінде көшіру",
                ["MenuSaveTxt"] = ".txt ретінде сақтау",
                ["MenuSaveJson"] = ".json ретінде сақтау",
                ["StatusCopied"] = "Алмасу буферіне көшірілді!",
                ["StatusImageCopied"] = "Сурет алмасу буферіне көшірілді!",
                ["StatusSaved"] = "Файл сақталды!",
                ["StatusErrorCopy"] = "Көшіру қатесі: ",
                ["StatusErrorSave"] = "Сақтау қатесі: ",
                ["StatusErrorGeneric"] = "Қате: ",
                ["VersionPrefix"] = "Нұсқа ",
                ["UpdateAvailableFormat"] = "Жаңа нұсқа қолжетімді (нұсқа {0}) — жүктеу үшін басыңыз",
                ["DialogTxtFilter"] = "Мәтіндік файл (*.txt)|*.txt",
                ["DialogJsonFilter"] = "JSON файлы (*.json)|*.json",
                ["NotDetected"] = "Анықтау мүмкін болмады",
                ["ExportHeader"] = "Компьютер сипаттамалары (NeuroSpec арқылы жиналды)",
                ["CategoryOs"] = "🖥️ Операциялық жүйе",
                ["CategoryInstallDate"] = "📅 Windows орнату күні",
                ["CategoryCpu"] = "⚙️ Процессор",
                ["CategoryRam"] = "💾 Жедел жад",
                ["CategoryGpu"] = "🎮 Видеокарта",
                ["CategoryMotherboard"] = "🔧 Аналық плата",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Сериялық нөмір",
                ["CategoryStorage"] = "💽 Жинақтауыштар",
                ["CategoryDiskSpace"] = "📦 Бос орын",
                ["CategoryNetwork"] = "🌐 Желі",
                ["CategoryMonitors"] = "📺 Мониторлар",
                ["GbUnit"] = "ГБ",
                ["BuildSuffixFormat"] = " (нұсқа {0})",
                ["CoresThreadsFormat"] = " ({0} өзек, {1} ағын)",
                ["ModuleOne"] = "модуль",
                ["ModuleFew"] = "модуль",
                ["ModuleMany"] = "модуль",
                ["MonitorOne"] = "монитор",
                ["MonitorFew"] = "монитор",
                ["MonitorMany"] = "монитор",
                ["PrimaryMonitorSuffix"] = " (негізгі)",
                ["BiosDateFormat"] = " ({0:dd.MM.yyyy} күнгі)",
                ["DiskSpaceFormat"] = "{0}: {2} ГБ ішінен {1} ГБ бос"
            };
        }

        private static Dictionary<string, string> Hy()
        {
            return new Dictionary<string, string>
            {
                ["WindowTitle"] = "NeuroSpec — համակարգչի բնութագրեր",
                ["Subtitle"] = "Ձեր համակարգչի բնութագրերը",
                ["Loading"] = "Համակարգի տվյալների հավաքում...",
                ["BtnRefresh"] = "Թարմացնել",
                ["BtnMore"] = "Ավելին ▾",
                ["BtnCopy"] = "Պատճենել",
                ["MenuCopyImage"] = "Պատճենել որպես նկար",
                ["MenuSaveTxt"] = "Պահպանել որպես .txt",
                ["MenuSaveJson"] = "Պահպանել որպես .json",
                ["StatusCopied"] = "Պատճենվեց փոխանակման բուֆերում!",
                ["StatusImageCopied"] = "Նկարը պատճենվեց փոխանակման բուֆերում!",
                ["StatusSaved"] = "Ֆայլը պահպանվեց!",
                ["StatusErrorCopy"] = "Պատճենման սխալ՝ ",
                ["StatusErrorSave"] = "Պահպանման սխալ՝ ",
                ["StatusErrorGeneric"] = "Սխալ՝ ",
                ["VersionPrefix"] = "Տարբերակ ",
                ["UpdateAvailableFormat"] = "Հասանելի է նոր տարբերակ ({0}) — սեղմեք ներբեռնելու համար",
                ["DialogTxtFilter"] = "Տեքստային ֆայլ (*.txt)|*.txt",
                ["DialogJsonFilter"] = "JSON ֆայլ (*.json)|*.json",
                ["NotDetected"] = "Չհաջողվեց որոշել",
                ["ExportHeader"] = "Համակարգչի բնութագրեր (հավաքվել է NeuroSpec-ով)",
                ["CategoryOs"] = "🖥️ Օպերացիոն համակարգ",
                ["CategoryInstallDate"] = "📅 Windows-ի տեղադրման ամսաթիվ",
                ["CategoryCpu"] = "⚙️ Պրոցեսոր",
                ["CategoryRam"] = "💾 Օպերատիվ հիշողություն",
                ["CategoryGpu"] = "🎮 Վիդեոքարտ",
                ["CategoryMotherboard"] = "🔧 Մայրական սալիկ",
                ["CategoryBios"] = "🔩 BIOS/UEFI",
                ["CategorySerial"] = "🏷️ Սերիական համար",
                ["CategoryStorage"] = "💽 Կրիչներ",
                ["CategoryDiskSpace"] = "📦 Ազատ տարածք",
                ["CategoryNetwork"] = "🌐 Ցանց",
                ["CategoryMonitors"] = "📺 Մոնիտորներ",
                ["GbUnit"] = "ԳԲ",
                ["BuildSuffixFormat"] = " (տարբերակ {0})",
                ["CoresThreadsFormat"] = " ({0} միջուկ, {1} հոսք)",
                ["ModuleOne"] = "մոդուլ",
                ["ModuleFew"] = "մոդուլ",
                ["ModuleMany"] = "մոդուլ",
                ["MonitorOne"] = "մոնիտոր",
                ["MonitorFew"] = "մոնիտոր",
                ["MonitorMany"] = "մոնիտոր",
                ["PrimaryMonitorSuffix"] = " (հիմնական)",
                ["BiosDateFormat"] = " ({0:dd.MM.yyyy} ամսաթվով)",
                ["DiskSpaceFormat"] = "{0}՝ ազատ է {1} ԳԲ ({2} ԳԲ-ից)"
            };
        }
    }
}
