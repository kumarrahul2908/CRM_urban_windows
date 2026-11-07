using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class NewLaundryOrderWindow : Window
{
    private sealed class BatchGarmentLine
    {
        public int GarmentId { get; init; }
        public string GarmentName { get; init; } = "";
        public int Quantity { get; init; }
    }

    private sealed class BatchLine
    {
        public int ServiceId { get; init; }
        public string ServiceName { get; init; } = "";
        public decimal Weight { get; init; }
        public decimal Rate { get; init; }
        public List<BatchGarmentLine> Garments { get; init; } = new();
        public int GarmentCount => Garments.Sum(g => g.Quantity);
        public decimal Amount => Rate * Weight;
    }

    private sealed class PaymentLineDraft
    {
        public int PaymentTypeId { get; init; }
        public string TypeName { get; init; } = "";
        public decimal Amount { get; init; }
    }

    private readonly ApiClient _api;
    private readonly string _clientRequestId = Guid.NewGuid().ToString("N");
    private readonly ObservableCollection<BatchLine> _batches = new();
    private readonly ObservableCollection<BatchGarmentLine> _draftGarments = new();
    private readonly ObservableCollection<PaymentLineDraft> _paymentLines = new();

    private CustomerSummary? _selectedCustomer;
    private List<CustomerSummary> _lastCustomerSearchResults = new();
    private List<StoreOption> _stores = new();
    private bool _storeSelectionRequired;
    private decimal _walletBalance;

    public NewLaundryOrderWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        BatchesGrid.ItemsSource = _batches;
        BatchGarmentsGrid.ItemsSource = _draftGarments;
        PaymentLinesGrid.ItemsSource = _paymentLines;
        Loaded += async (_, _) => await LoadLookupsAsync();
    }

    private async Task LoadLookupsAsync()
    {
        try
        {
            var levels = await _api.GetUrgentLevelsAsync();
            var levelItems = new List<UrgentLevel> { new() { Id = 0, LevelName = "(none)" } };
            levelItems.AddRange(levels);
            UrgentLevelBox.ItemsSource = levelItems;
            UrgentLevelBox.SelectedIndex = 0;

            var paymentTypes = await _api.GetPaymentTypesAsync();
            PaymentTypeBox.ItemsSource = paymentTypes.PaymentTypes;
            if (paymentTypes.PaymentTypes.Count > 0) PaymentTypeBox.SelectedIndex = 0;

            // laundry_orders_create now accepts a store_id for floating-store accounts the same
            // way pos_orders_create already did (see resolveAuthorizedStoreId()'s 3-arg call in
            // orders.ts) - mirrors NewOrderWindow's own store picker exactly.
            var storesResult = await _api.GetAvailableStoresAsync();
            _stores = storesResult.Stores;
            _storeSelectionRequired = !storesResult.IsFixed;
            if (_storeSelectionRequired)
            {
                StoreCard.Visibility = Visibility.Visible;
                StoreBox.ItemsSource = _stores;
                if (_stores.Count > 0) StoreBox.SelectedIndex = 0;
            }
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load order setup data: {ex.Message}");
        }
    }

    // ---- customer selection -----------------------------------------------------------------

    private async void CustomerSearchButton_Click(object sender, RoutedEventArgs e) => await SearchCustomersAsync();
    private async void CustomerSearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) await SearchCustomersAsync();
    }

    private async Task SearchCustomersAsync()
    {
        var search = CustomerSearchBox.Text.Trim();
        if (search.Length == 0) return;
        try
        {
            var result = await _api.GetCustomersAsync(search, 1);
            _lastCustomerSearchResults = result.Results;
            CustomerResultsList.ItemsSource = _lastCustomerSearchResults
                .Select(c => $"{c.FullName} - {c.Mobile} ({c.CustomerCode})").ToList();
            CustomerResultsList.Visibility = _lastCustomerSearchResults.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        catch (ApiException ex)
        {
            ShowError($"Could not search customers: {ex.Message}");
        }
    }

    private async void CustomerResultsList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var index = CustomerResultsList.SelectedIndex;
        if (index < 0 || index >= _lastCustomerSearchResults.Count) return;
        await SelectCustomerAsync(_lastCustomerSearchResults[index]);
    }

    private async Task SelectCustomerAsync(CustomerSummary customer)
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var summary = await _api.GetPosCustomerSummaryAsync(customer.Id);
            var products = await _api.GetProductsAsync(customer.Id);
            _selectedCustomer = customer;
            _walletBalance = summary.WalletBalance;

            SelectedCustomerText.Text = $"{summary.Customer.Name} ({customer.CustomerCode})";
            SelectedCustomerSubText.Text = $"{summary.Customer.Mobile} - {summary.PriceListName ?? "Default"} price list - Outstanding {Money(summary.Outstanding)}";
            WalletBalanceText.Text = $"Wallet balance: {Money(summary.WalletBalance)}";
            SelectedCustomerCard.Visibility = Visibility.Visible;
            CustomerSearchPanel.Visibility = Visibility.Collapsed;
            CustomerResultsList.Visibility = Visibility.Collapsed;

            HomeDeliveryBox.IsChecked = summary.Customer.HomeDelivery;
            if (summary.Customer.DiscountPercent > 0) DiscountValueBox.Text = summary.Customer.DiscountPercent.ToString(CultureInfo.InvariantCulture);

            ServiceBox.ItemsSource = products.UniversalServices;
            GarmentBox.ItemsSource = products.Garments;
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load this customer: {ex.Message}");
        }
    }

    private void ChangeCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_batches.Count > 0)
        {
            var choice = MessageBox.Show("Changing the customer clears the batches already added (rates are specific to their price list). Continue?",
                "Change customer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (choice != MessageBoxResult.Yes) return;
        }
        _batches.Clear();
        _draftGarments.Clear();
        _paymentLines.Clear();
        WalletAmountBox.Text = "0";
        RecomputeEstimate();
        RecomputePaymentSummary();
        _selectedCustomer = null;
        _walletBalance = 0;
        ServiceBox.ItemsSource = null;
        GarmentBox.ItemsSource = null;
        RateText.Text = "-";
        WalletBalanceText.Text = "";
        SelectedCustomerCard.Visibility = Visibility.Collapsed;
        CustomerSearchPanel.Visibility = Visibility.Visible;
        CustomerSearchBox.Text = "";
    }

    // ---- batches ----------------------------------------------------------------------------

    private void ServiceBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        RateText.Text = ServiceBox.SelectedItem is ProductRate { WeightRate: not null } rate ? Money(rate.WeightRate.Value) : "-";
    }

    private void AddGarmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (GarmentBox.SelectedItem is not ProductGarment garment)
        {
            ShowError("Select a garment to tag first.");
            return;
        }
        if (!int.TryParse(GarmentQuantityBox.Text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
        {
            ShowError("Enter a garment quantity greater than zero.");
            return;
        }
        ErrorBanner.Visibility = Visibility.Collapsed;
        _draftGarments.Add(new BatchGarmentLine { GarmentId = garment.Id, GarmentName = garment.Name, Quantity = qty });
        GarmentQuantityBox.Text = "1";
    }

    private void RemoveGarmentButton_Click(object sender, RoutedEventArgs e)
    {
        if (BatchGarmentsGrid.SelectedItem is BatchGarmentLine line) _draftGarments.Remove(line);
    }

    private void AddBatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (ServiceBox.SelectedItem is not ProductRate rate || rate.WeightRate is null)
        {
            ShowError("Select a weight-priced service with a configured rate for this customer's price list.");
            return;
        }
        if (!decimal.TryParse(WeightBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var weight) || weight <= 0)
        {
            ShowError("Enter a weight (kg) greater than zero.");
            return;
        }
        if (_draftGarments.Count == 0)
        {
            ShowError("Tag at least one garment for this batch.");
            return;
        }

        ErrorBanner.Visibility = Visibility.Collapsed;
        _batches.Add(new BatchLine
        {
            ServiceId = rate.ServiceId,
            ServiceName = rate.ServiceName,
            Weight = weight,
            Rate = rate.WeightRate.Value,
            Garments = _draftGarments.ToList(),
        });
        _draftGarments.Clear();
        WeightBox.Text = "";
        RecomputeEstimate();
    }

    private void RemoveBatchButton_Click(object sender, RoutedEventArgs e)
    {
        if (BatchesGrid.SelectedItem is BatchLine line)
        {
            _batches.Remove(line);
            RecomputeEstimate();
        }
    }

    // ---- order details / estimate --------------------------------------------------------------

    private void HomeDeliveryBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        DeliveryChargePanel.Visibility = HomeDeliveryBox.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        RecomputeEstimate();
    }

    private void DiscountValueBox_TextChanged(object sender, TextChangedEventArgs e) => RecomputeEstimate();

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private void RecomputeEstimate()
    {
        var subtotal = _batches.Sum(b => b.Amount);
        var discountType = (DiscountTypeBox.SelectedItem as ComboBoxItem)?.Content as string ?? "Percent";
        decimal.TryParse(DiscountValueBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var discountValue);
        var discountAmount = discountType == "Percent" ? subtotal * discountValue / 100m : discountValue;
        discountAmount = Math.Max(0, Math.Min(discountAmount, subtotal));
        var deliveryCharge = 0m;
        if (HomeDeliveryBox.IsChecked == true) decimal.TryParse(DeliveryChargeBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out deliveryCharge);

        var estimate = Math.Max(0, subtotal - discountAmount) + deliveryCharge;
        EstimateText.Text = $"{_batches.Count} batch(es) - Subtotal {Money(subtotal)} - estimate excludes tax/urgent charge";
        EstimateTotalText.Text = $"≈ {Money(estimate)}";
    }

    // ---- payment (optional) ----------------------------------------------------------------------

    private void AddPaymentLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentTypeBox.SelectedItem is not PaymentTypeOption paymentType)
        {
            ShowError("Choose a payment method.");
            return;
        }
        if (!decimal.TryParse(PaymentAmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var amount) || amount <= 0)
        {
            ShowError("Enter a valid payment amount greater than zero.");
            return;
        }
        ErrorBanner.Visibility = Visibility.Collapsed;
        _paymentLines.Add(new PaymentLineDraft { PaymentTypeId = paymentType.Id, TypeName = paymentType.TypeName, Amount = amount });
        PaymentAmountBox.Text = "";
        RecomputePaymentSummary();
    }

    private void RemovePaymentLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (PaymentLinesGrid.SelectedItem is PaymentLineDraft line)
        {
            _paymentLines.Remove(line);
            RecomputePaymentSummary();
        }
    }

    private void WalletAmountBox_TextChanged(object sender, TextChangedEventArgs e) => RecomputePaymentSummary();

    private void RecomputePaymentSummary()
    {
        var paid = _paymentLines.Sum(l => l.Amount);
        decimal.TryParse(WalletAmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var walletAmount);
        var total = paid + Math.Max(0, walletAmount);
        PaymentSummaryText.Text = total > 0
            ? $"Collecting {Money(total)} now ({_paymentLines.Count} line(s) + wallet) - the real due balance is confirmed once the order is created."
            : "";
    }

    // ---- submit ---------------------------------------------------------------------------------

    private async void CreateOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomer is null) { ShowError("Select a customer first."); return; }
        if (_batches.Count == 0) { ShowError("Add at least one batch to the order."); return; }
        if (_storeSelectionRequired && StoreBox.SelectedItem is not StoreOption) { ShowError("Select a store."); return; }

        decimal.TryParse(WalletAmountBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var walletAmount);
        walletAmount = Math.Max(0, walletAmount);
        if (walletAmount > _walletBalance + 0.01m)
        {
            ShowError($"Wallet amount cannot exceed the customer's available balance of {Money(_walletBalance)}.");
            return;
        }

        var urgentLevel = UrgentLevelBox.SelectedItem as UrgentLevel;
        var discountType = (DiscountTypeBox.SelectedItem as ComboBoxItem)?.Content as string ?? "Percent";
        decimal.TryParse(DiscountValueBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var discountValue);
        var homeDelivery = HomeDeliveryBox.IsChecked == true;
        decimal deliveryCharge = 0;
        if (homeDelivery) decimal.TryParse(DeliveryChargeBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out deliveryCharge);

        var request = new CreateLaundryOrderRequest
        {
            ClientRequestId = _clientRequestId,
            CustomerId = _selectedCustomer.Id,
            StoreId = _storeSelectionRequired ? (StoreBox.SelectedItem as StoreOption)?.Id : null,
            Batches = _batches.Select(b => new CreateLaundryBatchLine
            {
                ServiceId = b.ServiceId,
                Weight = b.Weight,
                Rate = b.Rate,
                GarmentCount = b.GarmentCount,
                Garments = b.Garments.Select(g => new LaundryBatchGarmentLine { GarmentId = g.GarmentId, Quantity = g.Quantity }).ToList(),
            }).ToList(),
            UrgentLevelId = urgentLevel is { Id: > 0 } ? urgentLevel.Id : null,
            DiscountType = discountType,
            DiscountValue = discountValue,
            HomeDelivery = homeDelivery,
            DeliveryCharge = deliveryCharge,
            Notes = NotesBox.Text.Trim(),
            DueDate = DueDatePicker.SelectedDate?.ToString("yyyy-MM-dd"),
            Payments = _paymentLines.Select(l => new PaymentLine { PaymentTypeId = l.PaymentTypeId, Amount = l.Amount }).ToList(),
            WalletAmount = walletAmount,
        };

        CreateOrderButton.IsEnabled = false;
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.CreateLaundryOrderAsync(request);
            var warningText = result.Warnings.Count > 0 ? "\n\n" + string.Join("\n", result.Warnings) : "";
            MessageBox.Show(this, $"Laundry order {result.OrderNumber} created - total {Money(result.GrandTotal)}.{warningText}",
                "Order created", MessageBoxButton.OK, MessageBoxImage.Information);
            DialogResult = true;
            Close();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            CreateOrderButton.IsEnabled = true;
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
