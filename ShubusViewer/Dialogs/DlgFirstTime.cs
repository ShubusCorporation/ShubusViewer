using System;
using System.Windows.Forms;
using System.Reflection;
using System.IO;
using System.Drawing;

namespace ShubusViewer
{
    public partial class DlgFirstTime : Form
    {
        public DlgFirstTime()
        {
            InitializeComponent();
            this.DialogResult = DialogResult.No;
        }

        private void DlgFirstTime_Load(object sender, EventArgs e)
        {
            Assembly _assembly;
            StreamReader _textStreamReader = null;
            this.textBox1.SelectionIndent = 5;
            this.moveButtons();

            try
            {
                _assembly = Assembly.GetExecutingAssembly();
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("ShubusViewer.Resources.License.txt"));
                this.textBox1.Text = _textStreamReader.ReadToEnd();
            }
            catch
            {
                MessageBox.Show("Error: Resource is corrupted!");
                this.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Yes;
            this.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DlgFirstTime_Resize(object sender, EventArgs e)
        {
            this.moveButtons();
        }

        private void moveButtons()
        {
            int xCenter = this.Size.Width >> 1;

            this.button1.Location = new Point(xCenter - this.button1.Size.Width - 10, this.button1.Location.Y);
            this.button2.Location = new Point(xCenter + 10, this.button2.Location.Y);
        }

        private void DlgFirstTime_Shown(object sender, EventArgs e)
        {
            this.button2.Focus();
        }
    }
}