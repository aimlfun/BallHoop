using BallHoop.Simulation;
using BallHoop.StickPerson;
using ML;
using System.ComponentModel;
using System.Drawing.Drawing2D;
using Timer = System.Windows.Forms.Timer;

namespace BallHoop;

/// <summary>
/// UserControl for a basketball simulation.
/// It uses the simulation class for the mechanics, and renders where the ball/hoop is.
/// </summary>
public partial class BasketBallSimulationUserControl : UserControl
{
    /// <summary>
    /// This is the simulation that we are going to use to simulate the basketball.
    /// </summary>
    private readonly BasketBallSimulation simulation = new();

    #region TIMERS
    /// <summary>
    /// Timer used to update the simulation at regular intervals.
    /// </summary>
    private readonly Timer timer = new();

    /// <summary>
    /// Time used to animate the stick person.
    /// </summary>
    private Timer? timerAnimation;
    #endregion

    #region PENS, BRUSHES, FONTS
    /// <summary>
    /// Brush used to draw the hoop/rim.
    /// </summary>
    private static readonly SolidBrush s_hoopBrush = new(Color.FromArgb(223, 187, 133));

    /// <summary>
    /// Net is drawn in green if the ball was successfully put the the net.
    /// </summary>
    private static readonly HatchBrush s_ballThruHoopNet = new(HatchStyle.OutlinedDiamond, Color.SeaGreen, Color.Transparent);

    /// <summary>
    /// Net is drawn in silver until ball successfully thrown through the net.
    /// </summary>
    private static readonly HatchBrush s_ballNotThruHoopNet = new(HatchStyle.OutlinedDiamond, Color.Silver, Color.Transparent);

    /// <summary>
    /// Brush used to draw the ball.
    /// </summary>
    private static readonly SolidBrush s_brushForDrawingBall = new(Color.FromArgb(20, 0, 200, 200));

    /// <summary>
    /// Used to draw the diagonal and verticle upright.
    /// </summary>
    private static readonly Pen s_penForBackboardSupport = new(Color.White, 8);

    /// <summary>
    /// Used to draw a poor trajectory.
    /// </summary>
    private static readonly Pen s_poorTrajectoryRedPen = new(Color.FromArgb(20, 255, 0, 0), 1);

    /// <summary>
    /// Used to draw a good trajectory.
    /// </summary>
    private static readonly Pen s_goodTrajectoryGreenPed = new(Color.FromArgb(20, 0, 255, 0), 1);

    /// <summary>
    /// Used to draw the court.
    /// </summary>
    private static readonly Pen s_courtColourPen = new(Color.FromArgb(223, 187, 133), 2);

    /// <summary>
    /// Font used to draw the score on the screen.
    /// </summary>
    private static readonly Font s_scoreFont = new("Segoe", 20);

    /// <summary>
    /// Font for drag me (comic style).
    /// </summary>
    private static readonly Font dragMeFont = new("Segoe Script", 13, FontStyle.Bold);
    #endregion

    /// <summary>
    /// Height of the court in pixels.
    /// </summary>
    private float _heightOfCourtInPixels;

    /// <summary>
    /// Scale factor to convert metres to pixels (multiply by this to get pixels).
    /// </summary>
    private float _scaleMetresToPixels = 50;

    /// <summary>
    /// Tracks the left position of the person in metres.
    /// </summary>
    private float _personLeftMetres;

    /// <summary>
    /// Keeps track of how long the ball has been in the air for.
    /// </summary>
    internal double timeSinceThrown = 0;

    /// <summary>
    /// Records the number of throws made.
    /// </summary>
    private int _throws = 0;

    /// <summary>
    /// List of points to draw the ball's trajectory.
    /// </summary>
    private readonly List<PointF> _trailOfBallPoints = [];

    private float backboardXinPX, backboardTopInPX, backboardBottomInPX, connectionYinPX, connectionXinPX, rimRightInPX, rimYinPixelsInPX;
    private float rimLeftPX, rimRightPX, rimYPX, rimThicknessPX, rimBottomPX, netBottomPX; // https://www.dimensions.com/element/basketball-rims-nets

    #region EVENT HANDLERS
    /// <summary>
    /// Used for a callback event for when the stick person is moved.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    public delegate void StickPersonMovedHandler(object sender, StickPersonMovedEventArgs e);

    /// <summary>
    /// Allows user control to notify when the stick person has been moved.
    /// </summary>
    public event StickPersonMovedHandler? StickPersonMoved;
    #endregion

