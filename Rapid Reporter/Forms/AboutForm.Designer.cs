namespace Rapid_Reporter.Forms
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.appName = new System.Windows.Forms.Label();
            this.appVer = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnClose = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // appName
            // 
            this.appName.AutoSize = true;
            this.appName.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.appName.Location = new System.Drawing.Point(12, 8);
            this.appName.Name = "appName";
            this.appName.Size = new System.Drawing.Size(96, 13);
            this.appName.TabIndex = 0;
            this.appName.Text = "App Name Here";
            // 
            // appVer
            // 
            this.appVer.AutoSize = true;
            this.appVer.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.appVer.Location = new System.Drawing.Point(311, 8);
            this.appVer.Name = "appVer";
            this.appVer.Size = new System.Drawing.Size(96, 13);
            this.appVer.TabIndex = 1;
            this.appVer.Text = "App Name Here";
            this.appVer.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // label1
            // 
            this.label1.Location = new System.Drawing.Point(12, 31);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(395, 210);
            this.label1.TabIndex = 2;
            this.label1.Text = resources.GetString("label1.Text");
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(332, 245);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 21);
            this.btnClose.TabIndex = 3;
            this.btnClose.Text = "&Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // AboutForm
            // 
            this.AcceptButton = this.btnClose;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(419, 274);
            this.ControlBox = false;
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.appVer);
            this.Controls.Add(this.appName);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "About Rapid Reporter Ex";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.AboutForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label appName;
        private System.Windows.Forms.Label appVer;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button btnClose;
    }
}