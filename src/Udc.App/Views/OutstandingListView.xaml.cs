using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Udc.Client;

namespace Udc.App.Views;

public partial class OutstandingListView : UserControl
{
    private readonly ApiClient _api;
    private string _search = "";

    public OutstandingListView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        Loaded += async (_, _) => await LoadAsync();
    }

    private static string Money(decimal amount) => "₹" + amount.ToString("N2", CultureInfo.InvariantCulture);

    private async Task LoadAsync()
    {
        ErrorBanner.Visibility = Visibility.Collapsed;
        try
        {
            var result = await _api.GetOutstandingListAsync(_search);
            GrandOutstandingText.Text = Money(result.GrandOutstanding);
            ReceivablesGrid.ItemsSource = result.Receivables;

            while (SummaryCardsPanel.Children.Count > 1) SummaryCardsPanel.Children.RemoveAt(1); // keep the static Grand Outstanding card
            foreach (var (bucket, total) in result.BucketTotals)
            {
                if (total <= 0) continue;
                AddBucketCard(bucket, total);
            }
        }
        catch (ApiException ex)
        {
            ErrorText.Text = $"Could not load the outstanding list: {ex.Message}";
            ErrorBanner.Visibility = Visibility.Visible;
        }
    }

    private void AddBucketCard(string bucket, decimal total)
    {
        var card = new Border
        {
            Width = 160,
            Margin = new Thickness(0, 0, 10, 10),
            Padding = new Thickness(14, 12, 14, 12),
            CornerRadius = new CornerRadius(10),
            Background = Brushes.White,
            BorderBrush = (Brush)FindResource("BorderBrush"),
            BorderThickness = new Thickness(1),
        };
        var stack = new StackPanel();
        stack.Children.Add(new TextBlock { Text = bucket, FontSize = 11, Foreground = (Brush)FindResource("MutedBrush") });
        stack.Children.Add(new TextBlock { Text = Money(total), FontSize = 17, FontWeight = FontWeights.Bold });
        card.Child = stack;
        SummaryCardsPanel.Children.Add(card);
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
}
