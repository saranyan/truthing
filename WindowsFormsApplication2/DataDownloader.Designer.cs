namespace EffiaTruther
{
    partial class DataDownloader
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DataDownloader));
            this.txtUrl = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.tasksToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadCLASSSUBCLASSFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.extractImageLinksFromURLFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openFileDialog1 = new System.Windows.Forms.OpenFileDialog();
            this.txtClass = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.textBox2 = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBox3 = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.txtData = new System.Windows.Forms.TextBox();
            this.axWebBrowser1 = new AxSHDocVw.AxWebBrowser();
            this.button1 = new System.Windows.Forms.Button();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axWebBrowser1)).BeginInit();
            this.SuspendLayout();
            // 
            // txtUrl
            // 
            this.txtUrl.Location = new System.Drawing.Point(67, 27);
            this.txtUrl.Name = "txtUrl";
            this.txtUrl.Size = new System.Drawing.Size(736, 20);
            this.txtUrl.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(43, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "URL ID";
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(15, 161);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(788, 115);
            this.richTextBox1.TabIndex = 2;
            this.richTextBox1.Text = "";
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.tasksToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(827, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // tasksToolStripMenuItem
            // 
            this.tasksToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.loadCLASSSUBCLASSFileToolStripMenuItem,
            this.extractImageLinksFromURLFileToolStripMenuItem});
            this.tasksToolStripMenuItem.Name = "tasksToolStripMenuItem";
            this.tasksToolStripMenuItem.Size = new System.Drawing.Size(46, 20);
            this.tasksToolStripMenuItem.Text = "Tasks";
            // 
            // loadCLASSSUBCLASSFileToolStripMenuItem
            // 
            this.loadCLASSSUBCLASSFileToolStripMenuItem.Name = "loadCLASSSUBCLASSFileToolStripMenuItem";
            this.loadCLASSSUBCLASSFileToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.loadCLASSSUBCLASSFileToolStripMenuItem.Text = "Extract links from CLASS SUBCLASS file";
            this.loadCLASSSUBCLASSFileToolStripMenuItem.Click += new System.EventHandler(this.loadCLASSSUBCLASSFileToolStripMenuItem_Click);
            // 
            // extractImageLinksFromURLFileToolStripMenuItem
            // 
            this.extractImageLinksFromURLFileToolStripMenuItem.Name = "extractImageLinksFromURLFileToolStripMenuItem";
            this.extractImageLinksFromURLFileToolStripMenuItem.Size = new System.Drawing.Size(272, 22);
            this.extractImageLinksFromURLFileToolStripMenuItem.Text = "Extract image links from URL file";
            this.extractImageLinksFromURLFileToolStripMenuItem.Click += new System.EventHandler(this.extractImageLinksFromURLFileToolStripMenuItem_Click);
            // 
            // openFileDialog1
            // 
            this.openFileDialog1.FileName = "openFileDialog1";
            // 
            // txtClass
            // 
            this.txtClass.Location = new System.Drawing.Point(700, 55);
            this.txtClass.Name = "txtClass";
            this.txtClass.Size = new System.Drawing.Size(100, 20);
            this.txtClass.TabIndex = 5;
            this.txtClass.Text = "330";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(651, 58);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(46, 13);
            this.label2.TabIndex = 6;
            this.label2.Text = "Class ID";
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(89, 70);
            this.textBox1.Name = "textBox1";
            this.textBox1.Size = new System.Drawing.Size(275, 20);
            this.textBox1.TabIndex = 7;
            this.textBox1.Text = "c:\\\\Workspace\\\\data\\\\images\\\\";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(12, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(71, 13);
            this.label3.TabIndex = 8;
            this.label3.Text = "Output PATH";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.ForeColor = System.Drawing.Color.DarkRed;
            this.label6.Location = new System.Drawing.Point(604, 117);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(81, 13);
            this.label6.TabIndex = 27;
            this.label6.Text = "DB Table name";
            // 
            // textBox2
            // 
            this.textBox2.Location = new System.Drawing.Point(414, 114);
            this.textBox2.Name = "textBox2";
            this.textBox2.Size = new System.Drawing.Size(184, 20);
            this.textBox2.TabIndex = 26;
            this.textBox2.Text = "Patents";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.ForeColor = System.Drawing.Color.DarkRed;
            this.label5.Location = new System.Drawing.Point(604, 88);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(72, 13);
            this.label5.TabIndex = 25;
            this.label5.Text = "ADO DB path";
            // 
            // textBox3
            // 
            this.textBox3.Location = new System.Drawing.Point(414, 88);
            this.textBox3.Name = "textBox3";
            this.textBox3.Size = new System.Drawing.Size(184, 20);
            this.textBox3.TabIndex = 24;
            this.textBox3.Text = "C:\\\\Workspace\\\\patents.mdb";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.ForeColor = System.Drawing.Color.DarkRed;
            this.label4.Location = new System.Drawing.Point(604, 61);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 13);
            this.label4.TabIndex = 23;
            this.label4.Text = "Path";
            // 
            // txtData
            // 
            this.txtData.Location = new System.Drawing.Point(414, 58);
            this.txtData.Name = "txtData";
            this.txtData.Size = new System.Drawing.Size(184, 20);
            this.txtData.TabIndex = 22;
            this.txtData.Text = "C:\\\\Workspace\\\\Images_Patent\\\\";
            // 
            // axWebBrowser1
            // 
            this.axWebBrowser1.Enabled = true;
            this.axWebBrowser1.Location = new System.Drawing.Point(15, 297);
            this.axWebBrowser1.OcxState = ((System.Windows.Forms.AxHost.State)(resources.GetObject("axWebBrowser1.OcxState")));
            this.axWebBrowser1.Size = new System.Drawing.Size(785, 207);
            this.axWebBrowser1.TabIndex = 28;
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(725, 519);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 29;
            this.button1.Text = "Done";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // DataDownloader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(827, 641);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.axWebBrowser1);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.textBox2);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.textBox3);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.txtData);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.txtClass);
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.txtUrl);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "DataDownloader";
            this.Text = "DataDownloader";
            this.Load += new System.EventHandler(this.DataDownloader_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.axWebBrowser1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem tasksToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadCLASSSUBCLASSFileToolStripMenuItem;
        private System.Windows.Forms.OpenFileDialog openFileDialog1;
        private System.Windows.Forms.TextBox txtClass;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.SaveFileDialog saveFileDialog1;
        private System.Windows.Forms.ToolStripMenuItem extractImageLinksFromURLFileToolStripMenuItem;
        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox textBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox textBox3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox txtData;
        private AxSHDocVw.AxWebBrowser axWebBrowser1;
        private System.Windows.Forms.Button button1;
    }
}