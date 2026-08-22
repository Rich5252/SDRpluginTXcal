namespace TXcalUi
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblFrequency = new System.Windows.Forms.Label();
            this.lblStatus = new System.Windows.Forms.Label();
            this.txtFrequency = new System.Windows.Forms.TextBox();
            this.btnSetFrequency = new System.Windows.Forms.Button();
            this.SuspendLayout();
            //
            // lblFrequency
            //
            this.lblFrequency.AutoSize = true;
            this.lblFrequency.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblFrequency.Location = new System.Drawing.Point(20, 20);
            this.lblFrequency.Name = "lblFrequency";
            this.lblFrequency.Size = new System.Drawing.Size(140, 25);
            this.lblFrequency.TabIndex = 0;
            this.lblFrequency.Text = "-- MHz";
            //
            // lblStatus
            //
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(20, 55);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(46, 15);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Stopped";
            //
            // txtFrequency
            //
            this.txtFrequency.Location = new System.Drawing.Point(20, 90);
            this.txtFrequency.Name = "txtFrequency";
            this.txtFrequency.Size = new System.Drawing.Size(120, 23);
            this.txtFrequency.TabIndex = 2;
            //
            // btnSetFrequency
            //
            this.btnSetFrequency.Location = new System.Drawing.Point(150, 90);
            this.btnSetFrequency.Name = "btnSetFrequency";
            this.btnSetFrequency.Size = new System.Drawing.Size(90, 23);
            this.btnSetFrequency.TabIndex = 3;
            this.btnSetFrequency.Text = "Set (MHz)";
            this.btnSetFrequency.UseVisualStyleBackColor = true;
            this.btnSetFrequency.Click += new System.EventHandler(this.btnSetFrequency_Click);
            //
            // MainForm
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(264, 141);
            this.Controls.Add(this.btnSetFrequency);
            this.Controls.Add(this.txtFrequency);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblFrequency);
            this.Name = "MainForm";
            this.Text = "My Plugin";
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblFrequency;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtFrequency;
        private System.Windows.Forms.Button btnSetFrequency;
    }
}
