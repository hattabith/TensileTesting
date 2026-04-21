namespace TensileTestingApp.Services.Abstractions;
    /// <summary>
    /// Stateful causal signal filter applied to a scalar stream in real time.
    /// </summary>
    public interface ISignalFilter
    {
        /// <summary>Filters the next sample and returns the smoothed value.</summary>
        double Filter(double value);

        /// <summary>Clears internal state (call before a new measurement session).</summary>
        void Reset();
    }
