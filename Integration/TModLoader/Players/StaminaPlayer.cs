#nullable enable

using OccidentMod.Features.Character.Attributes;
using Terraria.ModLoader;

namespace OccidentMod.Integration.TModLoader.Players;

public sealed class StaminaPlayer : ModPlayer
{
    public StaminaState Stamina { get; private set; } = new(StaminaState.DefaultMaximum);

    public override void Initialize() => Stamina = new StaminaState(StaminaState.DefaultMaximum);

    public override void PostUpdate() => Stamina.UpdateRecovery();

    public override void OnEnterWorld() => Stamina.Restore(Stamina.Maximum);

    public override void OnRespawn() => Stamina.Restore(Stamina.Maximum);
}
