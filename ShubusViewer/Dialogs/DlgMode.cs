using System;
using System.Drawing;
using System.Windows.Forms;
using StateMachine;

namespace ShubusViewer
{
    public partial class DlgMode : Form
    {
        public bool autoClose = true;
        private AppController myController;
        private ToolStripMenuItem disabled;

        public DlgMode(AppController aController)
        {
            InitializeComponent();
            this.myController = aController;
        }

        private void modeManager(object sender)
        {
            if (this.disabled != null)
                this.disabled.Enabled = true;

            ((ToolStripMenuItem)sender).Enabled = false;
            this.disabled = (ToolStripMenuItem)sender;

            if (this.checkBox1.Checked)
                this.Close();
        }

        private void hexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.myController.processOperation(TypeAction.EModeHex);
            this.modeManager(sender);
        }

        private void textToolStripMenuItem_Click(object sender, EventArgs e)
        {     
            this.myController.processOperation(TypeAction.EModeText);
            this.modeManager(sender);
        }

        private void DlgModeSelect_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape) this.Close(); // this.KeyPreview = true;
        }

        private void browserToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.myController.processOperation(TypeAction.EModeExplorer);
            this.modeManager(sender);
        }

        private void binToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.myController.processOperation(TypeAction.EModeDumpBin);
            this.modeManager(sender);
        }

        private void button1_Click(object sender, EventArgs e)        
        {
            this.Close();
        }

        private Color oldColor = Color.White;

        private void button1_MouseEnter(object sender, EventArgs e)
        {
            this.oldColor = this.button1.BackColor;
            this.button1.BackColor = Color.FromArgb(148, 156, 176);
        }

        private void button1_MouseLeave(object sender, EventArgs e)
        {
            this.button1.BackColor = this.oldColor;
        }

        private void otherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.otherToolStripMenuItem.Enabled = false;

            MessageBox.Show(ExtendedData.Constants.STR_NOPLUGINS_MSG, ExtendedData.Constants.STR_APP_TITLE
              , MessageBoxButtons.OK, MessageBoxIcon.Asterisk);
        }

        private void DlgMode_Shown(object sender, EventArgs e)
        {
            if (this.disabled != null)
                this.disabled.Enabled = true;

            this.otherToolStripMenuItem.Enabled = true;            
            this.checkBox1.Checked = this.autoClose;
            //this.menuStrip1.Focus(); - CR 030 fix.
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            this.autoClose = this.checkBox1.Checked;
        }
        // Idea: http://stackoverflow.com/questions/298491/how-do-i-close-a-form-when-a-user-clicks-outside-the-forms-window
    }
}
