using ML;
using System.Text;

namespace BallHoop.Simulation;

/// <summary>
/// Simulates the motion of a basketball in a basketball court.
/// </summary>
internal class BasketBallSimulation
{
    #region SIMULATION CONSTANTS
    /// <summary>
    /// Gravity constant in m/s^2.
    /// </summary>
    private const float c_gravity = 9.81f; // m/s^2

    /// <summary>
    /// Drag coefficient for a sphere.
    /// </summary>
    private const float c_dragCoefficient = 0.47f;

    /// <summary>
    /// Air density at sea level in kg/m^3.
    /// </summary>
    private const float c_airDensity = 1.225f;
    #endregion

    /// <summary>
    /// In fun mode, the ball will not stop moving when it goes through the hoop or past it.
    /// False: speed up AI training etc, by avoiding waiting for things that don't change the outcome.
    /// </summary>
    internal bool FunMode = false;

    #region BALL DYNAMICS
    /// <summary>
    /// Elasticity of the ball (coefficient of restitution)
    /// </summary>
    private const float c_restitutionCoefficient = 0.6f;

    /// <summary>
    /// Approximate basketball radius in metres.
    /// </summary>
    internal const float c_ballRadiusInMetres = 0.242f / 2f; // https://www.dimensions.com/element/basketball

    /// <summary>
    /// Mass of the ball in kg.
    /// </summary>
    private const float c_ballMassKg = 0.623f; // Mass in kg (22 ounces) // https://www.dimensions.com/element/basketball

    /// <summary>
    /// Indicates whether the ball has stopped moving.
    /// </summary>
    private bool _ballStopped = true;

    /// <summary>
    /// Indicates whether the ball has bounced on the floor.
    /// </summary>
    private bool _bouncedOnFloor = false;

    /// <summary>
    /// The X position of the ball.
    /// </summary>
    private double _ballXInMetres;

    /// <summary>
    /// The Y position of the ball.
    /// </summary>
    private double _ballYInMetres;

    /// <summary>
    /// The velocity of the ball in the X direction.
    /// </summary>
    private double _ballXvelocityInMetresPerSecond;

    /// <summary>
    /// The velocity of the ball in the Y direction.
    /// </summary>
    private double _ballYvelocityInMetresPerSecond;

    /// <summary>
    /// Indicates ball went above the hoop.
    /// </summary>
    private bool _ballWentAboveHoop = false;

    /// <summary>
    /// Indicates whether the ball went above the hoop during the throw.
    /// This signal is used to determine if the ball went through the hoop. If the ball goes below the hoop after going above it, 
    /// it is considered a successful shot.
    /// </summary>
    private bool _ballOnTargetAboveHoop = false;

    /// <summary>
    /// Indicates whether the ball went below the hoop during the throw.
    /// </summary>
    private bool _ballOnTargetBelowHoop = false;

    /// <summary>
    /// Indicates whether the ball went too high - defined as above the backboard.
    /// </summary>
    private bool _ballWentAboveTheBackboard = false;

    /// <summary>
    /// Indicates ball bounced off the hoop.
    /// </summary>
    private bool _ballHitHoop = false;
    
    /// <summary>
    /// Returns the current location of the ball.
    /// </summary>
    internal PointF BallLocationInMetres
    {
        get
        {
            return new PointF((float)_ballXInMetres, (float)_ballYInMetres);
        }
    }

    /// <summary>
    /// Returns the current velocity of the ball.
    /// </summary>
    internal PointF BallVelocityInMetresPerSecond
    {
        get
        {
            return new PointF((float)_ballXvelocityInMetresPerSecond, (float)_ballYvelocityInMetresPerSecond);
        }
    }

    /// <summary>
    /// Indicates whether the ball has stopped moving.
    /// </summary>
    internal bool BallStopped
    {
        get => _ballStopped;
    }
    #endregion

    #region COURT DIMENSIONS AND PROPERTIES
    /// <summary>
    /// Length of the court in metres.
    /// </summary>
    internal const float c_courtLengthInMetres = 50; // Length of the court in metres (normally 30)

    /// <summary>
    /// Height of the court in metres.
    /// </summary>
    internal const float c_courtHeightMetres = 10;

    /// <summary>
    /// Coefficient of friction for the floor.
    /// </summary>
    internal const float c_frictionCoefficient = 0.1f;

    /// <summary>
    /// Width of the visible court in metres.
    /// </summary>
    internal const float c_lengthOfVisibleCourtMetres = c_courtLengthInMetres + 4;

    #region RIM / HOOP
    // Basketball rims, or hoops, are orange painted goals attached to the backboard and used for scoring points in a game of basketball

