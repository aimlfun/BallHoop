using BallHoop.Simulation;
using BallHoop.UI;
using ML;
using System.ComponentModel;

namespace BallHoop;

public partial class Form1 : Form
{
    /// <summary>
    /// Position of the stickman.
    /// </summary>
    private float numericUpDownDistanceValue = 20f;

    /// <summary>
    /// Timer to plot the heatmap of the AI accuracy as it trains.
    /// </summary>
    System.Windows.Forms.Timer? timerHeatmap;

    /// <summary>
    /// Constructor for the form. Initializes the form and sets the suggested force label to an empty string.
    /// We defer most of the initialization to the Load event handler.
    /// </summary>
    public Form1()
    {
        InitializeComponent();

        lblSuggestedForce.Text = "";

        // no AI file, so disable the AI buttons (except generate training data)
        bool trainingFileExists = File.Exists(NeuralNetwork.c_trainingFileName);
        bool aimodelFileExists = File.Exists(NeuralNetwork.c_aiModelFilePath);

        // create the temp directory if it doesn't exist. I know one can use %temp%, but I prefer this way
        if (!Directory.Exists(@"c:\temp")) Directory.CreateDirectory(@"c:\temp");

        // copy the training file from the "Trained" folder to the current directory
        if (!trainingFileExists)
        {
            File.Copy(Path.Combine(@".\Trained\", Path.GetFileName(NeuralNetwork.c_trainingFileName)), NeuralNetwork.c_trainingFileName);
            trainingFileExists = true;
        }

        if (!aimodelFileExists)
        {
            File.Copy(Path.Combine(@".\Trained\", Path.GetFileName(NeuralNetwork.c_aiModelFilePath)), NeuralNetwork.c_aiModelFilePath);
            aimodelFileExists = true;
        }

        if (trainingFileExists && !aimodelFileExists)
        {
            buttonAI.Enabled = true;
        }
        else
        {
            buttonAI.Enabled = false;
        }
    }

    /// <summary>
    /// Ensure the stickman is in the correct place
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_Load(object sender, EventArgs e)
    {
        comboBoxLearningRateTypes.Items.AddRange(TrainModelManager.LearningRateTypes);
        comboBoxLearningRateTypes.SelectedIndex = 3;
        comboBoxLearningRateTypes.SelectedIndexChanged += ComboBoxLearningTypes_SelectedIndexChanged;

        fluentSliderForce.Maximum = (decimal)NeuralNetwork.c_forceNormaliser * 2;

        basketBallSimulation1.SetDistance(numericUpDownDistanceValue);

        basketBallSimulation1.ProtractorAngle = 45;
        basketBallSimulation1.ShowRuler = true;
        basketBallSimulation1.ShowProtractor = true;

        PlotAITrajectoryAngles();

        double score = 0;

        basketBallSimulation1.Invalidate();
        basketBallSimulation1.StickPersonMoved += BasketBallSimulation1_StickPersonMoved;

        AngleDistanceHeatmap.HeatmapNeedsDrawing(true);
        AngleDistanceHeatmap.PlotHeatmap(pictureBoxHeatMap, numericUpDownDistanceValue, basketBallSimulation1.ProtractorAngle);

        labelTrainPct.Text = $"{(score * 100):f2}%";

        labelNetwork.Text = string.Join("-", NeuralNetwork.c_defaultLayers);
    }

    /// <summary>
    /// Update the model manager when the learning rate type is changed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ComboBoxLearningTypes_SelectedIndexChanged(object? sender, EventArgs e)
    {
        TrainModelManager.LearningType = TrainModelManager.MapLearningRateType(comboBoxLearningRateTypes.Items[comboBoxLearningRateTypes.SelectedIndex].ToString());
    }

    /// <summary>
    /// This method is called when the user clicks the "Throw" button. It uses the force and angle values provided to define how to throw the ball.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonThrow_Click(object sender, EventArgs e)
    {
        double force = (double)fluentSliderForce.Value;

        if (force < 3 || force > (float)fluentSliderForce.Maximum)
        {
            MessageBox.Show($"Force value must be between 3 and {fluentSliderForce.Maximum} Newtons.");
            return;
        }

        float angle = basketBallSimulation1.ProtractorAngle;

        double forcerequired = basketBallSimulation1.Throw(forceInNewtons: force, angleInDegrees: angle);
        lblSuggestedForce.Text = forcerequired <= 0 ? "n/a" : forcerequired.ToString();
        richTextLog.Clear();

        // output the way we computed the force
        richTextLog.AppendText($"Angle:  {angle} degrees\n");
        richTextLog.AppendText($"Force:  {force} Newtons\n");
        richTextLog.AppendText($"CALCULATION:");
        richTextLog.AppendText($"  basketBallSimulation1.Throw( forceInNewtons: {force}, angleInDegrees: {angle} )\n");
        richTextLog.AppendText(basketBallSimulation1.Calc);
        richTextLog.AppendText($"OUTPUT: {lblSuggestedForce.Text}\n");
    }

    /// <summary>
    /// Copy the suggested force value to the force slider when the user clicks the link.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void LabelSuggestedForce_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
    {
        if (lblSuggestedForce.Text == "n/a") return;

        decimal forceRequired = decimal.Parse(lblSuggestedForce.Text);

        // it could require a force greater than the slider can handle, and it will throw an exception if we try
        if (forceRequired > fluentSliderForce.Maximum)
        {
            MessageBox.Show($"The required force of {forceRequired} exceeds the max slider value of {fluentSliderForce.Maximum}.");
            return;
        }

        fluentSliderForce.Value = forceRequired;
    }

    /// <summary>
    /// Update the distance when the stickman is moved.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void BasketBallSimulation1_StickPersonMoved(object sender, StickPersonMovedEventArgs e)
    {
        float value = BasketBallSimulation.c_rimCenterInMetres - e.Location.X;

        numericUpDownDistanceValue = value;
        lblSuggestedForce.Text = "";

        basketBallSimulation1.SetAITrajectoryAccuracyDrawPoints([]); // clear the trajectory lines
        basketBallSimulation1.SetDistance(value);
        
        PlotAITrajectoryAngles();
        
        AngleDistanceHeatmap.HeatmapNeedsDrawing(true);
        AngleDistanceHeatmap.PlotHeatmap(pictureBoxHeatMap, numericUpDownDistanceValue, basketBallSimulation1.ProtractorAngle);
        
        basketBallSimulation1.Invalidate();
    }

    #region TRAINING AI MODEL / GENERATE DATA
    /// <summary>
    /// Training method to find the best force and angle to throw the ball.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonTrain_Click(object sender, EventArgs e)
    {
        // add a confirmation dialog
        if (MessageBox.Show("You have asked to create a training file.\n" +
                            "This will take a few minutes. Are you sure you want to continue?", "Training", MessageBoxButtons.YesNo) == DialogResult.No)
        {
            return;
        }

        labelTraining.Text = "Generating training data...";

        basketBallSimulation1.FunMode = false;
        buttonRunAITraining.Enabled = false;
        buttonCreateTrainingData.Enabled = false;
        buttonThrow.Enabled = false;

        panelTrain.Visible = true;

        try
        {
            CsvGeneration.GenerateCsvTrainingData(progressBarTraining, basketBallSimulation1);
        }
        finally
        {
            buttonCreateTrainingData.Enabled = true;
            buttonRunAITraining.Enabled = true;
            basketBallSimulation1.FunMode = true;
            buttonThrow.Enabled = true;

            panelTrain.Visible = false;
        }
    }

    /// <summary>
    /// Plot the AI trajectory. i.e. the path the ball will take when thrown by the AI for all different angles, from that starting position.
    /// This allows you to see what the AI is predicting (training).
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PlotAITrajectoryAngles()
    {
        // because we may be training the AI, we need to use a shadow copy of the AI, to avoid inconsistency of data issues
        NeuralNetwork neuralNetworkShadow = new(NeuralNetwork.c_defaultLayers, NeuralNetwork.InitType.Xavier, true);

        // load the AI from disk, so we can "te" the AI
        neuralNetworkShadow.LoadWeightsAndBiases();

        // this is where the ball starts from
        double ballXPosInMetres = BasketBallSimulation.c_rimCenterInMetres - numericUpDownDistanceValue + 0.01f - 0.5f;
        double ballYPosInMetres = 2.25f;

        List<(bool, List<PointF>)> lines = [];

        // loop through the angles, calculate the force using the neural network, and throw the ball.
        // plot the trajectory of the ball
        for (double angleInDegrees = 15; angleInDegrees < 70; angleInDegrees += 1f)
        {
            (double _, double angleRadiansScaled, double distScaled) =
                NeuralNetwork.Normalise(
                    force: 0,
                    angleInDegrees: angleInDegrees,
                    dist: numericUpDownDistanceValue + 0.4f);

            double[] inputs = [angleRadiansScaled, distScaled];
            double[] result = neuralNetworkShadow.FeedForward(inputs);
            double aiForce = NeuralNetwork.ForceUnscaled(result[0]);

            if (aiForce > NeuralNetwork.c_forceNormaliser * 2 || aiForce < 3) continue; // it could compute and angle that would require 300+, but throw doesn't allow it

            // we need a new simulation for each throw
            BasketBallSimulation s = new()
            {
                BallReleasePointInMetres = new PointF((float)(ballXPosInMetres + 0.4f), (float)ballYPosInMetres) // release position
            };

            s.Throw(aiForce, angleInDegrees, guessTheForce: false); // we know the force, so don't guess

            StoreTrajectory(lines, s);
        }

        basketBallSimulation1.SetAITrajectoryAccuracyDrawPoints(lines);
    }

    /// <summary>
    /// Store the trajectory of the ball as it flies through the air, as points in the list of lines.
    /// </summary>
    /// <param name="lines"></param>
    /// <param name="s"></param>
    private static void StoreTrajectory(List<(bool, List<PointF>)> lines, BasketBallSimulation s)
    {
        // store the trajectory of the ball as it flies through the air
        List<PointF> points = [];

        while (!s.BallStopped)
        {
            // more than 1 metres above the basket, and we can stop
            if (s.BallLocationInMetres.Y > BasketBallSimulation.c_courtHeightMetres + 1f) return;

            points.Add(s.BallLocationInMetres);
            s.MoveBall();
        }

        // true if the ball went in the basket, false otherwise
        lines.Add((s.Score >= 999900, points));
    }

    #region TRAINING AI MODEL IN BACKGROUND

    BackgroundWorker? backgroundWorkerToTrain = null;

    /// <summary>
    /// Train the AI in the background / Stop training. This is done in the background so the UI remains responsive.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonRunAITraining_Click(object sender, EventArgs e)
    {
        buttonAI.Enabled = false;

        if (buttonRunAITraining.Text == "Stop Training")
        {
            backgroundWorkerToTrain?.CancelAsync();
            buttonRunAITraining.Text = "Train";
            buttonAI.Enabled = true;
            buttonCreateTrainingData.Enabled = true;
            return;
        }

        buttonCreateTrainingData.Enabled = false;
        buttonRunAITraining.Text = "Stop Training";

        TrainUsingBackgroundWorker();

        SetupTimerToDrawHeatMapOfAccuracyAsItTrains();
    }

    /// <summary>
    /// Train the AI using a background worker.
    /// </summary>
    private void TrainUsingBackgroundWorker()
    {
        backgroundWorkerToTrain?.Dispose();

        backgroundWorkerToTrain = new()
        {
            WorkerReportsProgress = true
        };

        backgroundWorkerToTrain.DoWork += TrainModelManager.RunInBackground;
        backgroundWorkerToTrain.WorkerSupportsCancellation = true;
        backgroundWorkerToTrain.ProgressChanged += ProgressChanged;
        backgroundWorkerToTrain.RunWorkerCompleted += TrainingCompleted;

        backgroundWorkerToTrain.RunWorkerAsync();
    }

    /// <summary>
    /// It's fun to see a heatmap of accuracy as the AI trains. We do this by plotting 
    /// the heatmap every 200ms using a timer.
    /// </summary>
    private void SetupTimerToDrawHeatMapOfAccuracyAsItTrains()
    {
        // create a timer to plot the heatmap of the AI accuracy as it trains

        timerHeatmap = new()
        {
#if SHOW_EVERY_1_SECOND
            Interval = 1000
#else
            Interval = 100
#endif
        };

        timerHeatmap.Tick += (s, e) =>
        {
#if SHOW_EVERY_1_SECOND
            AngleDistanceHeatmap.HeatmapNeedsDrawing(true);
#endif
            AngleDistanceHeatmap.PlotHeatmap(pictureBoxHeatMap, numericUpDownDistanceValue, basketBallSimulation1.ProtractorAngle);
        };

        timerHeatmap.Start();
    }

    /// <summary>
    /// Update the progress label percentage of the training that has been completed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void ProgressChanged(object? sender, ProgressChangedEventArgs e)
    {
        labelTrainPct.Text = (e.ProgressPercentage / 100f).ToString() + "%";

        PlotAITrajectoryAngles();

        AngleDistanceHeatmap.HeatmapNeedsDrawing(true);
    }

    /// <summary>
    /// Update the UI when the training has completed.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public void TrainingCompleted(object? sender, RunWorkerCompletedEventArgs e)
    {
        buttonCreateTrainingData.Enabled = true;
        buttonRunAITraining.Text = "Train";

        if (timerHeatmap is not null) timerHeatmap.Enabled = false;
    }
#endregion

#endregion

    /// <summary>
    /// Button to use AI to throw the ball.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonAIThrow_Click(object sender, EventArgs e)
    {
        float angleInDegrees = basketBallSimulation1.ProtractorAngle;

        basketBallSimulation1.SetDistance(numericUpDownDistanceValue);

        NeuralNetwork.s_neuralNetwork.LoadWeightsAndBiases();

        // AI requires all values to be between -1 and 1
        (double _, double angleRadiansScaled, double distScaled) =
            NeuralNetwork.Normalise(
                force: 10.63101562,
                angleInDegrees: angleInDegrees,
                dist: (double)numericUpDownDistanceValue + 0.4f);

        double[] inputs = [angleRadiansScaled, distScaled];
        double[] result = NeuralNetwork.s_neuralNetwork.FeedForward(inputs);
        double aiForce = NeuralNetwork.ForceUnscaled(result[0]);

        richTextLog.Clear();

        // output the way we computed the force
        richTextLog.AppendText($"{angleInDegrees} degrees => Radians scaled (1)\n");
        richTextLog.AppendText($"{numericUpDownDistanceValue} metres => scaled (2)\n\n");
        richTextLog.AppendText($"INPUT: [(1) {inputs[0]}, (2) {inputs[1]}]\n");
        richTextLog.AppendText($"CALCULATION:");
        richTextLog.AppendText($"  OUTPUT = _neuralNetwork.FeedForward( INPUT )   Layers: [{string.Join(",", NeuralNetwork.c_defaultLayers)}]\n");
        richTextLog.AppendText($"OUTPUT: [{result[0]}] => scaled\n");
        richTextLog.AppendText($"      = {aiForce}\n\n");
        richTextLog.AppendText($"You can achieve that using this formula (generated by the neural network):\n");
        richTextLog.AppendText($"  {NeuralNetwork.s_neuralNetwork.Formula()}\n");

        richTextLog.AppendText("Function returns: " + NeuralNetwork.GetAIForce(inputs[0], inputs[1]).ToString());

        if (aiForce < (float)fluentSliderForce.Maximum) fluentSliderForce.Value = (decimal)aiForce;

        lblSuggestedForce.Text = basketBallSimulation1.Throw(forceInNewtons: aiForce, angleInDegrees: angleInDegrees).ToString();
    }

    /// <summary>
    /// Stop the background worker when the form is closing, to avoid a situation of the form closing but the background worker still running.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Form1_FormClosing(object sender, FormClosingEventArgs e)
    {
        backgroundWorkerToTrain?.CancelAsync();
    }

    /// <summary>
    /// Reset the Adam optimizer, and learning rate.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ButtonAdamReset_Click(object sender, EventArgs e)
    {
        NeuralNetwork.Reset();
    }
}