    /// <summary>
    /// Sets the height of the player in metres.
    /// </summary>
    [Description("Height of the player"), Category("Player")]
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public float PersonHeightMetres
    {
        get => simulation._personHeightMetres;
        set
        {
            if (simulation._personHeightMetres == value) return;

            if (value < 0.5f || value > 2.5f)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Person height must be between 0.5 and 2.5 metres.");
            }

            simulation._personHeightMetres = value;

            Invalidate();
        }
    }

    /// <summary>
    /// Stick person (side view) used to draw the player throwing the ball.
    /// </summary>
    private StickPersonSideView? _stickPersonSideView;

    /// <summary>
    /// Constructor.
    /// </summary>
    public BasketBallSimulationUserControl() : base()
    {
        InitializeComponent();

        simulation.FunMode = true; // the simulation doesn't end prematurely

        // when timer ticks, move ball
        timer.Tick += Timer_Tick;
        timer.Interval = 1; // it's still slower than real time, but we can't go faster than 1ms
    }

    /// <summary>
    /// Returns the score of the simulation.
    /// </summary>
    public double Score => simulation.Score;

    /// <summary>
    /// Returns the calculation of the simulation.
    /// </summary>
    public string Calc => simulation.Calc;

    /// <summary>
    /// Indicates we are in the middle of a tick.
    /// </summary>
    private bool _inTick = false;

    /// <summary>
    /// Lock object to prevent re-entrancy.
    /// </summary>
    private readonly Lock _lockObject = new();

    /// <summary>
    /// Move ball at realistic speed, draw the ball against its environment, and update the score.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Timer_Tick(object? sender, EventArgs e)
    {
        // tick may take too long to calc and paint, we have to skip if we are in the middle of a tick
        lock (_lockObject)
        {
            if (_inTick) return;
            _inTick = true;
        }

        simulation.MoveBall();

        RedrawSimulatedEnvironment();

        timeSinceThrown += simulation._timeStepSeconds;

        _inTick = false;
    }

    /// <summary>
    /// Redraws the simulated environment.
    /// </summary>
    private void RedrawSimulatedEnvironment() => pictureBoxSideElevation.Invalidate();

    /// <summary>
    /// Onload sets up the simulation.
    /// </summary>
    /// <param name="e"></param>
    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);

        // every thing is in metres, so we need to scale it to pixels
        _scaleMetresToPixels = Width / BasketBallSimulation.c_lengthOfVisibleCourtMetres;

        int widthOfCourtInPixels = (int)(BasketBallSimulation.c_lengthOfVisibleCourtMetres * _scaleMetresToPixels);

        _heightOfCourtInPixels = BasketBallSimulation.c_courtHeightMetres * _scaleMetresToPixels;
        _personLeftMetres = 5;

        PrecomputeStaticPositionsToImprovePerformance();

        Height = (int)_heightOfCourtInPixels;

        // the picture box is used to render the side elevation of the court
        pictureBoxSideElevation.Height = (int)_heightOfCourtInPixels;
        pictureBoxSideElevation.Width = widthOfCourtInPixels;
        pictureBoxSideElevation.Image = new Bitmap(pictureBoxSideElevation.Width, pictureBoxSideElevation.Height);
        pictureBoxSideElevation.Paint += PictureBoxCourt_Paint;

        // place the ball at the start position
        simulation.BallReleasePointInMetres = new PointF(_personLeftMetres + 0.4f, simulation._personHeightMetres + 0.25f); // release position

        timeSinceThrown = 0;

        // we create a new stick person (side view)
        _stickPersonSideView = new StickPersonSideView(pictureBoxSideElevation.Height, _scaleMetresToPixels, new PointF(_personLeftMetres + 0.5f, simulation._personHeightMetres - 0.25f), simulation._personHeightMetres);

        Invalidate();
    }

    /// <summary>
    /// Every frame we draw the ball, the hoop, the court, the stick person, and the ball's trajectory.
    /// The last thing we want to be doing is computing all of these static positions every frame, when they don't change.
    /// </summary>
    /// <param name="widthOfCourtInPixels"></param>
    private void PrecomputeStaticPositionsToImprovePerformance()
    {
        // compute the rim position, so when we draw each frame we don't have to recalculate

        rimLeftPX = (BasketBallSimulation.c_rimCenterInMetres - BasketBallSimulation.c_rimRadiusMetres) * _scaleMetresToPixels;
        rimRightPX = (BasketBallSimulation.c_rimCenterInMetres + BasketBallSimulation.c_rimRadiusMetres) * _scaleMetresToPixels;
        rimYPX = _heightOfCourtInPixels - BasketBallSimulation.c_rimHeightFromGroundMetres * _scaleMetresToPixels;

        rimThicknessPX = (simulation.c_rimThicknessCM * _scaleMetresToPixels);
        rimBottomPX = (rimYPX + rimThicknessPX);
        netBottomPX = (rimBottomPX + 0.40f * _scaleMetresToPixels); // https://www.dimensions.com/element/basketball-rims-nets
        
        backboardXinPX = BasketBallSimulation.c_backboardXPosMetres * _scaleMetresToPixels;
        backboardTopInPX = _heightOfCourtInPixels - ((BasketBallSimulation.c_rimHeightFromGroundMetres + BasketBallSimulation.c_backboardHeight) * _scaleMetresToPixels);
        backboardBottomInPX = _heightOfCourtInPixels - (BasketBallSimulation.c_rimHeightFromGroundMetres * _scaleMetresToPixels) + 5;

        connectionYinPX = backboardBottomInPX + (0.5f * _scaleMetresToPixels);
        connectionXinPX = backboardXinPX + 1f * _scaleMetresToPixels;

        rimRightInPX = (BasketBallSimulation.c_rimCenterInMetres + BasketBallSimulation.c_rimRadiusMetres) * _scaleMetresToPixels;
        rimYinPixelsInPX = _heightOfCourtInPixels - BasketBallSimulation.c_rimHeightFromGroundMetres * _scaleMetresToPixels;
    }

    #region PROTRACTOR
    // determines size of protractor
    private const float c_protractorRadiusPX = 150;

    /// <summary>
    /// Brush to draw the red arc showing the angle range, that the ball can be thrown.
    /// </summary>
    private static readonly SolidBrush s_protractorRedSupportedAnglesOverlayBrush = new(Color.FromArgb(15, 255, 0, 0));

    /// <summary>
    /// This is the brush used to fill the semi circle of the protractor.
    /// </summary>
    private static readonly SolidBrush s_protractorBackgroundBrush = new(Color.FromArgb(30, 100, 100, 100));

    /// <summary>
    /// This is the pen used to draw the markings on the protractor.
    /// </summary>
    private static readonly Pen s_protractorPen = new(Color.FromArgb(80, 200, 200, 200), 0.25f);

    /// <summary>
    /// This is used to draw the outline of the protractor.
    /// </summary>
    private static readonly Pen s_protractorOutlinePen = new(Color.FromArgb(50, 200, 200, 255), 1);

    /// <summary>
    /// Pointy arrow line for protractor, to show the angle.
    /// </summary>
    private static readonly Pen s_protractorThickArrowLine = new(Color.FromArgb(60, 255, 255, 255), 5)
    {
        EndCap = LineCap.ArrowAnchor
    };

    /// <summary>
    /// Hit box for the plus angle button.
    /// </summary>
    private static RectangleF s_protractorMinusAngleHitBox;

    /// <summary>
    /// Hit box for the minus angle button.
    /// </summary>
    private static RectangleF s_protractorPlusAngleHitBox;

    /// <summary>
    /// Whether to show the protractor on the screen.
    /// </summary>
    bool _showProtractor = false;

    /// <summary>
    /// Gets or sets whether to show the protractor on the screen.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ShowProtractor
    {
        get => _showProtractor;
        set
        {
            if (_showProtractor == value) return;

            _showProtractor = value;

            RedrawSimulatedEnvironment();
        }
    }

    /// <summary>
    /// The angle of the protractor in degrees. 0=horizontal, 90=vertical.
    /// </summary>
    private float _protractorAngle = -1;

    /// <summary>
    /// Gets or sets the angle of the protractor in degrees.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    /// <summary>
    /// Gets or sets the angle of the protractor in degrees.
    /// </summary>
    public float ProtractorAngle
    {
        get
        {
            return _protractorAngle;
        }
        set
        {
            _protractorAngle = value;

            // force repaint
            RedrawSimulatedEnvironment();
        }
    }

    /// <summary>
    /// Draws a protractor on the screen. 0=horizontal, 90=vertical.
    /// It's a semi-circle with a radius of 150 pixels, with markings every 10 degrees.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawProtractor(Graphics graphics)
    {
        // we don't draw if ball is in motion
        if (!simulation.BallStopped) return;

        PointF releasePointMetres = simulation.BallReleasePointInMetres;

        // center of protractor
        float centerX = releasePointMetres.X * _scaleMetresToPixels;
        float centerY = pictureBoxSideElevation.Height - releasePointMetres.Y * _scaleMetresToPixels;

        // draws small INNER circle at centre of protractor
        graphics.FillPie(s_protractorBackgroundBrush, centerX - 10, centerY - 10, 20, 20, -90, 90);
        graphics.FillPie(s_protractorBackgroundBrush, centerX - 9, centerY - 9, 20, 20, -90, 90);

        // draw OUTER semi-circle, and overlay alpha transparency
        graphics.FillPie(s_protractorBackgroundBrush, centerX - c_protractorRadiusPX, centerY - c_protractorRadiusPX, 2 * c_protractorRadiusPX, 2 * c_protractorRadiusPX, 270, 90);
        graphics.FillRectangle(s_protractorBackgroundBrush, centerX - 10, centerY - c_protractorRadiusPX - 1, 10, c_protractorRadiusPX);
        graphics.FillRectangle(s_protractorBackgroundBrush, centerX - 10, centerY, c_protractorRadiusPX + 10, 10);

        for (int angleInDegrees = 0; angleInDegrees < 179; angleInDegrees += 10)
        {
            if (angleInDegrees >= 95) continue; // only show 0-90.

            double angleInRadians = AngleInDegreesToRadians(angleInDegrees + 270);

            double cosAngle = Math.Cos(angleInRadians);
            double sinAngle = Math.Sin(angleInRadians);

            Draw10DegreeGraduations(graphics, c_protractorRadiusPX, centerX, centerY, cosAngle, sinAngle, out float x1, out float y1, out float x3, out float y3);

            if (angleInDegrees < 90)
            {
                Draw1DegreeGraduations(graphics, c_protractorRadiusPX, centerX, centerY, angleInDegrees);
            }

            int angleToShow = -(angleInDegrees - 90);

            WriteTheAngleNumbersToProtractor(graphics, centerX, centerY, cosAngle, sinAngle, x1, y1, x3, y3, angleToShow);
        }

        // Draw the red overlay for the supported angles
        graphics.FillPie(s_protractorRedSupportedAnglesOverlayBrush, centerX - c_protractorRadiusPX / 2, centerY - c_protractorRadiusPX / 2, c_protractorRadiusPX, c_protractorRadiusPX, -15, -54);

        // display user angle onto protractor
        OverlayAngleOnProtractor(graphics, c_protractorRadiusPX, centerX, centerY);

        DrawPlusMinus(graphics);
    }

    /// <summary>
    /// Provide a plus/minus control.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawPlusMinus(Graphics graphics)
    {
        // draw 2 squares to left of stick person: a square with plus, and square with minus
        float xMinus = _personLeftMetres * _scaleMetresToPixels + 163;
        float yMinus = pictureBoxSideElevation.Height - 72;

        float xPlus = _personLeftMetres * _scaleMetresToPixels;
        float yPlus = pictureBoxSideElevation.Height - 237;

        SetPlusMinusAngleHitBoxes(xMinus, yMinus, xPlus, yPlus);

        graphics.FillRectangle(Brushes.White, s_protractorMinusAngleHitBox);
        graphics.FillRectangle(Brushes.White, s_protractorPlusAngleHitBox);

        graphics.DrawString("+", Font, Brushes.Black, xPlus + 3, yPlus + 2);
        graphics.DrawString("-", Font, Brushes.Black, xMinus + 5, yMinus + 2);
    }

    /// <summary>
    /// Track the locations of the [+]/[-] buttons.
    /// </summary>
    /// <param name="xMinus"></param>
    /// <param name="yMinus"></param>
    /// <param name="xPlus"></param>
    /// <param name="yPlus"></param>
    private static void SetPlusMinusAngleHitBoxes(float xMinus, float yMinus, float xPlus, float yPlus)
    {
        s_protractorMinusAngleHitBox = new RectangleF(xMinus, yMinus, 20, 20);
        s_protractorPlusAngleHitBox = new RectangleF(xPlus, yPlus, 20, 20);
    }

    /// <summary>
    /// Writes the angle numbers to the protractor (0..90).
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="centerX"></param>
    /// <param name="centerY"></param>
    /// <param name="cosAngle"></param>
    /// <param name="sinAngle"></param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x3"></param>
    /// <param name="y3"></param>
    /// <param name="angleToShow"></param>
    private void WriteTheAngleNumbersToProtractor(Graphics graphics, float centerX, float centerY, double cosAngle, double sinAngle, float x1, float y1, float x3, float y3, int angleToShow)
    {
        if (angleToShow > -91 && angleToShow < 91)
        {
            float cx1 = (float)(centerX + 10 * cosAngle);
            float cy1 = (float)(centerY + 10 * sinAngle);

            if (angleToShow == 0 || angleToShow == 90 || angleToShow == -90)
            {
                cx1 = centerX - (angleToShow == 0 ? 10 : 0);
                cy1 = centerY;
            }

            graphics.DrawLine(s_protractorPen, cx1, cy1, x1, y1);

            // measure string and draw it in the right place
            SizeF size = graphics.MeasureString(angleToShow.ToString(), Font);

            // Save the current state of the graphics object
            var s = graphics.Save();

            // Translate the graphics object to the point where the text will be drawn
            graphics.TranslateTransform(x3, y3);

            // Rotate the graphics object by the angle
            graphics.RotateTransform(360 - (angleToShow - 90));

            // Draw the text at the origin of the rotated graphics object
            graphics.DrawString(angleToShow.ToString(), Font, Brushes.Black, -size.Width / 2 - 1, -size.Height / 2 - 1);
            graphics.DrawString(angleToShow.ToString(), Font, Brushes.Gray, -size.Width / 2, -size.Height / 2);

            // Restore the graphics object to its original state
            graphics.Restore(s);
        }
    }

    /// <summary>
    /// Draws the 10 degree graduations on the protractor.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="radiusPX"></param>
    /// <param name="centerX"></param>
    /// <param name="centerY"></param>
    /// <param name="cosAngle"></param>
    /// <param name="sinAngle"></param>
    /// <param name="x1"></param>
    /// <param name="y1"></param>
    /// <param name="x3"></param>
    /// <param name="y3"></param>
    private static void Draw10DegreeGraduations(Graphics graphics, float radiusPX, float centerX, float centerY, double cosAngle, double sinAngle, out float x1, out float y1, out float x3, out float y3)
    {
        // outer edge of circle
        x1 = (float)(centerX + radiusPX * cosAngle);
        y1 = (float)(centerY + radiusPX * sinAngle);

        // outer edge minus a bit
        float x2 = (float)(centerX + (radiusPX - 12) * cosAngle);
        float y2 = (float)(centerY + (radiusPX - 12) * sinAngle);

        // outer edge minus enough to write numbers
        x3 = (float)(centerX + (radiusPX - 20) * cosAngle);
        y3 = (float)(centerY + (radiusPX - 20) * sinAngle);

        graphics.DrawLine(s_protractorOutlinePen, x1, y1, x2, y2);
    }

    /// <summary>
    /// Draws the 1 degree graduations on the protractor.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="radiusPX"></param>
    /// <param name="centerX"></param>
    /// <param name="centerY"></param>
    /// <param name="angleInDegrees"></param>
    private static void Draw1DegreeGraduations(Graphics graphics, float radiusPX, float centerX, float centerY, int angleInDegrees)
    {
        for (int oneDegreeGraduations = 0; oneDegreeGraduations < 10; oneDegreeGraduations++)
        {
            double angle2 = AngleInDegreesToRadians(angleInDegrees + oneDegreeGraduations + 270);

            double cosAngle2 = Math.Cos(angle2);
            double sinAngle2 = Math.Sin(angle2);

            // 5 degree has larger graduations
            float x5 = (float)(centerX + (radiusPX - (oneDegreeGraduations % 5 == 0 ? 9 : 6)) * cosAngle2);
            float y5 = (float)(centerY + (radiusPX - (oneDegreeGraduations % 5 == 0 ? 9 : 6)) * sinAngle2);

            float x4 = (float)(centerX + radiusPX * cosAngle2);
            float y4 = (float)(centerY + radiusPX * sinAngle2);

            graphics.DrawLine(s_protractorPen, x5, y5, x4, y4);
        }
    }

    /// <summary>
    /// Draws the designated angle on the protractor.
    /// </summary>
    /// <param name="graphics"></param>
    /// <param name="radiusPX"></param>
    /// <param name="centerX"></param>
    /// <param name="centerY"></param>
    private void OverlayAngleOnProtractor(Graphics graphics, float radiusPX, float centerX, float centerY)
    {
        if (_protractorAngle <= 0)
        {
            return;
        }

        double angle = AngleInDegreesToRadians(90 - _protractorAngle + 270);
        float x1 = (float)(centerX + radiusPX * Math.Cos(angle));
        float y1 = (float)(centerY + radiusPX * Math.Sin(angle));

        graphics.DrawLine(s_protractorThickArrowLine, centerX, centerY, x1, y1);
    }

    /// <summary>
    /// Converts angle in degrees to radians.
    /// </summary>
    /// <param name="angleInDegrees"></param>
    /// <returns></returns>
    private static double AngleInDegreesToRadians(float angleInDegrees)
    {
        return angleInDegrees * Math.PI / 180;
    }
    #endregion

    /// <summary>
    /// Start the simulation by throwing the ball.
    /// </summary>
    /// <param name="forceInNewtons">3..300</param>
    /// <param name="angleInDegrees">15..89</param>
    internal double Throw(double forceInNewtons, double angleInDegrees)
    {
        /*
         * The force a basketball player can exert when throwing a basketball depends on various factors such as their strength, technique, 
         * and the speed of the throw. However, let's talk in terms of some real-world context.
         * 
         * A professional basketball player, when making a fast throw, might exert a force in the range of 200 to 300 Newtons. This estimate 
         * considers the player accelerating the ball from a stationary position to a speed of around 15 to 20 meters per second (roughly 54
         * to 72 kilometers per hour or 33 to 45 miles per hour).
         * 
         * Less than 3N is too small to get it in the hoop from close range.
         */

        if (forceInNewtons < 3 || forceInNewtons > NeuralNetwork.c_forceNormaliser*2)
        {
            throw new ArgumentOutOfRangeException(nameof(forceInNewtons), $"Force must be between 3 and {NeuralNetwork.c_forceNormaliser*2} Newtons.");
        }

        if (angleInDegrees < 15 || angleInDegrees > 89)
        {
            throw new ArgumentOutOfRangeException(nameof(angleInDegrees), "Angle must be between 15 and 89 degrees.");
        }

        _trailOfBallPoints.Clear();

        // before we throw the ball, we need to reset the stick person to the start position
        if (_stickPersonSideView is not null)
        {
            _stickPersonSideView.SetAllLimbsToValues(92, -179, -171, -61, -72, 66, 55, -10, -3, 5, 9, -86, -95);
            _stickPersonSideView.CelebrationFinished = false;
        }

        timeSinceThrown = 0;

        double requiredVelocity = simulation.Throw(forceInNewtons, angleInDegrees);

        timer.Start(); // animation timer stepping the simulation
        ++_throws;

        return requiredVelocity;
    }

    #region RULER
    // ruler brushes with varying transparency, to give a better effect

    /// <summary>
    /// Red dotted line pen used to draw the ruler.
    /// </summary>
    private static readonly Pen s_dottedLinePenForRuler = new(Color.FromArgb(50, 255, 255, 255))
    {
        Width = 1
    };

    /// <summary>
    /// Main part of ruler.
    /// </summary>
    private static readonly SolidBrush s_brushMainRuler = new(Color.FromArgb(10, 255, 255, 255));

    /// <summary>
    /// Part of ruler that shows graduations.
    /// </summary>
    private static readonly SolidBrush s_brushGraduations = new(Color.FromArgb(20, 255, 255, 255));

    /// <summary>
    /// Bottom edge of ruler.
    /// </summary>
    private static readonly SolidBrush s_brushHighlight = new(Color.FromArgb(30, 255, 255, 255));

    /// <summary>
    /// Whether to show the ruler on the screen.
    /// </summary>
    bool _showRuler = false;

    /// <summary>
    /// Whether to show the ruler on the screen.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
    public bool ShowRuler
    {
        get => _showRuler;
        set
        {
            if (_showRuler == value) return;

            _showRuler = value;

            RedrawSimulatedEnvironment();
        }
    }

    /// <summary>
    /// Draws ruler between person throwing and hoop.
    /// </summary>
    /// <param name="g"></param>
    private void DrawRuler(Graphics g)
    {
        if (!simulation.BallStopped) return;

        double pointWhereBallStartsXinMetres = simulation.BallReleasePointInMetres.X;
        double pointWhereHoopIsXinMetres = BasketBallSimulation.c_rimCenterInMetres;
        double distanceBetweenBallAndHoopMetres = Math.Abs(pointWhereHoopIsXinMetres - pointWhereBallStartsXinMetres);
        float bottomEdgeOfRulePX = _heightOfCourtInPixels - 4;

        if (distanceBetweenBallAndHoopMetres < 1) return; // too small to draw

        // +-------------------------------------------------------------------------+
        // | |    .    |    .    |    .    |    .    |    .    |    .    |    .    | | <- main ruler
        // | |::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::| | <- graduations
        // +=|=====================================================================|=+ <- highlight
        //  ^ gap

        float rulerLeftEdgeInPX = (float)(pointWhereBallStartsXinMetres * _scaleMetresToPixels) - 8f;
        float rulerOriginPX = rulerLeftEdgeInPX + 10f;

        double div = (pointWhereHoopIsXinMetres - pointWhereBallStartsXinMetres);
        float maxXinPX = (float)(div * _scaleMetresToPixels + rulerOriginPX);
        float rulerRightEdgeInPX = maxXinPX + 10f;

        // we'll mark 1 metre intervals
        for (int interval = 0; interval < distanceBetweenBallAndHoopMetres; interval++)
        {
            float x = rulerOriginPX + interval * _scaleMetresToPixels;
            g.DrawLine(s_dottedLinePenForRuler, x, bottomEdgeOfRulePX, x, bottomEdgeOfRulePX - 12);

            // label every 1m

            SizeF sizeF = g.MeasureString(interval.ToString(), Font);

            g.DrawString(interval.ToString(), Font, Brushes.Gray, x - sizeF.Width / 2 - 1, bottomEdgeOfRulePX - 27 - 1);
            g.DrawString(interval.ToString(), Font, Brushes.Silver, x - sizeF.Width / 2, bottomEdgeOfRulePX - 27);

            // draw 0.5m intervals slightly shorter

            x = rulerOriginPX + (interval + 0.5f) * _scaleMetresToPixels;

            if (x < maxXinPX)
            {
                g.DrawLine(s_dottedLinePenForRuler, x, bottomEdgeOfRulePX, x, bottomEdgeOfRulePX - 8);
            }

            // draw 0.1m intervals even shorter
            for (int oneMetreInterval = 1; oneMetreInterval < 10; oneMetreInterval++)
            {
                x = rulerOriginPX + (interval + oneMetreInterval * 0.1f) * _scaleMetresToPixels;

                if (x < maxXinPX)
                {
                    g.DrawLine(s_dottedLinePenForRuler, x, bottomEdgeOfRulePX, x, bottomEdgeOfRulePX - 5);
                }
            }
        }

        // write distance between ball and hoop in metres to the RIGHT of the hoop
        string distanceText = $"{(distanceBetweenBallAndHoopMetres - 0.09f):F2}m";

        float distanceX = (float)(pointWhereHoopIsXinMetres * _scaleMetresToPixels + 55);
        g.DrawString(distanceText, Font, Brushes.White, distanceX, bottomEdgeOfRulePX - 20);

        // overlay ruler with varying transparency
        g.FillRectangle(
            s_brushMainRuler,
            new RectangleF(
                x: rulerLeftEdgeInPX,
                y: bottomEdgeOfRulePX - 30f,
                width: (rulerRightEdgeInPX - rulerLeftEdgeInPX),
                height: 20f)
            );

        // | |    .    |    .    |    .    |    .    |    .    |    .    |    .    | |
        // | |::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::|::::| | <- graduations
        g.FillRectangle(
            s_brushGraduations,
            new RectangleF(
                x: rulerLeftEdgeInPX,
                y: bottomEdgeOfRulePX - 10f,
                width: (rulerRightEdgeInPX - rulerLeftEdgeInPX),
                height: 10f)
            );

        // +=|=====================================================================|=+ <- highlight
        g.FillRectangle(
            s_brushHighlight,
            new RectangleF(
                x: rulerLeftEdgeInPX,
                y: bottomEdgeOfRulePX,
                width: (rulerRightEdgeInPX - rulerLeftEdgeInPX),
                height: 2f)
            );
    }
    #endregion

    #region DRAW
    /// <summary>
    /// Paint event to draw court, person and ball
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PictureBoxCourt_Paint(object? sender, PaintEventArgs e)
    {
        Graphics g = e.Graphics;

        g.SmoothingMode = SmoothingMode.HighQuality;
        g.CompositingQuality = CompositingQuality.HighQuality;

        g.FillRectangle(Brushes.Black, 0, 0, (int)(BasketBallSimulation.c_lengthOfVisibleCourtMetres * _scaleMetresToPixels), pictureBoxSideElevation.Height);

        // show score if ball has stopped
        if (simulation.BallStopped)
        {
            if (timer.Enabled) timer.Stop();

            // reset the ball to the start position
            simulation.BallReleasePointInMetres = new PointF(_personLeftMetres + 0.4f, simulation._personHeightMetres + 0.25f); // release position

            if (!inDraggingStickPerson) _stickPersonSideView?.SetThrowPosition();

            // write "drag me" to the left of the stick man
            SizeF size = g.MeasureString("drag me", dragMeFont);
            g.DrawString("drag me", dragMeFont, Brushes.White, _personLeftMetres * _scaleMetresToPixels - size.Width - 5, pictureBoxSideElevation.Height - size.Height - 20);
        }

        DrawTrajectoryAccuracy(g);
        DrawCourt(g);
        DrawPerson(g);
        DrawBall(g);
        DrawHoopAndNet(g); // we draw this on top of the ball, so it appears to go through it.

        if (_showRuler) DrawRuler(g);
        if (_showProtractor) DrawProtractor(g);

        // show score if ball has stopped
        DrawScore(g);
    }

    /// <summary>
    /// Plots pre-computed accuracy (green for correct, red for incorrect) on the screen. It's calculated by the neural network.
    /// For each angle, it plots the path of the ball.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawTrajectoryAccuracy(Graphics graphics)
    {
        if (_accuracyTrajectoryPointsToDraw.Count == 0) return;
        if (!simulation.BallStopped) return; // drawing whilst moving kills the framerate.

        foreach (var circle in _accuracyTrajectoryPointsToDraw) // item1= correct/poor, item2 = list of points
        {
            List<PointF> pixelPoints = [];

            foreach (var point in circle.Item2)
            {
                double ballXInPixels = point.X * _scaleMetresToPixels;
                double ballYInPixels = _heightOfCourtInPixels - point.Y * _scaleMetresToPixels; // (0,0) = top left, so we invert height

                pixelPoints.Add(new Point((int)ballXInPixels, (int)ballYInPixels));
            }

            // draw trajectory as a line of points, rather than lots of points or lines (improves performance)
            graphics.DrawLines(circle.Item1 ? s_goodTrajectoryGreenPed : s_poorTrajectoryRedPen, pixelPoints.ToArray());
        }
    }

    /// <summary>
    /// Draws the score on the screen.
    /// </summary>
    /// <param name="g"></param>
    private void DrawScore(Graphics g)
    {
        if (!simulation.BallStopped) return; // ball is moving, showing it would be an unreadable blur

        if (_throws == 0) return; // before throwing they have no score

        string score = simulation.Score >= 999900 ? "GOAL" : ((int)(simulation.Score)).ToString();

        SizeF size = g.MeasureString(score, s_scoreFont);

        // write score to center of screen
        g.DrawString(score, s_scoreFont, Brushes.White, pictureBoxSideElevation.Width - size.Width - 25, 5);

        // draw a small circle red or green in top right of graphics. green if ball in hoop, red if not
        g.FillEllipse(brush: simulation.BallInHoop ? Brushes.LightGreen : Brushes.Red,
            x: pictureBoxSideElevation.Width - 25, y: 10,
            width: 20, height: 20);
    }

    /// <summary>
    /// Draw person on court in blue color, as a rectangle.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawPerson(Graphics graphics)
    {
        if (_stickPersonSideView is null) return;

        _stickPersonSideView?.Draw(graphics);

        if (_stickPersonSideView!.CelebrationFinished) return;

        // if the ball is in the hoop, then the person is celebrating
        if (simulation.BallInHoop)
        {
            _stickPersonSideView.Celebrate(timeSinceThrown);
        }

        // if the ball is moving ensure hand is touching ball
        if (!simulation.BallStopped)
        {
            PointF ballLocation = simulation.BallLocationInMetres;
            PointF ballVelocity = simulation.BallVelocityInMetresPerSecond;

            // ball too far? might as well relax arms
            if (!_stickPersonSideView.LeftHandCanTouch(
                    ballPixels: new PointF(ballLocation.X * _scaleMetresToPixels, _heightOfCourtInPixels - ballLocation.Y * _scaleMetresToPixels),
                    radiusOfBallInPixels: BasketBallSimulation.c_ballRadiusInMetres * _scaleMetresToPixels,
                    velocity: ballVelocity) &&
                timeSinceThrown < 2)
            {
                // arms rotate downwards until they are vertical in line with body, taking 2 seconds to reach this position
                _stickPersonSideView.RelaxArms();
            }
        }
    }

    /// <summary>
    /// Draw court with backboard and rim.
    /// </summary>
    /// <param name="graphics"></param>
    private void DrawCourt(Graphics graphics)
    {
        // draw the floor
        graphics.DrawLine(s_courtColourPen, 0, _heightOfCourtInPixels - 1, pictureBoxSideElevation.Width - 1, _heightOfCourtInPixels - 1);

        // draw the backboard

        // draw line from centre of backboard to floor for support
        graphics.FillRectangle(Brushes.White, backboardXinPX, backboardTopInPX, (BasketBallSimulation.c_backboardThickness * _scaleMetresToPixels), (BasketBallSimulation.c_backboardHeight * _scaleMetresToPixels));
        
        // draw line from backboard diagonally to 1/3 of the way to the floor
        graphics.FillRectangle(Brushes.White,
            new RectangleF(x: rimRightInPX,
                           y: rimYinPixelsInPX - 2f,
                           width: backboardXinPX - rimRightInPX,
                           height: 4f)
            );

        graphics.FillRectangle(Brushes.White,
            new RectangleF(x: backboardXinPX,
                           y: backboardBottomInPX - (BasketBallSimulation.c_backboardHeight * _scaleMetresToPixels / 2f) - 2f,
                           width: 8f,
                           height: 8f)
            );

        // draw diagonal
        graphics.DrawLine(s_penForBackboardSupport,
            pt1: new PointF(backboardXinPX + 3, backboardBottomInPX - (BasketBallSimulation.c_backboardHeight * _scaleMetresToPixels / 2f)),
            pt2: new PointF(connectionXinPX, connectionYinPX));

        // draw line vertically
        graphics.DrawLine(s_penForBackboardSupport,
            pt1: new PointF(connectionXinPX, connectionYinPX - 0.4f * _scaleMetresToPixels),
            pt2: new PointF(connectionXinPX, _heightOfCourtInPixels));
    }
    
    /// <summary>
    /// Draw hoop and net on court.
    /// </summary>
    /// <param name="g"></param>
    private void DrawHoopAndNet(Graphics g)
    {
        //                         |
        //                         |  <-- backboard
        //                         |
        //              =========##|  <-- rim
        //              \/\/\/\//
        //               \/\/\//      <-- net
        //                \/\//
        //              
        //              [--------] 
        //                  ^ this

        // Draw rim
        g.FillRectangle(s_hoopBrush,
            x: rimLeftPX,
            y: rimYPX,
            width: (2 * BasketBallSimulation.c_rimRadiusMetres * _scaleMetresToPixels),
            height: rimThicknessPX);

        // draw hatched trapezium for net under hoop
        PointF[] hoopNet =
        [
            new PointF(rimLeftPX, rimBottomPX),
            new PointF(rimRightPX, rimBottomPX),
            new PointF(rimRightPX - 0.1f * _scaleMetresToPixels, netBottomPX),
            new PointF(rimLeftPX + 0.1f * _scaleMetresToPixels, netBottomPX),
            new PointF(rimLeftPX, rimBottomPX),
        ];
        
        g.FillPolygon(simulation.BallInHoop ? s_ballThruHoopNet: s_ballNotThruHoopNet, hoopNet);
        g.DrawPolygon(simulation.BallInHoop ? Pens.SeaGreen : Pens.Silver, hoopNet);
    }

    /// <summary>
    /// Draw ball on court in orange color, as a circle.
    /// </summary>
    /// <param name="g"></param>
    private void DrawBall(Graphics g)
    {
        float ballRadiusInPixels = (BasketBallSimulation.c_ballRadiusInMetres * _scaleMetresToPixels);

        int alpha = 0;

        foreach (var point in _trailOfBallPoints)
        {
            alpha += 5;
            s_brushForDrawingBall.Color = Color.FromArgb(Math.Min(alpha, 255), 0, 200, 200);
            g.FillEllipse(s_brushForDrawingBall, point.X, point.Y, 2f * ballRadiusInPixels, 2f * ballRadiusInPixels);
        }

        using Pen pointPen = new(Color.Red)
        {
            EndCap = LineCap.ArrowAnchor,
            DashStyle = DashStyle.Dash
        };

        // Draw ball
        PointF ballLocation = simulation.BallLocationInMetres;
        float ballXInPixels = ballLocation.X * _scaleMetresToPixels;
        float ballYInPixels = _heightOfCourtInPixels - ballLocation.Y * _scaleMetresToPixels; // (0,0) = top left, so we invert height

        if (ballYInPixels < 0)
        {
            // draw arrow to show offscreen ball. length of arrow pointing upwards is proportional to how far offscreen it is.
            float arrowLength = -ballYInPixels / 10f;
            g.DrawLine(pointPen, ballXInPixels, arrowLength, ballXInPixels, 0);
        }

        g.FillEllipse(Brushes.Orange, ballXInPixels - ballRadiusInPixels, ballYInPixels - ballRadiusInPixels, 2f * ballRadiusInPixels, 2f * ballRadiusInPixels);

        if (!simulation.BallStopped)
        {
            _trailOfBallPoints.Add(new PointF(ballXInPixels - ballRadiusInPixels, ballYInPixels - ballRadiusInPixels));
        }

#if !KEEP_TRAIL
        if (_trailOfBallPoints.Count > 35) _trailOfBallPoints.RemoveAt(0);
#endif

        PointF ballVelocity = simulation.BallVelocityInMetresPerSecond;
        double ballXVelocity = ballVelocity.X;
        double ballYvelocity = ballVelocity.Y;

        double vel = Math.Sqrt(ballXVelocity * ballXVelocity + ballYvelocity * ballYvelocity);

        if (vel > 0.01f)
        {
            // write velocity to the top right of ball
            g.DrawString($"{vel:F2}m/s", Font, Brushes.White, ballXInPixels + ballRadiusInPixels, ballYInPixels - 15);
        }
    }

    /// <summary>
    /// Set the distance of the person from the hoop.
    /// </summary>
    /// <param name="value"></param>
    internal void SetDistance(float value)
    {
        value = BasketBallSimulation.c_rimCenterInMetres - value + 0.01f;

        _trailOfBallPoints.Clear(); // remove trail as the position of the person has changed

        _personLeftMetres = value - 0.5f;
        simulation.BallReleasePointInMetres = new PointF(_personLeftMetres + 0.4f, simulation._personHeightMetres + 0.25f); // release position

        _stickPersonSideView = new StickPersonSideView(pictureBoxSideElevation.Height, _scaleMetresToPixels, new PointF(_personLeftMetres + 0.5f, simulation._personHeightMetres - 0.20f), simulation._personHeightMetres);

        RedrawSimulatedEnvironment();
    }
    #endregion

    #region DRAGGING STICK PERSON
    /// <summary>
    /// If the stick person is being dragged.
    /// </summary>
    bool inDraggingStickPerson = false;

    /// <summary>
    /// Used to store the offset of the mouse position to the stick person, for dragging purposes.
    /// </summary>
    PointF offsetOfMousePosToStickPerson = new(0, 0);

    /// <summary>
    /// Used to adjust the angle of the protractor when the user hovers over the [+]/[-] angle buttons.
    /// </summary>
    private Timer? angleAdjustmentTimer;

    /// <summary>
    /// Determines if the angle is increasing or decreasing when use is hovering over the [+]/[-] angle buttons.
    /// </summary>
    private bool isIncreasingAngle = false;

    /// <summary>
    /// Use clicked on picture box. Check if stick person was clicked.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PictureBoxSideElevation_MouseDown(object sender, MouseEventArgs e)
    {
        if (_stickPersonSideView is null) return; // no stick person to drag

        // get the mouse position and check if stick person was clicked
        Point mouse = pictureBoxSideElevation.PointToClient(MousePosition);

        RectangleF stickPersonBoundingBox = _stickPersonSideView.BoundingBoxInPX;

        // if stick person was clicked, animate it and start dragging
        if (stickPersonBoundingBox.Contains(mouse))
        {
            StartStickPersonDrag(mouse);
            _stickPersonSideView.CelebrationFinished = true;
            return;
        }

        // Mouse hover over the [+]/[-] angle buttons.

        // if the angle buttons were clicked
        if (s_protractorMinusAngleHitBox.Contains(mouse)) // reduce angle
        {
            isIncreasingAngle = false;
            StartAngleAdjustment();
            return;
        }

        if (s_protractorPlusAngleHitBox.Contains(mouse)) // increase angle
        {
            isIncreasingAngle = true;
            StartAngleAdjustment();
        }
    }

    /// <summary>
    /// Mouse click released. Stop dragging stick person. Which means drop them.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PictureBoxSideElevation_MouseUp(object sender, MouseEventArgs e)
    {
        pictureBoxSideElevation.Cursor = Cursors.Default;

        // Stop the angle adjustment timer
        angleAdjustmentTimer?.Stop();

        if (!inDraggingStickPerson) return; // nothing to tidy up

        inDraggingStickPerson = false;

        _showProtractor = true; // we hide whilst dragging, so now we can re-show it

        timerAnimation?.Dispose();

        timerAnimation = new Timer
        {
            Interval = 10
        };

        if (_stickPersonSideView is null) return; // no stick person to drop

        _stickPersonSideView.GetReady();

        StickPersonMovedEventArgs stickPersonMovedEventArgs = new(new PointF(_stickPersonSideView.AnchorPX.X / _scaleMetresToPixels, _stickPersonSideView.AnchorPX.Y / _scaleMetresToPixels));

        StickPersonMoved?.Invoke(this, stickPersonMovedEventArgs);

        // this drags the stick person downwards.
        timerAnimation.Tick += (s, e) =>
        {
            _stickPersonSideView!.ApplyGravityToStickPerson();

            if (_stickPersonSideView.IsOnGround())
            {
                ResetStickPerson();
                return;
            }

            RedrawSimulatedEnvironment();
        };

        timerAnimation.Start();
    }

    /// <summary>
    /// 
    /// </summary>
    private void StartAngleAdjustment()
    {
        angleAdjustmentTimer?.Dispose();

        angleAdjustmentTimer = new Timer
        {
            Interval = 50 // Adjust the interval as needed
        };

        angleAdjustmentTimer.Tick += (s, e) =>
        {
            if (isIncreasingAngle)
            {
                ProtractorAngle = Math.Min(89, ProtractorAngle + 0.5f);
            }
            else
            {
                ProtractorAngle = Math.Max(15, ProtractorAngle - 0.5f);
            }
        };

        angleAdjustmentTimer.Start();
    }

    /// <summary>
    /// As the stick person is dranked it moves its arms / legs via an animation.
    /// </summary>
    /// <param name="mouse"></param>
    private void StartStickPersonDrag(Point mouse)
    {
        pictureBoxSideElevation.Cursor = Cursors.VSplit;

        _showProtractor = false;

        timerAnimation?.Dispose(); // stop any previous animation

        timerAnimation = new Timer
        {
            Interval = 10
        };

        // stick person mouse down
        inDraggingStickPerson = true;

        timerAnimation.Tick += (s, e) =>
        {
            _stickPersonSideView!.Struggle(); // waggles arms and legs

            RedrawSimulatedEnvironment();
        };

        // relate mouse position to stick person, so we can drag it

        PointF headCentre = _stickPersonSideView!.AnchorPX;

        offsetOfMousePosToStickPerson = new PointF(mouse.X - headCentre.X, mouse.Y - headCentre.Y);
        timerAnimation.Start();
    }

    /// <summary>
    /// Mouse moved, if in a drag we respond by dragging the stick person.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PictureBoxSideElevation_MouseMove(object sender, MouseEventArgs e)
    {
        // relate mouse position to stick person
        Point mouse = pictureBoxSideElevation.PointToClient(MousePosition);

        // if the angle buttons were clicked
        if (s_protractorMinusAngleHitBox.Contains(mouse) || s_protractorPlusAngleHitBox.Contains(mouse)) // [+]/[-] angle
        {
            Cursor = Cursors.Hand;
            return;
        }

        if (_stickPersonSideView is null) return; // there should always be a stick person

        RectangleF stickPersonBoundingBox = _stickPersonSideView.BoundingBoxInPX;

        // are they hovering the stick person?
        // ensure stick person can't be placed off screen. If it was done they would be able undo.
        if (!inDraggingStickPerson || mouse.X < 10 || mouse.Y < 10)
        {

            pictureBoxSideElevation.Cursor = stickPersonBoundingBox.Contains(mouse) ? Cursors.VSplit : Cursors.Default;
            return; // not dragging stick person
        }

        pictureBoxSideElevation.Cursor = Cursors.VSplit;

        PointF newPos = new(mouse.X + offsetOfMousePosToStickPerson.X, mouse.Y - offsetOfMousePosToStickPerson.Y);

        // person is too close to the hoop? Don't allow them to go any further...
        if (simulation.HoopXInMetres - (newPos.X / _scaleMetresToPixels) < 0.5) return;

        // translate pixel position to metres
        _stickPersonSideView.MoveTo(new PointF(newPos.X / _scaleMetresToPixels, (pictureBoxSideElevation.Height - newPos.Y) / _scaleMetresToPixels));

        _personLeftMetres = _stickPersonSideView.AnchorPX.X / _scaleMetresToPixels - 0.5f;
        simulation.BallReleasePointInMetres = new PointF(_personLeftMetres + 0.4f, simulation._personHeightMetres + 0.25f); // release position

        RedrawSimulatedEnvironment();
    }

    /// <summary>
    /// Reset the stick person to the start position.
    /// </summary>
    private void ResetStickPerson()
    {
        timerAnimation?.Stop();
        timerAnimation?.Dispose();

        if (_stickPersonSideView is null) return;

        _stickPersonSideView.SetThrowPosition();
        _stickPersonSideView.SetOnGround();

        _personLeftMetres = _stickPersonSideView.AnchorPX.X / _scaleMetresToPixels - 0.5f;

        simulation.BallReleasePointInMetres = new PointF(_personLeftMetres + 0.4f, simulation._personHeightMetres + 0.25f); // release position

        RedrawSimulatedEnvironment();
    }
    #endregion

    #region TRAJECTORY PLOTTING
    /// <summary>
    /// We need to store the points of the accuracy trajectory to draw.
    /// It asks the AI to return a force for each angle and, plots the trajectory of the ball.
    /// They are colour coded, green if the ball goes in the hoop (true), red if it doesn't (false).
    /// </summary>
    List<(bool, List<PointF>)> _accuracyTrajectoryPointsToDraw = [];

    /// <summary>
    /// Draw the accuracy points on the screen.
    /// </summary>
    /// <param name="points"></param>
    internal void SetAITrajectoryAccuracyDrawPoints(List<(bool, List<PointF>)> points)
    {
        _accuracyTrajectoryPointsToDraw = points;

        RedrawSimulatedEnvironment();
    }
    #endregion

    /// <summary>
    /// Impacts whether the ball gets 0 points for bouncing.
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool FunMode
    {
        get => simulation.FunMode;
        set
        {
            if (simulation.FunMode == value) return;

            simulation.FunMode = value;
            RedrawSimulatedEnvironment();
        }
    }
}