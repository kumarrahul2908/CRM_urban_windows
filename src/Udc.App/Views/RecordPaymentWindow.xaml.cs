using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class RecordPaymentWindow : Window
{
    private sealed class PaymentLineDraft
    {
        public int PaymentTypeId { get; init; }
        public string TypeName { get; init; } = "";
        public decimal Amount { get; init; }
    }

    private readonly ApiClient _api;
    private readonly int _orderId;
    private readonly string _clientRequestId = Guid.NewGuid().ToString("N");
    private readonly ObservableCollection<PaymentLineDraft> _lines = new();
    private readonly decimal _currentDue;

    public RecordPaymentWindow(ApiClient api, int orderId, decimal currentDue)
    {
        InitializeComponent();
        _api = api;
        _orderId = orderId;
        _currentDue = currentDue;
        PaymentLinesGrid.ItemsSource = _lines;
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

    private bool TryReadLineInput(out PaymentLineDraft line)
    {
        line = null!;
        if (PaymentTypeBox.SelectedItem is not PaymentTypeOption paymentType)
        {
            ShowError("Choose a payment method.");
            return false;
        }
        if (!decimal.TryParse(AmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            ShowError("Enter a valid amount greater than zero.");
            return false;
        }
        line = new PaymentLineDraft { PaymentTypeId = paymentType.Id, TypeName = paymentType.TypeName, Amount = amount };
        return true;
    }

    private void AddLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (!TryReadLineInput(out var line)) return;
        ErrorBanner.Visibility = Visibility.Collapsed;
        _lines.Add(line);
        var remaining = Math.Max(0, _currentDue - _lines.Sum(l => l.Amount));
        AmountBox.Text = remaining.ToString("0.##", CultureInfo.InvariantCulture);
        RecomputeRemaining();
    }

    private void RemoveLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentLinesGrid.SelectedItem is PaymentLineDraft line)
        {
            _lines.Remove(line);
            RecomputeRemaining();
        }
    }

    private void RecomputeRemaining()
    {
        if (_lines.Count == 0) { RemainingText.Text = ""; return; }
        var total = _lines.Sum(l => l.Amount);
        RemainingText.Text = $"{_lines.Count} line(s) added - total ₹{total:N2} - remaining due ₹{Math.Max(0, _currentDue - total):N2}";
    }

    private async void SubmitButton_Click(object sender, RoutedEventArgs e)
    {
        List<PaymentLine> lines;
        if (_lines.Count > 0)
        {
            // one or more lines already added via Add Line - submit exactly those, ignoring
            // whatever is currently sitting unsubmitted in the method/amount boxes.
            lines = _lines.Select(l => new PaymentLine { PaymentTypeId = l.PaymentTypeId, Amount = l.Amount }).ToList();
        }
        else
        {
            // the common case: fill the method/amount boxes once and submit directly.
            if (!TryReadLineInput(out var line)) return;
            lines = new List<PaymentLine> { new() { PaymentTypeId = line.PaymentTypeId, Amount = line.Amount } };
        }

        SubmitButton.IsEnabled = false;
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            await _api.RecordPaymentAsync(new RecordPaymentRequest
            {
                OrderId = _orderId,
                ClientRequestId = _clientRequestId,
                PaymentLines = lines,
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
