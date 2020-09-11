using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Text;
using ExtListBox1;
using System.Drawing.Drawing2D;

namespace ShubusViewer
{
    public partial class DlgView : Form
    {
        private FontFamily[] fontFamily;
        private FontStyle selectedStyle;
        private int selectedFamily;
        private FontStyle oldStyle;
        private int oldFamily;
        private int oldStyleIndex; // For listBox2.
        private RichTextBox myTextBox;
        private int oldBgIndex = -1;
        private int oldFgIndex = -1;

        public static bool AutoApplyColor = true;
        public static bool AutoApplyFont = true;
        /*
        public bool AutoApplyColor
        {
            get { return this.autoApplyColor; }
            set { this.autoApplyColor = value; }
        }

        public bool AutoApplyFont
        {
            get { return this.autoApplyFont; }
            set { this.autoApplyFont = value; }
        }
         * */

        public DlgView(RichTextBox appTextBox)
        {
            InitializeComponent();
            // Init Font list box:
            this.myTextBox = appTextBox;

            InstalledFontCollection collection = new InstalledFontCollection();
            this.fontFamily = collection.Families;

            foreach (FontFamily f in this.fontFamily)
            {
                listBox1.Items.Add(f.Name);
            }
            // Init color list boxes:
            string[] colorNames = System.Enum.GetNames(typeof(KnownColor));
            this.bgColorBox.Items.AddRange(colorNames);
            this.fontColorBox.Items.AddRange(colorNames);
            this.bgColorBox.Sorted = this.fontColorBox.Sorted = true;
        }

        private void setOldFont()
        {
            try
            {
                this.listBox1.SelectedIndex = this.oldFamily;
                this.listBox2.SelectedIndex = this.oldStyleIndex;
            }
            catch (Exception)
            {
                this.listBox1.SelectedIndex = 0;
                this.listBox2.SelectedIndex = 0;
            }
        }

        private void DlgView_Shown(object sender, EventArgs e)
        {
            int index = 0;
            foreach (FontFamily f in this.fontFamily)
            {
                if (myTextBox.Font.FontFamily.Name == f.Name)
                {
                    break;
                }
                index++;
            }
            this.oldFamily = index;
            this.oldStyle = myTextBox.Font.Style;
            this.setOldFont();
            this.checkBox1.Checked = DlgView.AutoApplyFont;
            this.checkBox2.Checked = DlgView.AutoApplyColor;

            // http://stackoverflow.com/questions/1137083/c-sharp-how-do-i-select-a-list-box-item-when-i-have-the-value-name-in-a-string
            try            
            {
                this.oldBgIndex = this.bgColorBox.FindString(this.myTextBox.BackColor.Name);
                this.oldFgIndex = this.fontColorBox.FindString(this.myTextBox.ForeColor.Name);
                this.bgColorBox.SetSelected(oldBgIndex, true);
                this.fontColorBox.SetSelected(oldFgIndex, true);
            }
            catch (Exception) { }
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            listBox2.Items.Clear();
            Array styles = Enum.GetValues(typeof(FontStyle));
            this.selectedFamily = listBox1.SelectedIndex;

            for (int i = 0, ind = 0; i < styles.Length; i++)
            {
                if (fontFamily[selectedFamily].IsStyleAvailable((FontStyle)styles.GetValue(i)))
                {
                    listBox2.Items.Add(styles.GetValue(i));

                    if (listBox1.SelectedIndex == this.oldFamily)
                    {
                        if (this.oldStyle.ToString() == styles.GetValue(i).ToString())
                        {
                            listBox2.SelectedIndex = ind;
                            this.oldStyleIndex = ind;
                        }
                    }
                    else
                    {
                        listBox2.SelectedIndex = 0;
                    }
                    ind++;
                }
            }          
        }

        private void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.selectedStyle = (FontStyle)listBox2.SelectedItem;

