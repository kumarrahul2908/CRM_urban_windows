using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Udc.Client;
using Udc.Client.Models;

namespace Udc.App.Views;

public partial class CustomersView : UserControl
{
    private readonly ApiClient _api;
    private int _page = 1;
    private int _totalPages = 1;
    private string _search = "";

    public CustomersView(ApiClient api)
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
            var result = await _api.GetCustomersAsync(_search, _page);
            _totalPages = Math.Max(1, result.TotalPages);
            CustomersGrid.ItemsSource = result.Results;
            SummaryText.Text = $"Page {result.Page} of {_totalPages} - {result.Total} customer(s)";
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load customers: {ex.Message}";
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

    private void NewCustomerButton_Click(object sender, RoutedEventArgs e)
    {
        var form = new CustomerFormWindow(_api) { Owner = Window.GetWindow(this) };
        if (form.ShowDialog() == true) _ = LoadAsync();
    }

    private void Grid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (CustomersGrid.SelectedItem is CustomerSummary selected)
        {
            var detail = new CustomerDetailWindow(_api, selected.Id) { Owner = Window.GetWindow(this) };
            detail.ShowDialog();
        }
    }
}
