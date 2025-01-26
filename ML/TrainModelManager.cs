#define SHUFFLE
using System.ComponentModel;
using System.Diagnostics;
using static ML.TrainModelManager;

namespace ML;

/// <summary>
/// Train the "BallHoop" neural network using the training data.
/// i.e. the neural network that predicts the force required to get the ball into the hoop.
/// </summary>
public static class TrainModelManager
{
    /// <summary>
    /// Learning rate types.
    /// </summary>
    public enum LearningRateType { StepDecay, ExponentialDecay, ReduceOnPlateau, CosineAnnealing, CyclicalLearningRate, none };

    /// <summary>
    /// The learning rate types dictionary mapping type used in switch to human readable string.
    /// </summary>
    private static readonly Dictionary<LearningRateType, string> _learningRateTypes = new()
    {
        { LearningRateType.StepDecay, "Step Decay" },
        { LearningRateType.ExponentialDecay, "Exponential Decay" },
        { LearningRateType.ReduceOnPlateau, "Reduce On Plateau" },
        { LearningRateType.CosineAnnealing, "Cosine Annealing" },
        { LearningRateType.CyclicalLearningRate, "Cyclical Learning Rate" },
        { LearningRateType.none, "None" }
    };

    /// <summary>
    /// Public method to map the learning rate type to a human readable string, for a given type.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public static LearningRateType MapLearningRateType(string type)
    {
        foreach (var item in _learningRateTypes)
        {
            if (item.Value == type) return item.Key;
        }

        return LearningRateType.none;
    }

    /// <summary>
    /// Array of learning rate types (human readable).
    /// </summary>
    public static string[] LearningRateTypes => [.. _learningRateTypes.Values];

    /// <summary>
    /// The current learning rate type.
    /// </summary>
    private static LearningRateType _learningRateType = LearningRateType.CosineAnnealing;

    /// <summary>
    /// Setter/Getter for the current learning rate type.
    /// </summary>
    public static LearningRateType LearningType
    {
        get => _learningRateType;
        set
        {
            _learningRateType = value;
        }
    }

    /// <summary>
    /// Define the decay rate for learning rate.
    /// </summary>
    private const float decayRate = 0.995f;

    /// <summary>
    /// Initialize the current learning rate.
    /// </summary>
    private static double currentLearningRate = NeuralNetwork.LearningRate;

    /// <summary>
    /// Initial learning rate.
    /// </summary>
    private const float initialLearningRate = 0.01f;

    /// <summary>
    /// The minimum delta for reduce on plateau, and in general. Any smaller won't be useful.
    /// </summary>
    private const float minDelta = 0.0001f;

    /// <summary>
    /// For cyclical learning rate
    /// </summary>
    private const double c_baseLearningRate = 0.00001F;

    /// <summary>
    /// For cyclical learning rate
    /// </summary>
    private const double c_maxLearningRate = 0.01F;

    /// <summary>
    /// For step decay and cyclical learning rate.
    /// </summary>
    private const int stepSize = 13;

    #region REDUCE ON PLATEAU SETTINGS
    /// <summary>
    /// For reduce on plateau
    /// </summary>
    private const int patience = 5;

    /// <summary>
    /// For reduce on plateau
    /// </summary>
    private static double bestValidationLoss = double.MaxValue;

    /// <summary>
    /// For reduce on plateau
    /// </summary>
    private static int epochsSinceImprovement = 0;
    #endregion

    /// <summary>
    /// How often we shuffle the training data.
    /// </summary>
    private const int c_shuffleInterval = 10;

    /// <summary>
    /// Current shuffle index (we increment and shuffle the training data when we reach c_shuffleInterval).
    /// </summary>
    private static int s_shuffleIndex = 0;

