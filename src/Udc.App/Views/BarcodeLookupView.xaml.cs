using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class BarcodeLookupView : UserControl
{
    private readonly ApiClient _api;
    private string? _lastBarcode;
    private BarcodeLookupResult? _lastResult;

    public BarcodeLookupView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += (_, _) => BarcodeBox.Focus();
    }

    private async void LookUpButton_Click(object sender, RoutedEventArgs e) => await LookUpAsync();

    private async void BarcodeBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await LookUpAsync();
    }

    private async Task LookUpAsync()
    {
        var barcode = BarcodeBox.Text.Trim();
        if (barcode.Length == 0) return;

        ErrorBanner.Visibility = Visibility.Collapsed;
        ResultCard.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.LookupBarcodeAsync(barcode);
            _lastBarcode = barcode;
            _lastResult = result;
            GarmentText.Text = result.GarmentName ?? "(unknown garment)";
            StatusText.Text = result.Status ?? "";

            FieldsPanel.Children.Clear();
            AddField("Order", result.OrderNumber);
            AddField("Invoice", result.InvoiceNumber);
            AddField("Customer", result.CustomerName);
            AddField("Mobile", result.CustomerMobile);
            AddField("Service", result.ServiceName);
            AddField("Due date", result.DueDate);
            if (result.Sequence is not null && result.TotalGarments is not null)
                AddField("Piece", $"{result.Sequence} of {result.TotalGarments}");
            AddField("Remarks", result.Remarks);

            ResultCard.Visibility = Visibility.Visible;
        }
        catch (ApiException ex)
        {
            ErrorText.Text = ex.Message;
            ErrorBanner.Visibility = Visibility.Visible;
        }
        finally
        {
            BarcodeBox.SelectAll();
            BarcodeBox.Focus();
        }
    }

    private void AddField(string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return;
        var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        var labelText = new TextBlock { Text = label, FontSize = 12.5, Foreground = (Brush)FindResource("MutedBrush") };
        var valueText = new TextBlock { Text = value, FontSize = 13, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(labelText, 0);
        Grid.SetColumn(valueText, 1);
        row.Children.Add(labelText);
        row.Children.Add(valueText);
        FieldsPanel.Children.Add(row);
    }

    // ---- tag printing ---------------------------------------------------------------------------
    // Re-prints a tag for the barcode just looked up - this app only ever renders the exact value
    // that was scanned/typed (already confirmed valid by LookUpAsync above), never a newly
    // generated one; it has no printer or scanner to verify a printed tag actually scans, so the
    // barcode graphic's correctness rests entirely on Code39Encoder matching its own external
    // reference table (see that class's doc comment), not on anything testable here.

    private void PrintTagButton_Click(object sender, RoutedEventArgs e)
    {
        if (_lastBarcode is null || _lastResult is null) return;
        if (!Code39Encoder.CanEncode(_lastBarcode))
        {
            MessageBox.Show(Window.GetWindow(this), $"\"{_lastBarcode}\" contains a character this app's barcode printer can't encode (Code 39 supports digits, A-Z, space, and -.$/+%).",
                "Can't print this tag", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true) return;

        var tag = BuildTagVisual(_lastBarcode, _lastResult);
        dialog.PrintVisual(tag, $"Urban Dry Clean CRM - tag {_lastBarcode}");
    }

    private static FrameworkElement BuildTagVisual(string barcode, BarcodeLookupResult result)
    {
        var panel = new StackPanel { Width = 320, Background = Brushes.White, Margin = new Thickness(8) };

        void AddLine(string text, double size, FontWeight weight) =>
            panel.Children.Add(new TextBlock
            {
                Text = text, FontSize = size, FontWeight = weight, TextWrapping = TextWrapping.Wrap,
                HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(0, 0, 0, 2),
            });

        AddLine(result.GarmentName ?? "Garment", 16, FontWeights.Bold);
        AddLine($"Order {result.OrderNumber ?? ""}", 12, FontWeights.Normal);
        if (!string.IsNullOrWhiteSpace(result.CustomerName)) AddLine(result.CustomerName!, 11, FontWeights.Normal);
        if (result.Sequence is not null && result.TotalGarments is not null) AddLine($"Piece {result.Sequence} of {result.TotalGarments}", 11, FontWeights.Normal);
        if (!string.IsNullOrWhiteSpace(result.DueDate)) AddLine($"Due {result.DueDate}", 11, FontWeights.Normal);

        panel.Children.Add(BuildBarcodeGraphic(barcode));
        AddLine(barcode, 13, FontWeights.Normal);

        panel.Measure(new Size(panel.Width, double.PositiveInfinity));
        panel.Arrange(new Rect(new Point(0, 0), new Size(panel.Width, panel.DesiredSize.Height)));
        return panel;
    }

    private static UIElement BuildBarcodeGraphic(string value, double moduleWidth = 2.2, double height = 50)
    {
        var modules = Code39Encoder.Encode(value);
        var canvas = new Canvas { Width = modules.Length * moduleWidth, Height = height, Background = Brushes.White, Margin = new Thickness(0, 6, 0, 6) };
        var x = 0.0;
        foreach (var m in modules)
        {
            if (m == '1')
            {
                var bar = new Rectangle { Width = moduleWidth, Height = height, Fill = Brushes.Black };
                Canvas.SetLeft(bar, x);
                Canvas.SetTop(bar, 0);
                canvas.Children.Add(bar);
            }
            x += moduleWidth;
        }
        return new Border { Child = canvas, HorizontalAlignment = HorizontalAlignment.Center };
    }
}
