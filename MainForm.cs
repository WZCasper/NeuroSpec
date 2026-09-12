using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuroSpec
{
    /// <summary>
    /// Главное окно приложения NeuroSpec.
    /// Отображает характеристики компьютера в тёмном графическом интерфейсе,
    /// позволяет скопировать/сохранить их и проверяет наличие новой версии.
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

        private readonly TableLayoutPanel _rootLayout;
        private readonly Panel _contentPanel;
        private readonly TableLayoutPanel _specsTable;
        private readonly Label _loadingLabel;
        private readonly Label _statusLabel;
        private readonly LinkLabel _updateLinkLabel;
        private readonly Button _copyButton;
        private readonly Button _refreshButton;
        private readonly Button _moreButton;
        private readonly ContextMenuStrip _moreMenu;
        private readonly System.Windows.Forms.Timer _statusTimer;

        private SystemSpecs _currentSpecs;

        public MainForm()
        {
            // ---------- Параметры окна ----------
            Text = "NeuroSpec — характеристики компьютера";
            BackColor = ColorBackground;
            ForeColor = ColorText;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(660, 760);
            MinimumSize = new Size(580, 560);
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = true;
            Icon = SystemIcons.Application;

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
                RowCount = 4,
                AutoSize = true,
                BackColor = ColorBackground,
                Margin = new Padding(0, 0, 0, 15)
            };
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

            var subtitleLabel = new Label
            {
                Text = "Характеристики вашего компьютера",
                Font = new Font("Segoe UI", 10f, FontStyle.Regular),
                ForeColor = ColorTextMuted,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 2, 0, 0)
            };

            var versionLabel = new Label
            {
                Text = "Версия " + GetDisplayVersion(),
                Font = new Font("Segoe UI", 8f, FontStyle.Regular),
                ForeColor = ColorTextMuted,
                AutoSize = true,
                BackColor = Color.Transparent,
                Margin = new Padding(0, 4, 0, 0)
            };

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

            headerPanel.Controls.Add(titleLabel, 0, 0);
            headerPanel.Controls.Add(subtitleLabel, 0, 1);
            headerPanel.Controls.Add(versionLabel, 0, 2);
            headerPanel.Controls.Add(_updateLinkLabel, 0, 3);
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
                Text = "Сбор данных о системе...",
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
                ColumnCount = 4,
                RowCount = 1,
                AutoSize = true,
                BackColor = ColorBackground,
                Margin = new Padding(0, 15, 0, 0)
            };
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _refreshButton = CreateButton("Обновить", isPrimary: false, width: 120);
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

            _moreMenu = CreateMoreMenu();
            _moreButton = CreateButton("Ещё ▾", isPrimary: false, width: 90);
            _moreButton.Click += (s, e) => _moreMenu.Show(_moreButton, new Point(0, _moreButton.Height));
            footerPanel.Controls.Add(_moreButton, 2, 0);

            _copyButton = CreateButton("Копировать", isPrimary: true, width: 140);
            _copyButton.Click += CopyButton_Click;
            footerPanel.Controls.Add(_copyButton, 3, 0);

            _rootLayout.Controls.Add(footerPanel, 0, 2);

            Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            _ = CheckForUpdatesAsync();
            await LoadSpecsAsync();
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            await LoadSpecsAsync();
        }

        // ==================== Кнопки и меню ====================

        private Button CreateButton(string text, bool isPrimary, int width)
        {
            var button = new Button
            {
                Text = text,
                Width = width,
                Height = 38,
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

        private ContextMenuStrip CreateMoreMenu()
        {
            var menu = new ContextMenuStrip
            {
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Renderer = new DarkMenuRenderer(),
                ShowImageMargin = false
            };

            var copyImageItem = new ToolStripMenuItem("Скопировать как картинку");
            copyImageItem.Click += (s, e) => CopySpecsAsImage();
            menu.Items.Add(copyImageItem);

            menu.Items.Add(new ToolStripSeparator());

            var saveTxtItem = new ToolStripMenuItem("Сохранить как .txt");
            saveTxtItem.Click += (s, e) => SaveSpecsToFile(asJson: false);
            menu.Items.Add(saveTxtItem);

            var saveJsonItem = new ToolStripMenuItem("Сохранить как .json");
            saveJsonItem.Click += (s, e) => SaveSpecsToFile(asJson: true);
            menu.Items.Add(saveJsonItem);

            return menu;
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
                _currentSpecs = await SystemInfoCollector.CollectAsync();
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

            AddSpecRow("Операционная система", specs.OperatingSystem);
            AddSpecRow("Дата установки Windows", specs.WindowsInstallDate);
            AddSpecRow("Процессор", specs.Processor);
            AddSpecRow("Оперативная память", specs.RandomAccessMemory);
            AddSpecRow("Видеокарта", specs.VideoController);
            AddSpecRow("Материнская плата", specs.Motherboard);
            AddSpecRow("BIOS/UEFI", specs.Bios);
            AddSpecRow("Серийный номер", specs.SerialNumber);
            AddSpecRow("Накопители", specs.Storage);
            AddSpecRow("Свободное место", specs.DiskSpace);
            AddSpecRow("Сеть", specs.NetworkAdapters);
            AddSpecRow("Мониторы", specs.Monitors);
        }

        private void AddSpecRow(string category, string value)
        {
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
                Text = value,
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
                Clipboard.SetText(_currentSpecs.ToClipboardText());
                ShowStatus("Скопировано в буфер обмена!", ColorSuccess);
            }
            catch (Exception ex)
            {
                ShowStatus("Ошибка копирования: " + ex.Message, ColorError);
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
                ShowStatus("Картинка скопирована в буфер обмена!", ColorSuccess);
            }
            catch (Exception ex)
            {
                ShowStatus("Ошибка: " + ex.Message, ColorError);
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
                Filter = asJson ? "JSON файл (*.json)|*.json" : "Текстовый файл (*.txt)|*.txt",
                FileName = asJson ? "NeuroSpec.json" : "NeuroSpec.txt"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                try
                {
                    string content = asJson ? _currentSpecs.ToJson() : _currentSpecs.ToClipboardText();
                    File.WriteAllText(dialog.FileName, content, System.Text.Encoding.UTF8);
                    ShowStatus("Файл сохранён!", ColorSuccess);
                }
                catch (Exception ex)
                {
                    ShowStatus("Ошибка сохранения: " + ex.Message, ColorError);
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
                return "неизвестна";
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
                    BeginInvoke(new Action(() =>
                    {
                        _updateLinkLabel.Text = $"Доступна новая версия (сборка {latestBuild}) — нажмите, чтобы скачать";
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
