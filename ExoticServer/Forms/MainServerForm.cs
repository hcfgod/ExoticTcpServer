using ExoticServer.App;
using ExoticServer.App.UI;
using System.Windows.Forms;

namespace ExoticServer.Forms
{
    public partial class MainServerForm : CustomForm
    {
        public MainServerForm()
        {
            InitializeComponent();
        }

        private void MainServerForm_Load(object sender, System.EventArgs e)
        {

        }

        private void ExitButton_Click(object sender, System.EventArgs e)
        {
            ChronicApplication.Instance.Shutdown();
        }

        private void MinimizeButton_Click(object sender, System.EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, System.EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
            {
                this.WindowState = FormWindowState.Normal;
            }
            else
            {
                this.WindowState = FormWindowState.Maximized;
            }
        }
    }
}
