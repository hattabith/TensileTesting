namespace TensileTestingApp.ViewModel;

using System.Collections.ObjectModel;
using System.ComponentModel;

public class ResultsDialogViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<(double Length, double Force)> DataPoints { get; } = new();

    private double _elasticModulus;
    public double ElasticModulus
    {
        get => _elasticModulus;
        set { if (_elasticModulus != value) { _elasticModulus = value; OnPropertyChanged(nameof(ElasticModulus)); } }
    }

    private double _yieldStrength;
    public double YieldStrength
    {
        get => _yieldStrength;
        set { if (_yieldStrength != value) { _yieldStrength = value; OnPropertyChanged(nameof(YieldStrength)); } }
    }

    private double _ultimateStrength;
    public double UltimateStrength
    {
        get => _ultimateStrength;
        set { if (_ultimateStrength != value) { _ultimateStrength = value; OnPropertyChanged(nameof(UltimateStrength)); } }
    }

    private string _specimenTypeName = "Unknown";
    public string SpecimenTypeName
    {
        get => _specimenTypeName;
        set { if (_specimenTypeName != value) { _specimenTypeName = value; OnPropertyChanged(nameof(SpecimenTypeName)); } }
    }

    protected void OnPropertyChanged(string propertyName) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
