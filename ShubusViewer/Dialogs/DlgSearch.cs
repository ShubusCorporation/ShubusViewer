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
    public partial class DlgSearch : Form
    {
        public string txtFind
        {
            get { return this.txtToFind; }
            set
            {
                if (value.Length > this.textBox1.MaxLength)
                {
                    this.txtToFind = value.Substring(0, this.textBox1.MaxLength);
                }
                else this.txtToFind = value;
            }
        }
        public bool cbox1
        { 
            get { return this.checkBox1.Checked; }
            set { this.checkBox1.Checked = value; }
        }
        public bool cbox2
        {
            get { return this.checkBox2.Checked; }
            set { this.checkBox2.Checked = value; }
        }
        public bool cbox3
        {
            get { return this.checkBox3.Checked; }
            set { this.checkBox3.Checked = value; }
        }
        public int ActiveTab{ get; set; }
        private string txtToFind = "";
        private int curTab;
        private TabPage second;
        private bool oldCHeckBox = false;
        private Dictionary<string, string> myDictionary = null;

        public bool secondTab
        {
            set
            {
                if (value && tabControl1.TabPages.Count < 2)
                {
                    tabControl1.TabPages.Add(second);
                    this.checkBox1.Checked = this.oldCHeckBox;
                }
                else if (value == false && tabControl1.TabPages.Count > 1)
                {
                    tabControl1.TabPages.Remove(tabControl1.TabPages[1]);
                    this.oldCHeckBox = this.checkBox1.Checked;
                }
            }
        }

        public bool caseSensitive
        {
            set
            {
                if (value)
                {
                    this.checkBox1.Enabled = true;
                }
                else
                {
                    this.checkBox1.Checked = true;
                    this.checkBox1.Enabled = false;
                }
            }
        }

        public string txtWhat
        {
            get
            {
                if (this.textBox2.Text == "^l") return "\n";
                return this.textBox2.Text;
            }
        }

        public string txtWith
        {
            get
            {
                if (this.textBox3.Text == "^l") return "\n";
                return this.textBox3.Text;
            }
        }

        public StringComparison sComparison
        {
            get
            {
                if (this.checkBox1.Checked)
                {
                    return StringComparison.CurrentCulture;
                }
                else
                {
                    return StringComparison.InvariantCultureIgnoreCase;
                }
            }
        }

        public StringComparison rComparison
        {
            get
            {
                if (this.checkBox3.Checked)
                {
                    return StringComparison.CurrentCulture;
                }
                else
                {
                    return StringComparison.InvariantCultureIgnoreCase;
                }
            }
        }

        public bool wholeWords
        {
            get
            {
                if (this.textBox2.Text == "^l" || this.textBox3.Text == "^l") return false;

                return this.checkBox2.Checked;
            }
        }

        public DlgSearch()
        {
            InitializeComponent();
            this.second = this.tabControl1.TabPages[1];
        }

        private void DlgSearch_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.F3:
                    this.txtFind = this.textBox1.Text;
                    this.DialogResult = System.Windows.Forms.DialogResult.OK;
                    this.Close();
                    break;

                case Keys.Escape:
                    this.textBox1.Text = this.txtFind;
                    this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                    this.Close();
                    break;
            }
        }
        // DialogResult.OK;
        private void button1_Click(object sender, EventArgs e)
        {
            this.txtFind = this.textBox1.Text;
            this.Close();
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.DialogResult = this.button1.DialogResult;
                this.txtFind = this.textBox1.Text;
                this.Close();
            }
        }

        private void DlgSearch_Shown(object sender, EventArgs e)
        {
            if (this.txtFind.Length > 0)
            {
                this.textBox1.Text = this.txtFind;
            }
            if (this.textBox1.Text.Length > 0)
            {
                this.textBox2.Text = this.textBox1.Text;
            }
            this.tabControl1.SelectTab(this.ActiveTab);

            this.tabControl1_SelectedIndexChanged(sender, null);
        }

        // DialogResult.Retry;
        private void button2_Click(object sender, EventArgs e)
        {
            if (true == (this.button2.Enabled = this.checkReplaceData()))
            {
                this.txtFind = this.textBox2.Text;
                this.fillDictionary();
                this.Close();
            }
        }

        private void fillDictionary()
        {
            if (this.myDictionary == null)
                this.myDictionary = new Dictionary<string, string>();

            if (this.textBox2.Text != "")
            {
                this.myDictionary[this.textBox2.Text] = this.textBox3.Text;
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            string str = this.textBox2.Text;
            this.textBox2.Text = this.textBox3.Text;
            this.textBox3.Text = str;
            this.textBox2.Focus();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            this.button2.Enabled = this.checkReplaceData();
            this.txtFind = this.textBox2.Text;

            if (this.myDictionary != null)
            {
                if (this.myDictionary.ContainsKey(this.txtFind))
                {
                    this.textBox3.Text = this.myDictionary[this.txtFind];
                }
                if (this.myDictionary.ContainsValue(this.txtFind))
                {
                    this.textBox3.Text = this.myDictionary.FirstOrDefault
                        (x => x.Value == this.txtFind).Key;
                }
            }
        }

        private bool checkReplaceData()
        {
            return (this.textBox2.Text.Length > 0);
        }

        private void tabControl1_Selected(object sender, TabControlEventArgs e)
        {
            this.curTab = e.TabPage.TabIndex;
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.curTab == 0) // Find tab.
            {
                this.textBox1.Focus();

                if (textBox1.Text.Length > 0)
                {
                    this.textBox1.SelectionStart = 0;
                    this.textBox1.SelectionLength = textBox1.TextLength;
                    this.button1.Enabled = true;
                }
                else this.button1.Enabled = false;
            }
            if (this.curTab == 1) // Find & Replace tab.
            {
                textBox2.SelectionStart = 0;
                textBox2.SelectionLength = textBox2.TextLength;
                textBox2.Focus();
            }
        }

        private void textBox2_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up || e.KeyCode == Keys.Enter)
            {
                this.textBox3.Focus();
                this.textBox3.SelectionStart = 0;
                this.textBox3.SelectionLength = this.textBox3.Text.Length;
            }
        }

        private void textBox3_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down || e.KeyCode == Keys.Up)
            {
                this.textBox2.Focus();
                this.textBox2.SelectionStart = 0;
                this.textBox2.SelectionLength = this.textBox2.Text.Length;
            }
            else if (e.KeyCode == Keys.Enter && checkReplaceData())
            {
                this.DialogResult = this.button2.DialogResult;
                this.fillDictionary();
                this.Close();
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            this.button1.Enabled = (this.textBox1.Text.Length > 0);
            this.txtFind = this.textBox1.Text;
        }

        private void checkBox1_Click(object sender, EventArgs e)
        {
            if (this.checkBox1.Enabled)
                this.oldCHeckBox = this.checkBox1.Checked;
        }

        private void DlgSearch_Resize(object sender, EventArgs e)
        {
            this.button1.Location =
            new Point((this.Size.Width - this.button1.Size.Width) / 2, this.button1.Location.Y);

            this.button2.Location =
            new Point((this.Size.Width - this.button2.Size.Width) / 2, this.button2.Location.Y);
        }
    }
}