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
                    vm.SpecimenTypeName,
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
        SolidColorBrush gridBrush = new(Color.FromRgb(220, 220, 220));
        const int tickCount = 5;

        // Grid and axis ticks with numeric labels.
        for (int i = 0; i <= tickCount; i++)
        {
            double t = i / (double)tickCount;

            double x = padding + t * (width - 2 * padding);
            double y = height - padding - t * (height - 2 * padding);

            // Vertical grid lines
            Line vGrid = new()
            {
                X1 = x,
                Y1 = padding,
                X2 = x,
                Y2 = height - padding,
                Stroke = gridBrush,
                StrokeThickness = 0.8
            };
            canvas.Children.Add(vGrid);

            // Horizontal grid lines
            Line hGrid = new()
            {
                X1 = padding,
                Y1 = y,
                X2 = width - padding,
                Y2 = y,
                Stroke = gridBrush,
                StrokeThickness = 0.8
            };
            canvas.Children.Add(hGrid);

            // X-axis ticks + labels
            Line xTick = new()
            {
                X1 = x,
                Y1 = height - padding,
                X2 = x,
                Y2 = height - padding + 4,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            canvas.Children.Add(xTick);

            double xValue = minX + t * (maxX - minX);
            TextBlock xTickLabel = new()
            {
                Text = xValue.ToString("F2"),
                FontSize = width >= 600 ? 11 : 9,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(xTickLabel, x - (width >= 600 ? 18 : 14));
            Canvas.SetTop(xTickLabel, height - padding + 6);
            canvas.Children.Add(xTickLabel);

            // Y-axis ticks + labels
            Line yTick = new()
            {
                X1 = padding - 4,
                Y1 = y,
                X2 = padding,
                Y2 = y,
                Stroke = Brushes.Black,
                StrokeThickness = 1
            };
            canvas.Children.Add(yTick);

            double yValue = minY + t * (maxY - minY);
            TextBlock yTickLabel = new()
            {
                Text = yValue.ToString("F2"),
                FontSize = width >= 600 ? 11 : 9,
                Foreground = Brushes.Black
            };
            Canvas.SetLeft(yTickLabel, width >= 600 ? 4 : 2);
            Canvas.SetTop(yTickLabel, y - (width >= 600 ? 8 : 7));
            canvas.Children.Add(yTickLabel);
        }

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
        Canvas.SetTop(xLabel, height - (width >= 600 ? 20 : 8));
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