    /// <summary>
    /// Rim radius in metres (45.72 cm).
    /// </summary>
    internal const float c_rimRadiusMetres = 0.4572f / 2; // https://www.dimensions.com/element/basketball-rims-nets

    /// <summary>
    /// Thickness of the rim in metres. // https://www.dimensions.com/element/basketball-rims-nets
    /// The rim itself is made of a 5/8” | 1.6 cm steel diameter steel rod that is formed into a ring.
    /// </summary>
    internal float c_rimThicknessCM = 1.6f / 100f;

    /// <summary>
    /// Rim/hoop height from the ground in metres.
    /// </summary>
    internal const float c_rimHeightFromGroundMetres = 3.05f; // https://www.dimensions.com/element/basketball-rims-nets

    /// <summary>
    /// Location of the rim center in metres from the backboard..
    /// </summary>
    internal const float c_rimCenterInMetres = c_backboardXPosMetres - 0.151f - c_rimRadiusMetres / 2f;
    #endregion

    #region BACKBOARD
    /// <summary>
    /// Backboard X position in metres from the right side of the court
    /// </summary>
    internal const float c_backboardXPosMetres = c_courtLengthInMetres - 4.6f;

    /// <summary>
    /// Height of the backboard in metres
    /// </summary>
    internal const float c_backboardHeight = 1.1f;

    /// <summary>
    /// Thickness of the backboard in metres.
    /// </summary>
    internal const float c_backboardThickness = 0.02f;
    #endregion

    #endregion

    /// <summary>
    /// Simulation time step in seconds 0.01.
    /// </summary>
    internal float _timeStepSeconds = 0.01f;

    /// <summary>
    /// Height of the player in metres.
    /// </summary>
    internal float _personHeightMetres = 2.0f;

    /// <summary>
    /// Reset all flags.
    /// </summary>
    private void ResetFlags()
    {
        _ballWentAboveHoop = false;
        _ballOnTargetAboveHoop = false;
        _ballOnTargetBelowHoop = false;
        _ballWentAboveTheBackboard = false;
        _ballHitHoop = false;

        _closestDistanceToHoop = double.MaxValue;
        _hitBackboard = false;
        _ballStopped = false;
        _bouncedOnFloor = false;

        calc.Clear();
    }

    /// <summary>
    /// Indicates whether the ball went through the hoop.
    /// </summary>
    internal bool BallInHoop
    {
        get => _ballOnTargetAboveHoop && _ballOnTargetBelowHoop;
    }

    /// <summary>
    /// Contains the release point of the ball in metres.
    /// </summary>
    private PointF _ballReleasePointInMetres;

    /// <summary>
    /// Indicates whether the ball hit the backboard.
    /// Can be used for scoring purposes.
    /// </summary>
    private bool _hitBackboard = false;

    /// <summary>
    /// Holds the closest distance to the hoop, populated from distanceFromRimCentreToBallCentreInMetres.
    /// </summary>
    private double _closestDistanceToHoop = double.MaxValue;

    /// <summary>
    /// Returns the score of the throw.
    /// </summary>
    public double Score
    {
        get
        {
            if (!FunMode && _bouncedOnFloor) return 0; // stop it going for bounces on the floor, because it's hard to get a NN to learn that

            double score;

            if (BallInHoop)
            {
                score = 1000000;

                if (_hitBackboard)
                {
                    score -= 100; // less for using the backboard
                }
            }
            else
            {
                // we didn't make it, so we score based on how close we got

                score = 0;

                if (_ballOnTargetAboveHoop) score += 10000;
                if (_ballWentAboveHoop) score += 500;

                // except this didn't get close to the hoop
                if (_closestDistanceToHoop == double.MaxValue)
                {
                    // further from hoop = less points
                    score += _ballYInMetres / 10; // encourage the ball to go upward
                    score -= (_ballWentAboveTheBackboard ? 20 : 0); // penalise for going above the backboard
                    score += Math.Min(1000 - Math.Abs(_ballXInMetres - c_rimCenterInMetres) * 25, 100); // encourage closer to the hoop
                }
                else
                {
                    // less than 1 million
                    score += Math.Min(2000 - Math.Max(_closestDistanceToHoop, 4) * 250, 50000) - (_ballWentAboveTheBackboard ? 100 : 0);
                }
            }

            return score;
        }
    }

    /// <summary>
    /// Retrieves / defines whether the ball starts (in the air, technically in Stick Man's hands).
    /// </summary>
    internal PointF BallReleasePointInMetres
    {
        get => _ballReleasePointInMetres;
        set
        {
            _ballStopped = true;
            _ballReleasePointInMetres = value;

            // we move the ball to this location
            _ballXInMetres = _ballReleasePointInMetres.X;
            _ballYInMetres = _ballReleasePointInMetres.Y;
        }
    }

