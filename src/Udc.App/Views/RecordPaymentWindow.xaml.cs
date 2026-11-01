using System.Globalization;
using System.Windows;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class RecordPaymentWindow : Window
{
    private readonly ApiClient _api;
    private readonly int _orderId;
    private readonly string _clientRequestId = Guid.NewGuid().ToString("N");

    public RecordPaymentWindow(ApiClient api, int orderId, decimal currentDue)
    {
        InitializeComponent();
        _api = api;
        _orderId = orderId;
        DueText.Text = $"Balance due: ₹{currentDue:N2}";
        AmountBox.Text = currentDue.ToString("0.##", CultureInfo.InvariantCulture);
        Loaded += async (_, _) => await LoadPaymentTypesAsync();
    }

    private async Task LoadPaymentTypesAsync()
    {
        try
        {
            var result = await _api.GetPaymentTypesAsync();
            PaymentTypeBox.ItemsSource = result.PaymentTypes;
            if (result.PaymentTypes.Count > 0) PaymentTypeBox.SelectedIndex = 0;
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load payment methods: {ex.Message}");
        }
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentTypeBox.SelectedItem is not PaymentTypeOption paymentType)
        {
            ShowError("Choose a payment method.");
            return;
        }
        if (!decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            ShowError("Enter a valid amount greater than zero.");
            return;
        }

        SubmitButton.IsEnabled = false;
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            await _api.RecordPaymentAsync(new RecordPaymentRequest
            {
                OrderId = _orderId,
                ClientRequestId = _clientRequestId,
                PaymentLines = new List<PaymentLine> { new() { PaymentTypeId = paymentType.Id, Amount = amount } },
            });
            DialogResult = true;
            Close();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            SubmitButton.IsEnabled = true;
        }
    }

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
