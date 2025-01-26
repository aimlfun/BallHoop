using ML;
using System.Drawing.Drawing2D;

namespace BallHoop.UI;

/// <summary>
/// This class is responsible for plotting the heatmap of the neural network accuracy.
/// </summary>
internal static class AngleDistanceHeatmap
{
    /// <summary>
    /// This flag is set to true when the heatmap needs to be drawn.
    /// </summary>
    private static bool _heatmapNeedsDrawing = true;

    /// <summary>
    /// Set the flag to true when the heatmap needs to be drawn.
    /// </summary>
    internal static void HeatmapNeedsDrawing(bool state) => _heatmapNeedsDrawing = state;

    /// <summary>
    /// This flag is set to true when the heatmap has been initialised. (This is used to avoid recalculating the heatmap variables every time.)
    /// </summary>
    private static bool s_initialised = false;

    /// <summary>
    /// 
    /// </summary>
    private static float minDist;

    /// <summary>
    /// 
    /// </summary>
    private static float maxDist;

    /// <summary>
    /// 
    /// </summary>
    private static float minAngle;

    /// <summary>
    /// 
    /// </summary>
    private static float maxAngle;

    /// <summary>
    /// 
    /// </summary>
    private static int heatmapHeight;

    /// <summary>
    /// 
    /// </summary>
    private static int heatmapWidth;

    /// <summary>
    /// 
    /// </summary>
    private static float scaleX;

    /// <summary>
    /// 
    /// </summary>
    private static float scaleY;

    /// <summary>
    /// Plot the heatmap of the neural network accuracy.
    /// </summary>
    internal static void PlotHeatmap(PictureBox pictureBoxHeatMap, float requestedDist, float requestedAngle)
    {
        if (!_heatmapNeedsDrawing) return; // nothing to draw

        _heatmapNeedsDrawing = false;

        // copy the list. Remember whilst getting it is locked
        List<(double dist, double angle, double accuracy)> accuracyHeatMap = TrainModelManager.AccuracyHeatMap;

        if (accuracyHeatMap.Count == 0) return;

        List<(double dist, double angle, double accuracy)> adjustedAccuracyHeatMap = [];

        // reverse the training data into human meaningful numbers (it was normalised)
        for (int i = 0; i < accuracyHeatMap.Count; i++)
        {
            (double dist, double angle, double accuracy) t = (
                NeuralNetwork.DistanceUnscaled(accuracyHeatMap[i].dist),
                NeuralNetwork.AngleUnscaled(accuracyHeatMap[i].angle),
                accuracyHeatMap[i].accuracy);

            adjustedAccuracyHeatMap.Add(t);
        }

        accuracyHeatMap = adjustedAccuracyHeatMap;

        if (!s_initialised) // these don't need to be calculated every time
        {
            Initialise(accuracyHeatMap, pictureBoxHeatMap.Height, pictureBoxHeatMap.Width);
        }

        // the first time, this will be null
        pictureBoxHeatMap.Image ??= new Bitmap(heatmapWidth, heatmapHeight);

        using Graphics graphics = Graphics.FromImage(pictureBoxHeatMap.Image);

        graphics.FillRectangle(Brushes.Black, 0, 0, heatmapWidth, heatmapHeight);
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.CompositingQuality = CompositingQuality.HighQuality;

        DrawAccuracyBlobs(accuracyHeatMap, graphics);
        DrawCrossShowingAngleAndDistanceOfStickPerson(pictureBoxHeatMap, requestedDist, requestedAngle, graphics);
        DrawDistanceAxis(graphics);
        DrawAngleAxis(graphics);

        graphics.Flush();

        pictureBoxHeatMap.Invalidate();
    }

    /// <summary>
    /// Initialise the heatmap variables, so we can draw the heatmap without worrying about scaling etc
    /// </summary>
    /// <param name="accuracyHeatMap"></param>
    private static void Initialise(List<(double dist, double angle, double accuracy)> accuracyHeatMap, int width, int height)
    {
        s_initialised = true;

        minDist = (float)accuracyHeatMap.Min(x => x.dist);
        minAngle = (float)accuracyHeatMap.Min(x => x.angle);

        maxDist = (float)accuracyHeatMap.Max(x => x.dist);
        maxAngle = (float)accuracyHeatMap.Max(x => x.angle);

        heatmapHeight = width; //px
        heatmapWidth = height; //px

        scaleX = heatmapWidth / (maxDist - minDist);
        scaleY = heatmapHeight / (maxAngle - minAngle);
    }

