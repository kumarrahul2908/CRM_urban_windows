using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class WorkshopScheduleView : UserControl
{
    private sealed class ScheduleDisplayRow
    {
        public string StoreName { get; init; } = "";
        public string? OrderNumber { get; init; }
        public string? Customer { get; init; }
        public string? DueTime { get; init; }
        public int Garments { get; init; }
        public decimal BillValue { get; init; }
        public string Statuses { get; init; } = "";
    }

    private sealed class OverdueDisplayRow
    {
        public string StoreName { get; init; } = "";
        public string? OrderNumber { get; init; }
        public string? Customer { get; init; }
        public string? Garment { get; init; }
        public int Qty { get; init; }
        public string? DueDate { get; init; }
        public int Days { get; init; }
        public string? Status { get; init; }
    }

    private readonly ApiClient _api;

    public WorkshopScheduleView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private static string Summary(ScheduleTotals t) => $"{t.Orders} order(s) - {t.Garments} garment(s) - {Money(t.BillValue)}";

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GetWorkshopScheduleAsync();

            TodayHeaderText.Text = $"DUE TODAY ({result.TodayDate})";
            TomorrowHeaderText.Text = $"DUE TOMORROW ({result.TomorrowDate})";
            TodaySummaryText.Text = Summary(result.TodayTotals);
            TomorrowSummaryText.Text = Summary(result.TomorrowTotals);
            OverdueSummaryText.Text = Summary(result.OverdueTotals);

            TodayGrid.ItemsSource = result.Today.SelectMany(block => block.Orders.Select(o => new ScheduleDisplayRow
            {
                StoreName = block.StoreName ?? "",
                OrderNumber = o.OrderNumber,
                Customer = o.Customer,
                DueTime = o.DueTime,
                Garments = o.Garments,
                BillValue = o.BillValue,
                Statuses = string.Join(", ", o.Statuses),
            })).ToList();

            TomorrowGrid.ItemsSource = result.Tomorrow.SelectMany(block => block.Orders.Select(o => new ScheduleDisplayRow
            {
                StoreName = block.StoreName ?? "",
                OrderNumber = o.OrderNumber,
                Customer = o.Customer,
                DueTime = o.DueTime,
                Garments = o.Garments,
                BillValue = o.BillValue,
                Statuses = string.Join(", ", o.Statuses),
            })).ToList();

            OverdueGrid.ItemsSource = result.Overdue.SelectMany(block => block.Rows.Select(r => new OverdueDisplayRow
            {
                StoreName = block.StoreName ?? "",
                OrderNumber = r.OrderNumber,
                Customer = r.Customer,
                Garment = r.Garment,
                Qty = r.Qty,
                DueDate = r.DueDate,
                Days = r.Days,
                Status = r.Status,
            })).ToList();
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load the workshop schedule: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }
}
