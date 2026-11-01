using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Udc.Client;

namespace Udc.App.Views;

public partial class DormantCustomersView : UserControl
{
    private readonly ApiClient _api;

    public DormantCustomersView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        if (!int.TryParse(MinDaysBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var minDays) || minDays < 0)
        {
            minDays = 30;
            MinDaysBox.Text = "30";
        }

        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GetDormantCustomersAsync(minDays);
            CustomersGrid.ItemsSource = result.Customers;
            SummaryText.Text = $"{result.Customers.Count} customer(s) with no order in at least {result.MinDays} day(s)";
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load dormant customers: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();
}
