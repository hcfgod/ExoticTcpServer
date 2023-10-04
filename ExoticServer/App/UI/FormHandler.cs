using ExoticServer.Forms;
using System.Windows.Forms;

namespace ExoticServer.App.UI
{
    public class FormHandler
    {
        public Form MainForm { get; }

        public FormHandler()
        {
            MainForm = new MainServerForm();
        }
    }
}