            try
            {
                if (this.checkBox1.Checked)
                {
                    Font newFont = new Font(fontFamily[selectedFamily]
                    , myTextBox.Font.Size, selectedStyle);

                    if (this.myTextBox.Font.Name != newFont.Name || this.myTextBox.Font.Style != selectedStyle)
                    {
                        this.myTextBox.Font = newFont;
                    }
                }
                this.textBox1.Font = new Font(fontFamily[selectedFamily]
                  , myTextBox.Font.Size, selectedStyle);
            }
            catch (Exception) { }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            bool old = this.checkBox1.Checked;
            this.checkBox1.Checked = true;
            this.setOldFont();
            this.checkBox1.Checked = old;
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.button2.Enabled = !(this.checkBox1.Checked);

            if (this.checkBox1.Checked)
            {
                this.myTextBox.Font = new Font(fontFamily[selectedFamily]
                , myTextBox.Font.Size, selectedStyle);
            }
            this.listBox1.Focus();
            DlgView.AutoApplyFont = this.checkBox1.Checked;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.myTextBox.Font = new Font(fontFamily[selectedFamily]
            , myTextBox.Font.Size, selectedStyle);
        }

        private void DlgView_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.oldStyleIndex = this.listBox2.SelectedIndex;
        }

        private void DlgView_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    this.button1_Click(sender, null);
                    this.Close();
                    break;

                case Keys.Enter:
                    this.Close();
                    break;
            }
        }

        private void bgColorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Color color = this.getColorFromExtListBox((ExtListBox)sender);

            try
            {
                if (this.checkBox2.Checked)
                {
                    this.myTextBox.BackColor = color;
                }
                this.textBox1.BackColor = color;
            }
            catch (Exception) { }
        }

        private void fontColorBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            Color color = this.getColorFromExtListBox((ExtListBox)sender);

            try
            {
                if (this.checkBox2.Checked)
                {
                    this.myTextBox.ForeColor = color;
                }
                this.textBox1.ForeColor = color;
            }
            catch (Exception) { }
        }
        // Gets selected color:
        private Color getColorFromExtListBox(ExtListBox listBox)
        {
            Color color = SystemColors.Window;
            try
            {
                KnownColor selectedColor = (KnownColor)System.Enum.Parse(typeof(KnownColor), listBox.Text);
                color = System.Drawing.Color.FromKnownColor(selectedColor);
            }
            catch (Exception) { }
            return color;
        }
        // Apply selected colors:
        private void button3_Click(object sender, EventArgs e)
        {
            this.myTextBox.BackColor = this.getColorFromExtListBox(this.bgColorBox);
            this.myTextBox.ForeColor = this.getColorFromExtListBox(this.fontColorBox);
        }

        private void checkBox2_CheckedChanged(object sender, EventArgs e)
        {
            this.button3.Enabled = !(this.checkBox2.Checked);

            if (this.checkBox2.Checked)
            {
                this.bgColorBox_SelectedIndexChanged(this.bgColorBox, null);
                this.fontColorBox_SelectedIndexChanged(this.fontColorBox, null);
            }
            DlgView.AutoApplyColor = this.checkBox2.Checked;
        }

        private void buttonRestColors_Click(object sender, EventArgs e)
        {
            this.restoreColors();
        }

        private void restoreColors()
        {
            try
            {
                bool old = this.checkBox2.Checked;
                this.checkBox2.Checked = true;
                this.bgColorBox.SetSelected(oldBgIndex, true);
                this.fontColorBox.SetSelected(oldFgIndex, true);
                this.checkBox2.Checked = old;
            }
            catch (Exception) { }
        }

        private void btDone_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void btCancel_Click(object sender, EventArgs e)
        {
            bool old1 = this.checkBox1.Checked;
            bool old2 = this.checkBox2.Checked;

            this.checkBox2.Checked = this.checkBox1.Checked = true;
            this.setOldFont();
            this.restoreColors();
            this.checkBox1.Checked = old1;
            this.checkBox2.Checked = old2;
            this.Close();
        }
    }
}