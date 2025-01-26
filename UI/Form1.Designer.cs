namespace BallHoop
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            panel1 = new Panel();
            label1 = new Label();
            buttonReset = new Button();
            comboBoxLearningRateTypes = new ComboBox();
            labelNetwork = new Label();
            labelTrainPct = new Label();
            label6 = new Label();
            lblSuggestedForce = new LinkLabel();
            label5 = new Label();
            fluentSliderForce = new FluentSlider();
            panelTrain = new Panel();
            labelTraining = new Label();
            progressBarTraining = new ProgressBar();
            buttonRunAITraining = new Button();
            buttonAI = new Button();
            buttonCreateTrainingData = new Button();
            buttonThrow = new Button();
            basketBallSimulation1 = new BasketBallSimulationUserControl();
            toolTip1 = new ToolTip(components);
            richTextLog = new RichTextBox();
            pictureBoxHeatMap = new PictureBox();
            panel1.SuspendLayout();
            panelTrain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBoxHeatMap).BeginInit();
            SuspendLayout();
            // 
            // panel1
            // 
            panel1.AllowDrop = true;
            panel1.BackColor = Color.White;
            panel1.Controls.Add(label1);
            panel1.Controls.Add(buttonReset);
            panel1.Controls.Add(comboBoxLearningRateTypes);
            panel1.Controls.Add(labelNetwork);
            panel1.Controls.Add(labelTrainPct);
            panel1.Controls.Add(label6);
            panel1.Controls.Add(lblSuggestedForce);
            panel1.Controls.Add(label5);
            panel1.Controls.Add(fluentSliderForce);
            panel1.Controls.Add(panelTrain);
            panel1.Controls.Add(buttonRunAITraining);
            panel1.Controls.Add(buttonAI);
            panel1.Controls.Add(buttonCreateTrainingData);
            panel1.Controls.Add(buttonThrow);
            panel1.Dock = DockStyle.Top;
            panel1.ForeColor = Color.Black;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1608, 71);
            panel1.TabIndex = 5;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI Light", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.ForeColor = Color.Black;
            label1.Location = new Point(12, 44);
            label1.Name = "label1";
            label1.Size = new Size(51, 21);
            label1.TabIndex = 32;
            label1.Text = "Force";
            // 
            // buttonReset
            // 
            buttonReset.BackColor = Color.FromArgb(255, 224, 192);
            buttonReset.Cursor = Cursors.Hand;
            buttonReset.Font = new Font("Segoe UI", 8F);
            buttonReset.ForeColor = Color.Black;
            buttonReset.Location = new Point(1173, 14);
            buttonReset.Name = "buttonReset";
            buttonReset.Size = new Size(65, 42);
            buttonReset.TabIndex = 31;
            buttonReset.Text = "Reset";
            toolTip1.SetToolTip(buttonReset, "Resets Adam and Learning Rate.");
            buttonReset.UseVisualStyleBackColor = false;
            buttonReset.Click += ButtonAdamReset_Click;
            // 
            // comboBoxLearningTypes
            // 
            comboBoxLearningRateTypes.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxLearningRateTypes.FormattingEnabled = true;
            comboBoxLearningRateTypes.Location = new Point(1395, 28);
            comboBoxLearningRateTypes.Name = "comboBoxLearningTypes";
            comboBoxLearningRateTypes.Size = new Size(184, 23);
            comboBoxLearningRateTypes.TabIndex = 30;
            // 
            // labelNetwork
            // 
            labelNetwork.Location = new Point(1232, 4);
            labelNetwork.Name = "labelNetwork";
            labelNetwork.Size = new Size(136, 23);
            labelNetwork.TabIndex = 29;
            labelNetwork.Text = "label1";
            labelNetwork.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // labelTrainPct
            // 
            labelTrainPct.Font = new Font("Segoe UI", 24F);
            labelTrainPct.Location = new Point(1242, 17);
            labelTrainPct.Name = "labelTrainPct";
            labelTrainPct.Size = new Size(126, 44);
            labelTrainPct.TabIndex = 28;
            labelTrainPct.Text = "99.5%";
            labelTrainPct.TextAlign = ContentAlignment.MiddleCenter;
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Font = new Font("Segoe UI Light", 9F);
            label6.ForeColor = Color.Black;
            label6.Location = new Point(488, 47);
            label6.Name = "label6";
            label6.Size = new Size(141, 15);
            label6.TabIndex = 25;
            label6.Text = "(calculated after you throw)";
            // 
            // lblSuggestedForce
            // 
            lblSuggestedForce.ForeColor = Color.Black;
            lblSuggestedForce.LinkColor = Color.Black;
            lblSuggestedForce.Location = new Point(422, 47);
            lblSuggestedForce.Name = "lblSuggestedForce";
            lblSuggestedForce.Size = new Size(60, 15);
            lblSuggestedForce.TabIndex = 15;
            lblSuggestedForce.TabStop = true;
            lblSuggestedForce.Text = "linkLabel1";
            lblSuggestedForce.LinkClicked += LabelSuggestedForce_LinkClicked;
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Font = new Font("Segoe UI Light", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label5.ForeColor = Color.Black;
            label5.Location = new Point(302, 47);
            label5.Name = "label5";
            label5.Size = new Size(107, 15);
            label5.TabIndex = 24;
            label5.Text = "Optimum Force (N):";
            // 
            // fluentSliderForce
            // 
            fluentSliderForce.BackColor = Color.Transparent;
            fluentSliderForce.BarPenColorTop = Color.FromArgb(55, 60, 74);
            fluentSliderForce.BorderRoundRectSize = new Size(8, 8);
            fluentSliderForce.Cursor = Cursors.Hand;
            fluentSliderForce.ElapsedInnerColor = Color.Red;
            fluentSliderForce.ElapsedPenColorBottom = Color.Red;
            fluentSliderForce.ElapsedPenColorTop = Color.FromArgb(255, 128, 128);
            fluentSliderForce.Font = new Font("Microsoft Sans Serif", 6F);
            fluentSliderForce.ForeColor = Color.Black;
            fluentSliderForce.LargeChange = new decimal(new int[] { 10, 0, 0, 0 });
            fluentSliderForce.Location = new Point(12, 13);
            fluentSliderForce.Maximum = new decimal(new int[] { 150, 0, 0, 0 });
            fluentSliderForce.Minimum = new decimal(new int[] { 0, 0, 0, 0 });
            fluentSliderForce.Name = "fluentSliderForce";
            fluentSliderForce.ScaleDivisions = new decimal(new int[] { 10, 0, 0, 0 });
            fluentSliderForce.ScaleSubDivisions = new decimal(new int[] { 5, 0, 0, 0 });
            fluentSliderForce.ShowSmallScale = true;
            fluentSliderForce.Size = new Size(622, 48);
            fluentSliderForce.SmallChange = new decimal(new int[] { 1, 0, 0, 0 });
            fluentSliderForce.TabIndex = 27;
            fluentSliderForce.ThumbRoundRectSize = new Size(16, 16);
            fluentSliderForce.ThumbSize = new Size(16, 16);
            fluentSliderForce.TickAdd = 0F;
            fluentSliderForce.TickColor = Color.Black;
            fluentSliderForce.TickDivide = 0F;
            toolTip1.SetToolTip(fluentSliderForce, "Determines the force by which the ball is thrown.");
            fluentSliderForce.Value = new decimal(new int[] { 10, 0, 0, 0 });
            // 
            // panelTrain
            // 
            panelTrain.Controls.Add(labelTraining);
            panelTrain.Controls.Add(progressBarTraining);
            panelTrain.Location = new Point(857, 6);
            panelTrain.Name = "panelTrain";
            panelTrain.Size = new Size(239, 59);
            panelTrain.TabIndex = 26;
            panelTrain.Visible = false;
            // 
            // labelTraining
            // 
            labelTraining.ForeColor = Color.Black;
            labelTraining.Location = new Point(11, 6);
            labelTraining.Name = "labelTraining";
            labelTraining.Size = new Size(217, 18);
            labelTraining.TabIndex = 1;
            labelTraining.Text = "Training Progress";
            labelTraining.TextAlign = ContentAlignment.TopCenter;
            // 
            // progressBarTraining
            // 
            progressBarTraining.Location = new Point(11, 27);
            progressBarTraining.Name = "progressBarTraining";
            progressBarTraining.Size = new Size(217, 27);
            progressBarTraining.TabIndex = 0;
            // 
            // buttonRunAITraining
            // 
            buttonRunAITraining.BackColor = Color.FromArgb(255, 224, 192);
            buttonRunAITraining.Cursor = Cursors.Hand;
            buttonRunAITraining.Font = new Font("Segoe UI", 8F);
            buttonRunAITraining.ForeColor = Color.Black;
            buttonRunAITraining.Location = new Point(1102, 14);
            buttonRunAITraining.Name = "buttonRunAITraining";
            buttonRunAITraining.Size = new Size(65, 42);
            buttonRunAITraining.TabIndex = 22;
            buttonRunAITraining.Text = "Train";
            toolTip1.SetToolTip(buttonRunAITraining, "Trains AI training in the background using created data.");
            buttonRunAITraining.UseVisualStyleBackColor = false;
            buttonRunAITraining.Click += ButtonRunAITraining_Click;
            // 
            // buttonAI
            // 
            buttonAI.BackColor = Color.FromArgb(255, 224, 192);
            buttonAI.Cursor = Cursors.Hand;
            buttonAI.Font = new Font("Segoe UI", 8F);
            buttonAI.ForeColor = Color.Black;
            buttonAI.Location = new Point(711, 14);
            buttonAI.Name = "buttonAI";
            buttonAI.Size = new Size(65, 42);
            buttonAI.TabIndex = 19;
            buttonAI.Text = "AI Throw";
            toolTip1.SetToolTip(buttonAI, "AI throws the ball.");
            buttonAI.UseVisualStyleBackColor = false;
            buttonAI.Click += ButtonAIThrow_Click;
            // 
            // buttonCreateTrainingData
            // 
            buttonCreateTrainingData.BackColor = Color.FromArgb(255, 224, 192);
            buttonCreateTrainingData.Cursor = Cursors.Hand;
            buttonCreateTrainingData.Font = new Font("Segoe UI", 8F);
            buttonCreateTrainingData.ForeColor = Color.Black;
            buttonCreateTrainingData.Location = new Point(782, 14);
            buttonCreateTrainingData.Name = "buttonCreateTrainingData";
            buttonCreateTrainingData.Size = new Size(65, 42);
            buttonCreateTrainingData.TabIndex = 18;
            buttonCreateTrainingData.Text = "Create Data";
            toolTip1.SetToolTip(buttonCreateTrainingData, "Algorithmically throws ball at different distances / angles, and saves as training data.");
            buttonCreateTrainingData.UseVisualStyleBackColor = false;
            buttonCreateTrainingData.Click += ButtonTrain_Click;
            // 
            // buttonThrow
            // 
            buttonThrow.BackColor = Color.FromArgb(255, 224, 192);
            buttonThrow.Cursor = Cursors.Hand;
            buttonThrow.DialogResult = DialogResult.OK;
            buttonThrow.Font = new Font("Segoe UI", 8F);
            buttonThrow.ForeColor = Color.Black;
            buttonThrow.Location = new Point(640, 14);
            buttonThrow.Name = "buttonThrow";
            buttonThrow.Size = new Size(65, 42);
            buttonThrow.TabIndex = 9;
            buttonThrow.Text = "Manual Throw";
            toolTip1.SetToolTip(buttonThrow, "Throw the ball at angle and force shown.");
            buttonThrow.UseVisualStyleBackColor = false;
            buttonThrow.Click += ButtonThrow_Click;
            // 
            // basketBallSimulation1
            // 
            basketBallSimulation1.Dock = DockStyle.Top;
            basketBallSimulation1.Location = new Point(0, 71);
            basketBallSimulation1.Name = "basketBallSimulation1";
            basketBallSimulation1.PersonHeightMetres = 2F;
            basketBallSimulation1.ShowProtractor = false;
            basketBallSimulation1.ShowRuler = false;
            basketBallSimulation1.Size = new Size(1608, 297);
            basketBallSimulation1.TabIndex = 6;
            // 
            // richTextLog
            // 
            richTextLog.BackColor = Color.White;
            richTextLog.Dock = DockStyle.Fill;
            richTextLog.Font = new Font("Cascadia Code", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            richTextLog.ForeColor = Color.Black;
            richTextLog.Location = new Point(1276, 368);
            richTextLog.Name = "richTextLog";
            richTextLog.ReadOnly = true;
            richTextLog.Size = new Size(332, 502);
            richTextLog.TabIndex = 9;
            richTextLog.Text = "";
            toolTip1.SetToolTip(richTextLog, "Throws the ball at your selected angle and force.");
            // 
            // pictureBoxHeatMap
            // 
            pictureBoxHeatMap.Dock = DockStyle.Left;
            pictureBoxHeatMap.Location = new Point(0, 368);
            pictureBoxHeatMap.Name = "pictureBoxHeatMap";
            pictureBoxHeatMap.Size = new Size(1276, 502);
            pictureBoxHeatMap.TabIndex = 8;
            pictureBoxHeatMap.TabStop = false;
            // 
            // Form1
            // 
            AcceptButton = buttonThrow;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1608, 870);
            Controls.Add(richTextLog);
            Controls.Add(pictureBoxHeatMap);
            Controls.Add(basketBallSimulation1);
            Controls.Add(panel1);
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "Form1";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Basketball And Hoop";
            WindowState = FormWindowState.Maximized;
            FormClosing += Form1_FormClosing;
            Load += Form1_Load;
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            panelTrain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)pictureBoxHeatMap).EndInit();
            ResumeLayout(false);
        }

        #endregion
        private Panel panel1;
        private Button buttonThrow;
        private LinkLabel lblSuggestedForce;
        private Button buttonCreateTrainingData;
        private Button buttonAI;
        private BasketBallSimulationUserControl basketBallSimulation1;
        private Button buttonRunAITraining;
        private ToolTip toolTip1;
        private Label label6;
        private Label label5;
        private Panel panelTrain;
        private Label labelTraining;
        private ProgressBar progressBarTraining;
        private FluentSlider fluentSliderForce;
        private Label labelTrainPct;
        private PictureBox pictureBoxHeatMap;
        private RichTextBox richTextLog;
        private Label labelNetwork;
        private ComboBox comboBoxLearningRateTypes;
        private Button buttonReset;
        private Label label1;
    }
}
