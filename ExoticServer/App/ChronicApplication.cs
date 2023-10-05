using System;
using System.Windows.Forms;

using ExoticServer.App.UI;
using ExoticServer.Classes.Server;
using Serilog;

namespace ExoticServer.App
{
    public class ChronicApplication
    {
        public static ChronicApplication Instance { get; private set; }

        private readonly FormHandler _formHandler;
        private readonly ILogger _logger;

        private ExoticTcpServer _tcpServer;

        public ChronicApplication()
        {
            if(Instance == null)
                Instance = this;

            _formHandler = new FormHandler();

            _tcpServer = new ExoticTcpServer(24000);

            _logger = new LoggerConfiguration()
                        .WriteTo.File("D:/Coding/Projects/C#/ServerAndClient Projects/ExoticServer/ExoticServer-logs.txt", rollingInterval: RollingInterval.Day)
                        .CreateLogger();

            _logger.Information($"(ChronicApplication.cs) - ChronicApplication(): App Started!");
        }

        public async void Initialize()
        {
            await _tcpServer.StartServer();
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
            _tcpServer.StopServer();

            Application.Exit();
            Environment.Exit(0);
        }

        public FormHandler FormHandler { get { return _formHandler; } }
        public ILogger Logger { get { return _logger;} }

        public ExoticTcpServer TcpServer { get { return _tcpServer; } }
    }
}
