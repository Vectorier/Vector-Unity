using DG.Tweening;

/// <summary>
/// Pause state of the running level for visuals that animate on their own clock (sprite animations,
/// item pickup tweens). The fixed-step simulation itself is paused by <see cref="LevelMainController"/>.
/// </summary>
public static class GameplayPause
{
    /// <summary>DOTween id for level tweens that must freeze while the game is paused.</summary>
    public const string TweenId = "Gameplay";

    public static bool IsPaused { get; private set; }

    public static void SetPaused(bool paused)
    {
        if (IsPaused == paused)
        {
            return;
        }
        IsPaused = paused;
        if (paused)
        {
            DOTween.Pause(TweenId);
        }
        else
        {
            DOTween.Play(TweenId);
        }
    }
}
