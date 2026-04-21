using TensileTestingApp.Services.Abstractions;

namespace TensileTestingApp.Services.Implementations;
    /// <summary>
    /// Exponential Moving Average filter.
    /// y[n] = α · x[n] + (1 − α) · y[n−1]
    /// Lower alpha → more smoothing, higher latency.
    /// alpha = 1.0 is a transparent passthrough (no smoothing).
    /// </summary>
    public sealed class ExponentialMovingAverageFilter : ISignalFilter
    {
        private readonly double _alpha;
        private readonly double _oneMinusAlpha;
        private double _previous;
        private bool _initialized;

        public ExponentialMovingAverageFilter(double alpha)
        {
            if (alpha <= 0.0 || alpha > 1.0)
                throw new ArgumentOutOfRangeException(nameof(alpha), "Alpha must be in the range (0, 1].");
            _alpha = alpha;
            _oneMinusAlpha = 1.0 - alpha;
        }

        public double Filter(double value)
        {
            if (!_initialized)
            {
                _previous = value;
                _initialized = true;
                return value;
            }

            double result = _alpha * value + _oneMinusAlpha * _previous;
            _previous = result;
            return result;
        }

        public void Reset()
        {
            _initialized = false;
            _previous = 0.0;
        }
    }
