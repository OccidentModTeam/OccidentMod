using Microsoft.Xna.Framework;
using ReLogic.Utilities;
using Terraria;
using Terraria.Audio;

namespace VesperMod.Presentation.Audio;

internal struct HighwatcherAudio
{
    private const int HumIntervalTicks = 8 * 60;

    private static readonly SoundStyle MachineHum = new("VesperMod/Assets/Sounds/NPCs/Highwatcher/MachineHum")
    {
        Volume = 0.55f,
        MaxInstances = 0,
        PauseBehavior = PauseBehavior.PauseWithGame,
    };

    private static readonly SoundStyle BirdCall = new("VesperMod/Assets/Sounds/NPCs/Highwatcher/BirdCall")
    {
        Volume = 0.72f,
        MaxInstances = 0,
        PauseBehavior = PauseBehavior.PauseWithGame,
    };

    private static readonly SoundStyle SummonCry = BirdCall with
    {
        Identifier = "VesperMod/HighwatcherSummonCry",
        IsLooped = true,
        Volume = 0.85f,
    };

    private int humDelay;
    private bool calling;
    private SlotId? humSlot;
    private SlotId? birdSlot;
    private SlotId? summonSlot;

    internal void Update(NPC npc, float? summonProgress)
    {
        bool humDue = humDelay == 0;
        humDelay = humDue ? HumIntervalTicks - 1 : humDelay - 1;

        if (summonProgress is float progress)
        {
            if (!calling)
            {
                calling = true;
                Stop(ref humSlot);
                Stop(ref birdSlot);
                summonSlot = PlayFollowing(SummonCry, npc);
            }

            if (summonSlot is SlotId id && SoundEngine.TryGetActiveSound(id, out ActiveSound cry))
                cry.Volume = MathHelper.Lerp(0.12f, 1f, MathHelper.Clamp(progress, 0f, 1f));
            return;
        }

        if (calling)
        {
            calling = false;
            Stop(ref summonSlot);
        }

        if (humDue && !(birdSlot is SlotId bird && SoundEngine.TryGetActiveSound(bird, out _)))
            humSlot = PlayFollowing(MachineHum, npc);
    }

    internal void Stomp(Vector2 position)
    {
        if (calling)
            return;

        Stop(ref humSlot);
        birdSlot = PlayBirdCall(position);
    }

    internal static SlotId PlayBirdCall(Vector2 position) => SoundEngine.PlaySound(BirdCall, position);

    private static SlotId PlayFollowing(SoundStyle style, NPC npc)
    {
        var owner = npc.ModNPC;
        return SoundEngine.PlaySound(style, npc.Center, sound =>
        {
            if (!npc.active || npc.life <= 0 || !ReferenceEquals(npc.ModNPC, owner))
                return false;

            sound.Position = npc.Center;
            return true;
        });
    }

    private static void Stop(ref SlotId? slot)
    {
        if (slot is SlotId id && SoundEngine.TryGetActiveSound(id, out ActiveSound sound))
            sound.Stop();

        slot = null;
    }
}