    /// <summary>
    /// Horizontal location of hoop.
    /// </summary>
    public float HoopXInMetres = c_rimCenterInMetres - c_rimRadiusMetres;

    /// <summary>
    /// AI / calculation output.
    /// </summary>
    private readonly StringBuilder calc = new(50);

    /// <summary>
    /// Constructor.
    /// </summary>
    public BasketBallSimulation()
    {
    }

    /// <summary>
    /// Constructor, sets the ball's initial position.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public BasketBallSimulation(double x, double y)
    {
        _ballXInMetres = x;
        _ballYInMetres = y;
    }

    /// <summary>
    /// Explains the calculation.
    /// </summary>
    public string Calc => calc.ToString();

    /// <summary>
    /// Start the simulation by throwing the ball.
    /// </summary>
    /// <param name="forceInNewtons">3..300 (1 is allowed, but won't reach the hoop)</param>
    /// <param name="angleInDegrees">15..89</param>
    internal double Throw(double forceInNewtons, double angleInDegrees, bool guessTheForce = true)
    {
        if (forceInNewtons <= 0 || forceInNewtons > 300) throw new ArgumentOutOfRangeException(nameof(forceInNewtons)); // we support 1, because training may try it.
        if (angleInDegrees < 15 || angleInDegrees > 89) throw new ArgumentOutOfRangeException(nameof(angleInDegrees));

        ResetFlags();

        // initialise ball position
        _ballXInMetres = _ballReleasePointInMetres.X; // Starting X position
        _ballYInMetres = _ballReleasePointInMetres.Y; // Starting Y position (release height)

        _ballOnTargetAboveHoop = false;
        _ballOnTargetBelowHoop = false;

        // Calculate the correct throwing angle and force for a perfect shot
        double targetX = c_rimCenterInMetres; // X-coordinate of the rim center
        double targetY = c_rimHeightFromGroundMetres; // Y-coordinate of the rim center

        // Calculate angle and initial velocity using projectile motion equations
        double deltaX = targetX - BallReleasePointInMetres.X;
        double deltaY = targetY - BallReleasePointInMetres.Y;

        // Initialize ball position and velocity based on throwing force and angle

        // Convert angle to radians
        double angleRadians = angleInDegrees * Math.PI / 180.0f;

        double requiredVelocity = guessTheForce ? ValidateForce(angleInDegrees, angleRadians, deltaX, deltaY) : 0;

        // Calculate initial velocities based on force and angle
        double initialVelocity = forceInNewtons / c_ballMassKg; // v = F / m

        _ballXvelocityInMetresPerSecond = initialVelocity * Math.Cos(angleRadians);
        _ballYvelocityInMetresPerSecond = initialVelocity * Math.Sin(angleRadians);

        _ballStopped = false;

        return requiredVelocity;
    }

    /// <summary>
    /// Narrow down force required for given dist/angle to put ball in hoop.
    /// </summary>
    /// <param name="angleRadians"></param>
    /// <param name="targetX"></param>
    /// <param name="targetY"></param>
    /// <returns></returns>
    private double ValidateForce(double angleInDegrees, double angleRadians, double targetX, double targetY)
    {
        calc.AppendLine($"angle: {angleRadians} targetX: {targetX} targetY: {targetY}\n");

        double low = 3; // Minimum possible force. At 1.1m away 62 degrees, 3 is the minimum force to hit the target
        double high = NeuralNetwork.c_forceNormaliser*2; // 28 would be an arbitrary high limit for force. 45 max distance is around 25.3, closest at 7 is 27.98. But 300 is what a basketball player can apply.
        double tolerance = 0.0005; // Acceptable error for hitting the target

        // We find the approximate angle that hits the target, then use the real logic to more accurately refine it.
        double mid = GetApproximateForce(angleRadians, targetX, targetY, ref low, ref high, tolerance);        

        low = mid - 2f; // 2N
        if (low < 3) low = 3;
        
        high = mid + 2f; // 2N

        if (high<low) high = low + 2;
        if (high > 300) high = 300;

        double bestScore = 0;
        double bestForce = 0;

        calc.AppendLine($"REFINED SEARCH: from {low} to {high}");
        
        for (mid = low; mid < high; mid += 0.01)
        {
            BasketBallSimulation b = new()
            {
                BallReleasePointInMetres = BallReleasePointInMetres,
                FunMode = false // this needs to be accurate and scored
            };

            double score = TestForceForThisAngle(mid, angleInDegrees, b);
            calc.AppendLine($"Test of {mid} @ {angleInDegrees} degrees => score: {score}");

            if (_bouncedOnFloor)
            {
                score = 0;
                calc.AppendLine("**floor bounce, ignoring");
            }

            if (score > 999999) // 1,000,000 = perfect shot
            {
                calc.AppendLine($"**GOAL**");
                return mid;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestForce = mid;
                calc.AppendLine($"<Best so far: {mid} score: {score}>");
            }
        }

        calc.AppendLine($"** Best force: {bestForce} {bestScore} **");

        return bestScore > 999000 ? bestForce : 0;
    }

