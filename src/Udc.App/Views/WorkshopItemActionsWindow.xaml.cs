using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class WorkshopItemActionsWindow : Window
{
    private readonly ApiClient _api;
    private readonly int _workshopItemId;

    public WorkshopItemActionsWindow(ApiClient api, int workshopItemId, string? garmentName, string? orderNumber, string itemStatus, string? currentStatus)
    {
        InitializeComponent();
        _api = api;
        _workshopItemId = workshopItemId;
        TitleText.Text = garmentName ?? "Workshop item";
        SubtitleText.Text = $"Order {orderNumber} - item status: {itemStatus} - current status: {currentStatus ?? "(none)"}";
    }

    private async void ActionButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: string action }) return;
        var remarks = RemarksBox.Text.Trim();
        if (action == "mark_unprocessed" && remarks.Length == 0)
        {
            ShowStatus(false, "A reason (remarks) is required to mark an item Unprocessed.");
            return;
        }

        try
        {
            var result = await _api.SendWorkshopItemActionAsync(new WorkshopItemActionRequest { Action = action, WorkshopItemId = _workshopItemId, Remarks = remarks });
            ShowStatus(true, result.Message ?? "Done.");
        }
        catch (ApiException ex)
        {
            ShowStatus(false, ex.Message);
        }
    }

    private void ShowStatus(bool ok, string message)
    {
        StatusText.Text = message;
        StatusBanner.Visibility = Visibility.Visible;
        StatusBanner.Background = ok ? (Brush)FindResource("OkBgBrush") : (Brush)FindResource("ErrorBgBrush");
        StatusText.Foreground = ok ? (Brush)FindResource("OkBrush") : (Brush)FindResource("ErrorBrush");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
