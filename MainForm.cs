using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuroSpec
{
    /// <summary>
    /// Главное окно приложения NeuroSpec.
    /// Отображает характеристики компьютера в тёмном графическом интерфейсе,
    /// позволяет скопировать/сохранить их, проверяет наличие новой версии
    /// и поддерживает переключение языка интерфейса.
    /// </summary>
    public class MainForm : Form
    {
        private const string RepoOwnerAndName = "WZCasper/NeuroSpec";

        // ---------- Цветовая схема (тёмная тема) ----------
        private static readonly Color ColorBackground = Color.FromArgb(24, 24, 27);
        private static readonly Color ColorPanel = Color.FromArgb(32, 32, 36);
        private static readonly Color ColorPanelBorder = Color.FromArgb(52, 52, 58);
        private static readonly Color ColorText = Color.FromArgb(230, 230, 230);
        private static readonly Color ColorTextMuted = Color.FromArgb(150, 150, 156);
        private static readonly Color ColorAccent = Color.FromArgb(88, 166, 255);
        private static readonly Color ColorAccentHover = Color.FromArgb(60, 140, 240);
        private static readonly Color ColorSuccess = Color.FromArgb(63, 185, 80);
        private static readonly Color ColorError = Color.FromArgb(240, 90, 90);
        private static readonly Color ColorDonate = Color.FromArgb(219, 68, 107);
        private static readonly Color ColorDonateHover = Color.FromArgb(235, 90, 125);

        // ---------- Тёмное оформление системной рамки окна (DWM) ----------
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaBorderColor = 34;

        private readonly TableLayoutPanel _rootLayout;
        private readonly Panel _contentPanel;
        private readonly TableLayoutPanel _specsTable;
        private readonly Label _subtitleLabel;
        private readonly Label _versionLabel;
        private readonly Label _loadingLabel;
        private readonly Label _statusLabel;
        private readonly Label _creditLabel;
        private readonly LinkLabel _updateLinkLabel;
        private readonly ComboBox _languageSelector;
        private readonly Button _copyButton;
        private readonly Button _refreshButton;
        private readonly Button _moreButton;
        private readonly Button _donateButton;
        private readonly ContextMenuStrip _moreMenu;
        private readonly ToolStripMenuItem _menuCopyImageItem;
        private readonly ToolStripMenuItem _menuSaveTxtItem;
        private readonly ToolStripMenuItem _menuSaveJsonItem;
        private readonly System.Windows.Forms.Timer _statusTimer;

        private AppLanguage _currentLanguage;
        private SystemSpecs _currentSpecs;
        private int? _latestUpdateBuild;

        public MainForm()
        {
            var settings = AppSettings.Load();
            _currentLanguage = Enum.TryParse(settings.Language, out AppLanguage parsedLang) ? parsedLang : AppLanguage.Ru;

            // ---------- Параметры окна ----------
            BackColor = ColorBackground;
            ForeColor = ColorText;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(680, 780);
            MinimumSize = new Size(600, 560);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;

            try
            {
                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch
            {
                Icon = SystemIcons.Application;
            }

            _statusTimer = new System.Windows.Forms.Timer { Interval = 2500 };
            _statusTimer.Tick += StatusTimer_Tick;

            // ---------- Корневая разметка: заголовок / контент / кнопки ----------
            _rootLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                BackColor = ColorBackground,
                Padding = new Padding(20)
            };
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100f));
            _rootLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(_rootLayout);

            // ---------- Заголовок ----------
            var headerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                AutoSize = true,
                BackColor = ColorBackground,
                Margin = new Padding(0, 0, 0, 15)
            };
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            headerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var titleLabel = new Label
            {
                Text = "NeuroSpec",
                Font = new Font("Segoe UI", 20f, FontStyle.Bold),
                ForeColor = ColorAccent,
                AutoSize = true,
                BackColor = Color.Transparent
            };

            _subtitleLabel = new Label
            {
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = ColorTextMuted,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 0)
            };

            var infoRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                AutoSize = true,
                BackColor = ColorBackground,
                Margin = new Padding(0, 8, 0, 0)
            };
            infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            infoRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _versionLabel = new Label
            {
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = ColorTextMuted,
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                BackColor = Color.Transparent
            };
            infoRow.Controls.Add(_versionLabel, 0, 0);

            _languageSelector = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                DrawMode = DrawMode.OwnerDrawFixed,
                ItemHeight = 22,
                Width = 170,
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 9f, FontStyle.Regular),
                Anchor = AnchorStyles.Right
            };
            AppLanguage[] allLanguages = (AppLanguage[])Enum.GetValues(typeof(AppLanguage));
            foreach (AppLanguage lang in allLanguages)
            {
                _languageSelector.Items.Add(Localization.LanguageNames[lang]);
            }
            _languageSelector.SelectedIndex = Array.IndexOf(allLanguages, _currentLanguage);
            _languageSelector.DrawItem += LanguageSelector_DrawItem;
            _languageSelector.SelectedIndexChanged += LanguageSelector_SelectedIndexChanged;
            infoRow.Controls.Add(_languageSelector, 1, 0);

            _updateLinkLabel = new LinkLabel
            {
                Text = "",
                AutoSize = true,
                Visible = false,
                LinkColor = ColorAccent,
                ActiveLinkColor = ColorAccentHover,
                VisitedLinkColor = ColorAccent,
                Font = new Font("Segoe UI", 9f, FontStyle.Underline),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 8, 0, 0)
            };
            _updateLinkLabel.LinkClicked += UpdateLinkLabel_LinkClicked;

            _creditLabel = new Label
            {
                Text = "by 亗 Casper",
                Font = new Font("Segoe UI", 8f, FontStyle.Italic),
                ForeColor = ColorTextMuted,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 6, 0, 0)
            };

            headerPanel.Controls.Add(titleLabel, 0, 0);
            headerPanel.Controls.Add(_subtitleLabel, 0, 1);
            headerPanel.Controls.Add(infoRow, 0, 2);
            headerPanel.Controls.Add(_updateLinkLabel, 0, 3);
            headerPanel.Controls.Add(_creditLabel, 0, 4);
            _rootLayout.Controls.Add(headerPanel, 0, 0);

            // ---------- Панель с характеристиками (прокручиваемая карточка) ----------
            _contentPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = ColorPanel,
                AutoScroll = true,
                Padding = new Padding(20)
            };
            _rootLayout.Controls.Add(_contentPanel, 0, 1);

            _loadingLabel = new Label
            {
                ForeColor = ColorTextMuted,
                Font = new Font("Segoe UI", 10f, FontStyle.Italic),
                AutoSize = true,
                Location = new Point(0, 0),
                Visible = true
            };
            _contentPanel.Controls.Add(_loadingLabel);

            _specsTable = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Top,
                BackColor = ColorPanel,
                Visible = false
            };
            _specsTable.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            _contentPanel.Controls.Add(_specsTable);

            // ---------- Нижняя панель с кнопками ----------
            var footerPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                RowCount = 1,
                AutoSize = true,
                BackColor = ColorBackground,
                Margin = new Padding(0, 15, 0, 0)
            };
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _refreshButton = CreateButton(isPrimary: false, minWidth: 100);
            _refreshButton.Click += RefreshButton_Click;
            footerPanel.Controls.Add(_refreshButton, 0, 0);

            _statusLabel = new Label
            {
                Text = "",
                ForeColor = ColorSuccess,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                AutoSize = true,
                Anchor = AnchorStyles.Left,
                TextAlign = ContentAlignment.MiddleLeft,
                Margin = new Padding(12, 10, 0, 0)
            };
            footerPanel.Controls.Add(_statusLabel, 1, 0);

            _menuCopyImageItem = new ToolStripMenuItem();
            _menuCopyImageItem.Click += (s, e) => CopySpecsAsImage();

            _menuSaveTxtItem = new ToolStripMenuItem();
            _menuSaveTxtItem.Click += (s, e) => SaveSpecsToFile(asJson: false);

            _menuSaveJsonItem = new ToolStripMenuItem();
            _menuSaveJsonItem.Click += (s, e) => SaveSpecsToFile(asJson: true);

            _moreMenu = new ContextMenuStrip
            {
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Renderer = new DarkMenuRenderer(),
                ShowImageMargin = false
            };
            _moreMenu.Items.Add(_menuCopyImageItem);
            _moreMenu.Items.Add(new ToolStripSeparator());
            _moreMenu.Items.Add(_menuSaveTxtItem);
            _moreMenu.Items.Add(_menuSaveJsonItem);

            _moreButton = CreateButton(isPrimary: false, minWidth: 80);
            _moreButton.Click += (s, e) => _moreMenu.Show(_moreButton, new Point(0, _moreButton.Height));
            footerPanel.Controls.Add(_moreButton, 2, 0);

            _copyButton = CreateButton(isPrimary: true, minWidth: 120);
            _copyButton.Click += CopyButton_Click;
            footerPanel.Controls.Add(_copyButton, 3, 0);

            _donateButton = new Button
            {
                Text = "\u2764 Donate",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(120, 38),
                Padding = new Padding(16, 0, 16, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand,
                BackColor = ColorDonate,
                ForeColor = Color.White
            };
            _donateButton.FlatAppearance.BorderSize = 0;
            _donateButton.MouseEnter += (s, e) => _donateButton.BackColor = ColorDonateHover;
            _donateButton.MouseLeave += (s, e) => _donateButton.BackColor = ColorDonate;
            _donateButton.Click += DonateButton_Click;
            footerPanel.Controls.Add(_donateButton, 4, 0);

            _rootLayout.Controls.Add(footerPanel, 0, 2);

            ApplyStaticTexts();

            Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            try
            {
                SetWindowTheme(_contentPanel.Handle, "DarkMode_Explorer", null);
            }
            catch
            {
                // Тёмные полосы прокрутки поддерживаются не на всех версиях Windows.
            }

            _ = CheckForUpdatesAsync();
            await LoadSpecsAsync();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int enableDark = 1;
                DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref enableDark, sizeof(int));

                int borderColor = ToColorRef(ColorAccent);
                DwmSetWindowAttribute(Handle, DwmwaBorderColor, ref borderColor, sizeof(int));
            }
            catch
            {
                // На старых версиях Windows тёмная рамка окна недоступна -
                // приложение просто останется со стандартной рамкой.
            }
        }

        private static int ToColorRef(Color color)
        {
            // DWM ожидает цвет в формате COLORREF (0x00BBGGRR), а не обычный RGB.
            return color.R | (color.G << 8) | (color.B << 16);
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            await LoadSpecsAsync();
        }

        private async void LanguageSelector_SelectedIndexChanged(object sender, EventArgs e)
        {
            AppLanguage[] languages = (AppLanguage[])Enum.GetValues(typeof(AppLanguage));
            int idx = _languageSelector.SelectedIndex;
            if (idx < 0 || idx >= languages.Length)
            {
                return;
            }

            AppLanguage selected = languages[idx];
            if (selected == _currentLanguage)
            {
                return;
            }

            _currentLanguage = selected;
            var settings = new AppSettings { Language = selected.ToString() };
            settings.Save();

            ApplyStaticTexts();

            // Значения (единицы измерения, склонения) тоже завязаны на язык,
            // поэтому при смене языка данные собираются заново.
            await LoadSpecsAsync();
        }

        private void LanguageSelector_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0)
            {
                return;
            }

            var combo = (ComboBox)sender;
            bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
            Color back = selected ? ColorAccent : ColorPanel;
            Color fore = selected ? Color.White : ColorText;

            using (var backBrush = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(backBrush, e.Bounds);
            }

            TextRenderer.DrawText(
                e.Graphics,
                combo.Items[e.Index].ToString(),
                combo.Font,
                e.Bounds,
                fore,
                back,
                TextFormatFlags.VerticalCenter | TextFormatFlags.Left | TextFormatFlags.NoPrefix);

            e.DrawFocusRectangle();
        }

        // ==================== Локализация статичных подписей ====================

        private void ApplyStaticTexts()
        {
            bool isRtl = _currentLanguage == AppLanguage.Ar;
            RightToLeft = isRtl ? RightToLeft.Yes : RightToLeft.No;

            Text = Localization.Get(_currentLanguage, "WindowTitle");
            _subtitleLabel.Text = Localization.Get(_currentLanguage, "Subtitle");
            _versionLabel.Text = Localization.Get(_currentLanguage, "VersionPrefix") + GetDisplayVersion();
            _loadingLabel.Text = Localization.Get(_currentLanguage, "Loading");
            _refreshButton.Text = Localization.Get(_currentLanguage, "BtnRefresh");
            _moreButton.Text = Localization.Get(_currentLanguage, "BtnMore");
            _copyButton.Text = Localization.Get(_currentLanguage, "BtnCopy");
            _menuCopyImageItem.Text = Localization.Get(_currentLanguage, "MenuCopyImage");
            _menuSaveTxtItem.Text = Localization.Get(_currentLanguage, "MenuSaveTxt");
            _menuSaveJsonItem.Text = Localization.Get(_currentLanguage, "MenuSaveJson");

            if (_latestUpdateBuild.HasValue)
            {
                _updateLinkLabel.Text = Localization.GetFormat(
                    _currentLanguage, "UpdateAvailableFormat", _latestUpdateBuild.Value);
            }
        }

        // ==================== Кнопки ====================

        private Button CreateButton(bool isPrimary, int minWidth)
        {
            var button = new Button
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(minWidth, 38),
                Padding = new Padding(16, 0, 16, 0),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = 0;

            if (isPrimary)
            {
                button.BackColor = ColorAccent;
                button.ForeColor = Color.White;
                button.MouseEnter += (s, e) => button.BackColor = ColorAccentHover;
                button.MouseLeave += (s, e) => button.BackColor = ColorAccent;
            }
            else
            {
                button.BackColor = ColorPanel;
                button.ForeColor = ColorText;
                button.FlatAppearance.BorderSize = 1;
                button.FlatAppearance.BorderColor = ColorPanelBorder;
                button.MouseEnter += (s, e) => button.BackColor = ColorPanelBorder;
                button.MouseLeave += (s, e) => button.BackColor = ColorPanel;
            }

            return button;
        }

        // ==================== Сбор и отображение данных ====================

        private async Task LoadSpecsAsync()
        {
            _loadingLabel.Visible = true;
            _specsTable.Visible = false;
            _copyButton.Enabled = false;
            _refreshButton.Enabled = false;
            _moreButton.Enabled = false;
            _statusLabel.Text = "";

            try
            {
                _currentSpecs = await SystemInfoCollector.CollectAsync(_currentLanguage);
                PopulateSpecsTable(_currentSpecs);
            }
            finally
            {
                _loadingLabel.Visible = false;
                _specsTable.Visible = true;
                _copyButton.Enabled = true;
                _refreshButton.Enabled = true;
                _moreButton.Enabled = true;
            }
        }

        private void PopulateSpecsTable(SystemSpecs specs)
        {
            _specsTable.Controls.Clear();
            _specsTable.RowStyles.Clear();
            _specsTable.RowCount = 0;

            AddSpecRow(Localization.Get(_currentLanguage, "CategoryOs"), specs.OperatingSystem);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryInstallDate"), specs.WindowsInstallDate);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryCpu"), specs.Processor);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryRam"), specs.RandomAccessMemory);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryGpu"), specs.VideoController);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryMotherboard"), specs.Motherboard);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryBios"), specs.Bios);
            AddSpecRow(Localization.Get(_currentLanguage, "CategorySerial"), specs.SerialNumber);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryStorage"), specs.Storage);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryDiskSpace"), specs.DiskSpace);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryNetwork"), specs.NetworkAdapters);
            AddSpecRow(Localization.Get(_currentLanguage, "CategoryMonitors"), specs.Monitors);
        }

        private void AddSpecRow(string category, string value)
        {
            string displayValue = string.IsNullOrWhiteSpace(value)
                ? Localization.Get(_currentLanguage, "NotDetected")
                : value;

            int rowIndex = _specsTable.RowCount;
            _specsTable.RowCount++;
            _specsTable.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var rowPanel = new TableLayoutPanel
            {
                ColumnCount = 1,
                RowCount = 2,
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = ColorPanel,
                Margin = new Padding(0, 0, 0, 18)
            };

            var categoryLabel = new Label
            {
                Text = category.ToUpperInvariant(),
                ForeColor = ColorAccent,
                Font = new Font("Segoe UI", 8.5f, FontStyle.Bold),
                AutoSize = true,
                BackColor = Color.Transparent
            };

            var valueLabel = new Label
            {
                Text = displayValue,
                ForeColor = ColorText,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                AutoSize = true,
                MaximumSize = new Size(560, 0),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 3, 0, 0)
            };

            rowPanel.Controls.Add(categoryLabel, 0, 0);
            rowPanel.Controls.Add(valueLabel, 0, 1);

            _specsTable.Controls.Add(rowPanel, 0, rowIndex);
        }

        // ==================== Копирование и экспорт ====================

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (_currentSpecs == null)
            {
                return;
            }

            try
            {
                Clipboard.SetText(SpecsFormatter.ToClipboardText(_currentSpecs, _currentLanguage));
                ShowStatus(Localization.Get(_currentLanguage, "StatusCopied"), ColorSuccess);
            }
            catch (Exception ex)
            {
                ShowStatus(Localization.Get(_currentLanguage, "StatusErrorCopy") + ex.Message, ColorError);
            }
        }

        private void CopySpecsAsImage()
        {
            if (_currentSpecs == null || _specsTable.Width <= 0 || _specsTable.Height <= 0)
            {
                return;
            }

            try
            {
                using var bitmap = new Bitmap(_specsTable.Width, _specsTable.Height);
                _specsTable.DrawToBitmap(bitmap, new Rectangle(Point.Empty, _specsTable.Size));
                Clipboard.SetImage(bitmap);
                ShowStatus(Localization.Get(_currentLanguage, "StatusImageCopied"), ColorSuccess);
            }
            catch (Exception ex)
            {
                ShowStatus(Localization.Get(_currentLanguage, "StatusErrorGeneric") + ex.Message, ColorError);
            }
        }

        private void SaveSpecsToFile(bool asJson)
        {
            if (_currentSpecs == null)
            {
                return;
            }

            using var dialog = new SaveFileDialog
            {
                Filter = asJson
                    ? Localization.Get(_currentLanguage, "DialogJsonFilter")
                    : Localization.Get(_currentLanguage, "DialogTxtFilter"),
                FileName = asJson ? "NeuroSpec.json" : "NeuroSpec.txt"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    string content = asJson
                        ? _currentSpecs.ToJson()
                        : SpecsFormatter.ToClipboardText(_currentSpecs, _currentLanguage);
                    File.WriteAllText(dialog.FileName, content, System.Text.Encoding.UTF8);
                    ShowStatus(Localization.Get(_currentLanguage, "StatusSaved"), ColorSuccess);
                }
                catch (Exception ex)
                {
                    ShowStatus(Localization.Get(_currentLanguage, "StatusErrorSave") + ex.Message, ColorError);
                }
            }
        }

        // ==================== Статус-сообщения ====================

        private void ShowStatus(string message, Color color)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = color;

            _statusTimer.Stop();
            _statusTimer.Start();
        }

        private void StatusTimer_Tick(object sender, EventArgs e)
        {
            _statusLabel.Text = "";
            _statusTimer.Stop();
        }

        // ==================== Версия и проверка обновлений ====================

        private static string GetDisplayVersion()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            if (version == null)
            {
                return "?";
            }
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        private static int GetCurrentBuildNumber()
        {
            var version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return version?.Build ?? 0;
        }

        private async Task CheckForUpdatesAsync()
        {
            try
            {
                using var http = new HttpClient
                {
                    Timeout = TimeSpan.FromSeconds(4)
                };
                http.DefaultRequestHeaders.UserAgent.ParseAdd("NeuroSpec-App");

                string json = await http.GetStringAsync(
                    $"https://api.github.com/repos/{RepoOwnerAndName}/releases/latest");

                using var doc = JsonDocument.Parse(json);
                string tagName = doc.RootElement.TryGetProperty("tag_name", out var tagProp)
                    ? tagProp.GetString()
                    : null;
                string htmlUrl = doc.RootElement.TryGetProperty("html_url", out var urlProp)
                    ? urlProp.GetString()
                    : null;

                if (string.IsNullOrWhiteSpace(tagName) || string.IsNullOrWhiteSpace(htmlUrl))
                {
                    return;
                }

                string numberPart = tagName.Replace("build-", "").Trim();
                if (!int.TryParse(numberPart, out int latestBuild))
                {
                    return;
                }

                int currentBuild = GetCurrentBuildNumber();

                if (latestBuild > currentBuild && !IsDisposed)
                {
                    _latestUpdateBuild = latestBuild;
                    BeginInvoke(new Action(() =>
                    {
                        _updateLinkLabel.Text = Localization.GetFormat(
                            _currentLanguage, "UpdateAvailableFormat", latestBuild);
                        _updateLinkLabel.Tag = htmlUrl;
                        _updateLinkLabel.Visible = true;
                    }));
                }
            }
            catch
            {
                // Нет интернета или GitHub недоступен - молча пропускаем проверку,
                // это не должно мешать основной работе приложения.
            }
        }

        private void UpdateLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (_updateLinkLabel.Tag is string url)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                catch
                {
                }
            }
        }

        private void DonateButton_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo("https://dalink.to/wz_casper") { UseShellExecute = true });
            }
            catch
            {
                // Если браузер не запустился - тихо игнорируем, чтобы не мешать работе приложения.
            }
        }

        // ==================== Тёмное оформление выпадающего меню ====================

        private class DarkColorTable : ProfessionalColorTable
        {
            public override Color MenuItemSelected => ColorAccent;
            public override Color MenuItemSelectedGradientBegin => ColorAccent;
            public override Color MenuItemSelectedGradientEnd => ColorAccent;
            public override Color MenuItemBorder => ColorAccent;
            public override Color MenuBorder => ColorPanelBorder;
            public override Color ToolStripDropDownBackground => ColorPanel;
            public override Color ImageMarginGradientBegin => ColorPanel;
            public override Color ImageMarginGradientMiddle => ColorPanel;
            public override Color ImageMarginGradientEnd => ColorPanel;
            public override Color SeparatorDark => ColorPanelBorder;
            public override Color SeparatorLight => ColorPanelBorder;
        }

        private class DarkMenuRenderer : ToolStripProfessionalRenderer
        {
            public DarkMenuRenderer() : base(new DarkColorTable())
            {
            }

            protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
            {
                e.TextColor = ColorText;
                base.OnRenderItemText(e);
            }
        }
    }
}
