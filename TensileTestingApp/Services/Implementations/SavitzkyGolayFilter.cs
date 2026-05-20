using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations;

/// <summary>
/// Savitzky-Golay smoothing filter with fixed symmetric coefficients.
/// Supported window sizes: 5 and 7 points.
/// </summary>
public sealed class SavitzkyGolayFilter : ISignalFilter
{
    private readonly double[] _coefficients;
    private readonly Queue<double> _buffer;

    public int WindowSize => _coefficients.Length;

    public SavitzkyGolayFilter(int windowSize)
    {
        // TODO: For more flexibility, consider allowing users to specify custom coefficients or support additional window sizes.
        _coefficients = windowSize switch
        {
            5 => new[] { -3d / 35d, 12d / 35d, 17d / 35d, 12d / 35d, -3d / 35d },
            7 => new[] { -2d / 21d, 3d / 21d, 6d / 21d, 7d / 21d, 6d / 21d, 3d / 21d, -2d / 21d },
            _ => throw new ArgumentOutOfRangeException(nameof(windowSize), "Supported window sizes are 5 or 7.")
        };

        _buffer = new Queue<double>(windowSize);
    }

    public double Filter(double value)
    {
        _buffer.Enqueue(value);
        if (_buffer.Count > _coefficients.Length)
            _buffer.Dequeue();

        // During warm-up (before full window), return simple average.
        if (_buffer.Count < _coefficients.Length)
            return _buffer.Average();

        double sum = 0.0;
        int index = 0;
        foreach (double sample in _buffer)
        {
            sum += sample * _coefficients[index++];
        }

        return sum;
    }

    public void Reset()
    {
        _buffer.Clear();
    }
}
