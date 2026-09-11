using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NeuroSpec
{
    /// <summary>
    /// Главное окно приложения NeuroSpec.
    /// Отображает характеристики компьютера в тёмном графическом интерфейсе
    /// и позволяет скопировать их в буфер обмена одной кнопкой.
    /// </summary>
    public class MainForm : Form
    {
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
        private readonly Button _copyButton;
        private readonly Button _refreshButton;
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
            Size = new Size(640, 700);
            MinimumSize = new Size(560, 520);
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
                RowCount = 2,
                AutoSize = true,
                BackColor = ColorBackground,
                Margin = new Padding(0, 0, 0, 15)
            };

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

            headerPanel.Controls.Add(titleLabel, 0, 0);
            headerPanel.Controls.Add(subtitleLabel, 0, 1);
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
                ColumnCount = 3,
                RowCount = 1,
                AutoSize = true,
                BackColor = ColorBackground,
                Margin = new Padding(0, 15, 0, 0)
            };
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            _refreshButton = CreateButton("Обновить", isPrimary: false);
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

            _copyButton = CreateButton("Копировать", isPrimary: true);
            _copyButton.Click += CopyButton_Click;
            footerPanel.Controls.Add(_copyButton, 2, 0);

            _rootLayout.Controls.Add(footerPanel, 0, 2);

            Load += MainForm_Load;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            await LoadSpecsAsync();
        }

        private async void RefreshButton_Click(object sender, EventArgs e)
        {
            await LoadSpecsAsync();
        }

        private Button CreateButton(string text, bool isPrimary)
        {
            var button = new Button
            {
                Text = text,
                Width = 140,
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

        private async Task LoadSpecsAsync()
        {
            _loadingLabel.Visible = true;
            _specsTable.Visible = false;
            _copyButton.Enabled = false;
            _refreshButton.Enabled = false;
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
            }
        }

        private void PopulateSpecsTable(SystemSpecs specs)
        {
            _specsTable.Controls.Clear();
            _specsTable.RowStyles.Clear();
            _specsTable.RowCount = 0;

            AddSpecRow("Операционная система", specs.OperatingSystem);
            AddSpecRow("Процессор", specs.Processor);
            AddSpecRow("Оперативная память", specs.RandomAccessMemory);
            AddSpecRow("Видеокарта", specs.VideoController);
            AddSpecRow("Материнская плата", specs.Motherboard);
            AddSpecRow("Накопители", specs.Storage);
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
                MaximumSize = new Size(520, 0),
                BackColor = Color.Transparent,
                Margin = new Padding(0, 3, 0, 0)
            };

            rowPanel.Controls.Add(categoryLabel, 0, 0);
            rowPanel.Controls.Add(valueLabel, 0, 1);

            _specsTable.Controls.Add(rowPanel, 0, rowIndex);
        }

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
    }
}
