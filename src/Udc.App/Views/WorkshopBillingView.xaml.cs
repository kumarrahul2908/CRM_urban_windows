using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class WorkshopBillingView : UserControl
{
    private readonly ApiClient _api;
    private string _statusFilter = "";
    private bool _lookupsLoaded;
    private List<WorkshopOption> _paymentTypes = new();

    public WorkshopBillingView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GetWorkshopBillingListAsync(_statusFilter);
            BillsGrid.ItemsSource = result.Bills;
            GenerateCard.Visibility = result.CanManageBills ? Visibility.Visible : Visibility.Collapsed;
            _paymentTypes = result.PaymentTypes;

            if (!_lookupsLoaded)
            {
                _lookupsLoaded = true;
                var stores = new List<WorkshopOption> { new() { Id = -1, Name = "All stores (combined)" } };
                stores.AddRange(result.Stores);
                GenStoreBox.ItemsSource = stores;
                if (stores.Count > 0) GenStoreBox.SelectedIndex = 0;

                GenWorkshopBox.ItemsSource = result.Workshops;
                if (result.Workshops.Count > 0) GenWorkshopBox.SelectedIndex = 0;
            }
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load workshop bills: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private async void StatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return; // fires once during InitializeComponent, before the first load
        var content = (StatusFilterBox.SelectedItem as ComboBoxItem)?.Content as string ?? "All statuses";
        _statusFilter = content == "All statuses" ? "" : content;
        await LoadAsync();
    }

    private void BillsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (BillsGrid.SelectedItem is not WorkshopBillRow bill) return;
        var dialog = new WorkshopBillingDetailWindow(_api, bill.Id, _paymentTypes) { Owner = Window.GetWindow(this) };
        dialog.ShowDialog();
        _ = LoadAsync();
    }

    private async void GenerateButton_Click(object sender, RoutedEventArgs e)
    {
        if (GenStoreBox.SelectedItem is not WorkshopOption store) { ShowError("Select a store."); return; }
        if (GenWorkshopBox.SelectedItem is not WorkshopBillingWorkshopOption workshop) { ShowError("Select a workshop."); return; }
        var from = GenFromDatePicker.SelectedDate?.ToString("yyyy-MM-dd");
        var to = GenToDatePicker.SelectedDate?.ToString("yyyy-MM-dd");
        if (from is null || to is null) { ShowError("Select a From and To date."); return; }

        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GenerateWorkshopBillAsync(new GenerateWorkshopBillRequest
            {
                StoreId = store.Id == -1 ? "all" : store.Id.ToString(),
                WorkshopId = workshop.Id,
                From = from,
                To = to,
                GstMode = (GstModeBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "none",
                Basis = (BasisBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "whole",
            });
            MessageBox.Show(Window.GetWindow(this), $"Bill {result.BillNumber} generated.", "Bill generated", MessageBoxButton.OK, MessageBoxImage.Information);
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
}
