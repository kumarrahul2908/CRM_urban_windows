using System.Windows;
using System.Windows.Controls;
using Udc.Client;

namespace Udc.App.Views;

public partial class ReportsView : UserControl
{
    private readonly ApiClient _api;

    public ReportsView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        ShowSummary();
    }

    private void SelectTab(Button active)
    {
        foreach (var b in new[] { TabSummary, TabOutstanding, TabDailyCollection })
            b.Style = (Style)FindResource(b == active ? "PrimaryButton" : "SecondaryButton");
    }

    private void ShowSummary()
    {
        SelectTab(TabSummary);
        SubContentArea.Content = new ReportsSummaryView(_api);
    }

    private void ShowOutstanding()
    {
        SelectTab(TabOutstanding);
        SubContentArea.Content = new OutstandingListView(_api);
    }

    private void ShowDailyCollection()
    {
        SelectTab(TabDailyCollection);
        SubContentArea.Content = new DailyCollectionView(_api);
    }

    private void TabSummary_Click(object sender, RoutedEventArgs e) => ShowSummary();
    private void TabOutstanding_Click(object sender, RoutedEventArgs e) => ShowOutstanding();
    private void TabDailyCollection_Click(object sender, RoutedEventArgs e) => ShowDailyCollection();
}
