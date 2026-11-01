namespace AlMadinaShahzad.Model
{
    partial class YesOrNoMessage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(YesOrNoMessage));
            this.btnYes = new Guna.UI2.WinForms.Guna2Button();
            this.lblM = new System.Windows.Forms.Label();
            this.btnNo = new Guna.UI2.WinForms.Guna2Button();
            this.lblMessageboxPOS = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // btnYes
            // 
            this.btnYes.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnYes.AutoRoundedCorners = true;
            this.btnYes.BorderRadius = 16;
            this.btnYes.CheckedState.Parent = this.btnYes;
            this.btnYes.CustomImages.Parent = this.btnYes;
            this.btnYes.FillColor = System.Drawing.Color.Crimson;
            this.btnYes.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnYes.ForeColor = System.Drawing.Color.White;
            this.btnYes.HoverState.Parent = this.btnYes;
            this.btnYes.Location = new System.Drawing.Point(217, 119);
            this.btnYes.Name = "btnYes";
            this.btnYes.ShadowDecoration.Parent = this.btnYes;
            this.btnYes.Size = new System.Drawing.Size(121, 34);
            this.btnYes.TabIndex = 25;
            this.btnYes.Text = "Yes";
            this.btnYes.Click += new System.EventHandler(this.btnYes_Click);
            // 
            // lblM
            // 
            this.lblM.AutoSize = true;
            this.lblM.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblM.ForeColor = System.Drawing.Color.White;
            this.lblM.Location = new System.Drawing.Point(113, 60);
            this.lblM.Name = "lblM";
            this.lblM.Size = new System.Drawing.Size(31, 21);
            this.lblM.TabIndex = 24;
            this.lblM.Text = "Ok";
            // 
            // btnNo
            // 
            this.btnNo.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnNo.AutoRoundedCorners = true;
            this.btnNo.BorderRadius = 16;
            this.btnNo.CheckedState.Parent = this.btnNo;
            this.btnNo.CustomImages.Parent = this.btnNo;
            this.btnNo.FillColor = System.Drawing.Color.Crimson;
            this.btnNo.Font = new System.Drawing.Font("Segoe UI Semibold", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNo.ForeColor = System.Drawing.Color.White;
            this.btnNo.HoverState.Parent = this.btnNo;
            this.btnNo.Location = new System.Drawing.Point(344, 119);
            this.btnNo.Name = "btnNo";
            this.btnNo.ShadowDecoration.Parent = this.btnNo;
            this.btnNo.Size = new System.Drawing.Size(121, 34);
            this.btnNo.TabIndex = 23;
            this.btnNo.Text = "No";
            this.btnNo.Click += new System.EventHandler(this.btnNo_Click);
            // 
            // lblMessageboxPOS
            // 
            this.lblMessageboxPOS.AutoSize = true;
            this.lblMessageboxPOS.Font = new System.Drawing.Font("Segoe UI", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblMessageboxPOS.ForeColor = System.Drawing.Color.White;
            this.lblMessageboxPOS.Location = new System.Drawing.Point(112, 27);
            this.lblMessageboxPOS.Name = "lblMessageboxPOS";
            this.lblMessageboxPOS.Size = new System.Drawing.Size(227, 25);
            this.lblMessageboxPOS.TabIndex = 22;
            this.lblMessageboxPOS.Text = "Al Madina Shezad Chhutta Services";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = ((System.Drawing.Image)(resources.GetObject("pictureBox1.Image")));
            this.pictureBox1.Location = new System.Drawing.Point(9, 13);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(97, 68);
            this.pictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pictureBox1.TabIndex = 21;
            this.pictureBox1.TabStop = false;
            // 
            // YesOrNoMessage
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(488, 166);
            this.Controls.Add(this.btnYes);
            this.Controls.Add(this.lblM);
            this.Controls.Add(this.btnNo);
            this.Controls.Add(this.lblMessageboxPOS);
            this.Controls.Add(this.pictureBox1);
            this.Name = "YesOrNoMessage";
            this.Text = "YesOrNoMessage";
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private Guna.UI2.WinForms.Guna2Button btnYes;
        private System.Windows.Forms.Label lblM;
        private Guna.UI2.WinForms.Guna2Button btnNo;
        private System.Windows.Forms.Label lblMessageboxPOS;
        private System.Windows.Forms.PictureBox pictureBox1;
    }
}