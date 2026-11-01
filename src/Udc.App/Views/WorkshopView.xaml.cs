using System.Windows;
using System.Windows.Controls;
using Udc.Client;

namespace Udc.App.Views;

public partial class WorkshopView : UserControl
{
    private readonly ApiClient _api;

    public WorkshopView(ApiClient api)
    {
        InitializeComponent();
        _api = api;
        ShowQueue();
    }

    private void SelectTab(Button active)
    {
        foreach (var b in new[] { TabQueue, TabSchedule, TabReport, TabBilling })
            b.Style = (Style)FindResource(b == active ? "PrimaryButton" : "SecondaryButton");
    }

    private void ShowQueue()
    {
        SelectTab(TabQueue);
        SubContentArea.Content = new WorkshopQueueView(_api);
    }

    private void ShowSchedule()
    {
        SelectTab(TabSchedule);
        SubContentArea.Content = new WorkshopScheduleView(_api);
    }

    private void ShowReport()
    {
        SelectTab(TabReport);
        SubContentArea.Content = new WorkshopReportView(_api);
    }

    private void ShowBilling()
    {
        SelectTab(TabBilling);
        SubContentArea.Content = new WorkshopBillingView(_api);
    }

    private void TabQueue_Click(object sender, RoutedEventArgs e) => ShowQueue();
    private void TabSchedule_Click(object sender, RoutedEventArgs e) => ShowSchedule();
    private void TabReport_Click(object sender, RoutedEventArgs e) => ShowReport();
    private void TabBilling_Click(object sender, RoutedEventArgs e) => ShowBilling();
}