    /// <summary>
    /// Uses binary search to find the approximate force that hits the target.
    /// It's approximate, because it doesn't include all the complicated hoop and rebounds.
    /// </summary>
    /// <param name="angleRadians"></param>
    /// <param name="targetX"></param>
    /// <param name="targetY"></param>
    /// <param name="low"></param>
    /// <param name="high"></param>
    /// <param name="tolerance"></param>
    /// <returns></returns>
    private double GetApproximateForce(double angleRadians, double targetX, double targetY, ref double low, ref double high, double tolerance)
    {
        bool found = false;
        double mid = 0;

        while (high - low > tolerance)
        {
            mid = (low + high) / 2;
            calc.AppendLine($"Binary search: FORCE low: {low} high: {high} mid: {mid}");

            if (SimulatesHit(mid, angleRadians, targetX, targetY, out bool wasShortOfTarget))
            {
                calc.AppendLine($"Approximate force: {mid}\n");
                found = true;
                break;
            }

            if (!wasShortOfTarget)
            {
                high = mid; // Try a smaller force
            }
            else
            {
                low = mid; // Try a larger force
            }
        }

        if (!found)
        {
            mid = (low + high) / 2; // Return the best approximation

            calc.AppendLine($"No force matched, using closest: {mid}\n");
        }

        return mid;
    }

    /// <summary>
    /// Try a force and angle to see if it hits the target.
    /// </summary>
    /// <param name="force"></param>
    /// <param name="angleInDegrees"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    private static double TestForceForThisAngle(double force, double angleInDegrees, BasketBallSimulation b)
    {
        b.Throw(force, angleInDegrees, false);

        while (!b._ballStopped) b.MoveBall();

        return b.Score;
    }

    /// <summary>
    /// Throw the ball with the specified force, at the angle specified and work out if it fell short or not.
    /// </summary>
    /// <param name="force"></param>
    /// <param name="angleRadians"></param>
    /// <param name="targetX"></param>
    /// <param name="targetY"></param>
    /// <param name="wasShortOfTarget"></param>
    /// <returns>true: angle & force hit the target</returns>
    private bool SimulatesHit(double force, double angleRadians, double targetX, double targetY, out bool wasShortOfTarget)
    {
        // Initialize ball position and velocity
        double simBallX = 0; // Starting X position
        double simBallY = 0; // Starting Y position (release height)

        // f=ma, so a = f/m, v=at.
        double simBallVX = force / c_ballMassKg * Math.Cos(angleRadians);
        double simBallVY = force / c_ballMassKg * Math.Sin(angleRadians);

        targetY += c_ballRadiusInMetres;

        while (simBallY >= -BallReleasePointInMetres.Y) // While the ball is above ground
        {
            // moves the ball
            simBallX += simBallVX * _timeStepSeconds;
            simBallY += simBallVY * _timeStepSeconds;

            // compute changes to velocity
            simBallVY -= c_gravity * _timeStepSeconds; 

            double speed = Math.Sqrt(simBallVX * simBallVX + simBallVY * simBallVY);
#if USING_SIMPLIFIED_AIR_RESISTANCE
            // Apply air resistance (drag force)
            double dragForce = 0.1f * c_gravity * c_ballMassKg; // 10% of the force of gravity
            double dragAcceleration = dragForce / c_ballMassKg; // a = F / m
            simBallVX -= dragAcceleration * (simBallVX / speed) * _timeStepSeconds;
            simBallVY -= dragAcceleration * (simBallVY / speed) * _timeStepSeconds;
#else
            double crossSectionalArea = Math.PI * c_ballRadiusInMetres * c_ballRadiusInMetres; // Cross-sectional area of the ball

            double dragForce = 0.25f * c_dragCoefficient * c_airDensity * crossSectionalArea * speed * speed;
            double dragAcceleration = dragForce / c_ballMassKg; // a = F / m 
            
            // Apply drag force proportional to the square of the speed
            simBallVX -= dragAcceleration * _timeStepSeconds;
            simBallVY -= dragAcceleration * _timeStepSeconds;
#endif

            // Check if ball is within target bounds
            if (Math.Abs(simBallX - targetX) < 0.1 && Math.Abs(simBallY - targetY) < 0.1)
            {
                wasShortOfTarget = false;
                calc.AppendLine("  match");
                return true;
            }

            if (simBallY > c_courtHeightMetres) // went flying past the court height
            {
                wasShortOfTarget = false;
                calc.AppendLine("  above court");
                return false;
            }

            // the flag sounds crazy, but you need to consider what happens to the ball by this point -
            // if it's higher as it goes past the target, it's not short of the target
            // gone past target?
            if (simBallX > targetX)
            {
                if (_ballHitHoop) // was not high enough to go in
                {
                    wasShortOfTarget = true;
                    calc.AppendLine("  hit hoop");
                    return false;
                }


                if (simBallY > targetY+11f) // went flying past the target
                {
                    wasShortOfTarget = false;
                    calc.AppendLine("  above target");
                    return false;
                }

                // yes.
                wasShortOfTarget = simBallY < targetY && (_ballYvelocityInMetresPerSecond <= 0 || simBallY < 0.3f); // if it's below the target, it was short of it, otherwise it went past it
                calc.AppendLine($"  short? {wasShortOfTarget}");

                return false;
            }
        }

        // ball went below the floor, and didn't reach the target
        wasShortOfTarget = true;

        calc.AppendLine($"  short? {wasShortOfTarget}");

        return false;
    }

