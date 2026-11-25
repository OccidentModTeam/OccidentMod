using System;

namespace VesperMod.Presentation.UI;

internal sealed class PotentialFullAnimation
{
    public const int FrameCount = 14;
    private const double FrameDuration = 0.055;
    private const double Duration = FrameCount * FrameDuration;

    private double elapsed;
    private bool wasFull;
    private bool wasReleasing;

    public int? FrameIndex { get; private set; }

    public void Update(bool isFull, bool releasing, double delta)
    {
        isFull = isFull && !releasing;
        if (!isFull && !releasing)
        {
            Reset();
            return;
        }

        if (releasing)
        {
            elapsed = wasReleasing ? Math.Min(Duration, elapsed + delta) : 0;
        }
        else
        {
            elapsed = wasFull ? (elapsed + delta) % Duration : 0;
        }

        wasFull = isFull;
        wasReleasing = releasing;

        FrameIndex = elapsed < Duration ? Math.Min(FrameCount - 1, (int)(elapsed / FrameDuration)) : null;
    }

    public void Reset()
    {
        elapsed = 0;
        wasFull = false;
        wasReleasing = false;
        FrameIndex = null;
    }
}
