//#define SHOW_TRAINING_DATA // <-- enable if you want to see the angle/forces it is generating training data for.
using BallHoop.Simulation;
using ML;

namespace BallHoop;

/// <summary>
/// Logic for generating the training data for the AI.
/// </summary>
internal static class CsvGeneration
{
    /// <summary>
    /// The accuracy of the distance and angle when generating the training data.
    /// </summary>
    const decimal c_accuracyOfDistance = 1m;
    const decimal c_accuracyOfAngle = 1m;

    /// <summary>
    /// Generate the training data for the AI by throwing the ball from different positions and angles.
    /// The training data is saved to a .csv file.
    /// </summary>
    internal static void GenerateCsvTrainingData(ProgressBar progressBarTraining, BasketBallSimulationUserControl basketBallSimulation)
    {
        // generate a training file (.csv) with the force, angle, x position, distance, and score

        using StreamWriter sw = new(NeuralNetwork.c_trainingFileName);
        sw.WriteLine("Force,Angle,XPos,Distance,Score");

#if SHOW_TRAINING_DATA
    List<(bool, List<PointF>)> lines = [];
#endif
        // to train the AI, we need to throw the ball from different positions and angles

        progressBarTraining.Maximum = (int)BasketBallSimulation.c_rimCenterInMetres;
        progressBarTraining.Value = 0;

        for (decimal distance = 2; distance < (decimal)BasketBallSimulation.c_rimCenterInMetres; distance += c_accuracyOfDistance)
        {
            if ((int)distance > progressBarTraining.Value) { progressBarTraining.Value = (int)distance; Application.DoEvents(); }

            float value = (float)((decimal)BasketBallSimulation.c_rimCenterInMetres - distance + 0.01m - 0.5m + 0.4m);

            float personX = value;

            // throw the ball from different angles
            for (decimal angle = 15; angle < 70; angle += c_accuracyOfAngle)
            {
                BasketBallSimulation s = new();

                float personY = s._personHeightMetres + 0.25f;

                s.BallReleasePointInMetres = new PointF(personX, personY);

                // determine the force to throw the ball
                double force = s.Throw(forceInNewtons: 3, angleInDegrees: (double)angle, guessTheForce: true);

                if (force <= 1 || force >= NeuralNetwork.c_forceNormaliser * 2) continue; // no point in training the AI with a force of 1 or less

                s = new()
                {
                    BallReleasePointInMetres = new PointF(personX, personY)
                };

                // throw the ball with the force we calculated, and capture data points if required

                s.Throw(forceInNewtons: force, angleInDegrees: (double)angle, guessTheForce: false);

#if SHOW_TRAINING_DATA
            // store the trajectory of the ball
            List<PointF> points = [];
#endif
                while (!s.BallStopped)
                {
#if SHOW_TRAINING_DATA
                points.Add(s.BallLocationInMetres);
#endif
                    s.MoveBall();

                    if (double.IsNaN(s.BallLocationInMetres.X)) break;
                }

                if (s.Score >= 999900)
                {
#if SHOW_TRAINING_DATA
                    lines.Add((true, points));

                    if (lines.Count > 15) lines.RemoveAt(0);

                    basketBallSimulation.SetAITrajectoryAccuracyDrawPoints(lines);
                    Application.DoEvents();
#endif
                    sw.WriteLine($"{force:f5},{angle},{personX},{distance},{s.Score}");
                }
            }
        }

        sw.Flush();
        sw.Close();
    }
}