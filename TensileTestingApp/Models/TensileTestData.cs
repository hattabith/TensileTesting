namespace TensileTestingApp.Models
{
    public class TensileTestData
    {
        public DateTime Timestamp { get; set; }
        /// <summary>Raw (unfiltered) force value from the ADC.</summary>
        public double Force { get; set; }
        /// <summary>Smoothed force value produced by the configured signal filter.</summary>
        public double FilteredForce { get; set; }
        public double Length { get; set; }
    }
}
