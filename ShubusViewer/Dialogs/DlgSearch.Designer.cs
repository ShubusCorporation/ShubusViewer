namespace ShubusViewer
{
    partial class DlgSearch
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
			this.components = new System.ComponentModel.Container();
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DlgSearch));
			this.textBox1 = new System.Windows.Forms.TextBox();
			this.searchButton = new System.Windows.Forms.Button();
			this.findSameCaseCheckBox = new System.Windows.Forms.CheckBox();
			this.groupBox1 = new System.Windows.Forms.GroupBox();
			this.tabControl1 = new System.Windows.Forms.TabControl();
			this.tabPage1 = new System.Windows.Forms.TabPage();
			this.tabPage2 = new System.Windows.Forms.TabPage();
			this.replaceSameCaseCheckBox = new System.Windows.Forms.CheckBox();
			this.replaceWholeWordCheckBox = new System.Windows.Forms.CheckBox();
			this.replaceButton = new System.Windows.Forms.Button();
			this.groupBox2 = new System.Windows.Forms.GroupBox();
			this.arrowButton = new System.Windows.Forms.Button();
			this.textBox3 = new System.Windows.Forms.TextBox();
			this.textBox2 = new System.Windows.Forms.TextBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label1 = new System.Windows.Forms.Label();
			this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
			this.groupBox1.SuspendLayout();
			this.tabControl1.SuspendLayout();
			this.tabPage1.SuspendLayout();
			this.tabPage2.SuspendLayout();
			this.groupBox2.SuspendLayout();
			this.SuspendLayout();
			// 
			// textBox1
			// 
			this.textBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox1.Location = new System.Drawing.Point(6, 19);
			this.textBox1.MaxLength = 2048;
			this.textBox1.Multiline = true;
			this.textBox1.Name = "textBox1";
			this.textBox1.Size = new System.Drawing.Size(365, 65);
			this.textBox1.TabIndex = 0;
			this.textBox1.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
			this.textBox1.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox1_KeyDown);
			// 
			// searchButton
			// 
			this.searchButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.searchButton.Location = new System.Drawing.Point(156, 127);
			this.searchButton.Name = "searchButton";
			this.searchButton.Size = new System.Drawing.Size(75, 23);
			this.searchButton.TabIndex = 1;
			this.searchButton.Text = "Ok";
			this.searchButton.UseVisualStyleBackColor = true;
			this.searchButton.Click += new System.EventHandler(this.searchButton_Click);
			// 
			// findSameCaseCheckBox
			// 
			this.findSameCaseCheckBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
			this.findSameCaseCheckBox.AutoSize = true;
			this.findSameCaseCheckBox.Location = new System.Drawing.Point(6, 108);
			this.findSameCaseCheckBox.Name = "findSameCaseCheckBox";
			this.findSameCaseCheckBox.Size = new System.Drawing.Size(132, 17);
			this.findSameCaseCheckBox.TabIndex = 2;
			this.findSameCaseCheckBox.Text = "In the same case only.";
			this.findSameCaseCheckBox.UseVisualStyleBackColor = true;
			this.findSameCaseCheckBox.Click += new System.EventHandler(this.findSameCaseCheckBox_Click);
			// 
			// groupBox1
			// 
			this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox1.Controls.Add(this.textBox1);
			this.groupBox1.Location = new System.Drawing.Point(4, 6);
			this.groupBox1.Name = "groupBox1";
			this.groupBox1.Size = new System.Drawing.Size(378, 96);
			this.groupBox1.TabIndex = 3;
			this.groupBox1.TabStop = false;
			this.groupBox1.Text = " Enter text, then press F3 to search : ";
			// 
			// tabControl1
			// 
			this.tabControl1.Controls.Add(this.tabPage1);
			this.tabControl1.Controls.Add(this.tabPage2);
			this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tabControl1.Location = new System.Drawing.Point(0, 0);
			this.tabControl1.Name = "tabControl1";
			this.tabControl1.SelectedIndex = 0;
			this.tabControl1.Size = new System.Drawing.Size(394, 182);
			this.tabControl1.TabIndex = 4;
			this.tabControl1.SelectedIndexChanged += new System.EventHandler(this.tabControl1_SelectedIndexChanged);
			this.tabControl1.Selected += new System.Windows.Forms.TabControlEventHandler(this.tabControl1_Selected);
			// 
			// tabPage1
			// 
			this.tabPage1.Controls.Add(this.groupBox1);
			this.tabPage1.Controls.Add(this.searchButton);
			this.tabPage1.Controls.Add(this.findSameCaseCheckBox);
			this.tabPage1.Location = new System.Drawing.Point(4, 22);
			this.tabPage1.Name = "tabPage1";
			this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage1.Size = new System.Drawing.Size(386, 156);
			this.tabPage1.TabIndex = 0;
			this.tabPage1.Text = "Find";
			this.tabPage1.UseVisualStyleBackColor = true;
			// 
			// tabPage2
			// 
			this.tabPage2.Controls.Add(this.replaceSameCaseCheckBox);
			this.tabPage2.Controls.Add(this.replaceWholeWordCheckBox);
			this.tabPage2.Controls.Add(this.replaceButton);
			this.tabPage2.Controls.Add(this.groupBox2);
			this.tabPage2.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.tabPage2.Location = new System.Drawing.Point(4, 22);
			this.tabPage2.Name = "tabPage2";
			this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
			this.tabPage2.Size = new System.Drawing.Size(386, 156);
			this.tabPage2.TabIndex = 1;
			this.tabPage2.Text = "Find & Replace";
			this.tabPage2.UseVisualStyleBackColor = true;
			// 
			// replaceSameCaseCheckBox
			// 
			this.replaceSameCaseCheckBox.AutoSize = true;
			this.replaceSameCaseCheckBox.Location = new System.Drawing.Point(112, 104);
			this.replaceSameCaseCheckBox.Name = "replaceSameCaseCheckBox";
			this.replaceSameCaseCheckBox.Size = new System.Drawing.Size(132, 17);
			this.replaceSameCaseCheckBox.TabIndex = 3;
			this.replaceSameCaseCheckBox.Text = "In the same case only.";
			this.replaceSameCaseCheckBox.UseVisualStyleBackColor = true;
			this.replaceSameCaseCheckBox.Click += new System.EventHandler(this.replaceSameCaseCheckBox_Click);
			// 
			// replaceWholeWordCheckBox
			// 
			this.replaceWholeWordCheckBox.AutoSize = true;
			this.replaceWholeWordCheckBox.Location = new System.Drawing.Point(13, 104);
			this.replaceWholeWordCheckBox.Name = "replaceWholeWordCheckBox";
			this.replaceWholeWordCheckBox.Size = new System.Drawing.Size(88, 17);
			this.replaceWholeWordCheckBox.TabIndex = 2;
			this.replaceWholeWordCheckBox.Text = "Whole words";
			this.replaceWholeWordCheckBox.UseVisualStyleBackColor = true;
			this.replaceWholeWordCheckBox.Click += new System.EventHandler(this.replaceWholeWordCheckBox_Click);
			// 
			// replaceButton
			// 
			this.replaceButton.Enabled = false;
			this.replaceButton.Location = new System.Drawing.Point(156, 125);
			this.replaceButton.Name = "replaceButton";
			this.replaceButton.Size = new System.Drawing.Size(91, 23);
			this.replaceButton.TabIndex = 1;
			this.replaceButton.Text = "Replace all";
			this.replaceButton.UseVisualStyleBackColor = true;
			this.replaceButton.Click += new System.EventHandler(this.replaceButton_Click);
			// 
			// groupBox2
			// 
			this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.groupBox2.Controls.Add(this.arrowButton);
			this.groupBox2.Controls.Add(this.textBox3);
			this.groupBox2.Controls.Add(this.textBox2);
			this.groupBox2.Controls.Add(this.label2);
			this.groupBox2.Controls.Add(this.label1);
			this.groupBox2.Location = new System.Drawing.Point(4, 15);
			this.groupBox2.Name = "groupBox2";
			this.groupBox2.Size = new System.Drawing.Size(378, 83);
			this.groupBox2.TabIndex = 0;
			this.groupBox2.TabStop = false;
			this.groupBox2.Text = " Quick replace : ";
			// 
			// arrowButton
			// 
			this.arrowButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
			this.arrowButton.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
			this.arrowButton.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
			this.arrowButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
			this.arrowButton.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(204)));
			this.arrowButton.Image = ((System.Drawing.Image)(resources.GetObject("arrowButton.Image")));
			this.arrowButton.Location = new System.Drawing.Point(352, 32);
			this.arrowButton.Name = "arrowButton";
			this.arrowButton.Size = new System.Drawing.Size(22, 24);
			this.arrowButton.TabIndex = 4;
			this.toolTip1.SetToolTip(this.arrowButton, "Exchange");
			this.arrowButton.UseVisualStyleBackColor = true;
			this.arrowButton.Click += new System.EventHandler(this.arrowButton_Click);
			// 
			// textBox3
			// 
			this.textBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox3.Location = new System.Drawing.Point(53, 49);
			this.textBox3.Name = "textBox3";
			this.textBox3.Size = new System.Drawing.Size(297, 20);
			this.textBox3.TabIndex = 3;
			this.textBox3.TextChanged += new System.EventHandler(this.textBox3_TextChanged);
			this.textBox3.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox3_KeyDown);
			// 
			// textBox2
			// 
			this.textBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
			this.textBox2.Location = new System.Drawing.Point(53, 20);
			this.textBox2.Name = "textBox2";
			this.textBox2.Size = new System.Drawing.Size(297, 20);
			this.textBox2.TabIndex = 2;
			this.textBox2.TextChanged += new System.EventHandler(this.textBox2_TextChanged);
			this.textBox2.KeyDown += new System.Windows.Forms.KeyEventHandler(this.textBox2_KeyDown);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(6, 49);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(38, 13);
			this.label2.TabIndex = 1;
			this.label2.Text = "With : ";
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(7, 20);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(39, 13);
			this.label1.TabIndex = 0;
			this.label1.Text = "What :";
			// 
			// DlgSearch
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(394, 182);
			this.Controls.Add(this.tabControl1);
			this.KeyPreview = true;
			this.Location = new System.Drawing.Point(500, 400);
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "DlgSearch";
			this.ShowIcon = false;
			this.ShowInTaskbar = false;
			this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			this.Text = "Search text:";
			this.Shown += new System.EventHandler(this.DlgSearch_Shown);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.DlgSearch_KeyDown);
			this.Resize += new System.EventHandler(this.DlgSearch_Resize);
			this.groupBox1.ResumeLayout(false);
			this.groupBox1.PerformLayout();
			this.tabControl1.ResumeLayout(false);
			this.tabPage1.ResumeLayout(false);
			this.tabPage1.PerformLayout();
			this.tabPage2.ResumeLayout(false);
			this.tabPage2.PerformLayout();
			this.groupBox2.ResumeLayout(false);
			this.groupBox2.PerformLayout();
			this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.CheckBox findSameCaseCheckBox;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.TabControl tabControl1;
        private System.Windows.Forms.TabPage tabPage1;
        private System.Windows.Forms.TabPage tabPage2;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button replaceButton;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.CheckBox replaceSameCaseCheckBox;
        private System.Windows.Forms.CheckBox replaceWholeWordCheckBox;
        private System.Windows.Forms.Button arrowButton;
        private System.Windows.Forms.ToolTip toolTip1;
    }
}