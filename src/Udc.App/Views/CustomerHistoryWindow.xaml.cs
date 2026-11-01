using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class CustomerHistoryWindow : Window
{
    private readonly ApiClient _api;
    private readonly int _customerId;
    private CustomerHistoryResult? _history;

    public CustomerHistoryWindow(ApiClient api, int customerId, string customerName)
    {
        InitializeComponent();
        _api = api;
        _customerId = customerId;
        TitleText.Text = $"History - {customerName}";
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            _history = await _api.GetCustomerHistoryAsync(_customerId);
            OutstandingText.Text = Money(_history.Outstanding);
            WalletText.Text = Money(_history.WalletBalance);
            ShowLedger();
        }
        catch (ApiException ex)
        {
            ShowError($"Could not load this customer's history: {ex.Message}");
        }
    }

    private void SelectTab(Button active)
    {
        foreach (var b in new[] { TabLedger, TabOrders, TabPayments, TabGarments, TabWallet, TabDeliveries, TabFeedback })
            b.Style = (Style)FindResource(b == active ? "PrimaryButton" : "SecondaryButton");
    }

    private void SetColumns(params (string Header, string Path, double Width, bool Star)[] columns)
    {
        HistoryGrid.Columns.Clear();
        foreach (var c in columns)
        {
            HistoryGrid.Columns.Add(new DataGridTextColumn
            {
                Header = c.Header,
                Binding = new Binding(c.Path),
                Width = c.Star ? new DataGridLength(c.Width, DataGridLengthUnitType.Star) : new DataGridLength(c.Width),
            });
        }
    }

    private void ShowLedger()
    {
        SelectTab(TabLedger);
        SetColumns(("Date", "Date", 120, false), ("Description", "Description", 2, true), ("Debit", "Debit", 100, false), ("Credit", "Credit", 100, false));
        HistoryGrid.ItemsSource = _history?.Ledger;
    }

    private void ShowOrders()
    {
        SelectTab(TabOrders);
        SetColumns(("Order #", "OrderNumber", 110, false), ("Date", "OrderDate", 1, true), ("Mode", "OrderMode", 80, false),
            ("Total", "GrandTotal", 90, false), ("Due", "CurrentDue", 90, false), ("Status", "StatusLabel", 1, true));
        HistoryGrid.ItemsSource = _history?.Orders;
    }

    private void ShowPayments()
    {
        SelectTab(TabPayments);
        SetColumns(("Payment #", "PaymentNumber", 110, false), ("Date", "PaymentDate", 1, true), ("Kind", "PaymentKind", 90, false),
            ("Amount", "TotalAmount", 90, false), ("Remarks", "Remarks", 2, true));
        HistoryGrid.ItemsSource = _history?.Payments;
    }

    private void ShowGarments()
    {
        SelectTab(TabGarments);
        SetColumns(("Order #", "OrderNumber", 110, false), ("Date", "OrderDate", 1, true), ("Garment", "GarmentName", 2, true),
            ("Service", "ServiceName", 2, true), ("Qty", "Quantity", 70, false), ("Weight", "Weight", 80, false), ("Rate", "Rate", 80, false));
        HistoryGrid.ItemsSource = _history?.Garments;
    }

    private void ShowWallet()
    {
        SelectTab(TabWallet);
        SetColumns(("Date", "CreatedAt", 1, true), ("Type", "TxnType", 90, false), ("Debit", "Debit", 90, false),
            ("Credit", "Credit", 90, false), ("Balance", "BalanceAfter", 90, false), ("Remarks", "Remarks", 2, true));
        HistoryGrid.ItemsSource = _history?.Wallet;
    }

    private void ShowDeliveries()
    {
        SelectTab(TabDeliveries);
        SetColumns(("Delivery #", "DeliveryNumber", 110, false), ("Date", "DeliveryDate", 1, true), ("Time slot", "TimeSlot", 1, true),
            ("Status", "Status", 1, true), ("Amount due", "AmountDue", 100, false));
        HistoryGrid.ItemsSource = _history?.Deliveries;
    }

    private void ShowFeedback()
    {
        SelectTab(TabFeedback);
        SetColumns(("Order #", "OrderNumber", 110, false), ("Rating", "Rating", 70, false), ("Comments", "Comments", 2, true),
            ("Status", "Status", 1, true), ("Date", "CreatedAt", 1, true));
        HistoryGrid.ItemsSource = _history?.Feedback;
    }

    private void TabLedger_Click(object sender, RoutedEventArgs e) => ShowLedger();
    private void TabOrders_Click(object sender, RoutedEventArgs e) => ShowOrders();
    private void TabPayments_Click(object sender, RoutedEventArgs e) => ShowPayments();
    private void TabGarments_Click(object sender, RoutedEventArgs e) => ShowGarments();
    private void TabWallet_Click(object sender, RoutedEventArgs e) => ShowWallet();
    private void TabDeliveries_Click(object sender, RoutedEventArgs e) => ShowDeliveries();
    private void TabFeedback_Click(object sender, RoutedEventArgs e) => ShowFeedback();

    private void ShowError(string message)
    {
        ErrorText.Text = message;
        ErrorBanner.Visibility = Visibility.Visible;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();
}
