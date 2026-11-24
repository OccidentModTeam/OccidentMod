namespace OccidentMod.Presentation.UI;

internal sealed class ResonanceAnimation
{
    private const int PreparationFrames = 3;
    private const int BurstFrames = 7;
    private const int TailFrames = 9;
    private const double PreparationFrameDuration = 0.07;
    private const double BurstFrameDuration = 0.04;
    private const double TailFrameDuration = 0.05;
    private const double PreparationEnd = PreparationFrames * PreparationFrameDuration;
    private const double BurstEnd = PreparationEnd + BurstFrames * BurstFrameDuration;
    private const double Duration = BurstEnd + TailFrames * TailFrameDuration;

    private double elapsed;

    public int? FrameIndex { get; private set; }

    public void Update(bool isFull, double delta)
    {
        if (!isFull)
        {
            Reset();
            return;
        }

        elapsed = FrameIndex.HasValue ? (elapsed + delta) % Duration : 0;
        FrameIndex = elapsed switch
        {
            < PreparationEnd => (int)(elapsed / PreparationFrameDuration),
            < BurstEnd => PreparationFrames + (int)((elapsed - PreparationEnd) / BurstFrameDuration),
            _ => PreparationFrames + BurstFrames + (int)((elapsed - BurstEnd) / TailFrameDuration)
        };
    }

    public void Reset()
    {
        elapsed = 0;
        FrameIndex = null;
    }
}
