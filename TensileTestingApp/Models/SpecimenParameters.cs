namespace TensileTestingApp.Models;

record SpecimenParameters(
    string Name,
    string Type,
    double GaugeLengthMm,
    double? DiameterMm,
    double? WidthMm,
    double? ThicknessMm,
    DateTime RecordedAt
);
