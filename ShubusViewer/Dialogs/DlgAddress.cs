using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace ShubusViewer
{
    public partial class DlgAddress : Form
    {
        public int address = 0;

        public DlgAddress()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.doIt();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.doIt();
            }
        }

        private void doIt()
        {
            if (this.textBox1.Text.Length > 0)
            {
                // http://msdn.microsoft.com/en-us/library/bb311038.aspx
                int fromBase = (this.checkBox1.Checked) ? 16 : 10;

                try
                {
                    this.address = Convert.ToInt32(this.textBox1.Text, fromBase);
                }
                catch (Exception ee)
                {
                    MessageBox.Show(ee.Message, ExtendedData.Constants.STR_APP_TITLE,
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
        }

        private void DlgAddress_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
            {
                this.DialogResult = DialogResult.Cancel;
                this.Close();
            }
        }

        private void DlgAddress_Shown(object sender, EventArgs e)
        {
            this.textBox1.Focus();
            this.textBox1.SelectAll();
        }
    }
}
