using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using StateMachine;
using ExtendedData;

namespace ShubusViewer
{
    public partial class DlgDecoder : Form
    {
        private AppController appController;
        private Encoding newEncoding;
        private Encoding oldEncoding;
        private EncodingInfo[] eInfoList;
        private int selectedListIndex;
        private Dictionary<string, Encoding> rbMapping;
        private string textReserve;
        private delegate void encodeCurrentText();
        private encodeCurrentText doEncoding;

        public DlgDecoder(AppController aController)
        {
            InitializeComponent();

            this.dlgComboBoxItem = staticDlgComboBoxItem;
            this.dlgManualItem = staticDlgManualItem;

            this.appController = aController;
            this.doEncoding = () => { };
            this.label1.Text = Constants.STR_NOTDEFINED_MSG;
        }

        // For init on application startup.
        static public int staticDlgComboBoxItem = 0;
        static public string staticDlgManualItem = string.Empty;

        public int dlgComboBoxItem
        {
            get { return this.encodingList.SelectedIndex; }
            set { this.selectedListIndex = value; }
        }

        public string dlgManualItem
        {
            get { return this.textBox1.Text; }
            set { this.textBox1.Text = value; }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void radioButton15_CheckedChanged(object sender, EventArgs e)
        {
            if (sender is RadioButton && ((RadioButton)sender).Checked)
            {
                ((Control)sender).Parent.Tag = sender;
                this.rbManual.Checked = this.rbTotalList.Checked = false;             
                appController.curEncoding = this.rbMapping[((RadioButton)sender).Name];
                this.doEncoding();
            }
        }

        private void rbManual_CheckedChanged(object sender, EventArgs e)
        {
            if (this.rbManual.Checked)
            {
                this.rbTotalList.Checked = false;
                ((RadioButton)groupBox1.Tag).Checked = false;
                this.button2_Click(sender, e);
                this.textBox1.Focus();
            }
            else this.button2.Enabled = false;
        }

        private void rbTotalList_CheckedChanged(object sender, EventArgs e)
        {
		    if (this.encodingList.SelectedIndex < 0)
                this.rbTotalList.Checked = false;

			if (this.rbTotalList.Checked)
            {
                this.rbManual.Checked = false;
                ((RadioButton)groupBox1.Tag).Checked = false;

                this.newEncoding = Encoding.GetEncoding(
                    this.eInfoList[this.encodingList.SelectedIndex].CodePage);

                appController.curEncoding = this.newEncoding;
                this.doEncoding();
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.button2.Enabled = false;

            if (this.textBox1.Text != "")
            {
                try
                {
                    int num = System.Convert.ToInt32(this.textBox1.Text); // CR 049
                    this.newEncoding = Encoding.GetEncoding(num);
                }
                catch (Exception)
                {
                    try
                    {
                        this.newEncoding = Encoding.GetEncoding(this.textBox1.Text);
                    }
                    catch (Exception e1)
                    {
                        this.label1.Text = Constants.STR_NOTDEFINED_MSG;
                        MessageBox.Show(e1.Message, Constants.STR_APP_TITLE
                            , MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        return;
                    }
                }
                this.label1.Text = "Applied: № " + this.newEncoding.CodePage + " - " + this.newEncoding.EncodingName;
                appController.curEncoding = this.newEncoding;
                this.doEncoding();
            }
            else this.label1.Text = Constants.STR_NOTDEFINED_MSG;
        }

        private void DlgDecoder_Shown(object sender, EventArgs e)
        {
            if (null == this.rbMapping)
            {
               this.initDlgStructs();
            }
            this.oldEncoding = appController.curEncoding ?? Encoding.Default;

            this.rbMapping[this.radioButton15.Name] = this.oldEncoding;
            this.radioButton15.Text = Utils.Utils.GetShortEncodingName(this.oldEncoding);

            this.Text = (this.appController.isFileChanged) ?
                Constants.STR_PREVIEWTITLE_DLG : Constants.STR_ENCODETITLE_DLG;
            this.btnAuto.Enabled = !this.appController.isFileChanged;

            if (this.appController.sharedData.fileType == TypeFileFormat.ESimpleText)
            {
                if (this.appController.isFileChanged) // Preview mode
                {
                    this.textReserve = this.appController.sharedData.basicInfo.container;
                    this.doEncoding = () =>
                    {
                        this.appController.sharedData.basicInfo.container = this.textReserve;
                        this.appController.processOperation(TypeAction.EPreview);
                    };
                    // Force rise up Encode action in the Preview dialog mode.
                    if (this.radioButton15.Checked == false)
                    {
                        this.radioButton15.Checked = true;
                    }
                    else
                    {
                        radioButton15_CheckedChanged((object)this.radioButton15, null);
                    }
                }
                else
                {
                    this.doEncoding = () => { };
                    this.radioButton15.Checked = true;

                    if (this.appController.sharedData.basicInfo.binary) // Check for Dump mode.
                    {
                        this.doEncoding = () => appController.processOperation(TypeAction.EDumpUpdate);
                    }
                    else
                    {
                        this.doEncoding = () => appController.processOperation(TypeAction.EEncode);
                    }
                }
            }
            else
            {
                this.doEncoding = () => { }; // Encode nothing in non text mode.
                this.radioButton15.Checked = true;
            }
            this.button2.Enabled = false;
        }

        private void initDlgStructs()
        {
            this.eInfoList = Encoding.GetEncodings();
            this.rbMapping = new Dictionary<string,Encoding>();

            this.rbMapping[this.radioButton1.Name] = Encoding.Unicode;
            this.rbMapping[this.radioButton2.Name] = Encoding.BigEndianUnicode;
            this.rbMapping[this.radioButton3.Name] = Encoding.UTF8;
            this.rbMapping[this.radioButton4.Name] = Encoding.UTF7;
            this.rbMapping[this.radioButton16.Name] = Encoding.ASCII;

            Dictionary<RadioButton, int> localMapping = new Dictionary<RadioButton, int>();

            localMapping[this.radioButton5] = 1250;
            localMapping[this.radioButton6] = 1251;
            localMapping[this.radioButton7] = 1252;
            localMapping[this.radioButton8] = 1253;
            localMapping[this.radioButton9] = 1254;
            localMapping[this.radioButton10] = 855;
            localMapping[this.radioButton11] = 866;
            localMapping[this.radioButton12] = 850;
            localMapping[this.radioButton13] = 852;

            localMapping[this.radioButton14] = 12000;
            localMapping[this.radioButton17] = 12001;
            localMapping[this.radioButton18] = 1257;
            localMapping[this.radioButton19] = 10000;
            localMapping[this.radioButton20] = 10029;
            localMapping[this.radioButton21] = 10007;

            foreach (var pair in localMapping)
            {
                try
                {
                    rbMapping[pair.Key.Name] = Encoding.GetEncoding(pair.Value);
                }
                catch (Exception)
                {
                    rbMapping[pair.Key.Name] = Encoding.Default;
                    pair.Key.Enabled = false;
                }
            }
            foreach (EncodingInfo el in this.eInfoList)
            {
                this.encodingList.Items.Add(el.CodePage + "  :  " + el.Name
                    + "  :  " + el.DisplayName);
            }
            try
            {
                if (this.selectedListIndex < 0)
                {
                    this.encodingList.SelectedIndex = 0;
                }
                else
                {
                    this.encodingList.SelectedIndex = this.selectedListIndex;
                }
            }
            catch (Exception)
            {
                try { this.encodingList.SelectedIndex = -1; }
                catch (Exception) { }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (this.appController.isFileChanged)
            {
                this.doEncoding = () => { };
                this.appController.sharedData.basicInfo.container = this.textReserve;
                this.appController.processOperation(TypeAction.ELastAction);
            }
            this.radioButton15.Checked = true;
            this.Close();
        }

        private void encodingList_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (this.rbTotalList.Checked)
            {
                try
                {
                    this.newEncoding = Encoding.GetEncoding(
                        this.eInfoList[this.encodingList.SelectedIndex].CodePage);
                }
                catch (Exception e1)
                {
                    MessageBox.Show(e1.Message, Constants.STR_APP_TITLE
                    , MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                this.appController.curEncoding = this.newEncoding;
                this.doEncoding();
            }
        }

        private void groupBox1_Layout(object sender, LayoutEventArgs e)
        {
            foreach (Control rb in groupBox1.Controls)
                if (rb is RadioButton)
                {
                    if (((RadioButton)rb).Checked)
                    {
                        groupBox1.Tag = rb;
                        break;
                    }
                }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            if (this.rbManual.Checked)
                this.button2.Enabled = true;

            this.label1.Text = Constants.STR_NOTDEFINED_MSG;

            if (this.textBox1.Text.Length == 0)
                this.button2.Enabled = false;
        }

        private void textBox1_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                if (this.rbManual.Checked == false)
                {
                    this.rbManual.Checked = true;
                }
                else if (this.button2.Enabled)
                {
                    this.button2_Click(sender, null);
                }
                else this.Close();
            }
        }

        private void btnAuto_Click(object sender, EventArgs e)
        {
            var en = appController.processOperation(TypeGetObject.EGetEncoding);
            this.rbManual.Checked = true;
            this.textBox1.Text = (en as Encoding).CodePage.ToString();
            button2_Click(this.button2, null);
        }
    }
}