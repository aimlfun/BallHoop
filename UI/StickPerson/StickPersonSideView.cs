namespace BallHoop.StickPerson;

/// <summary>
/// Simple stick person side view.
/// Who'd have thought that a stick person could be so complex? It's all in the angles.
/// Based on an anchor point for the head, all limbs are drawn relative to this (torso, arms, legs). Those
/// limbs have lengths and angles relative to each other.
/// SETTER/GETTER enforce no movement beyond the limits of the human body (approx).
/// </summary>
internal class StickPersonSideView
{
    /// <summary>
    /// This is 2024, the world is in chaos, and the only thing that can save us is a stick person.
    /// And better still it includes a diverse range of skin colours, inclusivity box ticked, or is it sticked?
    /// </summary>
    /// <returns></returns>
    private static Color GetRandomSkinColor()
    {
        Color[] skinColors =
        [
            Color.FromArgb(100, 255, 224, 189), // Light skin
            Color.FromArgb(100, 198, 134, 66),  // Medium skin
            Color.FromArgb(100, 141, 85, 36)    // Dark skin
            // sample your photo using RGB tool, and add yours instead
        ];

        // pick one of the colours at random
        Random rand = new();

        return skinColors[rand.Next(skinColors.Length)];
    }

    /// <summary>
    /// Colour of the stick person.
    /// </summary>
    private readonly Pen s_penPerson;

    /// <summary>
    /// Colour of the stick person.
    /// </summary>
    private readonly SolidBrush s_brushPerson;

    /// <summary>
    /// Graphics and Bitmaps are inverted, so we need to know what we are subtracting from.
    /// </summary>
    private readonly double _canvasHeightInPixels;

    /// <summary>
    /// The scale of the world to the canvas.
    /// </summary>
    private readonly double _scaleMetresToPixels;

    /// <summary>
    /// This is where the centre of the head is.
    /// </summary>
    private PointF _worldLocationInMetres;

    /// <summary>
    /// Get the world location of the stick person (the centre of the head).
    /// </summary>
    public PointF WorldLocationInMetres
    {
        get => _worldLocationInMetres;
    }

    /// <summary>
    /// This where the centre of the stick person's head in pixels.
    /// </summary>
    public PointF LocationInPx
    {
        get
        {
            return new((float)(_worldLocationInMetres.X * _scaleMetresToPixels), (float)(_canvasHeightInPixels - _worldLocationInMetres.Y * _scaleMetresToPixels));
        }
    }

    #region POSITION OF LIMBS (COMPUTED IN PX)
    /// <summary>
    /// Centre of the head in pixels.
    /// </summary>
    private PointF _headCentrePX;

    /// <summary>
    /// Point where the torso is anchored to the head in pixels.
    /// </summary>
    private PointF _torsoAnchoredToHeadPX;

    /// <summary>
    /// Bottom of the torso in pixels.
    /// </summary>
    private PointF _torsoBottomPX;

    /// <summary>
    /// Point where the shoulder is in pixels.
    /// </summary>
    private PointF _shoulderPX;

    /// <summary>
    /// Point where the left elbow is in pixels.
    /// </summary>
    private PointF _leftElbowPX;

    /// <summary>
    /// Point where the right elbow is in pixels.
    /// </summary>
    private PointF _rightElbowPX;

    /// <summary>
    /// Point where the left wrist is in pixels.
    /// </summary>
    private PointF _leftWristPX;

    /// <summary>
    /// Point where the right wrist is in pixels.
    /// </summary>
    private PointF _rightWristPX;

    /// <summary>
    /// Point where the left hand is in pixels.
    /// </summary>
    private PointF _leftHandPX;

    /// <summary>
    /// Point where the right hand is in pixels.
    /// </summary>
    private PointF _rightHandPX;

    /// <summary>
    /// Point where the hip is in pixels.
    /// </summary>
    private PointF _hipPX;

    /// <summary>
    /// Point where the left knee is in pixels.
    /// </summary>
    private PointF _leftKneePX;

    /// <summary>
    /// Point where the right knee is in pixels.
    /// </summary>
    private PointF _rightKneePX;

    /// <summary>
    /// Point where the left ankle is in pixels.
    /// </summary>
    private PointF _leftAnklePX;

    /// <summary>
    /// Point where the right ankle is in pixels.
    /// </summary>
    private PointF _rightAnklePX;

    /// <summary>
    /// Point where the left foot is in pixels.
    /// </summary>
    private PointF _leftFootPX;

    /// <summary>
    /// Point where the right foot is in pixels.
    /// </summary>
    private PointF _rightFootPX;

    /// <summary>
    /// Set to true if the limb positions need to be recomputed.
    /// We don't recompute them needlessly, because it is computationally expensive.
    /// </summary>
    private bool _recomputeLimbPositions = true;
    #endregion

    #region HUMAN ANATOMY
    // Vague idea from this: https://roymech.org/Useful_Tables/Human/Human_sizes.html

    /// <summary>
    /// The length of the upper arm in metres.
    /// </summary>
    private double _upperArmLengthInMetres = 0.32f;

    /// <summary>
    /// The length of the lower arm in metres.
    /// </summary>
    private double _lowerArmLengthInMetres = 0.285f;

    /// <summary>
    /// The length of the wrist in metres.
    /// </summary>
    private double _wristToHandLengthInMetres = 0.21f;

    /// <summary>
    /// The distance from the hip to the knee in metres.
    /// </summary>
    private double _upperLegLengthInMetres = 0.45f;

    /// <summary>
    /// The distance from the knee to the foot in metres.
    /// </summary>
    private double _lowerLegLengthInMetres = 0.45f;

    /// <summary>
    /// The length of the foot in metres.
    /// </summary>
    private double _footLengthInMetres = 0.24f;

    /// <summary>
    /// The length of the head in metres.
    /// </summary>
    private double _headLengthInMetres = 0.24f; // [26]

    /// <summary>
    /// The length of the neck in metres.
    /// </summary>
    private double _kneckLengthInMetres = 0.15f;

    /// <summary>
    /// The length of the torso in metres (from the hip to the shoulder).
    /// </summary>
    private double _torsoLengthInMetres = 0.56f;
    #endregion

    #region ANGLES
    /// <summary>
    /// The angle of the torso in degrees from the vertical. Main limb angles are relative to this.
    /// </summary>
    private float _torsoAngleInDegrees = 90;

