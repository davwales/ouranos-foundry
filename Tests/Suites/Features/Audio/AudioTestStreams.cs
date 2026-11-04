using Godot;

namespace Ouranos.Foundry.Tests.Features.Audio;

internal static class AudioTestStreams
{
    /// <summary>A silent 16-bit mono stream, long enough not to end during a test.</summary>
    public static AudioStreamWav Silent(float seconds = 10f)
    {
        const int mixRate = 8000;
        return new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = mixRate,
            Data = new byte[(int)(mixRate * seconds) * 2],
        };
    }
}