    /// <summary>
    /// ENTRY POINT (console application).
    /// </summary>
    public static void RunInBackground(object? sender, DoWorkEventArgs e)
    {
        Initialise(out List<(double Angle, double Dist, double Force)> trainingData, out int items, out long ticks);

        // report the quality of the neural network to the user to the BackgroundWorker at the start
        (sender as BackgroundWorker)?.ReportProgress((int)(NeuralNetwork.s_neuralNetwork.CurrentModelQuality * 10000));

        while (true)
        {
            if (sender is BackgroundWorker { CancellationPending: true })
            {
                break;
            }

            SupervisedTraining(trainingData, items);

            // find out how well the neural network is doing
            double qualityPct = GetErrorActualVsExpected(trainingData, items);

            // if we have a better score, save the neural network
            if (qualityPct > NeuralNetwork.s_neuralNetwork.CurrentModelQuality)
            {
                NeuralNetwork.s_neuralNetwork.SaveWeightsAndBiases(qualityPct);

                // report the quality of the neural network to the user to the BackgroundWorker
                (sender as BackgroundWorker)?.ReportProgress((int)(qualityPct * 10000));

                if (qualityPct >= 0.9999)
                {
                    return; // we're accurate enough
                }
            }

#if SHUFFLE
            s_shuffleIndex = (s_shuffleIndex + 1) % c_shuffleInterval;

            if (s_shuffleIndex == 0)
            {
                trainingData = ShuffleData(trainingData);
            }
#endif

            switch(_learningRateType)
            {
                case LearningRateType.StepDecay:
                    ApplyStepDecay();
                    break;
                case LearningRateType.ExponentialDecay:
                    ApplyExponentialDecay();
                    break;
                case LearningRateType.ReduceOnPlateau:
                    ApplyReduceOnPlateau();
                    break;
                case LearningRateType.CosineAnnealing:
                    ApplyCosineAnnealing();
                    break;
                case LearningRateType.CyclicalLearningRate:
                    ApplyCyclicalLearningRate();
                    break;
                case LearningRateType.none:
                    break;
            }            
        }
    }

    /// <summary>
    /// Initialise the neural networrk etc.
    /// </summary>
    /// <param name="trainingData"></param>
    /// <param name="items"></param>
    /// <param name="ticks"></param>
    private static void Initialise(out List<(double Angle, double Dist, double Force)> trainingData, out int items, out long ticks)
    {
        NeuralNetwork.s_neuralNetwork.LoadWeightsAndBiases();
        NeuralNetwork.s_neuralNetwork.InitialiseAdam(); // override what it loaded from the file

        // this loads 20mb of training data into a list (csv, parsed to doubles etc)
        FetchTrainingData(out trainingData);

        items = trainingData.Count;
        ticks = DateTime.Now.Ticks;

        double quality = GetErrorActualVsExpected(trainingData, items);

        Console.WriteLine($"Initial quality: {(100 * quality):F9}%");
    }

    /// <summary>
    /// Cosine annealing learning rate.
    /// </summary>
    private static void ApplyCosineAnnealing()
    {
        const int c_totalEpochs = 500;

        NeuralNetwork.LearningRate = 0.01F * (0.5 * (1 + Math.Cos(Math.PI * NeuralNetwork.Epoch / c_totalEpochs)));
    }

    /// <summary>
    /// A cyclical learning rate. The learning rate oscillates between "baseLearningRate" and "maxLearningRate".
    /// It is a triangular wave, with the period being "2 * stepSize".
    /// </summary>
    private static void ApplyCyclicalLearningRate()
    {
        double cycle = Math.Floor((double)1 + NeuralNetwork.Epoch / (2 * stepSize));
        double x = Math.Abs(NeuralNetwork.Epoch / (double)stepSize - 2 * cycle + 1);

        NeuralNetwork.LearningRate = c_baseLearningRate + (c_maxLearningRate - c_baseLearningRate) * Math.Max(0, 1 - x);
    }

