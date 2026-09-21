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
            this.tbCalF = new System.Windows.Forms.TextBox();
            this.butImage = new System.Windows.Forms.Button();
            this.butMicr = new System.Windows.Forms.Button();
            this.butMeasDuty = new System.Windows.Forms.Button();
            this.butMidBand = new System.Windows.Forms.Button();
            this.butWide = new System.Windows.Forms.Button();
            this.butSpread = new System.Windows.Forms.Button();
            this.butFloor = new System.Windows.Forms.Button();
            this.tbResults = new System.Windows.Forms.TextBox();
            this.butWideIMD = new System.Windows.Forms.Button();
            this.lblPreset = new System.Windows.Forms.Label();
            this.tbPreset = new System.Windows.Forms.TextBox();
            this.tbInterp = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.tbSlew = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.tbFloor = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbDlin = new System.Windows.Forms.TextBox();
            this.label7 = new System.Windows.Forms.Label();
            this.tbRFon = new System.Windows.Forms.TextBox();
            this.label8 = new System.Windows.Forms.Label();
            this.tbGain = new System.Windows.Forms.TextBox();
            this.label9 = new System.Windows.Forms.Label();
            this.tbComp = new System.Windows.Forms.TextBox();
            this.label10 = new System.Windows.Forms.Label();
            this.tbEQU = new System.Windows.Forms.TextBox();
            this.label11 = new System.Windows.Forms.Label();
            this.tbLPF = new System.Windows.Forms.TextBox();
            this.label12 = new System.Windows.Forms.Label();
            this.tbGdeq = new System.Windows.Forms.TextBox();
            this.label13 = new System.Windows.Forms.Label();
            this.tbScale = new System.Windows.Forms.TextBox();
            this.label14 = new System.Windows.Forms.Label();
            this.tbOffset = new System.Windows.Forms.TextBox();
            this.label15 = new System.Windows.Forms.Label();
            this.tbDelay = new System.Windows.Forms.TextBox();
            this.label16 = new System.Windows.Forms.Label();
            this.tbCurve = new System.Windows.Forms.TextBox();
            this.label17 = new System.Windows.Forms.Label();
            this.label18 = new System.Windows.Forms.Label();
            this.tbTone1 = new System.Windows.Forms.TextBox();
            this.tbTone2 = new System.Windows.Forms.TextBox();
            this.tbaShelf = new System.Windows.Forms.TextBox();
            this.label19 = new System.Windows.Forms.Label();
            this.tbShelfA = new System.Windows.Forms.TextBox();
            this.label20 = new System.Windows.Forms.Label();
            this.tbGdOpt = new System.Windows.Forms.TextBox();
            this.label21 = new System.Windows.Forms.Label();
            this.butSaveLog = new System.Windows.Forms.Button();
            this.butClearLog = new System.Windows.Forms.Button();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.audioDeviceMenu = new System.Windows.Forms.ToolStripMenuItem();
            this.SaveAudioFreq = new System.Windows.Forms.ToolStripMenuItem();
            this.butUpdate = new System.Windows.Forms.Button();
            this.tbALC = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.tbSoftLimiter = new System.Windows.Forms.TextBox();
            this.label22 = new System.Windows.Forms.Label();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblFrequency
            // 
            this.lblFrequency.AutoSize = true;
            this.lblFrequency.Font = new System.Drawing.Font("Segoe UI", 14F, System.Drawing.FontStyle.Bold);
            this.lblFrequency.Location = new System.Drawing.Point(12, 41);
            this.lblFrequency.Name = "lblFrequency";
            this.lblFrequency.Size = new System.Drawing.Size(75, 25);
            this.lblFrequency.TabIndex = 0;
            this.lblFrequency.Text = "-- MHz";
            // 
            // lblStatus
            // 
            this.lblStatus.AutoSize = true;
            this.lblStatus.Location = new System.Drawing.Point(14, 66);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Size = new System.Drawing.Size(47, 13);
            this.lblStatus.TabIndex = 1;
            this.lblStatus.Text = "Stopped";
            // 
            // txtFrequency
            // 
            this.txtFrequency.Location = new System.Drawing.Point(17, 119);
            this.txtFrequency.Name = "txtFrequency";
            this.txtFrequency.Size = new System.Drawing.Size(84, 20);
            this.txtFrequency.TabIndex = 2;
            // 
            // btnSetFrequency
            // 
            this.btnSetFrequency.Location = new System.Drawing.Point(107, 119);
            this.btnSetFrequency.Name = "btnSetFrequency";
            this.btnSetFrequency.Size = new System.Drawing.Size(69, 20);
            this.btnSetFrequency.TabIndex = 3;
            this.btnSetFrequency.Text = "Set (MHz)";
            this.btnSetFrequency.UseVisualStyleBackColor = true;
            this.btnSetFrequency.Click += new System.EventHandler(this.btnSetFrequency_Click);
            // 
            // tbData
            // 
            this.tbData.Location = new System.Drawing.Point(21, 175);
            this.tbData.Multiline = true;
            this.tbData.Name = "tbData";
            this.tbData.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbData.Size = new System.Drawing.Size(541, 284);
            this.tbData.TabIndex = 4;
            this.tbData.WordWrap = false;
            // 
            // butMeas
            // 
            this.butMeas.Location = new System.Drawing.Point(570, 145);
            this.butMeas.Name = "butMeas";
            this.butMeas.Size = new System.Drawing.Size(89, 24);
            this.butMeas.TabIndex = 5;
            this.butMeas.Text = "Measure D";
            this.butMeas.UseVisualStyleBackColor = true;
            this.butMeas.Click += new System.EventHandler(this.butMeas_Click);
            // 
            // btStop
            // 
            this.btStop.Location = new System.Drawing.Point(570, 204);
            this.btStop.Name = "btStop";
            this.btStop.Size = new System.Drawing.Size(89, 27);
            this.btStop.TabIndex = 6;
            this.btStop.Text = "Stop";
            this.btStop.UseVisualStyleBackColor = true;
            this.btStop.Click += new System.EventHandler(this.btStop_Click);
            // 
            // but3rdIMD
            // 
            this.but3rdIMD.Location = new System.Drawing.Point(570, 298);
            this.but3rdIMD.Name = "but3rdIMD";
            this.but3rdIMD.Size = new System.Drawing.Size(89, 23);
            this.but3rdIMD.TabIndex = 7;
            this.but3rdIMD.Text = "3rd IMD";
            this.but3rdIMD.UseVisualStyleBackColor = true;
            this.but3rdIMD.Click += new System.EventHandler(this.but3rdIMD_Click);
            // 
            // tbCmd
            // 
            this.tbCmd.Location = new System.Drawing.Point(214, 147);
            this.tbCmd.Name = "tbCmd";
            this.tbCmd.Size = new System.Drawing.Size(72, 20);
            this.tbCmd.TabIndex = 8;
            this.tbCmd.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.tbCmd_KeyPress);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(185, 150);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(27, 13);
            this.label1.TabIndex = 9;
            this.label1.Text = "cmd";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(20, 94);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(31, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Cal F";
            // 
            // tbCalF
            // 
            this.tbCalF.Location = new System.Drawing.Point(65, 87);
            this.tbCalF.Name = "tbCalF";
            this.tbCalF.Size = new System.Drawing.Size(48, 20);
            this.tbCalF.TabIndex = 11;
            this.tbCalF.Text = "0";
            // 
            // butImage
            // 
            this.butImage.Location = new System.Drawing.Point(570, 339);
            this.butImage.Name = "butImage";
            this.butImage.Size = new System.Drawing.Size(89, 23);
            this.butImage.TabIndex = 12;
            this.butImage.Text = "Image";
            this.butImage.UseVisualStyleBackColor = true;
            this.butImage.Click += new System.EventHandler(this.butImage_Click);
            // 
            // butMicr
            // 
            this.butMicr.Location = new System.Drawing.Point(570, 381);
            this.butMicr.Name = "butMicr";
            this.butMicr.Size = new System.Drawing.Size(89, 23);
            this.butMicr.TabIndex = 13;
            this.butMicr.Text = "Microphone";
            this.butMicr.UseVisualStyleBackColor = true;
            this.butMicr.Click += new System.EventHandler(this.butMicr_Click);
            // 
            // butMeasDuty
            // 
            this.butMeasDuty.Location = new System.Drawing.Point(570, 175);
            this.butMeasDuty.Name = "butMeasDuty";
            this.butMeasDuty.Size = new System.Drawing.Size(89, 23);
            this.butMeasDuty.TabIndex = 14;
            this.butMeasDuty.Text = "Meas Duty";
            this.butMeasDuty.UseVisualStyleBackColor = true;
            this.butMeasDuty.Click += new System.EventHandler(this.butMeasDuty_Click);
            // 
            // butMidBand
            // 
            this.butMidBand.Location = new System.Drawing.Point(570, 446);
            this.butMidBand.Name = "butMidBand";
            this.butMidBand.Size = new System.Drawing.Size(89, 23);
            this.butMidBand.TabIndex = 15;
            this.butMidBand.Text = "Mid Band";
            this.butMidBand.UseVisualStyleBackColor = true;
            this.butMidBand.Click += new System.EventHandler(this.butMidBand_Click);
            // 
            // butWide
            // 
            this.butWide.Location = new System.Drawing.Point(570, 488);
            this.butWide.Name = "butWide";
            this.butWide.Size = new System.Drawing.Size(89, 23);
            this.butWide.TabIndex = 16;
            this.butWide.Text = "Wide";
            this.butWide.UseVisualStyleBackColor = true;
            this.butWide.Click += new System.EventHandler(this.butWide_Click);
            // 
            // butSpread
            // 
            this.butSpread.Location = new System.Drawing.Point(570, 410);
            this.butSpread.Name = "butSpread";
            this.butSpread.Size = new System.Drawing.Size(89, 23);
            this.butSpread.TabIndex = 17;
            this.butSpread.Text = "5 10 20 kHz";
            this.butSpread.UseVisualStyleBackColor = true;
            this.butSpread.Click += new System.EventHandler(this.butSpread_Click);
            // 
            // butFloor
            // 
            this.butFloor.Location = new System.Drawing.Point(570, 257);
            this.butFloor.Name = "butFloor";
            this.butFloor.Size = new System.Drawing.Size(89, 23);
            this.butFloor.TabIndex = 18;
            this.butFloor.Text = "Noise floor";
            this.butFloor.UseVisualStyleBackColor = true;
            this.butFloor.Click += new System.EventHandler(this.butFloor_Click);
            // 
            // tbResults
            // 
            this.tbResults.Location = new System.Drawing.Point(23, 461);
            this.tbResults.Multiline = true;
            this.tbResults.Name = "tbResults";
            this.tbResults.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.tbResults.Size = new System.Drawing.Size(540, 201);
            this.tbResults.TabIndex = 19;
            this.tbResults.WordWrap = false;
            // 
            // butWideIMD
            // 
            this.butWideIMD.Location = new System.Drawing.Point(570, 525);
            this.butWideIMD.Name = "butWideIMD";
            this.butWideIMD.Size = new System.Drawing.Size(89, 23);
            this.butWideIMD.TabIndex = 20;
            this.butWideIMD.Text = "Wide IMDs";
            this.butWideIMD.UseVisualStyleBackColor = true;
            this.butWideIMD.Click += new System.EventHandler(this.butWideIMD_Click);
            // 
            // lblPreset
            // 
            this.lblPreset.AutoSize = true;
            this.lblPreset.Location = new System.Drawing.Point(163, 44);
            this.lblPreset.Name = "lblPreset";
            this.lblPreset.Size = new System.Drawing.Size(37, 13);
            this.lblPreset.TabIndex = 21;
            this.lblPreset.Text = "Preset";
            // 
            // tbPreset
            // 
            this.tbPreset.Location = new System.Drawing.Point(223, 41);
            this.tbPreset.Name = "tbPreset";
            this.tbPreset.Size = new System.Drawing.Size(41, 20);
            this.tbPreset.TabIndex = 22;
            // 
            // tbInterp
            // 
            this.tbInterp.Location = new System.Drawing.Point(522, 94);
            this.tbInterp.Name = "tbInterp";
            this.tbInterp.Size = new System.Drawing.Size(41, 20);
            this.tbInterp.TabIndex = 24;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(481, 99);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(34, 13);
            this.label4.TabIndex = 23;
            this.label4.Text = "Interp";
            // 
            // tbSlew
            // 
            this.tbSlew.Location = new System.Drawing.Point(427, 94);
            this.tbSlew.Name = "tbSlew";
            this.tbSlew.Size = new System.Drawing.Size(41, 20);
            this.tbSlew.TabIndex = 26;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(386, 97);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(28, 13);
            this.label5.TabIndex = 25;
            this.label5.Text = "slew";
            // 
            // tbFloor
            // 
            this.tbFloor.Location = new System.Drawing.Point(328, 94);
            this.tbFloor.Name = "tbFloor";
            this.tbFloor.Size = new System.Drawing.Size(41, 20);
            this.tbFloor.TabIndex = 28;
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(287, 97);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(27, 13);
            this.label6.TabIndex = 27;
            this.label6.Text = "floor";
            // 
            // tbDlin
            // 
            this.tbDlin.Location = new System.Drawing.Point(223, 94);
            this.tbDlin.Name = "tbDlin";
            this.tbDlin.Size = new System.Drawing.Size(41, 20);
            this.tbDlin.TabIndex = 30;
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(182, 97);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(28, 13);
            this.label7.TabIndex = 29;
            this.label7.Text = "D lin";
            // 
            // tbRFon
            // 
            this.tbRFon.Location = new System.Drawing.Point(618, 69);
            this.tbRFon.Name = "tbRFon";
            this.tbRFon.Size = new System.Drawing.Size(41, 20);
            this.tbRFon.TabIndex = 32;
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(577, 72);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(36, 13);
            this.label8.TabIndex = 31;
            this.label8.Text = "RF on";
            // 
            // tbGain
            // 
            this.tbGain.Location = new System.Drawing.Point(521, 67);
            this.tbGain.Name = "tbGain";
            this.tbGain.Size = new System.Drawing.Size(41, 20);
            this.tbGain.TabIndex = 34;
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(480, 70);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(27, 13);
            this.label9.TabIndex = 33;
            this.label9.Text = "gain";
            // 
            // tbComp
            // 
            this.tbComp.Location = new System.Drawing.Point(427, 66);
            this.tbComp.Name = "tbComp";
            this.tbComp.Size = new System.Drawing.Size(41, 20);
            this.tbComp.TabIndex = 36;
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(386, 69);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(33, 13);
            this.label10.TabIndex = 35;
            this.label10.Text = "comp";
            // 
            // tbEQU
            // 
            this.tbEQU.Location = new System.Drawing.Point(328, 67);
            this.tbEQU.Name = "tbEQU";
            this.tbEQU.Size = new System.Drawing.Size(41, 20);
            this.tbEQU.TabIndex = 38;
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(287, 70);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(19, 13);
            this.label11.TabIndex = 37;
            this.label11.Text = "eq";
            // 
            // tbLPF
            // 
            this.tbLPF.Location = new System.Drawing.Point(223, 66);
            this.tbLPF.Name = "tbLPF";
            this.tbLPF.Size = new System.Drawing.Size(41, 20);
            this.tbLPF.TabIndex = 40;
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(182, 70);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(18, 13);
            this.label12.TabIndex = 39;
            this.label12.Text = "lpf";
            // 
            // tbGdeq
            // 
            this.tbGdeq.Location = new System.Drawing.Point(618, 41);
            this.tbGdeq.Name = "tbGdeq";
            this.tbGdeq.Size = new System.Drawing.Size(41, 20);
            this.tbGdeq.TabIndex = 42;
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(577, 44);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(31, 13);
            this.label13.TabIndex = 41;
            this.label13.Text = "gdeq";
            // 
            // tbScale
            // 
            this.tbScale.Location = new System.Drawing.Point(521, 41);
            this.tbScale.Name = "tbScale";
            this.tbScale.Size = new System.Drawing.Size(41, 20);
            this.tbScale.TabIndex = 44;
            // 
            // label14
            // 
            this.label14.AutoSize = true;
            this.label14.Location = new System.Drawing.Point(480, 44);
            this.label14.Name = "label14";
            this.label14.Size = new System.Drawing.Size(32, 13);
            this.label14.TabIndex = 43;
            this.label14.Text = "scale";
            // 
            // tbOffset
            // 
            this.tbOffset.Location = new System.Drawing.Point(427, 41);
            this.tbOffset.Name = "tbOffset";
            this.tbOffset.Size = new System.Drawing.Size(41, 20);
            this.tbOffset.TabIndex = 46;
            // 
            // label15
            // 
            this.label15.AutoSize = true;
            this.label15.Location = new System.Drawing.Point(386, 44);
            this.label15.Name = "label15";
            this.label15.Size = new System.Drawing.Size(33, 13);
            this.label15.TabIndex = 45;
            this.label15.Text = "offset";
            // 
            // tbDelay
            // 
            this.tbDelay.Location = new System.Drawing.Point(328, 41);
            this.tbDelay.Name = "tbDelay";
            this.tbDelay.Size = new System.Drawing.Size(41, 20);
            this.tbDelay.TabIndex = 48;
            // 
            // label16
            // 
            this.label16.AutoSize = true;
            this.label16.Location = new System.Drawing.Point(287, 44);
            this.label16.Name = "label16";
            this.label16.Size = new System.Drawing.Size(34, 13);
            this.label16.TabIndex = 47;
            this.label16.Text = "Delay";
            // 
            // tbCurve
            // 
            this.tbCurve.Location = new System.Drawing.Point(618, 94);
            this.tbCurve.Name = "tbCurve";
            this.tbCurve.Size = new System.Drawing.Size(41, 20);
            this.tbCurve.TabIndex = 50;
            // 
            // label17
            // 
            this.label17.AutoSize = true;
            this.label17.Location = new System.Drawing.Point(577, 99);
            this.label17.Name = "label17";
            this.label17.Size = new System.Drawing.Size(27, 13);
            this.label17.TabIndex = 49;
            this.label17.Text = "I typ";
            // 
            // label18
            // 
            this.label18.AutoSize = true;
            this.label18.Location = new System.Drawing.Point(292, 151);
            this.label18.Name = "label18";
            this.label18.Size = new System.Drawing.Size(67, 13);
            this.label18.TabIndex = 51;
            this.label18.Text = "2Tone Freqs";
            // 
            // tbTone1
            // 
            this.tbTone1.Location = new System.Drawing.Point(368, 148);
            this.tbTone1.Name = "tbTone1";
            this.tbTone1.Size = new System.Drawing.Size(53, 20);
            this.tbTone1.TabIndex = 52;
            this.tbTone1.Text = "700";
            // 
            // tbTone2
            // 
            this.tbTone2.Location = new System.Drawing.Point(427, 148);
            this.tbTone2.Name = "tbTone2";
            this.tbTone2.Size = new System.Drawing.Size(53, 20);
            this.tbTone2.TabIndex = 53;
            this.tbTone2.Text = "1700";
            // 
            // tbaShelf
            // 
            this.tbaShelf.Location = new System.Drawing.Point(223, 119);
            this.tbaShelf.Name = "tbaShelf";
            this.tbaShelf.Size = new System.Drawing.Size(41, 20);
            this.tbaShelf.TabIndex = 59;
            // 
            // label19
            // 
            this.label19.AutoSize = true;
            this.label19.Location = new System.Drawing.Point(182, 122);
            this.label19.Name = "label19";
            this.label19.Size = new System.Drawing.Size(38, 13);
            this.label19.TabIndex = 58;
            this.label19.Text = "a shelf";
            // 
            // tbShelfA
            // 
            this.tbShelfA.Location = new System.Drawing.Point(328, 119);
            this.tbShelfA.Name = "tbShelfA";
            this.tbShelfA.Size = new System.Drawing.Size(41, 20);
            this.tbShelfA.TabIndex = 57;
            // 
            // label20
            // 
            this.label20.AutoSize = true;
            this.label20.Location = new System.Drawing.Point(287, 122);
            this.label20.Name = "label20";
            this.label20.Size = new System.Drawing.Size(39, 13);
            this.label20.TabIndex = 56;
            this.label20.Text = "A shelf";
            // 
            // tbGdOpt
            // 
            this.tbGdOpt.Location = new System.Drawing.Point(427, 119);
            this.tbGdOpt.Name = "tbGdOpt";
            this.tbGdOpt.Size = new System.Drawing.Size(41, 20);
            this.tbGdOpt.TabIndex = 55;
            // 
            // label21
            // 
            this.label21.AutoSize = true;
            this.label21.Location = new System.Drawing.Point(386, 122);
            this.label21.Name = "label21";
            this.label21.Size = new System.Drawing.Size(39, 13);
            this.label21.TabIndex = 54;
            this.label21.Text = "gd Opt";
            // 
            // butSaveLog
            // 
            this.butSaveLog.Location = new System.Drawing.Point(21, 146);
            this.butSaveLog.Name = "butSaveLog";
            this.butSaveLog.Size = new System.Drawing.Size(75, 23);
            this.butSaveLog.TabIndex = 60;
            this.butSaveLog.Text = "Save Log";
            this.butSaveLog.UseVisualStyleBackColor = true;
            this.butSaveLog.Click += new System.EventHandler(this.butSaveLog_Click);
            // 
            // butClearLog
            // 
            this.butClearLog.Location = new System.Drawing.Point(128, 147);
            this.butClearLog.Name = "butClearLog";
            this.butClearLog.Size = new System.Drawing.Size(48, 23);
            this.butClearLog.TabIndex = 61;
            this.butClearLog.Text = "Clear";
            this.butClearLog.UseVisualStyleBackColor = true;
            this.butClearLog.Click += new System.EventHandler(this.butClearLog_Click);
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.audioDeviceMenu,
            this.SaveAudioFreq});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(671, 24);
            this.menuStrip1.TabIndex = 62;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // audioDeviceMenu
            // 
            this.audioDeviceMenu.Name = "audioDeviceMenu";
            this.audioDeviceMenu.Size = new System.Drawing.Size(123, 20);
            this.audioDeviceMenu.Text = "Select Audio Device";
            this.audioDeviceMenu.DropDownOpening += new System.EventHandler(this.audioDeviceMenu_DropDownOpening);
            // 
            // SaveAudioFreq
            // 
            this.SaveAudioFreq.Name = "SaveAudioFreq";
            this.SaveAudioFreq.Size = new System.Drawing.Size(79, 20);
            this.SaveAudioFreq.Text = "Detect Freq";
            this.SaveAudioFreq.Click += new System.EventHandler(this.butMeasureAudioFreq_Click);
            // 
            // butUpdate
            // 
            this.butUpdate.Location = new System.Drawing.Point(501, 145);
            this.butUpdate.Name = "butUpdate";
            this.butUpdate.Size = new System.Drawing.Size(61, 23);
            this.butUpdate.TabIndex = 63;
            this.butUpdate.Text = "Update";
            this.butUpdate.UseVisualStyleBackColor = true;
            this.butUpdate.Click += new System.EventHandler(this.butUpdate_Click);
            // 
            // tbALC
            // 
            this.tbALC.Location = new System.Drawing.Point(522, 120);
            this.tbALC.Name = "tbALC";
            this.tbALC.Size = new System.Drawing.Size(41, 20);
            this.tbALC.TabIndex = 65;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(481, 123);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(27, 13);
            this.label3.TabIndex = 64;
            this.label3.Text = "ALC";
            // 
            // tbSoftLimiter
            // 
            this.tbSoftLimiter.Location = new System.Drawing.Point(618, 120);
            this.tbSoftLimiter.Name = "tbSoftLimiter";
            this.tbSoftLimiter.Size = new System.Drawing.Size(41, 20);
            this.tbSoftLimiter.TabIndex = 67;
            // 
            // label22
            // 
            this.label22.AutoSize = true;
            this.label22.Location = new System.Drawing.Point(569, 123);
            this.label22.Name = "label22";
            this.label22.Size = new System.Drawing.Size(45, 13);
            this.label22.TabIndex = 66;
            this.label22.Text = "Soft Lim";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(671, 667);
            this.Controls.Add(this.tbSoftLimiter);
            this.Controls.Add(this.label22);
            this.Controls.Add(this.tbALC);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.butUpdate);
            this.Controls.Add(this.butClearLog);
            this.Controls.Add(this.butSaveLog);
            this.Controls.Add(this.tbaShelf);
            this.Controls.Add(this.label19);
            this.Controls.Add(this.tbShelfA);
            this.Controls.Add(this.label20);
            this.Controls.Add(this.tbGdOpt);
            this.Controls.Add(this.label21);
            this.Controls.Add(this.tbTone2);
            this.Controls.Add(this.tbTone1);
            this.Controls.Add(this.label18);
            this.Controls.Add(this.tbCurve);
            this.Controls.Add(this.label17);
            this.Controls.Add(this.tbDelay);
            this.Controls.Add(this.label16);
            this.Controls.Add(this.tbOffset);
            this.Controls.Add(this.label15);
            this.Controls.Add(this.tbScale);
            this.Controls.Add(this.label14);
            this.Controls.Add(this.tbGdeq);
            this.Controls.Add(this.label13);
            this.Controls.Add(this.tbLPF);
            this.Controls.Add(this.label12);
            this.Controls.Add(this.tbEQU);
            this.Controls.Add(this.label11);
            this.Controls.Add(this.tbComp);
            this.Controls.Add(this.label10);
            this.Controls.Add(this.tbGain);
            this.Controls.Add(this.label9);
            this.Controls.Add(this.tbRFon);
            this.Controls.Add(this.label8);
            this.Controls.Add(this.tbDlin);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.tbFloor);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.tbSlew);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.tbInterp);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.tbPreset);
            this.Controls.Add(this.lblPreset);
            this.Controls.Add(this.butWideIMD);
            this.Controls.Add(this.tbResults);
            this.Controls.Add(this.butFloor);
            this.Controls.Add(this.butSpread);
            this.Controls.Add(this.butWide);
            this.Controls.Add(this.butMidBand);
            this.Controls.Add(this.butMeasDuty);
            this.Controls.Add(this.butMicr);
            this.Controls.Add(this.butImage);
            this.Controls.Add(this.tbCalF);
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
            this.Controls.Add(this.menuStrip1);
            this.Name = "MainForm";
            this.Text = "My Plugin";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            this.Shown += new System.EventHandler(this.MainForm_Shown);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
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
        private System.Windows.Forms.TextBox tbCalF;
        private System.Windows.Forms.Button butImage;
        private System.Windows.Forms.Button butMicr;
        private System.Windows.Forms.Button butMeasDuty;
        private System.Windows.Forms.Button butMidBand;
        private System.Windows.Forms.Button butWide;
        private System.Windows.Forms.Button butSpread;
        private System.Windows.Forms.Button butFloor;
        private System.Windows.Forms.TextBox tbResults;
        private System.Windows.Forms.Button butWideIMD;
        private System.Windows.Forms.Label lblPreset;
        private System.Windows.Forms.TextBox tbPreset;
        private System.Windows.Forms.TextBox tbInterp;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbSlew;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.TextBox tbFloor;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbDlin;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.TextBox tbRFon;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.TextBox tbGain;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.TextBox tbComp;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.TextBox tbEQU;
        private System.Windows.Forms.Label label11;
        private System.Windows.Forms.TextBox tbLPF;
        private System.Windows.Forms.Label label12;
        private System.Windows.Forms.TextBox tbGdeq;
        private System.Windows.Forms.Label label13;
        private System.Windows.Forms.TextBox tbScale;
        private System.Windows.Forms.Label label14;
        private System.Windows.Forms.TextBox tbOffset;
        private System.Windows.Forms.Label label15;
        private System.Windows.Forms.TextBox tbDelay;
        private System.Windows.Forms.Label label16;
        private System.Windows.Forms.TextBox tbCurve;
        private System.Windows.Forms.Label label17;
        private System.Windows.Forms.Label label18;
        private System.Windows.Forms.TextBox tbTone1;
        private System.Windows.Forms.TextBox tbTone2;
        private System.Windows.Forms.TextBox tbaShelf;
        private System.Windows.Forms.Label label19;
        private System.Windows.Forms.TextBox tbShelfA;
        private System.Windows.Forms.Label label20;
        private System.Windows.Forms.TextBox tbGdOpt;
        private System.Windows.Forms.Label label21;
        private System.Windows.Forms.Button butSaveLog;
        private System.Windows.Forms.Button butClearLog;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem audioDeviceMenu;
        private System.Windows.Forms.ToolStripMenuItem SaveAudioFreq;
        private System.Windows.Forms.Button butUpdate;
        private System.Windows.Forms.TextBox tbALC;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox tbSoftLimiter;
        private System.Windows.Forms.Label label22;
    }
}
