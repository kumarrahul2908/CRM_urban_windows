using System.Linq;
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
    private readonly string _itemStatus;
    private readonly string? _currentStatus;
    private readonly string? _workshopStatus;
    private readonly bool _isLaundry;

    /// <summary>
    /// <paramref name="workshopStatus"/> is the RAW <c>workshop_items.status</c> column
    /// (Sent/Processing/Completed/...) - available for piece items (see
    /// <c>WorkshopPieceItem.WorkshopStatus</c>) but not exposed by <c>workshop_queue</c> for Laundry
    /// items at all, so pass null there (every gate that needs it then fails open - enabled rather
    /// than guessed-disabled). <paramref name="currentStatus"/> is the history-derived status
    /// (<c>WorkshopService.getCurrentStatus()</c>) both kinds expose. <paramref name="isLaundry"/>
    /// unconditionally disables the three QC/Ironing/Packaging actions, which always error for a
    /// Laundry item server-side regardless of state (see workshopService.ts's
    /// itemStatusSubStage()).
    /// </summary>
    public WorkshopItemActionsWindow(ApiClient api, int workshopItemId, string? garmentName, string? orderNumber, string itemStatus, string? currentStatus, string? workshopStatus, bool isLaundry)
    {
        InitializeComponent();
        _api = api;
        _workshopItemId = workshopItemId;
        _itemStatus = itemStatus;
        _currentStatus = currentStatus;
        _workshopStatus = workshopStatus;
        _isLaundry = isLaundry;
        TitleText.Text = garmentName ?? "Workshop item";
        SubtitleText.Text = $"Order {orderNumber} - item status: {itemStatus} - current status: {currentStatus ?? "(none)"}";
        UpdateButtonStates();
    }

    /// <summary>
    /// Mirrors workshopService.ts's own from-state checks as closely as the data available to this
    /// app allows - see this class's constructor doc comment for what isn't available for Laundry
    /// items. One deliberate judgment call: acceptItem() literally only allows a transition from
    /// history status "Received" (historyOnly(['Received'], 'Accepted', ...)), but nothing in this
    /// codebase (checked workshopService.ts exhaustively) ever writes exactly that value - every
    /// other place that cares about "ready to accept" (web/pages/workshop.ts's Incoming bucket,
    /// barcodeTags.ts's own Accept-button condition) treats Sent/Workshop/Received as the same
    /// bucket. Accept is enabled here for all three, matching that broader precedent rather than
    /// the single narrowest literal check - if that's ever wrong for a specific item, the server's
    /// own error message is still the final word, same as always.
    /// </summary>
    private void UpdateButtonStates()
    {
        bool WorkshopStatusIs(params string[] values) => _workshopStatus is not null && values.Contains(_workshopStatus);
        bool CurrentStatusIs(params string[] values) => _currentStatus is not null && values.Contains(_currentStatus);

        StartProcessingButton.IsEnabled = _workshopStatus is null || WorkshopStatusIs("Sent", "Received");

        if (_isLaundry)
        {
            CompleteWashingButton.IsEnabled = false;
            PassQcButton.IsEnabled = false;
            CompleteIroningButton.IsEnabled = false;
            CompleteProcessingButton.IsEnabled = _workshopStatus is null || WorkshopStatusIs("Processing");
        }
        else
        {
            CompleteWashingButton.IsEnabled = WorkshopStatusIs("Processing") && _itemStatus == "Processing";
            PassQcButton.IsEnabled = WorkshopStatusIs("Processing") && _itemStatus == "QC";
            CompleteIroningButton.IsEnabled = WorkshopStatusIs("Processing") && _itemStatus == "Ready";
            CompleteProcessingButton.IsEnabled = WorkshopStatusIs("Processing") && _itemStatus == "Packed";
        }

        AcceptButton.IsEnabled = CurrentStatusIs("Received", "Sent", "Workshop");
        ReturnToStoreButton.IsEnabled = CurrentStatusIs("Completed");
        StoreReceiveButton.IsEnabled = CurrentStatusIs("Returned");
        ForceReceiveButton.IsEnabled = _currentStatus is not null && !CurrentStatusIs("Store Received", "Unprocessed", "Rejected");
        MarkUnprocessedButton.IsEnabled = _currentStatus is null || !CurrentStatusIs("Rejected", "Unprocessed");
        ResolveUnprocessedButton.IsEnabled = CurrentStatusIs("Unprocessed");
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
            ShowStatus(true, $"{result.Message ?? "Done."} Close this window and reopen it from the refreshed list to see which actions apply next.");
            DisableAllActionButtons();
        }
        catch (ApiException ex)
        {
            ShowStatus(false, ex.Message);
        }
    }

    /// <summary>After any successful action, this item's state has changed server-side in a way
    /// this already-open window has no way to re-fetch and recompute against - leaving stale
    /// buttons enabled could invite a second, now-invalid action. Disabling everything until the
    /// window is reopened from a freshly-reloaded queue is the safe choice.</summary>
    private void DisableAllActionButtons()
    {
        foreach (var button in new[]
                 {
                     StartProcessingButton, CompleteWashingButton, PassQcButton, CompleteIroningButton, CompleteProcessingButton,
                     AcceptButton, ReturnToStoreButton, StoreReceiveButton, ForceReceiveButton, MarkUnprocessedButton, ResolveUnprocessedButton,
                 })
        {
            button.IsEnabled = false;
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
