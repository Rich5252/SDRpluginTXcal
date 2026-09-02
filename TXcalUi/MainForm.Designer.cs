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
            this.tbData = new System.Windows.Forms.TextBox();
            this.butMeas = new System.Windows.Forms.Button();
            this.btStop = new System.Windows.Forms.Button();
            this.but3rdIMD = new System.Windows.Forms.Button();
            this.tbCmd = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.tbDeltaF = new System.Windows.Forms.TextBox();
            this.butImage = new System.Windows.Forms.Button();
            this.butMicr = new System.Windows.Forms.Button();
            this.butMeasDuty = new System.Windows.Forms.Button();
            this.butMidBand = new System.Windows.Forms.Button();
            this.butWide = new System.Windows.Forms.Button();
            this.butSpread = new System.Windows.Forms.Button();
            this.butFloor = new System.Windows.Forms.Button();
            this.tbResults = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // lblFrequency
            // 
            this.lblFrequency.AutoSize = true;
            this.lblFrequency.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblFrequency.Location = new System.Drawing.Point(17, 17);
            this.lblFrequency.Name = "lblFrequency";
            this.lblFrequency.Size = new System.Drawing.Size(75, 25);
            this.lblFrequency.TabIndex = 0;
            this.lblFrequency.Text = "-- MHz";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(17, 48);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(47, 13);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Stopped";
            // 
            // txtFrequency
            // 
            this.txtFrequency.Location = new System.Drawing.Point(17, 78);
            this.txtFrequency.Name = "txtFrequency";
            this.txtFrequency.Size = new System.Drawing.Size(103, 20);
            this.txtFrequency.TabIndex = 2;
            // 
            // btnSetFrequency
            // 
            this.btnSetFrequency.Location = new System.Drawing.Point(129, 78);
            this.btnSetFrequency.Name = "btnSetFrequency";
            this.btnSetFrequency.Size = new System.Drawing.Size(77, 20);
            this.btnSetFrequency.TabIndex = 3;
            this.btnSetFrequency.Text = "Set (MHz)";
            this.btnSetFrequency.UseVisualStyleBackColor = true;
            this.btnSetFrequency.Click += new System.EventHandler(this.btnSetFrequency_Click);
            // 
            // tbData
            // 
            this.tbData.Location = new System.Drawing.Point(23, 113);
            this.tbData.Multiline = true;
            this.tbData.Name = "tbData";
            this.tbData.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbData.Size = new System.Drawing.Size(541, 310);
            this.tbData.TabIndex = 4;
            this.tbData.WordWrap = false;
            // 
            // butMeas
            // 
            this.butMeas.Location = new System.Drawing.Point(570, 113);
            this.butMeas.Name = "butMeas";
            this.butMeas.Size = new System.Drawing.Size(89, 24);
            this.butMeas.TabIndex = 5;
            this.butMeas.Text = "Measure D";
            this.butMeas.UseVisualStyleBackColor = true;
            this.butMeas.Click += new System.EventHandler(this.butMeas_Click);
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(570, 172);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(89, 27);
            this.btStop.TabIndex = 6;
            this.btStop.Text = "Stop";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // but3rdIMD
            // 
            this.but3rdIMD.Location = new System.Drawing.Point(570, 266);
            this.but3rdIMD.Name = "but3rdIMD";
            this.but3rdIMD.Size = new System.Drawing.Size(89, 23);
            this.but3rdIMD.TabIndex = 7;
            this.but3rdIMD.Text = "3rd IMD";
            this.but3rdIMD.UseVisualStyleBackColor = true;
            this.but3rdIMD.Click += new System.EventHandler(this.but3rdIMD_Click);
            // 
            // tbCmd
            // 
            this.tbCmd.Location = new System.Drawing.Point(283, 78);
            this.tbCmd.Name = "tbCmd";
            this.tbCmd.Size = new System.Drawing.Size(100, 20);
            this.tbCmd.TabIndex = 8;
            this.tbCmd.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbCmd_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(250, 81);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "cmd";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(238, 29);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(39, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "delta F";
            // 
            // tbDeltaF
            // 
            this.tbDeltaF.Location = new System.Drawing.Point(283, 26);
            this.tbDeltaF.Name = "tbDeltaF";
            this.tbDeltaF.Size = new System.Drawing.Size(100, 20);
            this.tbDeltaF.TabIndex = 11;
            this.tbDeltaF.Text = "0";
            // 
            // butImage
            // 
            this.butImage.Location = new System.Drawing.Point(570, 307);
            this.butImage.Name = "butImage";
            this.butImage.Size = new System.Drawing.Size(89, 23);
            this.butImage.TabIndex = 12;
            this.butImage.Text = "Image";
            this.butImage.UseVisualStyleBackColor = true;
            this.butImage.Click += new System.EventHandler(this.butImage_Click);
            // 
            // butMicr
            // 
            this.butMicr.Location = new System.Drawing.Point(570, 349);
            this.butMicr.Name = "butMicr";
            this.butMicr.Size = new System.Drawing.Size(89, 23);
            this.butMicr.TabIndex = 13;
            this.butMicr.Text = "Microphone";
            this.butMicr.UseVisualStyleBackColor = true;
            this.butMicr.Click += new System.EventHandler(this.butMicr_Click);
            // 
            // butMeasDuty
            // 
            this.butMeasDuty.Location = new System.Drawing.Point(570, 143);
            this.butMeasDuty.Name = "butMeasDuty";
            this.butMeasDuty.Size = new System.Drawing.Size(89, 23);
            this.butMeasDuty.TabIndex = 14;
            this.butMeasDuty.Text = "Meas Duty";
            this.butMeasDuty.UseVisualStyleBackColor = true;
            this.butMeasDuty.Click += new System.EventHandler(this.butMeasDuty_Click);
            // 
            // butMidBand
            // 
            this.butMidBand.Location = new System.Drawing.Point(570, 414);
            this.butMidBand.Name = "butMidBand";
            this.butMidBand.Size = new System.Drawing.Size(89, 23);
            this.butMidBand.TabIndex = 15;
            this.butMidBand.Text = "Mid Band";
            this.butMidBand.UseVisualStyleBackColor = true;
            this.butMidBand.Click += new System.EventHandler(this.butMidBand_Click);
            // 
            // butWide
            // 
            this.butWide.Location = new System.Drawing.Point(570, 456);
            this.butWide.Name = "butWide";
            this.butWide.Size = new System.Drawing.Size(89, 23);
            this.butWide.TabIndex = 16;
            this.butWide.Text = "Wide";
            this.butWide.UseVisualStyleBackColor = true;
            this.butWide.Click += new System.EventHandler(this.butWide_Click);
            // 
            // butSpread
            // 
            this.butSpread.Location = new System.Drawing.Point(570, 378);
            this.butSpread.Name = "butSpread";
            this.butSpread.Size = new System.Drawing.Size(89, 23);
            this.butSpread.TabIndex = 17;
            this.butSpread.Text = "5 10 20 kHz";
            this.butSpread.UseVisualStyleBackColor = true;
            this.butSpread.Click += new System.EventHandler(this.butSpread_Click);
            // 
            // butFloor
            // 
            this.butFloor.Location = new System.Drawing.Point(570, 225);
            this.butFloor.Name = "butFloor";
            this.butFloor.Size = new System.Drawing.Size(89, 23);
            this.butFloor.TabIndex = 18;
            this.butFloor.Text = "Noise floor";
            this.butFloor.UseVisualStyleBackColor = true;
            this.butFloor.Click += new System.EventHandler(this.butFloor_Click);
            // 
            // tbResults
            // 
            this.tbResults.Location = new System.Drawing.Point(23, 429);
            this.tbResults.Multiline = true;
            this.tbResults.Name = "tbResults";
            this.tbResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbResults.Size = new System.Drawing.Size(540, 201);
            this.tbResults.TabIndex = 19;
            this.tbResults.WordWrap = false;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(671, 642);
            this.Controls.Add(this.tbResults);
            this.Controls.Add(this.butFloor);
            this.Controls.Add(this.butSpread);
            this.Controls.Add(this.butWide);
            this.Controls.Add(this.butMidBand);
            this.Controls.Add(this.butMeasDuty);
            this.Controls.Add(this.butMicr);
            this.Controls.Add(this.butImage);
            this.Controls.Add(this.tbDeltaF);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbCmd);
            this.Controls.Add(this.but3rdIMD);
            this.Controls.Add(this.btStop);
            this.Controls.Add(this.butMeas);
            this.Controls.Add(this.tbData);
            this.Controls.Add(this.btnSetFrequency);
            this.Controls.Add(this.txtFrequency);
            this.Controls.Add(this.lblStatus);
            this.Controls.Add(this.lblFrequency);
            this.Name = "MainForm";
            this.Text = "My Plugin";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Label lblFrequency;
        private System.Windows.Forms.Label lblStatus;
        private System.Windows.Forms.TextBox txtFrequency;
        private System.Windows.Forms.Button btnSetFrequency;
        private System.Windows.Forms.TextBox tbData;
        private System.Windows.Forms.Button butMeas;
        private System.Windows.Forms.Button btStop;
        private System.Windows.Forms.Button but3rdIMD;
        private System.Windows.Forms.TextBox tbCmd;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox tbDeltaF;
        private System.Windows.Forms.Button butImage;
        private System.Windows.Forms.Button butMicr;
        private System.Windows.Forms.Button butMeasDuty;
        private System.Windows.Forms.Button butMidBand;
        private System.Windows.Forms.Button butWide;
        private System.Windows.Forms.Button butSpread;
        private System.Windows.Forms.Button butFloor;
        private System.Windows.Forms.TextBox tbResults;
    }
}
