using System;
using System.Windows.Forms;

namespace ShubusViewer
{
    public partial class DlgGoTo : Form
    {
        private RichTextBox targetBox;

        public DlgGoTo(RichTextBox aBox)
        {
            this.targetBox = aBox;
            InitializeComponent();
        }

        private void DlgGoTo_Shown(object sender, EventArgs e)
        {
            int s = this.targetBox.SelectionStart;
            int curLine = this.targetBox.GetLineFromCharIndex(s);
            int curCol = s - this.targetBox.GetFirstCharIndexFromLine(curLine);

            this.textBox1.Text = (++curLine).ToString();
            this.textBox2.Text = (++curCol).ToString();
            this.textBox1.Focus();
            this.textBox1.SelectAll();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if ((e.KeyCode == Keys.Enter && this.textBox2.Text == "") || e.KeyCode == Keys.Down
                || e.KeyCode == Keys.Up)
            {               
                this.textBox2.Focus();
                this.textBox2.SelectAll();
            }
            else if (e.KeyCode == Keys.Enter)
            {
                this.DoRequest();
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.textBox1.Text == "")
                    this.textBox1.Focus();
                else
                    this.DoRequest();
            }
            else if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down)
            {               
                this.textBox1.Focus();
                this.textBox1.SelectAll();
            }
        }

        private void DlgGoTo_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape)
                this.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.DoRequest();
        }

        private void DoRequest()
        {
            int line = 0, col = 0;

            try
            {
                line = System.Convert.ToInt32(this.textBox1.Text) - 1;
                col = System.Convert.ToInt32(this.textBox2.Text) - 1;
            }
            catch (Exception ee)
            {
                MessageBox.Show(ee.Message, ExtendedData.Constants.STR_APP_TITLE
                    , MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            if (line < 0 || col < 0) return;

            try
            {
                int num = this.targetBox.GetFirstCharIndexFromLine(line);
                int line2 = this.targetBox.GetLineFromCharIndex(num + col);

                if (line2 == line)
                {
                    this.targetBox.SelectionStart = num + col;
                    this.targetBox.ScrollToCaret();
                }
                else
                {
                    MessageBox.Show(
                    string.Format(ExtendedData.Constants.STR_NOCOLUMN_MSG, line + 1, col + 1),
                    ExtendedData.Constants.STR_APP_TITLE,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
            }
            catch (Exception e1)
            {
                MessageBox.Show(e1.Message, ExtendedData.Constants.STR_APP_TITLE
                    , MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            this.Close();
        }
    }
}