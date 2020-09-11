using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ShubusViewer
{
    public partial class DlgRecentDescr : Form
    {
        public string Desr
        {
            get
            {
                return this.textBox1.Text;
            }
        }

        public DlgRecentDescr(string descr)
        {
            InitializeComponent();
            this.textBox1.Text = descr;
            this.DialogResult = DialogResult.Cancel;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            this.button1.Enabled = (textBox1.Text.Length > 0);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
        }
    }
}