    /// <summary>
    /// Learning rate is scaled every epoch, raising the decay rate to the epoch.
    /// </summary>
    private static void ApplyExponentialDecay()
    {
        currentLearningRate = Math.Max(initialLearningRate * (float)Math.Pow(decayRate, NeuralNetwork.Epoch), minDelta / 100); // max to ensure it doesn't go too small.

        NeuralNetwork.LearningRate = currentLearningRate;
    }

    /// <summary>
    /// Every "stepSize" epochs, the learning rate is multiplied by "decayRate".
    /// </summary>
    private static void ApplyStepDecay()
    {
        if (NeuralNetwork.Epoch % stepSize != 0) return;

        currentLearningRate *= decayRate;
        currentLearningRate = Math.Max(currentLearningRate, minDelta / 10);

        NeuralNetwork.LearningRate = currentLearningRate;
    }

    /// <summary>
    /// When the overall loss is small enough, the learning rate is reduced by "minDelta". 
    /// If the loss doesn't improve after "patience" epochs, the learning rate is multiplied by "decayRate".
    /// </summary>
    private static void ApplyReduceOnPlateau()
    {
        // if the loss is less than the best loss, we're improving
        if (loss < bestValidationLoss - minDelta)
        {
            bestValidationLoss = loss;
            epochsSinceImprovement = 0;

            currentLearningRate -= minDelta;
        }
        else
        {
            // we're not improving, so reduce the learning rate
            epochsSinceImprovement++;

            if (epochsSinceImprovement >= patience)
            {
                currentLearningRate *= decayRate;
                epochsSinceImprovement = 0;
            }
        }

        // don't let the learning rate get too small
        if (currentLearningRate < minDelta) currentLearningRate = minDelta;

        NeuralNetwork.LearningRate = currentLearningRate;
    }


    /// <summary>
    /// Fetch the training data from the file.
    /// </summary>
    /// <param name="trainingData"></param>
    private static void FetchTrainingData(out List<(double Angle, double Dist, double Force)> trainingData)
    {
        if (!File.Exists(NeuralNetwork.c_trainingFileName))
        {
            throw new FileNotFoundException($"Training data file not found: {NeuralNetwork.c_trainingFileName}");
        }

        // Train the neural network
        string[] data = File.ReadAllLines(NeuralNetwork.c_trainingFileName);

        trainingData = [];

        double minAngle = double.MaxValue;
        double maxAngle = double.MinValue;
        double minDist = double.MaxValue;
        double maxDist = double.MinValue;
        double minForce = double.MaxValue;
        double maxForce = double.MinValue;

        foreach (string line in data)
        {
            if (line.StartsWith("Force")) continue;

            string[] values = line.Split(',');

            // Force,Angle,XPos,Distance,Score
            // 4.8180494,33.800037,42.534733,2.599968,999900
            //  ^f         ^A          ^X      ^ d     ^s
            //  [0]        [1]         [2]     [3]     [4]

            (double forceScaled, double angleRadiansScaled, double distScaled) = NeuralNetwork.Normalise(
                force: double.Parse(values[0]), angleInDegrees: double.Parse(values[1]), dist: double.Parse(values[3]));

            // all values should be between -1 and 1, because that is what the neural network expects
            if (angleRadiansScaled < -1 || angleRadiansScaled > 1 ||
                distScaled < -1 || distScaled > 1 ||
                forceScaled < -1 || forceScaled > 1)
            {
                Debug.WriteLine($"Invalid data: {line}");
                Debugger.Break();
                throw new InvalidOperationException("Normalisation error");
            }

            // ensure the unscaling of force matches scaling of force
            double aiForce = NeuralNetwork.ForceUnscaled(forceScaled);

            if (Math.Abs(double.Parse(values[0]) - aiForce) > 0.0001f)
            {
                Debug.WriteLine($"Force: {forceScaled} AI: {aiForce}");
                throw new InvalidOperationException("Unscaled normalisation error");
            }

            trainingData.Add((angleRadiansScaled, distScaled, forceScaled));

            // keep track of the min and max values, so we can confirm the normalisation is correct
            if (minAngle > angleRadiansScaled) minAngle = angleRadiansScaled;
            if (maxAngle < angleRadiansScaled) maxAngle = angleRadiansScaled;

            if (minDist > distScaled) minDist = distScaled;
            if (maxDist < distScaled) maxDist = distScaled;

            if (minForce > forceScaled) minForce = forceScaled;
            if (maxForce < forceScaled) maxForce = forceScaled;
        }

        // manually confirm everything is normalised around 0.
        Console.WriteLine($"Angle: {minAngle:F2} to {maxAngle:F2}");
        Console.WriteLine($"Dist: {minDist:F2} to {maxDist:F2}");
        Console.WriteLine($"Force: {minForce:F2} to {maxForce:F2}");
    }

