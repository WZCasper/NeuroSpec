using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NeuroSpec
{
    /// <summary>
    /// Окно с результатом оценки производительности: общий балл, разбивка по компонентам
    /// и (если выбрана игра) вывод о том, на каких настройках она пойдёт.
    /// </summary>
    public class PerformanceResultForm : Form
    {
        private static readonly Color ColorBackground = Color.FromArgb(24, 24, 27);
        private static readonly Color ColorPanel = Color.FromArgb(32, 32, 36);
        private static readonly Color ColorPanelBorder = Color.FromArgb(52, 52, 58);
        private static readonly Color ColorText = Color.FromArgb(230, 230, 230);
        private static readonly Color ColorTextMuted = Color.FromArgb(150, 150, 156);
        private static readonly Color ColorAccent = Color.FromArgb(88, 166, 255);
        private static readonly Color ColorSuccess = Color.FromArgb(63, 185, 80);
        private static readonly Color ColorWarning = Color.FromArgb(230, 180, 60);
        private static readonly Color ColorError = Color.FromArgb(240, 90, 90);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int pvAttribute, int cbAttribute);

        private const int DwmwaUseImmersiveDarkMode = 20;
        private const int DwmwaBorderColor = 34;

        public PerformanceResultForm(PerformanceResult result)
        {
            bool hasGame = !string.IsNullOrEmpty(result.GameName);

            BackColor = ColorBackground;
            ForeColor = ColorText;
            Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            Text = "Оценка производительности";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(0);

            var root = new TableLayoutPanel
            {
                ColumnCount = 1,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Padding = new Padding(24),
                BackColor = ColorBackground
            };

            var overallTitle = new Label
            {
                Text = "ОБЩАЯ ОЦЕНКА ПРОИЗВОДИТЕЛЬНОСТИ",
                Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                ForeColor = ColorAccent,
                AutoSize = true
            };

            var overallScoreLabel = new Label
            {
                Text = $"{result.OverallScore} / 100",
                Font = new Font("Segoe UI", 28f, FontStyle.Bold),
                ForeColor = ColorText,
                AutoSize = true,
                Margin = new Padding(0, 4, 0, 0)
            };

            var overallRatingLabel = new Label
            {
                Text = result.OverallRating,
                Font = new Font("Segoe UI", 11f, FontStyle.Regular),
                ForeColor = ScoreColor(result.OverallScore),
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 14)
            };

            root.Controls.Add(overallTitle);
            root.Controls.Add(overallScoreLabel);
            root.Controls.Add(overallRatingLabel);
            root.Controls.Add(BuildBreakdownPanel(result));

            if (hasGame)
            {
                var separator = new Panel
                {
                    Height = 1,
                    Width = 380,
                    BackColor = ColorPanelBorder,
                    Margin = new Padding(0, 18, 0, 16)
                };

                var gameTitle = new Label
                {
                    Text = "ИГРА: " + result.GameName.ToUpperInvariant(),
                    Font = new Font("Segoe UI", 9f, FontStyle.Bold),
                    ForeColor = ColorAccent,
                    AutoSize = true
                };

                var gameTier = new Label
                {
                    Text = result.GameQualityTier,
                    Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                    ForeColor = ScoreColor(result.OverallScore),
                    AutoSize = true,
                    Margin = new Padding(0, 4, 0, 6)
                };

                var gameVerdict = new Label
                {
                    Text = result.GameVerdict,
                    ForeColor = ColorTextMuted,
                    AutoSize = true,
                    MaximumSize = new Size(380, 0)
                };

                root.Controls.Add(separator);
                root.Controls.Add(gameTitle);
                root.Controls.Add(gameTier);
                root.Controls.Add(gameVerdict);
            }

            var disclaimer = new Label
            {
                Text = "Оценка приблизительная: основана на модели и объёме железа, а не на реальном тесте (бенчмарке).",
                Font = new Font("Segoe UI", 7.5f, FontStyle.Italic),
                ForeColor = ColorTextMuted,
                AutoSize = true,
                MaximumSize = new Size(380, 0),
                Margin = new Padding(0, 18, 0, 14)
            };
            root.Controls.Add(disclaimer);

            var closeButton = new Button
            {
                Text = "Закрыть",
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                MinimumSize = new Size(100, 36),
                Padding = new Padding(16, 0, 16, 0),
                FlatStyle = FlatStyle.Flat,
                BackColor = ColorPanel,
                ForeColor = ColorText,
                Cursor = Cursors.Hand,
                DialogResult = DialogResult.OK
            };
            closeButton.FlatAppearance.BorderSize = 1;
            closeButton.FlatAppearance.BorderColor = ColorPanelBorder;
            closeButton.MouseEnter += (s, e) => closeButton.BackColor = ColorPanelBorder;
            closeButton.MouseLeave += (s, e) => closeButton.BackColor = ColorPanel;
            root.Controls.Add(closeButton);

            Controls.Add(root);
            AcceptButton = closeButton;
            CancelButton = closeButton;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            try
            {
                int enableDark = 1;
                DwmSetWindowAttribute(Handle, DwmwaUseImmersiveDarkMode, ref enableDark, sizeof(int));

                int borderColor = ColorAccent.R | (ColorAccent.G << 8) | (ColorAccent.B << 16);
                DwmSetWindowAttribute(Handle, DwmwaBorderColor, ref borderColor, sizeof(int));
            }
            catch
            {
                // На старых версиях Windows тёмная рамка окна недоступна.
            }
        }

        private static Color ScoreColor(int score)
        {
            if (score >= 65) return ColorSuccess;
            if (score >= 40) return ColorWarning;
            return ColorError;
        }

        private Control BuildBreakdownPanel(PerformanceResult result)
        {
            var panel = new TableLayoutPanel
            {
                ColumnCount = 2,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                BackColor = ColorBackground
            };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            AddBreakdownRow(panel, "CPU", result.CpuScore);
            AddBreakdownRow(panel, "RAM", result.RamScore);
            AddBreakdownRow(panel, "GPU", result.GpuScore);
            AddBreakdownRow(panel, "Накопитель", result.StorageScore);

            return panel;
        }

        private void AddBreakdownRow(TableLayoutPanel panel, string label, int score)
        {
            int row = panel.RowCount;
            panel.RowCount++;
            panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var nameLabel = new Label
            {
                Text = label,
                ForeColor = ColorTextMuted,
                AutoSize = true,
                Margin = new Padding(0, 8, 12, 4)
            };

            var barOuter = new Panel
            {
                Height = 10,
                Width = 200,
                BackColor = ColorPanel,
                Margin = new Padding(0, 12, 8, 4)
            };
            var barInner = new Panel
            {
                Height = 10,
                Width = (int)(200 * Math.Clamp(score, 0, 100) / 100.0),
                BackColor = ScoreColor(score),
                Dock = DockStyle.Left
            };
            barOuter.Controls.Add(barInner);

            var scoreLabel = new Label
            {
                Text = score.ToString(),
                ForeColor = ColorText,
                AutoSize = true,
                Margin = new Padding(0, 8, 0, 4)
            };

            var rowFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                WrapContents = false,
                BackColor = ColorBackground,
                Margin = new Padding(0)
            };
            rowFlow.Controls.Add(barOuter);
            rowFlow.Controls.Add(scoreLabel);

            panel.Controls.Add(nameLabel, 0, row);
            panel.Controls.Add(rowFlow, 1, row);
        }
    }
}