    /// <summary>
    /// Move the ball in the simulation, updating its position and velocity with each time step.
    /// Simulate the ball's motion and handle collisions with the floor, backboard, and rim.
    /// </summary>
    internal void MoveBall()
    {
        if (_ballStopped) return;

        _timeStepSeconds = UseSlowMotion() ? 0.001f : 0.01f;

        ApplyBallPhysics();

        // Check for collisions
        HandleFloorCollision();
        HandleBackboardCollision();
        HandleRimCollision();
        HandleHittingEdges();
    }

    /// <summary>
    /// Apply gravity and air resistance to the ball.
    /// </summary>
    private void ApplyBallPhysics()
    {
        // Update ball position and velocity
        _ballXInMetres += _ballXvelocityInMetresPerSecond * _timeStepSeconds;
        _ballYInMetres += _ballYvelocityInMetresPerSecond * _timeStepSeconds;

        // for learning we can score more if it reached hoop height
        if (!_ballWentAboveHoop && _ballYInMetres >= c_rimHeightFromGroundMetres) _ballWentAboveHoop = true;

        // Apply gravity
        _ballYvelocityInMetresPerSecond -= c_gravity * _timeStepSeconds;

        // https://secretsofshooting.com/physics-based-basketball-shooting/
        // At typical speeds in a basketball game, air resistance (drag force) is about 10% of the force of gravity. Drag force varies as the speed of the square.
        // A correlation exists between the size of the ball and drag force. The ball must move the air out of its path as it travels.The bigger the ball the more area there is to “sweep away”.

        double speed = Math.Sqrt(_ballXvelocityInMetresPerSecond * _ballXvelocityInMetresPerSecond + _ballYvelocityInMetresPerSecond * _ballYvelocityInMetresPerSecond);

#if USING_SIMPLIFIED_AIR_RESISTANCE
        // Apply air resistance (drag force)
        double dragForce = 0.1f * c_gravity * c_ballMassKg; // 10% of the force of gravity
        double dragAcceleration = dragForce / c_ballMassKg; // a = F / m

        _ballXvelocityInMetresPerSecond -= dragAcceleration * (_ballXvelocityInMetresPerSecond / speed) * _timeStepSeconds;
        _ballYvelocityInMetresPerSecond -= dragAcceleration * (_ballYvelocityInMetresPerSecond / speed) * _timeStepSeconds;
#else                
        double crossSectionalArea = Math.PI * c_ballRadiusInMetres * c_ballRadiusInMetres; // Cross-sectional area of the ball

        double dragForce = 0.25f * c_dragCoefficient * c_airDensity * crossSectionalArea * speed * speed;
        double dragAcceleration = dragForce / c_ballMassKg; // a = F / m 

        // Apply drag force
        _ballXvelocityInMetresPerSecond -= dragAcceleration * _timeStepSeconds;
        _ballYvelocityInMetresPerSecond -= dragAcceleration * _timeStepSeconds;
#endif
    }

