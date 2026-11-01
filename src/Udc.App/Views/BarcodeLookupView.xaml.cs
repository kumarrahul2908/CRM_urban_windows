using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Udc.Client;

namespace Udc.App.Views;

public partial class BarcodeLookupView : UserControl
{
    private readonly ApiClient _api;

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
        var labelText = new TextBlock { Text = label, FontSize = 12.5, Foreground = (System.Windows.Media.Brush)FindResource("MutedBrush") };
        var valueText = new TextBlock { Text = value, FontSize = 13, TextWrapping = TextWrapping.Wrap };
        Grid.SetColumn(labelText, 0);
        Grid.SetColumn(valueText, 1);
        row.Children.Add(labelText);
        row.Children.Add(valueText);
        FieldsPanel.Children.Add(row);
    }
}
