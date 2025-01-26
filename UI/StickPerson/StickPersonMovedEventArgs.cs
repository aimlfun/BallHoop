//#define DRAW_TRAJECTORY
namespace BallHoop
{
    public class StickPersonMovedEventArgs
    {
        public PointF Location { get; }

        public StickPersonMovedEventArgs(PointF location) => Location = location;
    }
}