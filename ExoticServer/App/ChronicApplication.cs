using System;
using System.Windows.Forms;

using ExoticServer.App.UI;

using Serilog;

namespace ExoticServer.App
{
    public class ChronicApplication
    {
        public static ChronicApplication Instance { get; private set; }

        private readonly FormHandler _formHandler;
        private readonly ILogger _logger;

        public ChronicApplication()
        {
            if(Instance == null)
                Instance = this;

            _formHandler = new FormHandler();
            _logger = new LoggerConfiguration()
                        .WriteTo.File("ExoticServer-logs.txt", rollingInterval: RollingInterval.Day)
                        .CreateLogger();
        }

        public void Initialize()
        {
            ShowForm(_formHandler.MainForm);
        }

        public void ShowForm(Form form)
        {
            form.Show();
        }

        public void HideForm(Form form)
        {
            form.Hide();
        }

        public void CloseForm(Form form)
        {
            form.Close();
        }

        public void Shutdown()
        {
            Application.Exit();
            Environment.Exit(0);
        }

        public FormHandler FormHandler { get { return _formHandler; } }
        public ILogger Logger { get { return _logger;} }
    }
}
