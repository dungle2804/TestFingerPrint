﻿namespace UareUSampleCSharp
{
    partial class Capture
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false</param>
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
            this.lblPlaceFinger = new System.Windows.Forms.Label();
            this.btnBack = new System.Windows.Forms.Button();
            this.pbFingerprint = new System.Windows.Forms.PictureBox();
            this.SuspendLayout();
            // 
            // lblPlaceFinger
            // 
            this.lblPlaceFinger.Location = new System.Drawing.Point(2, 380);
            this.lblPlaceFinger.Name = "lblPlaceFinger";
            this.lblPlaceFinger.Size = new System.Drawing.Size(187, 19);
            this.lblPlaceFinger.Text = "Place a finger on the reader";
            // 
            // btnBack
            // 
            this.btnBack.Location = new System.Drawing.Point(197, 375);
            this.btnBack.Name = "btnBack";
            this.btnBack.Size = new System.Drawing.Size(56, 23);
            this.btnBack.TabIndex = 4;
            this.btnBack.Text = "Back";
            this.btnBack.Click += new System.EventHandler(this.btnBack_Click);
            // 
            // pbFingerprint
            // 
            this.pbFingerprint.Location = new System.Drawing.Point(3, 1);
            this.pbFingerprint.Name = "pbFingerprint";
            this.pbFingerprint.Size = new System.Drawing.Size(256, 360);
            this.pbFingerprint.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            // 
            // Capture
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Inherit;
            this.ClientSize = new System.Drawing.Size(256, 360);
            this.Controls.Add(this.btnBack);
            this.Controls.Add(this.lblPlaceFinger);
            this.Controls.Add(this.pbFingerprint);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
#if !WindowsCE
            this.MaximumSize = new System.Drawing.Size(275, 440);
            this.MinimumSize = new System.Drawing.Size(275, 440);
            this.ClientSize = new System.Drawing.Size(275, 440);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
#endif
            this.Name = "Capture";
            this.Text = "Capture";
            this.Load += new System.EventHandler(this.Capture_Load);
            this.Closed += new System.EventHandler(this.Capture_Closed);
            this.ResumeLayout(false);

        }

        #endregion

        internal System.Windows.Forms.Label lblPlaceFinger;
        internal System.Windows.Forms.Button btnBack;
        internal System.Windows.Forms.PictureBox pbFingerprint;
    }
}