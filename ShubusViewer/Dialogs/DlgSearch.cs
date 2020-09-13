using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using SearchData;

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
            get { return this.findSameCaseCheckBox.Checked; }
            set { this.findSameCaseCheckBox.Checked = value; }
        }
        public bool cbox2
        {
            get { return this.replaceWholeWordCheckBox.Checked; }
            set { this.replaceWholeWordCheckBox.Checked = value; }
        }
        public bool cbox3
        {
            get { return this.replaceSameCaseCheckBox.Checked; }
            set { this.replaceSameCaseCheckBox.Checked = value; }
        }
        public int ActiveTab { get; set; }
        private string txtToFind = "";
        private int curTab;
        private TabPage second;
        private bool oldCHeckBox = false;
        private Dictionary<string, string> myDictionary = null;
        private SearchDialogData searchDTO;
        private searchCallback findCB;
        private searchCallback replaceCB;

        private void UpdateDTO()
        {
            this.searchDTO.findSameCase = this.cbox1;
            this.searchDTO.replaceWholeWord = this.wholeWords;
            this.searchDTO.replaceSameCase = this.cbox3;
            this.searchDTO.txtFind = this.txtFind;
            this.searchDTO.txtWhat = this.txtWhat;
            this.searchDTO.txtWith = this.txtWith;
            this.searchDTO.dirty = true;
        }

        public bool secondTab
        {
            set
            {
                if (value && tabControl1.TabPages.Count < 2)
                {
                    tabControl1.TabPages.Add(second);
                    this.findSameCaseCheckBox.Checked = this.oldCHeckBox;
                }
                else if (value == false && tabControl1.TabPages.Count > 1)
                {
                    tabControl1.TabPages.Remove(tabControl1.TabPages[1]);
                    this.oldCHeckBox = this.findSameCaseCheckBox.Checked;
                }
            }
        }

        public bool caseSensitive
        {
            set
            {
                if (value)
                {
                    this.findSameCaseCheckBox.Enabled = true;
                }
                else
                {
                    this.findSameCaseCheckBox.Checked = true;
                    this.findSameCaseCheckBox.Enabled = false;
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

        public bool wholeWords
        {
            get
            {
                if (this.textBox2.Text == "^l" || this.textBox3.Text == "^l") return false;

                return this.replaceWholeWordCheckBox.Checked;
            }
        }

        private void doFindCB()
        {
            this.UpdateDTO();
            this.findCB();
        }

        private void doReplaceCB()
        {
            this.UpdateDTO();
            this.replaceCB();
        }

        public DlgSearch(SearchDialogData searchData, searchCallback findCB, searchCallback replaceCB)
        {
            InitializeComponent();
            this.second = this.tabControl1.TabPages[1];
            this.searchDTO = searchData;

            this.cbox1 = this.searchDTO.findSameCase;
            this.cbox2 = this.searchDTO.replaceWholeWord;
            this.cbox3 = this.searchDTO.replaceSameCase;
            this.txtFind = this.searchDTO.txtFind;
            this.findCB = findCB;
            this.replaceCB = replaceCB;
        }

        private void DlgSearch_KeyDown(object sender, KeyEventArgs e)
        {
            switch(e.KeyCode)
            {
                case Keys.F3:
                    e.SuppressKeyPress = true;
                    this.txtFind = this.textBox1.Text;
                    this.doFindCB(); // Callback FindFirst / FindNext
                    break;

                case Keys.Escape:
                    this.textBox1.Text = this.txtFind;
                    this.DialogResult = System.Windows.Forms.DialogResult.Cancel;
                    this.Close();
                    break;
            }
        }

        private void searchButton_Click(object sender, EventArgs e)
        {
            this.txtFind = this.textBox1.Text;
            this.doFindCB(); // Callback FindFirst / FindNext
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                this.DialogResult = this.searchButton.DialogResult;
                this.txtFind = this.textBox1.Text;
                this.doFindCB(); //  Callback FindFirst / FindNext
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

        private void replaceButton_Click(object sender, EventArgs e)
        {
            if (true == (this.replaceButton.Enabled = this.checkReplaceData()))
            {
                this.txtFind = this.textBox2.Text;
                this.fillDictionary();
                this.doReplaceCB(); // Callback Replace
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

        private void arrowButton_Click(object sender, EventArgs e)
        {
            string str = this.textBox2.Text;
            this.textBox2.Text = this.textBox3.Text;
            this.textBox3.Text = str;
            this.textBox2.Focus();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            this.replaceButton.Enabled = this.checkReplaceData();
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
            this.UpdateDTO();
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
                    this.searchButton.Enabled = true;
                }
                else this.searchButton.Enabled = false;
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
                this.DialogResult = this.replaceButton.DialogResult;
                this.fillDictionary();
                this.doReplaceCB();  // Callback 'Replace'
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            this.searchButton.Enabled = (this.textBox1.Text.Length > 0);
            this.txtFind = this.textBox1.Text;
            this.UpdateDTO();
        }

        private void findSameCaseCheckBox_Click(object sender, EventArgs e)
        {
            if (this.findSameCaseCheckBox.Enabled)
                this.oldCHeckBox = this.findSameCaseCheckBox.Checked;
            this.UpdateDTO();
        }

        private void DlgSearch_Resize(object sender, EventArgs e)
        {
            this.searchButton.Location =
            new Point((this.Size.Width - this.searchButton.Size.Width) / 2, this.searchButton.Location.Y);

            this.replaceButton.Location =
            new Point((this.Size.Width - this.replaceButton.Size.Width) / 2, this.replaceButton.Location.Y);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            this.UpdateDTO();
        }

        private void replaceWholeWordCheckBox_Click(object sender, EventArgs e)
        {
            this.UpdateDTO();
        }

        private void replaceSameCaseCheckBox_Click(object sender, EventArgs e)
        {
            this.UpdateDTO();
        }
    }
}