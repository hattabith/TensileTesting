namespace TensileTestingApp.Models;
    public class TensileTestData
    {
        public DateTime Timestamp { get; set; }
        /// <summary>Raw (unfiltered) force value from the ADC.</summary>
        public double Force { get; set; }
        /// <summary>Smoothed force value produced by the configured signal filter.</summary>
        public double FilteredForce { get; set; }
        /// <summary>Raw (unfiltered) length value from the ADC.</summary>
        public double Length { get; set; }
        /// <summary>Smoothed length value produced by the configured signal filter.</summary>
        public double FilteredLength { get; set; }
        /// <summary>Force value after zero-offset correction is applied.</summary>
        public double CorrectedForce { get; set; }
        /// <summary>Length value after zero-offset correction is applied.</summary>
        public double CorrectedLength { get; set; }
        /// <summary>Force value after preload adjustment (mode-dependent).</summary>
        public double PreloadAdjustedForce { get; set; }
        /// <summary>Length value after preload adjustment (mode-dependent).</summary>
        public double PreloadAdjustedLength { get; set; }
    }
