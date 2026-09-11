using System;
using System.Windows.Forms;

namespace NeuroSpec
{
    /// <summary>
    /// Точка входа в приложение NeuroSpec.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Глобальный перехват непредвиденных ошибок, чтобы приложение
            // не завершалось аварийно без объяснения причины.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (sender, e) =>
            {
                MessageBox.Show(
                    "Произошла непредвиденная ошибка:\n" + e.Exception.Message,
                    "NeuroSpec — ошибка",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            };

            Application.Run(new MainForm());
        }
    }
}
