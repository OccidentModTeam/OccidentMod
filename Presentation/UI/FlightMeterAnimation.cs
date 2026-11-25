using System;

namespace VesperMod.Presentation.UI;

internal sealed class FlightMeterAnimation
{
    public const int RefillFrameCount = 36;
    public const int LoopFrameCount = 30;
    private const double FrameDuration = 0.05;
    private const double RefillDuration = RefillFrameCount * FrameDuration;
    private const double LoopDuration = LoopFrameCount * FrameDuration;
    private const double FadeDuration = 0.25;

    private enum Phase { Normal, Refill, Infinite, FinishLoop, Fade }

    private Phase phase;
    private double time;
    private bool wasBelowFull;

    public bool UsesLoop => phase is Phase.Infinite or Phase.FinishLoop or Phase.Fade;
    public bool IsAnimating => phase != Phase.Normal;
    public float LoopOpacity => phase == Phase.Fade ? (float)(1 - time / FadeDuration) : 1f;
    public int Frame => phase == Phase.Fade ? LoopFrameCount - 1
        : Math.Min((int)(time / FrameDuration), (UsesLoop ? LoopFrameCount : RefillFrameCount) - 1);

    public void Reset()
    {
        phase = Phase.Normal;
        time = 0;
        wasBelowFull = false;
    }

    public void Update(double elapsed, bool hasFlight, bool full, bool infinite)
    {
        bool refilled = hasFlight && full && wasBelowFull;
        wasBelowFull = hasFlight && !full && !infinite;

        if (infinite)
        {
            if (phase is Phase.Infinite or Phase.FinishLoop)
                time = (time + elapsed) % LoopDuration;
            else
                time = 0;

            phase = Phase.Infinite;
            return;
        }

        if (phase == Phase.Infinite)
            phase = Phase.FinishLoop;

        if (phase == Phase.FinishLoop)
        {
            time += elapsed;
            if (time < LoopDuration)
                return;

            phase = Phase.Fade;
            elapsed = time - LoopDuration;
            time = 0;
        }

        if (phase == Phase.Fade)
        {
            time += elapsed;
            if (time >= FadeDuration)
                phase = Phase.Normal;
            return;
        }

        if (!hasFlight)
        {
            Reset();
        }
        else if (refilled)
        {
            phase = Phase.Refill;
            time = 0;
        }
        else if (phase == Phase.Refill)
        {
            time += elapsed;
            if (time >= RefillDuration)
                phase = Phase.Normal;
        }
    }
}
