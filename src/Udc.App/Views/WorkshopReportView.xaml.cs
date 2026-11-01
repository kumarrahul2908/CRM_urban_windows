using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class WorkshopReportView : UserControl
{
    private readonly ApiClient _api;
    private bool _storesLoaded;

    public WorkshopReportView(ApiClient api)
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
            var period = (PeriodBox.SelectedItem as ComboBoxItem)?.Tag as string ?? "week";
            var storeId = (StoreFilterBox.SelectedItem as WorkshopOption) is { Id: > 0 } selected ? selected.Id : (int?)null;
            var from = FromDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
            var to = ToDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";

            var result = await _api.GetWorkshopReportAsync(period, storeId, from, to);
            RangeText.Text = $"{result.From} to {result.To} - {result.Report.Count} store(s)";
            StoresGrid.ItemsSource = result.Report;
            GarmentsGrid.ItemsSource = null;
            CustomersList.ItemsSource = null;

            if (!_storesLoaded)
            {
                _storesLoaded = true;
                var stores = new List<WorkshopOption> { new() { Id = 0, Name = "(all stores)" } };
                stores.AddRange(result.Stores);
                StoreFilterBox.ItemsSource = stores;
                StoreFilterBox.SelectedIndex = 0;
            }
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load the workshop report: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private void PeriodBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var isCustom = (PeriodBox.SelectedItem as ComboBoxItem)?.Tag as string == "custom";
        FromDatePicker.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
        ToDatePicker.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void ApplyButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();

    private void StoresGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StoresGrid.SelectedItem is not WorkshopReportStoreRow row)
        {
            GarmentsGrid.ItemsSource = null;
            CustomersList.ItemsSource = null;
            return;
        }
        GarmentsGrid.ItemsSource = row.Garments;
        CustomersList.ItemsSource = row.Customers;
    }
}
