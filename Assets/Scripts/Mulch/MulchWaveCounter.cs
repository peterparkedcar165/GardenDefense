// counts whole waves for a mulch that lasts "the next N waves". only a wave that starts after the
// count begins is counted, so picking a mulch in the middle of a wave never counts the wave that
// was already running when it was picked
public class MulchWaveCounter
{
    private int firstCountedWave = int.MaxValue;
    public int Completed { get; private set; }

    public void WaveStarted(int wave)
    {
        if (firstCountedWave == int.MaxValue) firstCountedWave = wave;
    }

    // whole waves still to go out of total, for status lines
    public int Left(int total) => System.Math.Max(0, total - Completed);

    // true when this wave counted
    public bool WaveEnded(int wave)
    {
        if (wave < firstCountedWave) return false;
        Completed++;
        return true;
    }
}