    /// <summary>
    /// Make the ball bounce off the floor.
    /// </summary>
    private void HandleFloorCollision()
    {
        if (_ballYInMetres - c_ballRadiusInMetres > 0) return;

        if (!_bouncedOnFloor && !BallInHoop && _ballXInMetres < c_rimCenterInMetres)
        {
            _bouncedOnFloor = true;
        }

        double velocityDueToGravity = c_gravity * _timeStepSeconds;

        if (_ballYvelocityInMetresPerSecond < -2.16 * velocityDueToGravity / c_restitutionCoefficient) // Ball is falling onto the floor
        {
            _ballYInMetres = c_ballRadiusInMetres; // Reset position to avoid sinking below ground
            _ballYvelocityInMetresPerSecond = -_ballYvelocityInMetresPerSecond * c_restitutionCoefficient; // Reverse and dampen velocity
        }

        if (Math.Abs(_ballYvelocityInMetresPerSecond) < 0.3) // Ball is rolling/sliding (negligible vertical velocity)
        {
            _ballYInMetres = c_ballRadiusInMetres; // Ensure the ball stays on the floor
            _ballYvelocityInMetresPerSecond = 0; // Stop vertical movement

            // Apply friction to horizontal velocity
            double frictionForce = c_frictionCoefficient * c_ballMassKg * c_gravity;
            double frictionAcceleration = frictionForce / c_ballMassKg; // a = F / m

            if (Math.Abs(_ballXvelocityInMetresPerSecond) > frictionAcceleration * _timeStepSeconds)
            {
                _ballXvelocityInMetresPerSecond -= Math.Sign(_ballXvelocityInMetresPerSecond) * frictionAcceleration * _timeStepSeconds; // Reduce velocity
            }
            else
            {
                _ballXvelocityInMetresPerSecond = 0; // Stop completely if friction exceeds velocity
                _ballStopped = true;
            }
        }
    }

    /// <summary>
    /// Handle the ball bouncing off the backboard.
    /// </summary>
    private void HandleBackboardCollision()
    {
        double backboardRight = c_backboardXPosMetres + c_backboardThickness; // allowing for thickness
        double boardTop = c_rimHeightFromGroundMetres + c_backboardHeight;
        double boardBottom = c_rimHeightFromGroundMetres;

        // test if ball is touching backboard


        // (0,0) is bottom left of court (cartesian coordinates)
        //                  ballX, ballY = ball center (+)
        //
        //                  | ballX - c_ballRadiusInMetres, 
        //
        //                        +--+ <-- top
        //                        |  |
        //                        |  |
        //                .--.    |  |     <-- ballY + c_ballRadiusInMetres  
        //               :  + :   |  |
        //                '--'    |  |     <-- ballY - c_ballRadiusInMetres
        //                        +--+ <-- bottom
        //                        | 
        //                        c_backboardXPosMetres

        // Compute the ball's next position
        double nextBallX = _ballXInMetres + _ballXvelocityInMetresPerSecond * _timeStepSeconds;

        // Check if the ball crosses the backboard during this step
        if (_ballXInMetres + c_ballRadiusInMetres < c_backboardXPosMetres && nextBallX + c_ballRadiusInMetres > c_backboardXPosMetres ||
            _ballXInMetres - c_ballRadiusInMetres < backboardRight && nextBallX + c_ballRadiusInMetres > backboardRight)
        {
            double nextBallY = _ballYInMetres + _ballYvelocityInMetresPerSecond * _timeStepSeconds;

            if (!_ballWentAboveTheBackboard && nextBallY + c_ballRadiusInMetres > c_backboardHeight + c_rimHeightFromGroundMetres) _ballWentAboveTheBackboard = true;

            // Check collision with front face of the backboard
            if (nextBallX + c_ballRadiusInMetres > c_backboardXPosMetres &&
                nextBallX - c_ballRadiusInMetres <= backboardRight &&
                nextBallY + c_ballRadiusInMetres >= boardBottom &&
                nextBallY - c_ballRadiusInMetres <= boardTop)
            {
                // Collision detected: Apply collision response
                _ballXvelocityInMetresPerSecond = -_ballXvelocityInMetresPerSecond * c_restitutionCoefficient; // Reverse and dampen velocity
                _hitBackboard = true;
            }
        }
    }

    /// <summary>
    /// Handle ball hitting either edge.
    /// </summary>
    private void HandleHittingEdges()
    {
        if (_ballXInMetres < 0.1f && _ballXvelocityInMetresPerSecond < 0f)
        {
            _ballXInMetres = 0.1f;
            _ballXvelocityInMetresPerSecond = -_ballXvelocityInMetresPerSecond * c_restitutionCoefficient * 0.8f; // Reverse and dampen velocity
        }

        if (_ballXInMetres + c_ballRadiusInMetres > c_lengthOfVisibleCourtMetres && _ballXvelocityInMetresPerSecond > 0)
        {
            _ballXInMetres = Math.Min(_ballXInMetres, c_lengthOfVisibleCourtMetres - 0.001f);
            _ballXvelocityInMetresPerSecond = -_ballXvelocityInMetresPerSecond * c_restitutionCoefficient * 0.8f; // Reverse and dampen velocity
        }
    }

