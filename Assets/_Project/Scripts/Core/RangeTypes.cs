namespace RangeControl.Core
{
    public enum TargetType
    {
        PaperBullseye,
        SteelPlate,
        MovingSilhouette,
        PopUp,
        NoShoot,
        ClayPigeon,
    }

    public enum MovementPattern
    {
        Static,
        LeftToRight,
        PingPong,
        Strafe,
        PopUp,
    }

    public enum TargetSize
    {
        Small,
        Normal,
        Large,
    }

    public enum SurfaceType
    {
        Dirt,
        Wood,
        Steel,
        Paper,
    }

    public enum FireMode
    {
        Semi,
        Auto,
    }

    public enum GameMode
    {
        FreePractice,
        MovingGallery,
        Precision,
        SteelChallenge,
        Reaction,
        Clays,
    }

    public enum RoundState
    {
        Idle,
        Countdown,
        Running,
        Results,
    }

    public enum Medal
    {
        None,
        Bronze,
        Silver,
        Gold,
        Developer,
    }

    public static class TargetSizeExtensions
    {
        /// <summary>World scale applied to a target: 0.7x, 1x or 1.5x.</summary>
        public static float Scale(this TargetSize size)
        {
            switch (size)
            {
                case TargetSize.Small: return 0.7f;
                case TargetSize.Large: return 1.5f;
                default: return 1f;
            }
        }
    }
}
