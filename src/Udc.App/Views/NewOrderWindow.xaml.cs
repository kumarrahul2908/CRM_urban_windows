using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class NewOrderWindow : Window
{
    private sealed class CartLine
    {
        public int GarmentId { get; init; }
        public string GarmentName { get; init; } = "";
        public int ServiceId { get; init; }
        public string ServiceName { get; init; } = "";
        public decimal Quantity { get; init; }
        public decimal Rate { get; init; }
        public decimal Amount => Rate * Quantity;
        public int? BrandId { get; init; }
        public string? BrandName { get; init; }
        public int? ColorId { get; init; }
        public string? ColorName { get; init; }
        public string Remarks { get; init; } = "";
        public bool ManualOverride { get; init; }
    }

    private sealed class PaymentLineDraft
    {
        public int PaymentTypeId { get; init; }
        public string TypeName { get; init; } = "";
        public decimal Amount { get; init; }
    }

    private readonly ApiClient _api;
    private readonly string _clientRequestId = Guid.NewGuid().ToString("N");
    private readonly ObservableCollection<CartLine> _cart = new();
    private readonly ObservableCollection<PaymentLineDraft> _paymentLines = new();

    private CustomerSummary? _selectedCustomer;
    private List<CustomerSummary> _lastCustomerSearchResults = new();
    private ProductsResult? _products;
    private List<StoreOption> _stores = new();
    private bool _storeSelectionRequired;
    private decimal _walletBalance;

    public NewOrderWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        CartGrid.ItemsSource = _cart;
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

            var storesResult = await _api.GetAvailableStoresAsync();
            _stores = storesResult.Stores;
            _storeSelectionRequired = !storesResult.IsFixed;
            if (_storeSelectionRequired)
            {
                StoreCard.Visibility = Visibility.Visible;
                StoreBox.ItemsSource = _stores;
                if (_stores.Count > 0) StoreBox.SelectedIndex = 0;
            }

            var brandItems = new List<BrandOption> { new() { Id = 0, BrandName = "(none)" } };
            brandItems.AddRange(await _api.GetBrandsAsync());
            BrandBox.ItemsSource = brandItems;
            BrandBox.SelectedIndex = 0;

            var colorItems = new List<ColorOption> { new() { Id = 0, ColorName = "(none)" } };
            colorItems.AddRange(await _api.GetColorsAsync());
            ColorBox.ItemsSource = colorItems;
            ColorBox.SelectedIndex = 0;

            var paymentTypes = await _api.GetPaymentTypesAsync();
            PaymentTypeBox.ItemsSource = paymentTypes.PaymentTypes;
            if (paymentTypes.PaymentTypes.Count > 0) PaymentTypeBox.SelectedIndex = 0;
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

            var products = await _api.GetProductsAsync(customer.Id);
            _products = products;
            GarmentsGrid.ItemsSource = products.Garments;
            ItemsCard.IsEnabled = true;
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load this customer: {ex.Message}");
        }
    }

    private void ChangeCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        if (_cart.Count > 0)
        {
            var choice = MessageBox.Show("Changing the customer clears the cart (rates are specific to their price list). Continue?",
                "Change customer", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (choice != MessageBoxResult.Yes) return;
        }
        _cart.Clear();
        _paymentLines.Clear();
        WalletAmountBox.Text = "0";
        RecomputeEstimate();
        RecomputePaymentSummary();
        _selectedCustomer = null;
        _products = null;
        _walletBalance = 0;
        ItemsCard.IsEnabled = false;
        GarmentsGrid.ItemsSource = null;
        ServiceBox.ItemsSource = null;
        WalletBalanceText.Text = "";
        SelectedCustomerCard.Visibility = Visibility.Collapsed;
        CustomerSearchPanel.Visibility = Visibility.Visible;
        CustomerSearchBox.Text = "";
    }

    // ---- add items ----------------------------------------------------------------------------

    private void GarmentSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_products is null) return;
        var search = GarmentSearchBox.Text.Trim();
        GarmentsGrid.ItemsSource = search.Length == 0
            ? _products.Garments
            : _products.Garments.Where(g =>
                g.Name.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (g.Sku?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
    }

    private void GarmentsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (GarmentsGrid.SelectedItem is not ProductGarment garment)
        {
            ServiceBox.ItemsSource = null;
            return;
        }
        var pieceRates = garment.Rates.Where(r => r.BillingUnit == "piece" && r.PieceRate is not null).ToList();
        ServiceBox.ItemsSource = pieceRates;
        var defaultIndex = pieceRates.FindIndex(r => r.ServiceId == garment.DefaultServiceId);
        ServiceBox.SelectedIndex = pieceRates.Count == 0 ? -1 : Math.Max(0, defaultIndex);
    }

    private void ManualOverrideBox_CheckedChanged(object sender, RoutedEventArgs e)
    {
        OverrideRatePanel.IsEnabled = ManualOverrideBox.IsChecked == true;
    }

    private void AddToCartButton_Click(object sender, RoutedEventArgs e)
    {
        if (GarmentsGrid.SelectedItem is not ProductGarment garment)
        {
            ShowError("Select a garment first.");
            return;
        }
        if (ServiceBox.SelectedItem is not ProductRate rate)
        {
            ShowError("This garment has no priced service in the customer's price list.");
            return;
        }
        if (!decimal.TryParse(QuantityBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var qty) || qty <= 0)
        {
            ShowError("Enter a quantity greater than zero.");
            return;
        }

        var manualOverride = ManualOverrideBox.IsChecked == true;
        var effectiveRate = rate.PieceRate ?? 0;
        if (manualOverride)
        {
            if (!decimal.TryParse(OverrideRateBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out effectiveRate) || effectiveRate < 0)
            {
                ShowError("Enter a non-negative override rate.");
                return;
            }
        }

        var brand = BrandBox.SelectedItem as BrandOption;
        var color = ColorBox.SelectedItem as ColorOption;

        ErrorBanner.Visibility = Visibility.Collapsed;
        _cart.Add(new CartLine
        {
            GarmentId = garment.Id,
            GarmentName = garment.Name,
            ServiceId = rate.ServiceId,
            ServiceName = rate.ServiceName,
            Quantity = qty,
            Rate = effectiveRate,
            BrandId = brand is { Id: > 0 } ? brand.Id : null,
            BrandName = brand is { Id: > 0 } ? brand.BrandName : null,
            ColorId = color is { Id: > 0 } ? color.Id : null,
            ColorName = color is { Id: > 0 } ? color.ColorName : null,
            Remarks = ItemRemarksBox.Text.Trim(),
            ManualOverride = manualOverride,
        });
        QuantityBox.Text = "1";
        ItemRemarksBox.Text = "";
        ManualOverrideBox.IsChecked = false;
        OverrideRateBox.Text = "";
        RecomputeEstimate();
    }

    private void RemoveCartLineButton_Click(object sender, RoutedEventArgs e)
    {
        if (CartGrid.SelectedItem is CartLine line)
        {
            _cart.Remove(line);
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
        var subtotal = _cart.Sum(c => c.Amount);
        var discountType = (DiscountTypeBox.SelectedItem as ComboBoxItem)?.Content as string ?? "Percent";
        decimal.TryParse(DiscountValueBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var discountValue);
        var discountAmount = discountType == "Percent" ? subtotal * discountValue / 100m : discountValue;
        discountAmount = Math.Max(0, Math.Min(discountAmount, subtotal));
        var deliveryCharge = 0m;
        if (HomeDeliveryBox.IsChecked == true) decimal.TryParse(DeliveryChargeBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out deliveryCharge);

        var estimate = Math.Max(0, subtotal - discountAmount) + deliveryCharge;
        EstimateText.Text = $"{_cart.Count} item(s) - Subtotal {Money(subtotal)} - estimate excludes tax/urgent charge";
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
        if (_cart.Count == 0) { ShowError("Add at least one item to the cart."); return; }
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

        var request = new CreateOrderRequest
        {
            ClientRequestId = _clientRequestId,
            CustomerId = _selectedCustomer.Id,
            StoreId = _storeSelectionRequired ? (StoreBox.SelectedItem as StoreOption)?.Id : null,
            Items = _cart.Select(c => new CreateOrderItemLine
            {
                GarmentId = c.GarmentId,
                ServiceId = c.ServiceId,
                Quantity = c.Quantity,
                BrandId = c.BrandId,
                ColorId = c.ColorId,
                Remarks = c.Remarks,
                ManualOverride = c.ManualOverride,
                RateOverride = c.ManualOverride ? c.Rate : null,
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
            var result = await _api.CreateOrderAsync(request);
            var warningText = result.Warnings.Count > 0 ? "\n\n" + string.Join("\n", result.Warnings) : "";
            MessageBox.Show(this, $"Order {result.OrderNumber} created - total {Money(result.GrandTotal)}.{warningText}",
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