    /// <summary>
    /// Slow motion improves the detection of hoop and backboard.
    /// It also is kind of cool because it slows down as it dunks, or near misses.
    /// </summary>
    /// <returns></returns>
    private bool UseSlowMotion()
    {
        double boardTop = c_rimHeightFromGroundMetres + c_backboardHeight;

        // ball is above backboard, no need to slow down
        if (_ballYInMetres > boardTop || BallInHoop) return false;

        double rimCenterY = c_rimHeightFromGroundMetres;

        // ball is below hoop , no need to slow down
        if (_ballYInMetres < rimCenterY - c_ballRadiusInMetres - 0.3f) return false;

        // centre of rim (hoop)
        double rimCenterX = c_rimCenterInMetres;
        double rimLeftEdge = rimCenterX - c_rimRadiusMetres;

        double rightEdgeOfBall = _ballXInMetres + c_ballRadiusInMetres;

        // ball is more than a balls width away from the the left of the rim, so no slowmotion
        if (rightEdgeOfBall < rimLeftEdge - c_ballRadiusInMetres) return false;

        double backboardRight = c_backboardXPosMetres + c_backboardThickness; // allowing for thickness

        // ball isn't near backboard right
        if (_ballXInMetres > backboardRight + c_ballRadiusInMetres * 1.5f) return false;

        // slow it down
        return true;
    }

