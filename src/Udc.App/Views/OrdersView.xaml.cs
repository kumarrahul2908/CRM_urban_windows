using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class OrdersView : UserControl
{
    private readonly ApiClient _api;
    private int _page = 1;
    private int _totalPages = 1;
    private string _search = "";

    public OrdersView(ApiClient api)
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
            var result = await _api.GetOrdersAsync(_search, _page);
            _totalPages = Math.Max(1, result.TotalPages);
            OrdersGrid.ItemsSource = result.Results;
            SummaryText.Text = $"Page {result.Page} of {_totalPages} - {result.Total} order(s)";
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load orders: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        _search = SearchBox.Text.Trim();
        _page = 1;
        await LoadAsync();
    }

    private async void SearchBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) { _search = SearchBox.Text.Trim(); _page = 1; await LoadAsync(); }
    }

    private async void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (_page <= 1) return;
        _page--;
        await LoadAsync();
    }

    private async void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_page >= _totalPages) return;
        _page++;
        await LoadAsync();
    }

    private async void OrdersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (OrdersGrid.SelectedItem is OrderSummary selected)
        {
            var detail = new OrderDetailWindow(_api, selected.Id) { Owner = Window.GetWindow(this) };
            detail.ShowDialog();
            await LoadAsync(); // a payment may have been recorded while the detail window was open
        }
    }

    private async void NewOrderButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new NewOrderWindow(_api) { Owner = Window.GetWindow(this) };
        if (form.ShowDialog() == true) await LoadAsync();
    }
}
