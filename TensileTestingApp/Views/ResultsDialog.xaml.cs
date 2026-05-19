namespace TensileTestingApp.Views;

using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Controls;
using System.IO;
using Microsoft.Win32;
using TensileTestingApp.Services;
using TensileTestingApp.Services.Implementations;
using TensileTestingApp.Configuration;
using TensileTestingApp.ViewModel;

public partial class ResultsDialog : Window
{
    private readonly PdfExportService? _pdfExportService;

    public ResultsDialog()
    {
        InitializeComponent();
        _pdfExportService = new PdfExportService(new AppLogger(
            new Configuration.LoggingSettings()));
        Loaded += ResultsDialog_Loaded;
        SaveButton.Click += SaveButton_Click;
        CloseButton.Click += CloseButton_Click;
    }

    private void ResultsDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is ResultsDialogViewModel vm)
        {
            ElasticModulusText.Text = vm.ElasticModulus.ToString("F2");
            YieldStrengthText.Text = vm.YieldStrength.ToString("F2");
            UltimateStrengthText.Text = vm.UltimateStrength.ToString("F2");
            RenderPlot(vm);
        }
    }

    private void RenderPlot(ResultsDialogViewModel vm)
    {
        // Simple polyline plot (Force vs Length)
        var host = PlotViewHost;
        host.Content = null;
        if (vm.DataPoints.Count < 2) return;

        double minX = vm.DataPoints.Min(p => p.Length);
        double maxX = vm.DataPoints.Max(p => p.Length);
        double minY = vm.DataPoints.Min(p => p.Force);
        double maxY = vm.DataPoints.Max(p => p.Force);
        double w = 320, h = 220, pad = 32;

        var canvas = new Canvas { Width = w, Height = h, Background = Brushes.White };
        Polyline line = new() { Stroke = Brushes.SteelBlue, StrokeThickness = 2 };
        foreach (var pt in vm.DataPoints)
        {
            double x = pad + (pt.Length - minX) / (maxX - minX) * (w - 2 * pad);
            double y = h - pad - (pt.Force - minY) / (maxY - minY) * (h - 2 * pad);
            line.Points.Add(new System.Windows.Point(x, y));
        }
        canvas.Children.Add(line);
        // Axes
        var xAxis = new Line { X1 = pad, Y1 = h - pad, X2 = w - pad, Y2 = h - pad, Stroke = Brushes.Black };
        var yAxis = new Line { X1 = pad, Y1 = h - pad, X2 = pad, Y2 = pad, Stroke = Brushes.Black };
        canvas.Children.Add(xAxis);
        canvas.Children.Add(yAxis);
        // Labels
        var xLabel = new TextBlock { Text = "Length, mm", FontSize = 12 };
        Canvas.SetLeft(xLabel, w / 2 - 30); Canvas.SetTop(xLabel, h - pad + 8);
        canvas.Children.Add(xLabel);
        var yLabel = new TextBlock { Text = "Force, N", FontSize = 12 };
        Canvas.SetLeft(yLabel, 2); Canvas.SetTop(yLabel, pad - 18);
        canvas.Children.Add(yLabel);
        host.Content = canvas;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ResultsDialogViewModel vm || _pdfExportService == null)
        {
            MessageBox.Show("No data to save.", "Error");
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            DefaultExt = ".pdf",
            FileName = $"TestResults_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.pdf"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                var dataPoints = vm.DataPoints.ToList();
                _pdfExportService.ExportTestResultsToPdf(
                    saveDialog.FileName,
                    vm.ElasticModulus,
                    vm.YieldStrength,
                    vm.UltimateStrength,
                    dataPoints,
                    "Test Results",
                    "Cylindrical");

                MessageBox.Show($"PDF saved successfully to:\n{saveDialog.FileName}", "Success");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save PDF: {ex.Message}", "Error");
            }
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }
}