    /// <summary>
    /// Handle collision with the rim of the hoop.
    /// There are a number of scenarios to handle:
    /// * Ball is above the hoop and likely to go in as it drops
    /// * Ball is below the hoop but previously was above and aligned with hoop
    /// * Ball hits hoop and needs to bounce off.
    /// * Ball is in net, and needs to be pushed to the center (net kills lateral speed)
    /// </summary>
    private void HandleRimCollision()
    {
        double normalX, normalY, velocityDotNormal;

        // centre of rim (hoop)
        double rimCenterX = c_rimCenterInMetres;
        double rimCenterY = c_rimHeightFromGroundMetres;

        double rimRightEdge = rimCenterX + c_rimRadiusMetres - 0.016f;
        double rimLeftEdge = rimCenterX - c_rimRadiusMetres + 0.016f;

        //                rimCenterX
        //      (1)         |         (2) 
        // rightEdgeOfBall     leftEdgeOfBall
        // < rimLeftEdge         > rimRightEdge
        //              :       :
        //          .--.:       :.--.
        //          '--':       :'--'
        //
        //              ====|==== <- rimCenterY
        //              \/\/\/\//
        //               \/\/\//
        //                \/\//
        //              

        // (1)
        double rightEdgeOfBall = _ballXInMetres + c_ballRadiusInMetres;
        if (rightEdgeOfBall < rimLeftEdge) return;

        // (2)
        double leftEdgeOfBall = _ballXInMetres - c_ballRadiusInMetres;
        if (leftEdgeOfBall > rimRightEdge) return;

        //         distanceBetweenBallYAndRimCenter
        //         .     [--] distanceBetweenBallXAndRimCenter
        //         .     | _ballX
        //         .        | rimCenterX
        //         .   .---.
        //         _   :   :
        //         |   '---'
        //         -    ========= <- rimCenterY
        //              \/\/\/\//
        //               \/\/\//
        //                \/\//

        // distance between rim and centre of ball
        double distanceBetweenBallXAndRimCenter = Math.Abs(_ballXInMetres - rimCenterX);
        double distanceBetweenBallYAndRimCenter = Math.Abs(_ballYInMetres - rimCenterY);

        //             .---.
        //             :  \ :
        //             '---\
        //              ====\===== 
        //              \/\/\/\//
        //               \/\/\//
        //                \/\//

        // it's a ball not a point, so Pythagoras to work out circle edge
        double distanceFromRimCentreToBallCentreInMetres = Math.Sqrt(distanceBetweenBallXAndRimCenter * distanceBetweenBallXAndRimCenter + distanceBetweenBallYAndRimCenter * distanceBetweenBallYAndRimCenter);

        if (distanceBetweenBallXAndRimCenter < _closestDistanceToHoop) _closestDistanceToHoop = distanceFromRimCentreToBallCentreInMetres;

        // is the ball horizontally within rim bounds, and within a balls radius of centre vertically (above or below)
        if (distanceBetweenBallXAndRimCenter <= c_ballRadiusInMetres * 2 && _ballXInMetres > rimLeftEdge && _ballXInMetres < rimRightEdge)
        {
            // track how close                
            // check if ball is above or below the hoop and set flags
            if (_ballYInMetres >= rimCenterY)
            {
                if(!_ballOnTargetAboveHoop) _ballOnTargetAboveHoop = !_ballOnTargetBelowHoop; // if it came bottom up, it's not a goal!
            }
            else
            {
                // it's below the hoop, but we ignore if it doesn't come from above
                if (_ballOnTargetAboveHoop && (_ballYInMetres - c_ballRadiusInMetres / 2 > rimCenterY - 0.3f || _ballOnTargetBelowHoop))
                {
                    _ballOnTargetBelowHoop = true;
                }
                else
                {
                    // if it's in the *net*, flatten lateral movement
                    if (rimCenterY - 0.2f < _ballYInMetres)
                    {
                        _ballXvelocityInMetresPerSecond = -1 * Math.Abs(_ballXvelocityInMetresPerSecond);
                        return; // it's in the "net", we don't need to do anything further
                    }
                }
            }
        }

        // do we need to handle a bounce off the hoop?
        if (rimCenterY >= _ballYInMetres - c_rimRadiusMetres && rimCenterY <= _ballYInMetres + c_rimRadiusMetres)
        {
            _ballHitHoop = true;
            double rimEdgeToBallDistance = Math.Sqrt(Math.Pow(rimLeftEdge - _ballXInMetres, 2) + Math.Pow(c_rimHeightFromGroundMetres - _ballYInMetres, 2));

            if (rimEdgeToBallDistance < c_ballRadiusInMetres)
            {
                // Simple elastic collision response
                normalX = distanceBetweenBallXAndRimCenter / distanceFromRimCentreToBallCentreInMetres;
                normalY = distanceBetweenBallYAndRimCenter / distanceFromRimCentreToBallCentreInMetres;

                // Project velocity onto the normal
                velocityDotNormal = _ballXvelocityInMetresPerSecond * normalX + _ballYvelocityInMetresPerSecond * normalY;

                _ballXvelocityInMetresPerSecond -= 2 * velocityDotNormal * normalX;

                // Apply restitution
                _ballXvelocityInMetresPerSecond *= c_restitutionCoefficient;

                _ballXInMetres += _ballXvelocityInMetresPerSecond * _timeStepSeconds;
                return;
            }
        }

        // ALL of the ball is ABOVE the rim
        if (_ballYInMetres + c_ballRadiusInMetres / 2 > rimCenterY) return;

        //         .   .---.
        //         _   :   :
        //         |   '---'
        //         -    ========= <- rimCenterY
        //              \/\/\/\//
        //               \/\/\//
        //                \/\//

        // collision with rim
        // ball within hoop and net
        if (_ballYInMetres < rimCenterY - 0.2f || _ballXInMetres < rimCenterX - c_rimRadiusMetres - 0.02f) return;

        //                .---.
        //             ===:   :== <- rimCenterY
        //              \/'---'//
        //               \/\/\//
        //                \/\//

        bool dunked = _ballXInMetres - c_ballRadiusInMetres > rimLeftEdge && _ballXInMetres + c_ballRadiusInMetres < rimRightEdge;

        if (dunked)
        {
            // we need to ensure the ball stays within the rim horizontally, not within the 20% closest to the edge             
            _ballXvelocityInMetresPerSecond = -(rimCenterX - _ballXInMetres) * 0.96f;
            _ballXInMetres += (rimCenterX - _ballXInMetres) / 50;
            _ballYvelocityInMetresPerSecond *= 0.99f;
            return;
        }

        // Simple elastic collision response
        normalX = distanceBetweenBallXAndRimCenter / distanceFromRimCentreToBallCentreInMetres;
        normalY = distanceBetweenBallYAndRimCenter / distanceFromRimCentreToBallCentreInMetres;

        // Project velocity onto the normal
        velocityDotNormal = _ballXvelocityInMetresPerSecond * normalX + _ballYvelocityInMetresPerSecond * normalY;

        _ballXvelocityInMetresPerSecond -= 2 * velocityDotNormal * normalX;

        // Apply restitution
        _ballXvelocityInMetresPerSecond *= c_restitutionCoefficient;

        if (!dunked)
        {
            _ballXInMetres += _ballXvelocityInMetresPerSecond * _timeStepSeconds;
        }
    }
}