using System;
using OccidentMod.Content.Mounts;

namespace OccidentMod.Presentation.Rendering;

internal static class PokaAnimation
{
    private const int CrouchBottomFrame = 4;
    internal const int IdleDuration = 46;
    private static readonly int[] IdleFrameTicks = [7, 4, 4, 4, 4, 7, 4, 4, 4, 4];
    private static readonly int[] CrouchFrameTicks = [4, 4, 4, 3, 4, 4, 4, 4, 5];

    internal static (string Animation, int Frame) GetPose(
        bool charging, float chargeProgress, bool moving, int crouchTick, float walkClock, float idleClock)
    {
        if (charging)
        {
            int frame = Math.Min(CrouchBottomFrame,
                (int)(chargeProgress * (CrouchBottomFrame + 1)));
            if (moving)
            {
                // The final two crouch poses share the same depth.
                int depth = Math.Min(frame, 3);
                return ("ChargeWalk", depth * PokaMount.WalkFrameCount + (int)walkClock);
            }

            return ("Crouch", frame);
        }

        if (crouchTick >= 0)
            return ("Crouch", FindFrame(crouchTick, CrouchFrameTicks));

        if (moving)
            return ("Walk", (int)walkClock);

        return ("Idle", FindFrame((int)idleClock, IdleFrameTicks));
    }

    private static int FindFrame(int tick, int[] frameTicks)
    {
        for (int frame = 0; frame < frameTicks.Length - 1; frame++)
        {
            tick -= frameTicks[frame];
            if (tick < 0)
                return frame;
        }

        return frameTicks.Length - 1;
    }
}
