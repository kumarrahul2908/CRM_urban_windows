using System.Globalization;
using System.Windows;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class WorkshopBillingDetailWindow : Window
{
    private readonly ApiClient _api;
    private readonly int _billId;
    private readonly List<WorkshopOption> _paymentTypes;

    public WorkshopBillingDetailWindow(ApiClient api, int billId, List<WorkshopOption> paymentTypes)
    {
        InitializeComponent();
        _api = api;
        _billId = billId;
        _paymentTypes = paymentTypes;
        PaymentTypeBox.ItemsSource = paymentTypes;
        if (paymentTypes.Count > 0) PaymentTypeBox.SelectedIndex = 0;
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GetWorkshopBillingDetailAsync(_billId);
            var b = result.Bill;
            TitleText.Text = $"Bill {b.BillNumber}";
            SubtitleText.Text = $"{b.StoreName} - {b.WorkshopName} - {b.PeriodStart} to {b.PeriodEnd} - {b.Status}";
            GrandTotalText.Text = Money(b.GrandTotal);
            AmountPaidText.Text = Money(b.AmountPaid);
            BalanceDueText.Text = Money(b.BalanceDue);
            ItemsGrid.ItemsSource = result.Items;
            PaymentsGrid.ItemsSource = result.Payments;

            MarkPaidCard.Visibility = result.CanManageBills && b.Status != "Paid" && b.Status != "Void" ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load this bill: {ex.Message}");
        }
    }

    private async void MarkPaidButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentTypeBox.SelectedItem is not WorkshopOption paymentType)
        {
            ShowError("Choose a payment method.");
            return;
        }
        decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount);

        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.MarkWorkshopBillPaidAsync(new MarkWorkshopBillPaidRequest
            {
                BillId = _billId,
                PaymentTypeId = paymentType.Id,
                Amount = Math.Max(0, amount),
                ReferenceNo = ReferenceBox.Text.Trim(),
            });
            MessageBox.Show(this, result.Message ?? "Payment recorded.", "Done", MessageBoxButton.OK, MessageBoxImage.Information);
            AmountBox.Text = "0";
            ReferenceBox.Text = "";
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
