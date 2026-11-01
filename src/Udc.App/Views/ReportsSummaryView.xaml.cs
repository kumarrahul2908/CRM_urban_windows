using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class ReportsSummaryView : UserControl
{
    private readonly ApiClient _api;

    public ReportsSummaryView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        var today = DateTime.Today;
        FromDatePicker.SelectedDate = new DateTime(today.Year, today.Month, 1);
        ToDatePicker.SelectedDate = today;
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var from = FromDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
            var to = ToDatePicker.SelectedDate?.ToString("yyyy-MM-dd") ?? "";
            var result = await _api.GetReportsSummaryAsync(from, to);
            Render(result);
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load the report: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private void Render(ReportsSummaryResult r)
    {
        SalesCardsPanel.Children.Clear();
        AddCard(SalesCardsPanel, "Orders", r.Sales.OrderCount.ToString());
        AddCard(SalesCardsPanel, "Order Value", Money(r.Sales.OrderValue), accent: true);
        AddCard(SalesCardsPanel, "Avg Order Value", Money(r.Sales.AvgOrderValue), accent: true);
        ByServiceGrid.ItemsSource = r.Sales.ByService;
        TopGarmentsGrid.ItemsSource = r.Sales.TopGarments;

        PaymentsCardsPanel.Children.Clear();
        AddCard(PaymentsCardsPanel, "Collected", Money(r.Payments.TotalCollected), accent: true);
        AddCard(PaymentsCardsPanel, "Refunded", Money(r.Payments.TotalRefunded));
        AddCard(PaymentsCardsPanel, "Advance Deposits", Money(r.Payments.AdvanceDeposits));
        AddCard(PaymentsCardsPanel, "Fully Paid Orders", r.Payments.OpenReceivables.Paid.ToString());
        AddCard(PaymentsCardsPanel, "Partially Paid", r.Payments.OpenReceivables.Partial.ToString());
        AddCard(PaymentsCardsPanel, "Unpaid", r.Payments.OpenReceivables.Unpaid.ToString());

        CustomersCardsPanel.Children.Clear();
        AddCard(CustomersCardsPanel, "New Customers", r.Customers.NewCustomers.ToString());
        AddCard(CustomersCardsPanel, "Returning Customers", r.Customers.ReturningCustomers.ToString());
        TopCustomersGrid.ItemsSource = r.Customers.TopCustomers;

        OperationsCardsPanel.Children.Clear();
        AddCard(OperationsCardsPanel, "In Workshop", r.Operations.InWorkshop.ToString());
        AddCard(OperationsCardsPanel, "Processing", r.Operations.Processing.ToString());
        AddCard(OperationsCardsPanel, "QC Pending", r.Operations.QcPending.ToString());
        AddCard(OperationsCardsPanel, "In Rework", r.Operations.InRework.ToString());
        AddCard(OperationsCardsPanel, "Ready to Pack", r.Operations.ReadyToPack.ToString());
        AddCard(OperationsCardsPanel, "Packed", r.Operations.Packed.ToString());
        AddCard(OperationsCardsPanel, "QC Passed", r.Operations.QcPassed.ToString());
        AddCard(OperationsCardsPanel, "QC Failed", r.Operations.QcFailed.ToString());
        AddCard(OperationsCardsPanel, "Overdue Orders", r.Operations.OverdueOrders.ToString());
        AddCard(OperationsCardsPanel, "Urgent Orders", r.Operations.UrgentOrders.ToString());
    }

    private void AddCard(Panel panel, string label, string value, bool accent = false)
    {
        var card = new Border
        {
            Width = 170,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(14, 12, 14, 12),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = label, FontSize = 11, Foreground = (Brush)FindResource("MutedBrush"), Margin = new Thickness(0, 0, 0, 6) });
        stack.Children.Add(new TextBlock { Text = value, FontSize = 19, FontWeight = FontWeights.Bold, Foreground = accent ? (Brush)FindResource("PrimaryBrush") : (Brush)FindResource("TextBrush") });
        card.Child = stack;
        panel.Children.Add(card);
    }

    private async void LoadButton_Click(object sender, RoutedEventArgs e) => await LoadAsync();
}
