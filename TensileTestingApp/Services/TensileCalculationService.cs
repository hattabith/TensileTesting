namespace TensileTestingApp.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using TensileTestingApp.Models;

public static class TensileCalculationService
{
    public const int MinimumPointCount = 100;

    public static (double ElasticModulus, double YieldStrength, double UltimateStrength) CalculateParameters(
        IList<TensileTestData> data,
        double specimenGaugeLengthMm,
        double specimenDiameterMm)
    {
        if (data.Count < MinimumPointCount)
        {
            throw new ArgumentException($"Not enough data for calculation. Minimum required points: {MinimumPointCount}.");
        }

        if (specimenGaugeLengthMm <= 0 || specimenDiameterMm <= 0)
        {
            throw new ArgumentException("Invalid specimen parameters. Diameter and gauge length must be > 0.");
        }

        // Use PreloadAdjustedForce/Length if available, else CorrectedForce/Length
        var points = data.Select(d => (
            Length: d.PreloadAdjustedLength != 0 ? d.PreloadAdjustedLength : d.CorrectedLength,
            Force: d.PreloadAdjustedForce != 0 ? d.PreloadAdjustedForce : d.CorrectedForce)).ToList();

        // Ultimate strength (max force)
        double maxForce = points.Max(p => p.Force);
        double ultimateStrength = maxForce / GetArea(specimenDiameterMm); // [MPa]

        // Elastic modulus: linear fit first 10-20% of max force
        double elasticLimit = maxForce * 0.2;
        var elasticPoints = points.Where(p => p.Force <= elasticLimit).ToList();
        if (elasticPoints.Count < 2) throw new ArgumentException("Not enough elastic region data.");
        var (slope, _) = LinearFit(elasticPoints);
        double elasticModulus = slope * specimenGaugeLengthMm / GetArea(specimenDiameterMm); // [MPa]

        // Yield strength (0.2% offset method)
        double offset = 0.002 * specimenGaugeLengthMm;
        double yieldStrength = FindYieldStrength(points, slope, offset, specimenGaugeLengthMm, specimenDiameterMm);

        return (elasticModulus, yieldStrength, ultimateStrength);
    }

    private static double GetArea(double diameterMm) => Math.PI * Math.Pow(diameterMm / 2, 2); // mm^2

    private static (double slope, double intercept) LinearFit(List<(double Length, double Force)> pts)
    {
        double n = pts.Count;
        double sumX = pts.Sum(p => p.Length);
        double sumY = pts.Sum(p => p.Force);
        double sumXY = pts.Sum(p => p.Length * p.Force);
        double sumX2 = pts.Sum(p => p.Length * p.Length);
        double slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        double intercept = (sumY - slope * sumX) / n;
        return (slope, intercept);
    }

    private static double FindYieldStrength(List<(double Length, double Force)> pts, double slope, double offset, double gaugeLength, double diameter)
    {
        // 0.2% offset line: y = slope * (x - offset)
        for (int i = 0; i < pts.Count; i++)
        {
            double x = pts[i].Length;
            double y = pts[i].Force;
            double yOffset = slope * (x - offset);
            if (y >= yOffset)
            {
                return y / GetArea(diameter); // [MPa]
            }
        }
        return pts.Last().Force / GetArea(diameter);
    }
}
