using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class WorkshopView : UserControl
{
    private readonly ApiClient _api;
    private string _search = "";
    private string _statusFilter = "";

    public WorkshopView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        var previouslySelectedWorkshopId = (WorkshopBox.SelectedItem as WorkshopOption)?.Id;
        try
        {
            var queue = await _api.GetWorkshopQueueAsync(_search, _statusFilter);

            PendingGrid.ItemsSource = queue.Piece.Items.Where(i => i.ItemStatus == "Pending").ToList();
            InProgressGrid.ItemsSource = queue.Piece.Items.Where(i => i.ItemStatus != "Pending").ToList();
            AcceptGrid.ItemsSource = queue.AcceptQueue;
            ReturnGrid.ItemsSource = queue.ReturnQueue;
            StoreReceiveGrid.ItemsSource = queue.StoreReceiveQueue;

            WorkshopBox.ItemsSource = queue.Workshops;
            var restoreIndex = previouslySelectedWorkshopId is null ? -1 : queue.Workshops.FindIndex(w => w.Id == previouslySelectedWorkshopId);
            WorkshopBox.SelectedIndex = restoreIndex >= 0 ? restoreIndex : (queue.Workshops.Count > 0 ? 0 : -1);
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load the workshop queue: {ex.Message}");
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _search = SearchBox.Text.Trim();
        await LoadAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { _search = SearchBox.Text.Trim(); await LoadAsync(); }
    }

    private async void StatusFilterBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!IsLoaded) return; // fires once during InitializeComponent, before _api is meaningfully usable for a reload
        var content = (StatusFilterBox.SelectedItem as ComboBoxItem)?.Content as string ?? "All statuses";
        _statusFilter = content == "All statuses" ? "" : content;
        await LoadAsync();
    }

    private async void SendToWorkshopButton_Click(object sender, RoutedEventArgs e)
    {
        var selected = PendingGrid.SelectedItems.Cast<WorkshopPieceItem>().ToList();
        if (selected.Count == 0) { ShowError("Select at least one item to send."); return; }
        if (WorkshopBox.SelectedItem is not WorkshopOption workshop) { ShowError("Select a workshop."); return; }

        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.CreateChallanAsync(new CreateChallanRequest
            {
                WorkshopId = workshop.Id,
                ItemIds = selected.Select(i => i.OrderItemId).ToList(),
                Remarks = SendRemarksBox.Text.Trim(),
            });
            MessageBox.Show(Window.GetWindow(this), result.Message ?? "Sent to workshop.", "Sent", MessageBoxButton.OK, MessageBoxImage.Information);
            SendRemarksBox.Text = "";
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError($"Could not send to workshop: {ex.Message}");
        }
    }

    private async void InProgressGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (InProgressGrid.SelectedItem is not WorkshopPieceItem item || item.WorkshopItemId is null) return;
        var dialog = new WorkshopItemActionsWindow(_api, item.WorkshopItemId.Value, item.GarmentName, item.OrderNumber, item.ItemStatus, item.CurrentStatus)
        {
            Owner = Window.GetWindow(this),
        };
        dialog.ShowDialog();
        await LoadAsync();
    }

    private async Task RunQueueActionAsync(DataGrid grid, string action)
    {
        if (grid.SelectedItem is not WorkshopActionQueueItem item) { ShowError("Select a row first."); return; }
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            await _api.SendWorkshopItemActionAsync(new WorkshopItemActionRequest { Action = action, WorkshopItemId = item.WorkshopItemId });
            await LoadAsync();
        }
        catch (ApiException ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void AcceptButton_Click(object sender, RoutedEventArgs e) => await RunQueueActionAsync(AcceptGrid, "accept");
    private async void ReturnButton_Click(object sender, RoutedEventArgs e) => await RunQueueActionAsync(ReturnGrid, "return_to_store");
    private async void StoreReceiveButton_Click(object sender, RoutedEventArgs e) => await RunQueueActionAsync(StoreReceiveGrid, "store_receive");
    private async void ForceReceiveButton_Click(object sender, RoutedEventArgs e) => await RunQueueActionAsync(StoreReceiveGrid, "force_receive");

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }
}