    /// <summary>
    /// Each blob represents the accuracy for a distance and angle, with a colour representing the accuracy.
    /// </summary>
    /// <param name="accuracyHeatMap"></param>
    /// <param name="graphics"></param>
    private static void DrawAccuracyBlobs(List<(double dist, double angle, double accuracy)> accuracyHeatMap, Graphics graphics)
    {
        float radius = 6;
     
        foreach (var item in accuracyHeatMap)
        {
            PointF accuracyPoint = TranslateDistAndAngleToGraphPoint(item.dist, item.angle);

            double accuracy = 1 - Math.Abs(item.accuracy); // 0 = match, non zero = error

            Color c;

            if (accuracy < 0.005)
            {
                c = Color.FromArgb(50, 0, 255, 0); // green
            }
            else
            {
                // accuracy is 0.1 to 1, so we need to scale it to 0 to 255
                int red = (int)Math.Min(155, 2 * accuracy * 155f) + 100;

                c = Color.FromArgb(red, red, 0, 0);
            }

            PointF pointAdjustedForRadius = new((float)Math.Round((accuracyPoint.X - radius)), (float)Math.Round((accuracyPoint.Y - radius)));

            using SolidBrush brush = new(c);

            graphics.FillEllipse(
                brush: brush,
                x: pointAdjustedForRadius.X,
                y: pointAdjustedForRadius.Y,
                width: (radius * 2),
                height: (radius * 2));
        }
    }

    /// <summary>
    /// Draw the angle vertical axis on the heatmap.
    /// </summary>
    /// <param name="g"></param>
    private static void DrawAngleAxis(Graphics g)
    {
        // 64.0---------------------------------------------------------
        //
        // 58.6---------------------------------------------------------
        //
        // 53.1---------------------------------------------------------
        // ...

        // Draw angle axis
        for (double angle = minAngle; angle <= maxAngle; angle += (maxAngle - minAngle) / 10)
        {
            PointF p = TranslateDistAndAngleToGraphPoint(0, angle);

            g.DrawLine(Pens.Gray, 0, p.Y, heatmapWidth, p.Y);

            // work out where to draw the text, so it is centred vertically
            string angleStr = angle.ToString("F1");
            SizeF size = g.MeasureString(angleStr, SystemFonts.DefaultFont);

            g.DrawString(angleStr, SystemFonts.DefaultFont, Brushes.White, 0, p.Y - size.Height / 2);
        }

        string label = "Angle (°)";
        SizeF labelSize = g.MeasureString(label, SystemFonts.DefaultFont);
        g.DrawString(label, SystemFonts.DefaultFont, Brushes.White, (float)heatmapWidth - labelSize.Width, heatmapHeight / 2 - labelSize.Height / 2);
    }

    /// <summary>
    /// Draw the distance horizontal axis on the heatmap.
    /// </summary>
    /// <param name="g"></param>
    private static void DrawDistanceAxis(Graphics g)
    {
        //   |     |     |     |     |     |     |     |     |     |  
        //   |     |     |     |     |     |     |     |     |     |  
        //   |     |     |     |     |     |     |     |     |     |  
        // 40.7  36.4  32.1  27.8  23.5  19.2  14.9  10.6   6.3   2.0

        // Draw distance axis
        for (double dist = minDist; dist <= maxDist; dist += (maxDist - minDist) / 10)
        {
            PointF p = TranslateDistAndAngleToGraphPoint(dist, 0);

            g.DrawLine(Pens.Gray, p.X, 0, p.X, heatmapHeight);

            // work out where to draw the text, so it is centred horizontally
            string distStr = dist.ToString("F1");
            SizeF size = g.MeasureString(distStr, SystemFonts.DefaultFont);

            g.DrawString(dist.ToString("F1"), SystemFonts.DefaultFont, Brushes.White, p.X - size.Width / 2, heatmapHeight - size.Height - 2);
        }

        g.DrawString("Distance (m)", SystemFonts.DefaultFont, Brushes.White, heatmapWidth / 2f, 10);
    }

    /// <summary>
    /// Draw a cross on the heatmap to show the angle and distance of the stick person.
    /// </summary>
    /// <param name="pictureBoxHeatMap"></param>
    /// <param name="requestedDist"></param>
    /// <param name="requestedAngle"></param>
    /// <param name="g"></param>
    private static void DrawCrossShowingAngleAndDistanceOfStickPerson(PictureBox pictureBoxHeatMap, float requestedDist, float requestedAngle, Graphics g)
    {
        PointF requested = TranslateDistAndAngleToGraphPoint(requestedDist, requestedAngle);

        g.DrawLine(Pens.Cyan, requested.X, 0, requested.X, heatmapHeight);
        g.DrawLine(Pens.Cyan, 0, requested.Y, pictureBoxHeatMap.Width, requested.Y);
    }

    /// <summary>
    /// Each distance and angle is translated to a point on the heatmap. To do this requires flipping both the x and y axis, and scaling them.
    /// </summary>
    /// <param name="dist"></param>
    /// <param name="angle"></param>
    /// <returns></returns>
    private static PointF TranslateDistAndAngleToGraphPoint(double dist, double angle)
    {
        return new PointF(
            (float)(heatmapWidth - (dist - minDist) * scaleX),
            (float)(heatmapHeight - (angle - minAngle) * scaleY));
    }
}