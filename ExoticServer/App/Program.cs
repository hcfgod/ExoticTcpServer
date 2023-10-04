using System;
using System.Windows.Forms;

namespace ExoticServer.App
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            ChronicApplication app = new ChronicApplication();
            app.Initialize();

            Application.Run(app.FormHandler.MainForm);
        }
    }
}
