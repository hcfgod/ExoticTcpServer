using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using ExoticServer.App.UI;
using ExoticServer.Classes.Server;
using ExoticServer.Classes.Utils;
using Newtonsoft.Json;
using Serilog;

namespace ExoticServer.App
{
    public class ChronicApplication
    {
        public static ChronicApplication Instance { get; private set; }

        private readonly FormHandler _formHandler;
        private readonly ILogger _logger;

        private readonly ExoticTcpServer _tcpServer;

        private readonly Database _database;

        public ChronicApplication()
        {
            if (Instance == null)
                Instance = this;

            _formHandler = new FormHandler();

            _database = new Database(GetConnectionStringFromConfig());

            CryptoUtility.Initialize();

            _tcpServer = new ExoticTcpServer(GetPortFromConfig());

            _logger = new LoggerConfiguration()
                        .WriteTo.File("..\\..\\ExoticServer-logs.txt", rollingInterval: RollingInterval.Day)
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

        public ILogger Logger { get { return _logger; } }

        public FormHandler FormHandler { get { return _formHandler; } }

        public Database Database { get { return _database; } }

        public ExoticTcpServer TcpServer { get { return _tcpServer; } }


        private string GetConnectionStringFromConfig()
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\App\\config.json")));
            return config["Database"]["ConnectionString"];
        }

        private int GetPortFromConfig()
        {
            var config = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..\\..\\App\\config.json")));
            return int.Parse(config["Server"]["Port"]);
        }
    }
}
