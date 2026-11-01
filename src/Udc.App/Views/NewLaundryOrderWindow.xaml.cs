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

    private readonly ApiClient _api;
    private readonly string _clientRequestId = Guid.NewGuid().ToString("N");
    private readonly ObservableCollection<BatchLine> _batches = new();
    private readonly ObservableCollection<BatchGarmentLine> _draftGarments = new();

    private CustomerSummary? _selectedCustomer;
    private List<CustomerSummary> _lastCustomerSearchResults = new();

    public NewLaundryOrderWindow(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        BatchesGrid.ItemsSource = _batches;
        BatchGarmentsGrid.ItemsSource = _draftGarments;
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

            // laundry_orders_create never reads a store_id from the payload - it always resolves
            // the signed-in user's own fixed store (see OrderService.createLaundryOrder() /
            // resolveAuthorizedStoreId()'s 2-arg call in orders.ts). A floating-store account has
            // no way to satisfy it from this endpoint at all, so block the form outright rather
            // than let the user fill it in and hit a guaranteed "select an operating store" error.
            var storesResult = await _api.GetAvailableStoresAsync();
            if (!storesResult.IsFixed)
            {
                NoStoreBanner.Visibility = Visibility.Visible;
                CustomerCard.IsEnabled = false;
                BatchCard.IsEnabled = false;
                DetailsCard.IsEnabled = false;
                CreateOrderButton.IsEnabled = false;
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

            SelectedCustomerText.Text = $"{summary.Customer.Name} ({customer.CustomerCode})";
            SelectedCustomerSubText.Text = $"{summary.Customer.Mobile} - {summary.PriceListName ?? "Default"} price list - Outstanding {Money(summary.Outstanding)}";
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
        RecomputeEstimate();
        _selectedCustomer = null;
        ServiceBox.ItemsSource = null;
        GarmentBox.ItemsSource = null;
        RateText.Text = "-";
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

    // ---- submit ---------------------------------------------------------------------------------

    private async void CreateOrderButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedCustomer is null) { ShowError("Select a customer first."); return; }
        if (_batches.Count == 0) { ShowError("Add at least one batch to the order."); return; }

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
