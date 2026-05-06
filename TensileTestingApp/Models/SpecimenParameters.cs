namespace TensileTestingApp.Models;

record SpecimenParameters(
    string Name,
    string Type,
    double DiameterMm,
    double GaugeLengthMm,
    DateTime RecordedAt
);