    /// <summary>
    /// Angle of the torso in degrees from the vertical. Remember this is side on.
    /// </summary>
    internal float TorsoAngleInDegrees
    {
        get => _torsoAngleInDegrees;
        set
        {
            if (value < 0 || value > 360)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Torso angle must be between 0 and 360 degrees");
            }

            _recomputeLimbPositions = true;
            _torsoAngleInDegrees = value;
        }
    }

    #region ARMS
    /// <summary>
    /// The angle of the left upper arm in degrees from the torso angle.
    /// </summary>
    private float _upperLeftArmAngleInDegreesFromTorso = 20f;

    /// <summary>
    /// The compound angle of the left upper arm in degrees from the vertical.
    /// </summary>
    private float UpperLeftArmAngle
    {
        get => _torsoAngleInDegrees + _upperLeftArmAngleInDegreesFromTorso;
    }

    /// <summary>
    /// Get/Set the angle of the left upper arm with respect to the torso.
    /// </summary>
    internal float RelativeUpperLeftArmAngle
    {
        get => _upperLeftArmAngleInDegreesFromTorso;

        set
        {
            if (value < -179 || value > 70)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _upperLeftArmAngleInDegreesFromTorso = value;
        }
    }

    /// <summary>
    /// The angle of the right upper arm in degrees from the torso angle.
    /// </summary>
    private float _upperRightArmAngleInDegreesFromTorso = 20f;

    /// <summary>
    /// Returns the compound angle of the right upper arm in degrees from the vertical.
    /// </summary>
    private float UpperRightArmAngle
    {
        get => _torsoAngleInDegrees + _upperRightArmAngleInDegreesFromTorso;
    }

    /// <summary>
    /// Get/Set the angle of the right upper arm with respect to the torso.
    /// </summary>
    internal float RelativeUpperRightArmAngle
    {
        get => _upperRightArmAngleInDegreesFromTorso;
        set
        {
            if (value < -179 || value > 70)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _upperRightArmAngleInDegreesFromTorso = value;
        }
    }

    /// <summary>
    /// The angle of the left lower arm in degrees from the left upper arm angle.
    /// </summary>
    private float _lowerLeftArmAngleInDegreesFromUpperArm = 240;

    /// <summary>
    /// Returns the compound angle of the left lower arm in degrees from the vertical.
    /// </summary>
    private float LowerLeftArmAngle
    {
        get => UpperLeftArmAngle + _lowerLeftArmAngleInDegreesFromUpperArm;
    }


    /// <summary>
    /// Get/Set the angle of the left lower arm with respect to the left upper arm.
    /// </summary>
    internal float RelativeLowerLeftArmAngle
    {
        get => _lowerLeftArmAngleInDegreesFromUpperArm;
        set
        {
            if (value < -170 || value > 0)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _lowerLeftArmAngleInDegreesFromUpperArm = value;
        }
    }

    /// <summary>
    /// The angle of the right lower arm in degrees from the right upper arm angle.
    /// </summary>
    private float _lowerRightArmAngleInDegreesFromUpperArm = 240;

    /// <summary>
    /// Returns the compound angle of the right lower arm in degrees from the vertical.
    /// </summary>
    private float LowerRightArmAngle
    {
        get => UpperRightArmAngle + _lowerRightArmAngleInDegreesFromUpperArm;
    }

    /// <summary>
    /// Get/Set the angle of the right lower arm with respect to the right upper arm.
    /// </summary>
    internal float RelativeLowerRightArmAngle
    {
        get => _lowerRightArmAngleInDegreesFromUpperArm;
        set
        {
            if (value < -170 || value > 0)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _lowerRightArmAngleInDegreesFromUpperArm = value;
        }
    }

    /// <summary>
    /// The angle of the left wrist in degrees from the lower arm angle.
    /// </summary>
    private float _leftWristAngleInDegreesFromLowerArm = 0;

    /// <summary>
    /// Returns the compound angle of the left wrist in degrees from the vertical.
    /// </summary>
    private float LeftWristAngle
    {
        get => LowerLeftArmAngle + _leftWristAngleInDegreesFromLowerArm;
    }

    /// <summary>
    /// Get/Set the angle of the left wrist with respect to the left lower arm.
    /// </summary>
    internal float RelativeLeftWristAngle
    {
        get => _leftWristAngleInDegreesFromLowerArm;
        set
        {
            if (value < -85 || value > 80)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _leftWristAngleInDegreesFromLowerArm = value;
        }
    }

    /// <summary>
    /// The angle of the right wrist in degrees from the lower arm angle.
    /// </summary>
    private float _rightWristAngleInDegreesFromLowerArm = 0;

    /// <summary>
    /// Returns the compound angle of the right wrist in degrees from the vertical.
    /// </summary>
    private float RightWristAngle
    {
        get => LowerRightArmAngle + _rightWristAngleInDegreesFromLowerArm;
    }

    /// <summary>
    /// Get/Set the angle of the right wrist with respect to the right lower arm.
    /// </summary>
    internal float RelativeRightWristAngle
    {
        get => _rightWristAngleInDegreesFromLowerArm;
        set
        {
            if (value < -85 || value > 80)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _rightWristAngleInDegreesFromLowerArm = value;
        }
    }
    #endregion

    #region LEGS
    /// <summary>
    /// The angle of the left upper leg in degrees from the torso angle.
    /// </summary>
    private float _leftUpperLegAngleInDegreesFromTorso = 10;

    /// <summary>
    /// Get/Set the angle of the left upper leg with respect to the torso.
    /// </summary>
    internal float RelativeLeftUpperLegAngle
    {
        get => _leftUpperLegAngleInDegreesFromTorso;
        set
        {
            if (value < -140 || value > 70)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _leftUpperLegAngleInDegreesFromTorso = value;
        }
    }

    /// <summary>
    /// The angle of the right upper leg in degrees from the torso angle.
    /// </summary>
    private float _rightUpperLegAngleInDegreesFromTorso = 10;

    /// <summary>
    /// Get/Set the angle of the right upper leg with respect to the torso.
    /// </summary>
    internal float RelativeRightUpperLegAngle
    {
        get => _rightUpperLegAngleInDegreesFromTorso;
        set
        {
            if (value < -140 || value > 70)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _rightUpperLegAngleInDegreesFromTorso = value;
        }
    }

    /// <summary>
    /// The angle of the left lower leg in degrees from the upper leg angle.
    /// </summary>
    private float _leftLowerLegAngleInDegreesFromUpperLeg = 240;

    /// <summary>
    /// The angle of the left knee in degrees from the torso angle.
    /// </summary>
    private float LeftKneeAngle
    {
        get => _leftUpperLegAngleInDegreesFromTorso + _torsoAngleInDegrees;
    }

    /// <summary>
    /// Get/Set the angle of the left lower leg with respect to the left upper leg.
    /// </summary>
    internal float RelativeLeftLowerLegAngle
    {
        get => _leftLowerLegAngleInDegreesFromUpperLeg;
        set
        {
            if (value < 0 || value > 170)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _leftLowerLegAngleInDegreesFromUpperLeg = value;
        }
    }

    /// <summary>
    /// The angle of the right lower leg in degrees from the upper leg angle.
    /// </summary>
    private float _rightLowerLegAngleInDegreesFromUpperLeg = 240;

    /// <summary>
    /// The angle of the right knee in degrees from the torso angle.
    /// </summary>
    private float RightKneeAngle
    {
        get => _rightUpperLegAngleInDegreesFromTorso + _torsoAngleInDegrees;
    }

    /// <summary>
    /// Get/Set the angle of the right lower leg with respect to the right upper leg.
    /// </summary>
    internal float RelativeRightLowerLegAngle
    {
        get => _rightLowerLegAngleInDegreesFromUpperLeg;
        set
        {
            if (value < 0 || value > 170)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _rightLowerLegAngleInDegreesFromUpperLeg = value;
        }
    }

    /// <summary>
    /// The angle of the left ankle in degrees from the lower leg angle.
    /// </summary>
    private float _leftAnkleAngleInDegreesFromLowerLeg = -90;

    /// <summary>
    /// Get the compound angle of the left ankle in degrees from the vertical.
    /// </summary>
    private float LeftAnkleAngle
    {
        get => LeftKneeAngle + _leftLowerLegAngleInDegreesFromUpperLeg;
    }

    /// <summary>
    /// Get/Set the angle of the left ankle with respect to the left lower leg.
    /// </summary>
    internal float RelativeLeftAnkleAngle
    {
        get => _leftAnkleAngleInDegreesFromLowerLeg;
        set
        {
            if (value < -95 || value > -5)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _leftAnkleAngleInDegreesFromLowerLeg = value;
        }
    }

    /// <summary>
    /// The angle of the right ankle in degrees from the lower leg angle.
    /// </summary>
    private float _rightAnkleAngleInDegreesFromLowerLeg = -90;

    /// <summary>
    /// Get the compound angle of the right ankle in degrees from the vertical.
    /// </summary>
    private float RightAnkleAngle
    {
        get => RightKneeAngle + _rightLowerLegAngleInDegreesFromUpperLeg;
    }

    /// <summary>
    /// Get/Set the angle of the right ankle with respect to the right lower leg.
    /// </summary>
    internal float RelativeRightAnkleAngle
    {
        get => _rightAnkleAngleInDegreesFromLowerLeg;
        set
        {
            if (value < -95 || value > -5)
            {
                return;
            }

            _recomputeLimbPositions = true;
            _rightAnkleAngleInDegreesFromLowerLeg = value;
        }
    }

    /// <summary>
    /// Get the compound angle of the left foot in degrees from the vertical.
    /// </summary>
    private float LeftFootAngle
    {
        get => LeftAnkleAngle + _leftAnkleAngleInDegreesFromLowerLeg;
    }

    /// <summary>
    /// Get the compound angle of the right foot in degrees from the vertical.
    /// </summary>
    private float RightFootAngle
    {
        get => RightAnkleAngle + _rightAnkleAngleInDegreesFromLowerLeg;
    }
    #endregion
    #endregion

    /// <summary>
    /// Returns the bounding box of the stick person in pixels.
    /// </summary>
    internal RectangleF BoundingBoxInPX
    {
        get
        {
            PointF[] limbPoints = [new PointF(
                                    _headCentrePX.X,
                                    _headCentrePX.Y+ (float) (_headLengthInMetres/_scaleMetresToPixels/2)),
                                    _torsoAnchoredToHeadPX,
                                    _torsoBottomPX,
                                    _shoulderPX,
                                    _leftElbowPX, _leftWristPX, _leftHandPX, _hipPX,
                                    _leftKneePX, _leftAnklePX, _leftFootPX,
                                    _rightElbowPX, _rightWristPX, _rightHandPX, _rightKneePX, _rightAnklePX, _rightFootPX];

            float minX = limbPoints.Min(p => p.X);
            float maxX = limbPoints.Max(p => p.X);

            float minY = limbPoints.Min(p => p.Y);
            float maxY = limbPoints.Max(p => p.Y);

            return new RectangleF(minX, minY, maxX - minX, maxY - minY);
        }
    }

    /// <summary>
    /// We draw based on the head position (center).
    /// </summary>
    internal PointF AnchorPX
    {
        get
        {
            return _headCentrePX;
        }
    }

    /// <summary>
    /// Rough position when standing.
    /// </summary>
    private readonly float _baseHeight;

    /// <summary>
    /// Constructor for the stick person side view.
    /// </summary>
    /// <param name="canvasHeightInPixels"></param>
    /// <param name="scaleMetresToPixels"></param>
    /// <param name="locationInWorld"></param>
    /// <param name="heightOfPersonInMetres"></param>
    internal StickPersonSideView(int canvasHeightInPixels, double scaleMetresToPixels, PointF locationInWorld, double heightOfPersonInMetres)
    {
        _canvasHeightInPixels = canvasHeightInPixels;
        _scaleMetresToPixels = scaleMetresToPixels;
        _worldLocationInMetres = locationInWorld;
        _baseHeight = locationInWorld.Y;

        // diversity in 3 lines
        Color skinColor = GetRandomSkinColor();
        s_penPerson = new Pen(skinColor, 4);
        s_brushPerson = new SolidBrush(Color.FromArgb(100, skinColor.R, skinColor.G, skinColor.B));

        ScaleLimbsBasedOnHeightOfPerson(heightOfPersonInMetres);

        SetThrowPosition();
    }

    /// <summary>
    /// Moves stick person to a new location in the world, recalculating the limb positions.
    /// </summary>
    /// <param name="locationInWorldMetres"></param>
    internal void MoveTo(PointF locationInWorldMetres)
    {
        _worldLocationInMetres = locationInWorldMetres;
        _headCentrePX = new PointF((float)(locationInWorldMetres.X * _scaleMetresToPixels), (float)(locationInWorldMetres.Y * _scaleMetresToPixels));

        _recomputeLimbPositions = true;
    }

    /// <summary>
    /// Scale the limbs based on the height of the person.
    /// </summary>
    /// <param name="heightOfPersonInMetres"></param>
    private void ScaleLimbsBasedOnHeightOfPerson(double heightOfPersonInMetres)
    {
        _upperArmLengthInMetres *= heightOfPersonInMetres / 1.8f;
        _lowerArmLengthInMetres *= heightOfPersonInMetres / 1.8f;
        _wristToHandLengthInMetres *= heightOfPersonInMetres / 1.8f;

        _torsoLengthInMetres *= heightOfPersonInMetres / 1.8f;

        _upperLegLengthInMetres *= heightOfPersonInMetres / 1.8f;
        _lowerLegLengthInMetres *= heightOfPersonInMetres / 1.8f;

        _footLengthInMetres *= heightOfPersonInMetres / 1.8f;

        _headLengthInMetres *= heightOfPersonInMetres / 1.8f;
        _kneckLengthInMetres *= heightOfPersonInMetres / 1.8f;
    }

    /// <summary>
    /// Draws the stick person from head to feet
    /// </summary>
    /// <param name="g"></param>
    internal void Draw(Graphics g)
    {
        float headRadiusInPX = (float)(_headLengthInMetres / 2f * _scaleMetresToPixels);
        ComputeLimbPositions(false);

        // Draw the stick person
        g.DrawLine(s_penPerson, _torsoAnchoredToHeadPX, _torsoBottomPX);

        g.DrawLines(s_penPerson, [_shoulderPX, _leftElbowPX, _leftWristPX, _leftHandPX]);
        g.DrawLines(s_penPerson, [_hipPX, _leftKneePX, _leftAnklePX, _leftFootPX]);

        // head goes on top of left drawing, and right side on top of left side + head
        g.FillEllipse(s_brushPerson, _headCentrePX.X - headRadiusInPX, _headCentrePX.Y - headRadiusInPX, headRadiusInPX * 2, headRadiusInPX * 2);

        g.DrawLines(s_penPerson, [_shoulderPX, _rightElbowPX, _rightWristPX, _rightHandPX]);
        g.DrawLines(s_penPerson, [_hipPX, _rightKneePX, _rightAnklePX, _rightFootPX]);
    }

    /// <summary>
    /// Puts the stick person on the floor.
    /// </summary>
    internal void SetOnGround()
    {
        _worldLocationInMetres.Y = _baseHeight;
        ComputeLimbPositions(true);
    }
    /// <summary>
    /// Vertical position of the stick person.
    /// </summary>
    private double _verticalVelocityInMetresPerSecond = 0;

    /// <summary>
    /// Applies gravity to the stick person, so they fall.
    /// </summary>
    internal void ApplyGravityToStickPerson()
    {
        MoveToTarget();

        double timeInterval = 10f / 1000f;

        // apply gravity
        _verticalVelocityInMetresPerSecond += 9.8f * timeInterval; // 9.8 m/s^2 * 0.02 s

        // move the stick person
        _worldLocationInMetres.Y -= (float)(_verticalVelocityInMetresPerSecond * timeInterval); // 0.02 s
        ComputeLimbPositions(true);

        // if the stick person is below the ground, then stop them        
        if (IsOnGround())
        {
            _verticalVelocityInMetresPerSecond = 0;
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    internal bool IsOnGround()
    {
        double z = BoundingBoxInPX.Bottom;

        if (z >= _canvasHeightInPixels - 1)
            return true;
        else
            return false;
    }

    /// <summary>
    /// Compute the positions of the limbs based on the angles and lengths.
    /// </summary>
    internal void ComputeLimbPositions(bool force = false)
    {
        if (!force && !_recomputeLimbPositions)
        {
            return;
        }

        _headCentrePX = LocationInPx;
        _torsoAnchoredToHeadPX = new(_headCentrePX.X, _headCentrePX.Y);
        _torsoBottomPX = RotatePoint(_torsoAnchoredToHeadPX, _torsoAngleInDegrees, (float)((_kneckLengthInMetres + _torsoLengthInMetres) * _scaleMetresToPixels));

        _shoulderPX = RotatePoint(_torsoAnchoredToHeadPX, _torsoAngleInDegrees, (float)(_kneckLengthInMetres * _scaleMetresToPixels));

        _leftElbowPX = RotatePoint(_shoulderPX, UpperLeftArmAngle, (float)(_upperArmLengthInMetres * _scaleMetresToPixels));
        _rightElbowPX = RotatePoint(_shoulderPX, UpperRightArmAngle, (float)(_upperArmLengthInMetres * _scaleMetresToPixels));

        _leftWristPX = RotatePoint(_leftElbowPX, LowerLeftArmAngle, (float)(_lowerArmLengthInMetres * _scaleMetresToPixels));
        _rightWristPX = RotatePoint(_rightElbowPX, LowerRightArmAngle, (float)(_lowerArmLengthInMetres * _scaleMetresToPixels));

        _leftHandPX = RotatePoint(_leftWristPX, LeftWristAngle, (float)(_wristToHandLengthInMetres * _scaleMetresToPixels));
        _rightHandPX = RotatePoint(_rightWristPX, RightWristAngle, (float)(_wristToHandLengthInMetres * _scaleMetresToPixels));

        _hipPX = new(_torsoBottomPX.X, _torsoBottomPX.Y);

        _leftKneePX = RotatePoint(_hipPX, LeftKneeAngle, (float)(_upperLegLengthInMetres * _scaleMetresToPixels));
        _rightKneePX = RotatePoint(_hipPX, RightKneeAngle, (float)(_upperLegLengthInMetres * _scaleMetresToPixels));

        _leftAnklePX = RotatePoint(_leftKneePX, LeftAnkleAngle, (float)(_lowerLegLengthInMetres * _scaleMetresToPixels));
        _rightAnklePX = RotatePoint(_rightKneePX, RightAnkleAngle, (float)(_lowerLegLengthInMetres * _scaleMetresToPixels));

        _leftFootPX = RotatePoint(_leftAnklePX, LeftFootAngle, (float)(_footLengthInMetres * _scaleMetresToPixels));
        _rightFootPX = RotatePoint(_rightAnklePX, RightFootAngle, (float)(_footLengthInMetres * _scaleMetresToPixels));

        _recomputeLimbPositions = false;
    }

    /// <summary>
    /// Returns a point that is a radius rotated by an angle around a point.
    /// </summary>
    /// <param name="torsoAnchoredToHeadPX"></param>
    /// <param name="torsoAngleInDegrees"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    private static PointF RotatePoint(PointF torsoAnchoredToHeadPX, float torsoAngleInDegrees, float radius)
    {
        // create a new point rotating around the torso
        return new PointF(
            (float)(torsoAnchoredToHeadPX.X + radius * Math.Cos(AngleInDegreesToRadians(torsoAngleInDegrees))),
            (float)(torsoAnchoredToHeadPX.Y + radius * Math.Sin(AngleInDegreesToRadians(torsoAngleInDegrees))));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="angleInDegrees"></param>
    /// <returns></returns>
    private static double AngleInDegreesToRadians(double angleInDegrees)
    {
        return angleInDegrees * Math.PI / 180;
    }

    /// <summary>
    /// Assigns the values to all the limbs.
    /// </summary>
    /// <param name="torsoAngleInDegrees"></param>
    /// <param name="upperLeftArmAngleInDegreesFromTorso"></param>
    /// <param name="upperRightArmAngleInDegreesFromTorso"></param>
    /// <param name="lowerLeftArmAngleInDegreesFromUpperArm"></param>
    /// <param name="lowerRightArmAngleInDegreesFromUpperArm"></param>
    /// <param name="leftWristAngleInDegreesFromLowerArm"></param>
    /// <param name="rightWristAngleInDegreesFromLowerArm"></param>
    /// <param name="leftUpperLegAngleInDegreesFromTorso"></param>
    /// <param name="rightUpperLegAngleInDegreesFromTorso"></param>
    /// <param name="leftLowerLegAngleInDegreesFromUpperLeg"></param>
    /// <param name="rightLowerLegAngleInDegreesFromUpperLeg"></param>
    /// <param name="leftAnkleAngleInDegreesFromLowerLeg"></param>
    /// <param name="rightAnkleAngleInDegreesFromLowerLeg"></param>
    internal void SetAllLimbsToValues(
        float torsoAngleInDegrees,
        float upperLeftArmAngleInDegreesFromTorso,
        float upperRightArmAngleInDegreesFromTorso,
        float lowerLeftArmAngleInDegreesFromUpperArm,
        float lowerRightArmAngleInDegreesFromUpperArm,
        float leftWristAngleInDegreesFromLowerArm,
        float rightWristAngleInDegreesFromLowerArm,
        float leftUpperLegAngleInDegreesFromTorso,
        float rightUpperLegAngleInDegreesFromTorso,
        float leftLowerLegAngleInDegreesFromUpperLeg,
        float rightLowerLegAngleInDegreesFromUpperLeg,
        float leftAnkleAngleInDegreesFromLowerLeg,
        float rightAnkleAngleInDegreesFromLowerLeg)
    {
        _torsoAngleInDegrees = torsoAngleInDegrees;
        _upperLeftArmAngleInDegreesFromTorso = upperLeftArmAngleInDegreesFromTorso;
        _upperRightArmAngleInDegreesFromTorso = upperRightArmAngleInDegreesFromTorso;
        _lowerLeftArmAngleInDegreesFromUpperArm = lowerLeftArmAngleInDegreesFromUpperArm;
        _lowerRightArmAngleInDegreesFromUpperArm = lowerRightArmAngleInDegreesFromUpperArm;
        _leftWristAngleInDegreesFromLowerArm = leftWristAngleInDegreesFromLowerArm;
        _rightWristAngleInDegreesFromLowerArm = rightWristAngleInDegreesFromLowerArm;
        _leftUpperLegAngleInDegreesFromTorso = leftUpperLegAngleInDegreesFromTorso;
        _rightUpperLegAngleInDegreesFromTorso = rightUpperLegAngleInDegreesFromTorso;
        _leftLowerLegAngleInDegreesFromUpperLeg = leftLowerLegAngleInDegreesFromUpperLeg;
        _rightLowerLegAngleInDegreesFromUpperLeg = rightLowerLegAngleInDegreesFromUpperLeg;
        _leftAnkleAngleInDegreesFromLowerLeg = leftAnkleAngleInDegreesFromLowerLeg;
        _rightAnkleAngleInDegreesFromLowerLeg = rightAnkleAngleInDegreesFromLowerLeg;

        _recomputeLimbPositions = true;
    }

    /// <summary>
    /// If the left hand can touch the ball, we move the left hand to the ball.
    /// That includes adjustments to the wrist and elbow.
    /// </summary>
    /// <param name="ballPixels"></param>
    /// <param name="radiusOfBallInPixels"></param>
    /// <param name="velocity"></param>
    /// <returns></returns>
    internal bool LeftHandCanTouch(PointF ballPixels, double radiusOfBallInPixels, PointF velocity)
    {
        double distX = _shoulderPX.X - ballPixels.X;
        double distY = _shoulderPX.Y - ballPixels.Y;

        double distanceToBallCentre = (double)Math.Sqrt(distX * distX + distY * distY);

        if (distanceToBallCentre > _upperArmLengthInMetres * _scaleMetresToPixels + _lowerArmLengthInMetres * _scaleMetresToPixels + _wristToHandLengthInMetres * _scaleMetresToPixels + radiusOfBallInPixels)
        {
            return false;
        }

        // The angle of the line from the shoulder to the ball
        double theta = Math.Atan2(velocity.Y, velocity.X);

        // this is the point the hand should be at
        float x2 = (float)(ballPixels.X + radiusOfBallInPixels * Math.Cos(theta));
        float y2 = (float)(ballPixels.Y + radiusOfBallInPixels * Math.Sin(theta));

        _leftHandPX = new PointF(x2, y2);

        // Calculate the intersection of the circles
        PointF[] intersections = FindCircleIntersections(_shoulderPX, _upperArmLengthInMetres * _scaleMetresToPixels, _leftHandPX, _lowerArmLengthInMetres * _scaleMetresToPixels);

        if (intersections.Length == 0)
        {
            return false;
        }

        // Choose the intersection point that is closer to the previous elbow position
        _leftElbowPX = Distance(intersections[0], _leftElbowPX) < Distance(intersections[1], _leftElbowPX) ? intersections[0] : intersections[1];

        // Calculate the angles
        float upperArmAngle = (float)Math.Atan2(_leftElbowPX.Y - _shoulderPX.Y, _leftElbowPX.X - _shoulderPX.X);
        float lowerArmAngle = (float)Math.Atan2(_leftHandPX.Y - _leftElbowPX.Y, _leftHandPX.X - _leftElbowPX.X);
        float wristAngle = (float)Math.Atan2(_leftHandPX.Y - _leftElbowPX.Y, _leftHandPX.X - _leftElbowPX.X);

        // Convert angles to degrees
        _upperLeftArmAngleInDegreesFromTorso = upperArmAngle * 180 / (float)Math.PI - _torsoAngleInDegrees;
        _lowerLeftArmAngleInDegreesFromUpperArm = lowerArmAngle * 180 / (float)Math.PI - upperArmAngle * 180 / (float)Math.PI;

        _leftWristAngleInDegreesFromLowerArm = -(wristAngle * 180 / (float)Math.PI - lowerArmAngle * 180 / (float)Math.PI);
        _rightWristAngleInDegreesFromLowerArm = -(wristAngle * 180 / (float)Math.PI - lowerArmAngle * 180 / (float)Math.PI);

        _upperRightArmAngleInDegreesFromTorso = upperArmAngle * 180 / (float)Math.PI - _torsoAngleInDegrees;
        _lowerRightArmAngleInDegreesFromUpperArm = lowerArmAngle * 180 / (float)Math.PI - upperArmAngle * 180 / (float)Math.PI;

        _recomputeLimbPositions = true;

        return true;
    }

    /// <summary>
    /// Determine the intersection of 2 arcs (center 1 using radius 1 etc).
    /// The shoulder is fixed, the wrist moves with the ball. Given bones are fixed length
    /// we have to work out where to move the elbow to.
    /// </summary>
    /// <param name="center1"></param>
    /// <param name="radius1"></param>
    /// <param name="center2"></param>
    /// <param name="radius2"></param>
    /// <returns></returns>
    private static PointF[] FindCircleIntersections(PointF center1, double radius1, PointF center2, double radius2)
    {
        double d = Distance(center1, center2);

        if (d > radius1 + radius2 || d < Math.Abs(radius1 - radius2))
        {
            return []; // No intersection
        }

        float a = (float)((radius1 * radius1 - radius2 * radius2 + d * d) / (2 * d));
        float h = (float)Math.Sqrt(radius1 * radius1 - a * a);

        PointF p2 = new(
            (float)(center1.X + a * (center2.X - center1.X) / d),
            (float)(center1.Y + a * (center2.Y - center1.Y) / d)
        );

        return
        [
            new PointF((float) (p2.X + h * (center2.Y - center1.Y) / d),
                       (float) (p2.Y - h * (center2.X - center1.X) / d)),
            new PointF((float) (p2.X - h * (center2.Y - center1.Y) / d),
                       (float) (p2.Y + h * (center2.X - center1.X) / d))
        ];
    }

    /// <summary>
    /// Pythagoras distance between 2 points.
    /// </summary>
    /// <param name="p1"></param>
    /// <param name="p2"></param>
    /// <returns></returns>
    private static double Distance(PointF p1, PointF p2)
    {
        return (double)Math.Sqrt((p1.X - p2.X) * (p1.X - p2.X) + (p1.Y - p2.Y) * (p1.Y - p2.Y));
    }

    /// <summary>
    /// Relaxes arms to sides.
    /// </summary>
    internal void RelaxArms()
    {
        _upperLeftArmAngleInDegreesFromTorso -= Math.Min(3, Math.Abs(_upperLeftArmAngleInDegreesFromTorso)) * Math.Sign(_upperLeftArmAngleInDegreesFromTorso);
        _upperRightArmAngleInDegreesFromTorso -= Math.Min(3, Math.Abs(_upperRightArmAngleInDegreesFromTorso)) * Math.Sign(_upperRightArmAngleInDegreesFromTorso);

        _lowerLeftArmAngleInDegreesFromUpperArm -= Math.Min(3, Math.Abs(_lowerLeftArmAngleInDegreesFromUpperArm)) * Math.Sign(_lowerLeftArmAngleInDegreesFromUpperArm);
        _lowerRightArmAngleInDegreesFromUpperArm -= Math.Min(3, Math.Abs(_lowerRightArmAngleInDegreesFromUpperArm)) * Math.Sign(_lowerRightArmAngleInDegreesFromUpperArm);

        _leftWristAngleInDegreesFromLowerArm -= 0.5f * Math.Sign(_leftWristAngleInDegreesFromLowerArm);
        _rightWristAngleInDegreesFromLowerArm -= 0.5f * Math.Sign(_rightWristAngleInDegreesFromLowerArm);

        _recomputeLimbPositions = true;
    }

    double _celebrationTime = -1;
    int _celebstep = -1;
    bool celebrationFinished = false;

    /// <summary>
    /// Get/Set if the celebration is finished.
    /// </summary>
    internal bool CelebrationFinished
    {
        get => celebrationFinished;
        set
        {
            if (celebrationFinished == value) return;
            celebrationFinished = value;

            if (celebrationFinished)
            {
                _celebrationTime = -1;
                _celebstep = -1;
            }

            _recomputeLimbPositions = true;
        }
    }

    /// <summary>
    /// List of target angles for the celebration.
    /// </summary>
    Dictionary<string, float> _celebrationTargets = [];

    /// <summary>
    /// Assign the target angles for the celebration.
    /// </summary>
    /// <param name="targets"></param>
    private void SetTarget(Dictionary<string, float> targets)
    {
        _celebrationTargets = targets;
    }

    /// <summary>
    /// Make the stick person squat.
    /// </summary>
    private void SetSquatTarget()
    {
        SetTarget(
            new Dictionary<string, float>
            {
                {"torsoAngleInDegrees", 92},
                {"upperLeftArmAngleInDegreesFromTorso", -3},
                {"upperRightArmAngleInDegreesFromTorso", -3},
                {"lowerLeftArmAngleInDegreesFromUpperArm", -6},
                {"lowerRightArmAngleInDegreesFromUpperArm", -7 },
                {"leftWristAngleInDegreesFromLowerArm", -7},
                {"rightWristAngleInDegreesFromLowerArm", -5},
                {"leftUpperLegAngleInDegreesFromTorso", -17},
                {"rightUpperLegAngleInDegreesFromTorso", -14},
                {"leftLowerLegAngleInDegreesFromUpperLeg", 47},
                {"rightLowerLegAngleInDegreesFromUpperLeg", 84},
                {"leftAnkleAngleInDegreesFromLowerLeg", -95},
                {"rightAnkleAngleInDegreesFromLowerLeg", -95}
            }
        );
    }

    /// <summary>
    /// Celebrate the goal.
    /// </summary>
    /// <param name="timeInSeconds"></param>
    internal void Celebrate(double timeInSeconds)
    {
        if (celebrationFinished) // already finished
        {
            _celebrationTargets.Clear();
            return;
        }

        if (_celebrationTime < 0)
        {
            _celebrationTime = timeInSeconds;
            _celebstep = 0;
            SetSquatTarget();
            CelebrationFinished = false;
        }

        MoveToTarget();

        if (!ReachedTarget()) return;

        ++_celebstep;

        switch (_celebstep)
        {
            case 1: // Jump
                SetHandsInAirTarget();
                break;
            case 2:
                SwingHandsLeftRight();
                break;
            case 3:
                SwingHandsRightLeft();
                break;
            case 4:
                SwingHandsLeftRight();
                break;
            case 5:
                SwingHandsRightLeft();
                break;
            case 6:
                SetHandsToReset();
                break;
            default:
                CelebrationFinished = true;
                break;
        }
    }

    /// <summary>
    /// Reset the stick person to the target angles.
    /// </summary>
    internal void SetHandsToReset()
    {
        SetTarget(
            new Dictionary<string, float>
            {
                {"upperLeftArmAngleInDegreesFromTorso", 3},
                {"upperRightArmAngleInDegreesFromTorso", 1},
                {"lowerLeftArmAngleInDegreesFromUpperArm", 0},
                {"lowerRightArmAngleInDegreesFromUpperArm",0},
                {"leftWristAngleInDegreesFromLowerArm", 6},
                {"rightWristAngleInDegreesFromLowerArm", 10}
            }
        );
    }

    /// <summary>
    /// One hand left, one right.
    /// </summary>
    private void SwingHandsRightLeft()
    {
        SetTarget(
            new Dictionary<string, float>
            {
                {"upperLeftArmAngleInDegreesFromTorso", 56},
                {"upperRightArmAngleInDegreesFromTorso", -45},
                {"lowerLeftArmAngleInDegreesFromUpperArm", -28},
                {"lowerRightArmAngleInDegreesFromUpperArm", -6},
                {"leftWristAngleInDegreesFromLowerArm", -41},
                {"rightWristAngleInDegreesFromLowerArm", -2},
                {"leftUpperLegAngleInDegreesFromTorso", -30},
                {"rightUpperLegAngleInDegreesFromTorso", 30},
                {"leftLowerLegAngleInDegreesFromUpperLeg", 20 },
                {"rightLowerLegAngleInDegreesFromUpperLeg", -20},
            }
        );
    }

    /// <summary>
    /// Other hand left, other right.
    /// </summary>
    private void SwingHandsLeftRight()
    {
        SetTarget(
            new Dictionary<string, float>
            {
                {"upperLeftArmAngleInDegreesFromTorso", -45},
                {"upperRightArmAngleInDegreesFromTorso", 56},
                {"lowerLeftArmAngleInDegreesFromUpperArm", -6},
                {"lowerRightArmAngleInDegreesFromUpperArm", -28},
                {"leftWristAngleInDegreesFromLowerArm", -22},
                {"rightWristAngleInDegreesFromLowerArm", -41},
                {"leftUpperLegAngleInDegreesFromTorso", 30},
                {"rightUpperLegAngleInDegreesFromTorso", -30},
                {"leftLowerLegAngleInDegreesFromUpperLeg", -20},
                {"rightLowerLegAngleInDegreesFromUpperLeg", 20},
            }
        );
    }

    /// <summary>
    /// Returns true if the stick person has reached the target angles.
    /// </summary>
    /// <returns></returns>
    private bool ReachedTarget()
    {
        // compare all the angles against the target angles
        foreach (string target in _celebrationTargets.Keys)
        {
            switch (target)
            {
                case "torsoAngleInDegrees":
                    if (Math.Abs(_torsoAngleInDegrees - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "upperLeftArmAngleInDegreesFromTorso":
                    if (Math.Abs(_upperLeftArmAngleInDegreesFromTorso - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "upperRightArmAngleInDegreesFromTorso":
                    if (Math.Abs(_upperRightArmAngleInDegreesFromTorso - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "lowerLeftArmAngleInDegreesFromUpperArm":
                    if (Math.Abs(_lowerLeftArmAngleInDegreesFromUpperArm - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "lowerRightArmAngleInDegreesFromUpperArm":
                    if (Math.Abs(_lowerRightArmAngleInDegreesFromUpperArm - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "leftWristAngleInDegreesFromLowerArm":
                    if (Math.Abs(_leftWristAngleInDegreesFromLowerArm - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "rightWristAngleInDegreesFromLowerArm":
                    if (Math.Abs(_rightWristAngleInDegreesFromLowerArm - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "leftUpperLegAngleInDegreesFromTorso":
                    if (Math.Abs(_leftUpperLegAngleInDegreesFromTorso - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "rightUpperLegAngleInDegreesFromTorso":
                    if (Math.Abs(_rightUpperLegAngleInDegreesFromTorso - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "leftLowerLegAngleInDegreesFromUpperLeg":
                    if (Math.Abs(_leftLowerLegAngleInDegreesFromUpperLeg - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }

                    break;

                case "rightLowerLegAngleInDegreesFromUpperLeg":
                    if (Math.Abs(_rightLowerLegAngleInDegreesFromUpperLeg - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "leftAnkleAngleInDegreesFromLowerLeg":
                    if (Math.Abs(_leftAnkleAngleInDegreesFromLowerLeg - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "rightAnkleAngleInDegreesFromLowerLeg":
                    if (Math.Abs(_rightAnkleAngleInDegreesFromLowerLeg - _celebrationTargets[target]) > 0.1)
                    {
                        return false;
                    }
                    break;

                case "worldLocationInMetres":
                    if (Math.Abs(_worldLocationInMetres.Y - _celebrationTargets[target]) > 0.01)
                    {
                        return false;
                    }
                    break;
            }
        }

        return true; // all angles are within 0.1 of the target
    }

    /// <summary>
    /// Celebrate by hands in the air.
    /// </summary>
    private void SetHandsInAirTarget()
    {
        SetTarget(
            new Dictionary<string, float>
            {
                {"torsoAngleInDegrees", 90},
                {"upperLeftArmAngleInDegreesFromTorso", -137},
                {"upperRightArmAngleInDegreesFromTorso", -126},
                {"lowerLeftArmAngleInDegreesFromUpperArm", -40},
                {"lowerRightArmAngleInDegreesFromUpperArm", -38 },
                {"leftWristAngleInDegreesFromLowerArm", -7},
                {"rightWristAngleInDegreesFromLowerArm", -7},
                {"leftUpperLegAngleInDegreesFromTorso", 0},
                {"rightUpperLegAngleInDegreesFromTorso", 1},
                {"leftLowerLegAngleInDegreesFromUpperLeg", 0},
                {"rightLowerLegAngleInDegreesFromUpperLeg", 1}
            }
        );
    }

    /// <summary>
    /// Put the stick person in the ready position.
    /// </summary>
    internal void GetReady()
    {
        SetTarget(
            new Dictionary<string, float>
            {
                { "torsoAngleInDegrees", 92},
                { "upperLeftArmAngleInDegreesFromTorso", -179},
                { "upperRightArmAngleInDegreesFromTorso", -171 },
                { "lowerLeftArmAngleInDegreesFromUpperArm", -61 },
                { "lowerRightArmAngleInDegreesFromUpperArm", -72 },
                { "leftWristAngleInDegreesFromLowerArm", 66 },
                { "rightWristAngleInDegreesFromLowerArm", 55 },
                { "leftUpperLegAngleInDegreesFromTorso", -10 },
                { "rightUpperLegAngleInDegreesFromTorso", -3 },
                { "leftLowerLegAngleInDegreesFromUpperLeg", 5 },
                { "rightLowerLegAngleInDegreesFromUpperLeg", 9 },
                { "leftAnkleAngleInDegreesFromLowerLeg", -86 },
                { "rightAnkleAngleInDegreesFromLowerLeg", -95  }
            });
    }

    /// <summary>
    /// Set the stick person to the throw position.
    /// </summary>
    internal void SetThrowPosition()
    {
        SetAllLimbsToValues(
            torsoAngleInDegrees: 92,
            upperLeftArmAngleInDegreesFromTorso: -179,
            upperRightArmAngleInDegreesFromTorso: -171,
            lowerLeftArmAngleInDegreesFromUpperArm: -65,
            lowerRightArmAngleInDegreesFromUpperArm: -76,
            leftWristAngleInDegreesFromLowerArm: 46,
            rightWristAngleInDegreesFromLowerArm: 35,
            leftUpperLegAngleInDegreesFromTorso: -10,
            rightUpperLegAngleInDegreesFromTorso: -3,
            leftLowerLegAngleInDegreesFromUpperLeg: 5,
            rightLowerLegAngleInDegreesFromUpperLeg: 9,
            leftAnkleAngleInDegreesFromLowerLeg: -86,
            rightAnkleAngleInDegreesFromLowerLeg: -95);
    }

    /// <summary>
    /// Adjust the angles of the limbs to move towards the target angles.
    /// </summary>
    private void MoveToTarget()
    {
        foreach (string target in _celebrationTargets.Keys)
        {
            switch (target)
            {
                case "torsoAngleInDegrees":
                    _torsoAngleInDegrees = MoveToTarget(_torsoAngleInDegrees, _celebrationTargets[target]);
                    break;

                case "upperLeftArmAngleInDegreesFromTorso":
                    _upperLeftArmAngleInDegreesFromTorso = MoveToTarget(_upperLeftArmAngleInDegreesFromTorso, _celebrationTargets[target]);
                    break;

                case "upperRightArmAngleInDegreesFromTorso":
                    _upperRightArmAngleInDegreesFromTorso = MoveToTarget(_upperRightArmAngleInDegreesFromTorso, _celebrationTargets[target]);
                    break;

                case "lowerLeftArmAngleInDegreesFromUpperArm":
                    _lowerLeftArmAngleInDegreesFromUpperArm = MoveToTarget(_lowerLeftArmAngleInDegreesFromUpperArm, _celebrationTargets[target]);
                    break;

                case "lowerRightArmAngleInDegreesFromUpperArm":
                    _lowerRightArmAngleInDegreesFromUpperArm = MoveToTarget(_lowerRightArmAngleInDegreesFromUpperArm, _celebrationTargets[target]);
                    break;

                case "leftWristAngleInDegreesFromLowerArm":
                    _leftWristAngleInDegreesFromLowerArm = MoveToTarget(_leftWristAngleInDegreesFromLowerArm, _celebrationTargets[target]);
                    break;

                case "rightWristAngleInDegreesFromLowerArm":
                    _rightWristAngleInDegreesFromLowerArm = MoveToTarget(_rightWristAngleInDegreesFromLowerArm, _celebrationTargets[target]);
                    break;

                case "leftUpperLegAngleInDegreesFromTorso":
                    _leftUpperLegAngleInDegreesFromTorso = MoveToTarget(_leftUpperLegAngleInDegreesFromTorso, _celebrationTargets[target]);
                    break;

                case "rightUpperLegAngleInDegreesFromTorso":
                    _rightUpperLegAngleInDegreesFromTorso = MoveToTarget(_rightUpperLegAngleInDegreesFromTorso, _celebrationTargets[target]);
                    break;

                case "leftLowerLegAngleInDegreesFromUpperLeg":
                    _leftLowerLegAngleInDegreesFromUpperLeg = MoveToTarget(_leftLowerLegAngleInDegreesFromUpperLeg, _celebrationTargets[target]);
                    break;

                case "rightLowerLegAngleInDegreesFromUpperLeg":
                    _rightLowerLegAngleInDegreesFromUpperLeg = MoveToTarget(_rightLowerLegAngleInDegreesFromUpperLeg, _celebrationTargets[target]);
                    break;

                case "leftAnkleAngleInDegreesFromLowerLeg":
                    _leftAnkleAngleInDegreesFromLowerLeg = MoveToTarget(_leftAnkleAngleInDegreesFromLowerLeg, _celebrationTargets[target]);
                    break;

                case "rightAnkleAngleInDegreesFromLowerLeg":
                    _rightAnkleAngleInDegreesFromLowerLeg = MoveToTarget(_rightAnkleAngleInDegreesFromLowerLeg, _celebrationTargets[target]);
                    break;

                case "worldLocationInMetres":
                    _worldLocationInMetres.Y = MoveToTarget((float)_worldLocationInMetres.Y, _celebrationTargets[target], 0.001f);
                    break;
            }
        }

        _recomputeLimbPositions = true;
    }

    /// <summary>
    /// Returns a the valuue moved towards the target value.
    /// </summary>
    /// <param name="existingValue"></param>
    /// <param name="targetValue"></param>
    /// <param name="mult"></param>
    /// <returns></returns>
    private static float MoveToTarget(float existingValue, float targetValue, float mult = 3f)
    {
        return existingValue + (Math.Abs(targetValue - existingValue) > 1 && mult > 0.4f ? Math.Sign(targetValue - existingValue) * mult : targetValue - existingValue);
    }

    /// <summary>
    /// Waggle arms and legs when picked up.
    /// </summary>
    internal void Struggle()
    {
        if (_celebrationTargets.Count > 0 && !ReachedTarget())
        {
            MoveToTarget();
            return;
        }

        // wave arms and hands
        Random random = new();

        SetTarget(
            new Dictionary<string, float>
            {
                {"torsoAngleInDegrees", 90+random.Next(10) - 5},
                {"upperLeftArmAngleInDegreesFromTorso", -139-random.Next(50)},
                {"upperRightArmAngleInDegreesFromTorso", -139-random.Next(50)},
                {"lowerLeftArmAngleInDegreesFromUpperArm", 50-random.Next(50)},
                {"lowerRightArmAngleInDegreesFromUpperArm", 50-random.Next(50)},
                {"leftWristAngleInDegreesFromLowerArm",  random.Next(40) - 40},
                {"rightWristAngleInDegreesFromLowerArm",  random.Next(40) - 40},
                {"leftUpperLegAngleInDegreesFromTorso", random.Next(50) - 25},
                {"rightUpperLegAngleInDegreesFromTorso", random.Next(50) - 25},
                {"leftLowerLegAngleInDegreesFromUpperLeg", random.Next(25) - 25},
                {"rightLowerLegAngleInDegreesFromUpperLeg", random.Next(25) - 25},
                {"leftAnkleAngleInDegreesFromLowerLeg",-90},
                {"rightAnkleAngleInDegreesFromLowerLeg", -90},
            }
        );

        _recomputeLimbPositions = true;
    }
}