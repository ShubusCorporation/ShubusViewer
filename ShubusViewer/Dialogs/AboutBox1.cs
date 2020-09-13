using System;
using System.Reflection;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ExtendedData;

namespace ShubusViewer
{
    partial class DlgAbout : Form
    {
        private short triggerCount = 0;
        private short maxCount = 10;
        private int minX = 76;
        private int maxX = 83;
        private int minY = 36;
        private int maxY = 44;
        private int dx, dy;
        private bool crossed = false;
        private string myCode = "";
        private AppExtendedData mySharedData;

        [DllImport("user32.dll", EntryPoint = "HideCaret")]
        private static extern bool HideCaret(IntPtr hWnd);

        public DlgAbout(AppExtendedData sharedData)
        {
            InitializeComponent();
            this.mySharedData = sharedData;

            this.dx = (maxX - minX) / 2;
            this.dy = (maxY - minY) / 2;
            this.textBox1.GotFocus += new EventHandler(textBox1_GotFocus);

            if (this.textBox2.Text.Length == 1)
            {
                this.textBox2.Text += ((long)Math.Tan(1.5707963267911234) - (long)Math.Tan(1.5707961924007996) - 1).ToString();
                this.myCode = this.textBox2.Text;
            }
            else
            {
                this.Close();
            }
        }

        void textBox1_GotFocus(object sender, EventArgs e)
        {
            HideCaret(textBox1.Handle);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void DlgAbout_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) this.Close();
        }

        private void DlgAbout_Shown(object sender, EventArgs e)
        {
            this.button1.Focus();
            this.textBox2.Text = this.myCode;
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (this.inTheRect(e.X, e.Y) != this.crossed)
            {
                this.crossed = !this.crossed;

                if (++this.triggerCount > this.maxCount)
                {
                    this.triggerCount = 0;
                    MessageBox.Show(ExtendedData.Constants.STR_SECRET_MSG,
                            ExtendedData.Constants.STR_APP_TITLE,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);

                    Form2 game = new Form2();
                    game.Show(this);
                }
            }
        }

        private bool inTheRect(int x, int y)
        {
            bool r = (x <= this.maxX && x >= this.minX && y <= this.maxY && y >= this.minY);

            if (x <= this.minX - dx || x >= this.maxX + dx ||
                y <= this.minY - dy || y >= this.maxY + dy)
            {
                this.triggerCount = 0;
            }
            return r;
        }

        private void DlgAbout_Load(object sender, EventArgs e)
        {
            if (this.textBox1.Text.Length > 0) return;

            Assembly _assembly;
            StreamReader _textStreamReader = null;

            try
            {
                _assembly = Assembly.GetExecutingAssembly();
                _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("ShubusViewer.Resources.About.txt"));
                this.textBox1.Text = _textStreamReader.ReadToEnd();
            }
            catch(Exception ex)
            {
                MessageBox.Show("DlgAbout_Load Exception: " + ex.Message);
            }
            this.Text += " v. " + AppUpdater.getCurrentVersionAndCopyright();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(ExtendedData.Constants.STR_EMAIL_PREFIX
                    + this.linkLabel1.Text);
            }
            catch (Exception) { }
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            if (sender is LinkLabel)
            {
                try
                {
                    System.Diagnostics.Process.Start("http://" + (sender as LinkLabel).Text);
                }
                catch (Exception) { }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            AppUpdater appUpdater = new AppUpdater();

            try { appUpdater.CheckForNewVersion(); }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message,
                    ExtendedData.Constants.STR_APP_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            Utils.Utils.ShowHelp();
        }
    }
}