    /// <summary>
    /// The accuracy heatmap. This is a list of (distance, angle, accuracy) tuples. This is used to visualise how well the neural network is doing.
    /// </summary>
    private static List<(double dist, double angle, double accuracy)> _accuracyHeatMap = [];

    /// <summary>
    /// Lock for the heatmap. This is required because the heatmap is updated on a different thread.
    /// </summary>
    private readonly static Lock heatmapLock = new();

    /// <summary>
    /// GETTER for the accuracy heatmap. This is a list of (distance, angle, accuracy) tuples. This is used to visualise how well the neural network is doing.
    /// </summary>
    public static List<(double dist, double angle, double accuracy)> AccuracyHeatMap
    {
        get
        {
            lock (heatmapLock)
            {
                return new(_accuracyHeatMap); // copy of whilst locked
            }
        }
    }

    private static double loss = 0;

    /// <summary>
    /// Iterate through the training data and calculate the error for each data point. The error is the difference between the expected and actual values.
    /// We keep the results so we can plot a heatmap of the neural network accuracy.
    /// </summary>
    /// <param name="trainingData"></param>
    /// <param name="items"></param>
    /// <returns>The percentage of the time the neural network is within 1% of the expected value.</returns>
    public static double GetErrorActualVsExpected(List<(double Angle, double Dist, double Force)> trainingData, int items)
    {
        int success = 0;

        List<(double dist, double angle, double accuracy)> accuracyHeatMap = [];

        for (int i = 0; i < items; i++)
        {
            (double angle, double dist, double force) = trainingData[i];

            double predicted = NeuralNetwork.s_neuralNetwork.FeedForward([angle, dist])[0];
            double accuracy = Math.Abs(predicted / force);

            loss+= Math.Abs(predicted - force);
            // within a small %. Using a success rate is a better metric than total error, because it is a better judge of overall performance rather than just the average error.
            if (accuracy > 0.995F && accuracy < 1.005F) ++success;

            accuracyHeatMap.Add((dist, angle, accuracy));
        }

        loss /= items;

        lock (heatmapLock)
        {
            _accuracyHeatMap = accuracyHeatMap;
        }

        return success / (double)items;
    }

    /// <summary>
    /// Train the neural network using the training data.
    /// </summary>
    /// <param name="trainingData"></param>
    /// <param name="items"></param>
    private static void SupervisedTraining(List<(double Angle, double Dist, double Force)> trainingData, int items)
    {
        for (int i = 0; i < items; i++)
        {
            var (angle, dist, force) = trainingData[i];

            NeuralNetwork.s_neuralNetwork.BackPropagate([angle, dist], [force]);
        }

        NeuralNetwork.IncrementEpoch();
    }

    /// <summary>
    /// Shuffle the training data.
    /// </summary>
    /// <param name="trainingData"></param>
    /// <returns></returns>
    private static List<(double Angle, double Dist, double Force)> ShuffleData(List<(double Angle, double Dist, double Force)> trainingData)
    {
        List<(double Angle, double Dist, double Force)> trainingData2 = [];

        trainingData2.AddRange([.. trainingData.OrderBy(x => ReproduceablePseudoRandomNumberGenerator.Next())]);

        return trainingData2;
    }
}