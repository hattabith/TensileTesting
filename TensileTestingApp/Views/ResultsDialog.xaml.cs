namespace TensileTestingApp.Views;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Controls;
using Microsoft.Win32;
using TensileTestingApp.Services;
using TensileTestingApp.ViewModel;

public partial class ResultsDialog : Window
{
    private readonly PdfExportService _pdfExportService;

    public ResultsDialog(PdfExportService pdfExportService)
    {
        InitializeComponent();
        _pdfExportService = pdfExportService;
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
        var host = PlotViewHost;
        host.Content = null;
        Canvas? canvas = CreatePlotCanvas(vm.DataPoints, 320, 220, 32);
        if (canvas is null)
            return;

        host.Content = canvas;
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not ResultsDialogViewModel vm)
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
                byte[]? chartImageData = RenderPlotImageBytes(dataPoints, 1400, 800);

                _pdfExportService.ExportTestResultsToPdf(
                    saveDialog.FileName,
                    vm.ElasticModulus,
                    vm.YieldStrength,
                    vm.UltimateStrength,
                    dataPoints,
                    "Test Results",
                    "Cylindrical",
                    chartImageData);

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

    private static Canvas? CreatePlotCanvas(IReadOnlyList<(double Length, double Force)> dataPoints, double width, double height, double padding)
    {
        if (dataPoints.Count < 2)
            return null;

        double minX = dataPoints.Min(p => p.Length);
        double maxX = dataPoints.Max(p => p.Length);
        double minY = dataPoints.Min(p => p.Force);
        double maxY = dataPoints.Max(p => p.Force);

        if (Math.Abs(maxX - minX) < 0.0001 || Math.Abs(maxY - minY) < 0.0001)
            return null;

        Canvas canvas = new() { Width = width, Height = height, Background = Brushes.White };
        Polyline line = new() { Stroke = Brushes.SteelBlue, StrokeThickness = 2 };

        foreach ((double length, double force) in dataPoints)
        {
            double x = padding + (length - minX) / (maxX - minX) * (width - 2 * padding);
            double y = height - padding - (force - minY) / (maxY - minY) * (height - 2 * padding);
            line.Points.Add(new System.Windows.Point(x, y));
        }

        canvas.Children.Add(line);

        Line xAxis = new() { X1 = padding, Y1 = height - padding, X2 = width - padding, Y2 = height - padding, Stroke = Brushes.Black };
        Line yAxis = new() { X1 = padding, Y1 = height - padding, X2 = padding, Y2 = padding, Stroke = Brushes.Black };
        canvas.Children.Add(xAxis);
        canvas.Children.Add(yAxis);

        TextBlock xLabel = new() { Text = "Length, mm", FontSize = 12 };
        Canvas.SetLeft(xLabel, width / 2 - 30);
        Canvas.SetTop(xLabel, height - padding + 8);
        canvas.Children.Add(xLabel);

        TextBlock yLabel = new() { Text = "Force, kN", FontSize = 12 };
        Canvas.SetLeft(yLabel, 2);
        Canvas.SetTop(yLabel, padding - 18);
        canvas.Children.Add(yLabel);

        return canvas;
    }

    private static byte[]? RenderPlotImageBytes(IReadOnlyList<(double Length, double Force)> dataPoints, int width, int height)
    {
        Canvas? canvas = CreatePlotCanvas(dataPoints, width, height, 60);
        if (canvas is null)
            return null;

        canvas.Measure(new Size(width, height));
        canvas.Arrange(new Rect(0, 0, width, height));
        canvas.UpdateLayout();

        RenderTargetBitmap renderBitmap = new(width, height, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(canvas);

        PngBitmapEncoder encoder = new();
        encoder.Frames.Add(BitmapFrame.Create(renderBitmap));

        using MemoryStream stream = new();
        encoder.Save(stream);
        return stream.ToArray();
    }
}
