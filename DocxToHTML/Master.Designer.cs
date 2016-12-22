namespace DocxToHTML
{
    partial class Master
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Master));
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.btnSaveHTML = new System.Windows.Forms.Button();
            this.btnConvert = new System.Windows.Forms.Button();
            this.lblFileName = new System.Windows.Forms.Label();
            this.btnOpenFile = new System.Windows.Forms.Button();
            this.wbHTMLViewer = new System.Windows.Forms.WebBrowser();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.btnSaveHTML);
            this.splitContainer1.Panel1.Controls.Add(this.btnConvert);
            this.splitContainer1.Panel1.Controls.Add(this.lblFileName);
            this.splitContainer1.Panel1.Controls.Add(this.btnOpenFile);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.wbHTMLViewer);
            this.splitContainer1.Size = new System.Drawing.Size(734, 457);
            this.splitContainer1.SplitterDistance = 168;
            this.splitContainer1.TabIndex = 0;
            // 
            // btnSaveHTML
            // 
            this.btnSaveHTML.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnSaveHTML.Location = new System.Drawing.Point(31, 267);
            this.btnSaveHTML.Name = "btnSaveHTML";
            this.btnSaveHTML.Size = new System.Drawing.Size(99, 23);
            this.btnSaveHTML.TabIndex = 3;
            this.btnSaveHTML.Text = "Save HTML";
            this.btnSaveHTML.UseVisualStyleBackColor = true;
            this.btnSaveHTML.Click += new System.EventHandler(this.btnSaveHTML_Click);
            // 
            // btnConvert
            // 
            this.btnConvert.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnConvert.Location = new System.Drawing.Point(31, 222);
            this.btnConvert.Name = "btnConvert";
            this.btnConvert.Size = new System.Drawing.Size(99, 23);
            this.btnConvert.TabIndex = 2;
            this.btnConvert.Text = "Convert to HTML";
            this.btnConvert.UseVisualStyleBackColor = true;
            this.btnConvert.Click += new System.EventHandler(this.btnConvert_Click);
            // 
            // lblFileName
            // 
            this.lblFileName.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.lblFileName.AutoSize = true;
            this.lblFileName.Location = new System.Drawing.Point(40, 186);
            this.lblFileName.Name = "lblFileName";
            this.lblFileName.Size = new System.Drawing.Size(80, 13);
            this.lblFileName.TabIndex = 1;
            this.lblFileName.Text = "No file selected";
            // 
            // btnOpenFile
            // 
            this.btnOpenFile.Anchor = System.Windows.Forms.AnchorStyles.None;
            this.btnOpenFile.Location = new System.Drawing.Point(31, 145);
            this.btnOpenFile.Name = "btnOpenFile";
            this.btnOpenFile.Size = new System.Drawing.Size(99, 23);
            this.btnOpenFile.TabIndex = 0;
            this.btnOpenFile.Text = "Open File";
            this.btnOpenFile.UseVisualStyleBackColor = true;
            this.btnOpenFile.Click += new System.EventHandler(this.btnOpenFile_Click);
            // 
            // wbHTMLViewer
            // 
            this.wbHTMLViewer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.wbHTMLViewer.Location = new System.Drawing.Point(0, 0);
            this.wbHTMLViewer.MinimumSize = new System.Drawing.Size(20, 20);
            this.wbHTMLViewer.Name = "wbHTMLViewer";
            this.wbHTMLViewer.Size = new System.Drawing.Size(562, 457);
            this.wbHTMLViewer.TabIndex = 0;
            // 
            // Master
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(734, 457);
            this.Controls.Add(this.splitContainer1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Master";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Converter";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel1.PerformLayout();
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.WebBrowser wbHTMLViewer;
        private System.Windows.Forms.Button btnOpenFile;
        private System.Windows.Forms.Label lblFileName;
        private System.Windows.Forms.Button btnConvert;
        private System.Windows.Forms.Button btnSaveHTML;
    }
}

