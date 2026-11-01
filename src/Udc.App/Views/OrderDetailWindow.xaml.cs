using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Udc.Client;

namespace Udc.App.Views;

public partial class OrderDetailWindow : Window
{
    private readonly ApiClient _api;
    private readonly int _orderId;
    private decimal _currentDue;

    public OrderDetailWindow(ApiClient api, int orderId)
    {
        InitializeComponent();
        _api = api;
        _orderId = orderId;
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GetOrderDetailAsync(_orderId);
            var o = result.Order;

            TitleText.Text = $"Order {o.OrderNumber}";
            SubtitleText.Text = $"{o.CustomerName} - {o.CustomerMobile}".Trim(' ', '-');
            StatusText.Text = o.StatusLabel ?? o.StatusKey ?? "";
            var statusColor = TryParseColor(o.Color) ?? Color.FromRgb(0x6B, 0x76, 0x88);
            StatusBadge.Background = new SolidColorBrush(statusColor);

            GrandTotalText.Text = Money(o.GrandTotal);
            PaidText.Text = Money(o.TotalPaid);
            DueText.Text = Money(o.CurrentDue);
            DueDateText.Text = o.DueDate ?? "-";
            _currentDue = o.CurrentDue;

            ItemsGrid.ItemsSource = result.Items;
            PaymentsGrid.ItemsSource = result.Payments;

            RecordPaymentButton.Visibility = o.CurrentDue > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load this order: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private static Color? TryParseColor(string? hex)
    {
        if (string.IsNullOrWhiteSpace(hex)) return null;
        try { return (Color)ColorConverter.ConvertFromString(hex); }
        catch (FormatException) { return null; }
    }

    private async void RecordPaymentButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new RecordPaymentWindow(_api, _orderId, _currentDue) { Owner = this };
        if (form.ShowDialog() == true) await LoadAsync();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
