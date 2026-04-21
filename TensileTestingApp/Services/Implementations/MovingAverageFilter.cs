using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations;
    /// <summary>
    /// Simple uniform moving average (sliding window) filter.
    /// Output is the arithmetic mean of the last <see cref="WindowSize"/> samples.
    /// Introduces a phase lag of WindowSize/2 samples.
    /// </summary>
    public sealed class MovingAverageFilter : ISignalFilter
    {
        private readonly int _windowSize;
        private readonly Queue<double> _buffer;
        private double _sum;

        public int WindowSize => _windowSize;

        public MovingAverageFilter(int windowSize)
        {
            if (windowSize < 1)
                throw new ArgumentOutOfRangeException(nameof(windowSize), "Window size must be at least 1.");
            _windowSize = windowSize;
            _buffer = new Queue<double>(windowSize);
        }

        public double Filter(double value)
        {
            _buffer.Enqueue(value);
            _sum += value;

            if (_buffer.Count > _windowSize)
            {
                _sum -= _buffer.Dequeue();
            }

            return _sum / _buffer.Count;
        }

        public void Reset()
        {
            _buffer.Clear();
            _sum = 0.0;
        }
    }
