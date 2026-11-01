using Udc.Client;
using Xunit;

namespace Udc.Client.Tests;

public class BackgroundJobRunnerTests
{
    [Fact]
    public void TimeUntil_TargetLaterToday_ReturnsRemainderOfToday()
    {
        var now = new DateTime(2026, 11, 1, 10, 0, 0);

        var result = BackgroundJobRunner.TimeUntil(now, 14, 30);

        Assert.Equal(new TimeSpan(4, 30, 0), result);
    }

    [Fact]
    public void TimeUntil_TargetAlreadyPassedToday_RollsOverToTomorrow()
    {
        var now = new DateTime(2026, 11, 1, 10, 0, 0);

        var result = BackgroundJobRunner.TimeUntil(now, 3, 15);

        Assert.Equal(new TimeSpan(17, 15, 0), result);
    }

    [Fact]
    public void TimeUntil_ExactlyNow_RollsOverToTomorrow_NotZero()
    {
        var now = new DateTime(2026, 11, 1, 2, 30, 0);

        var result = BackgroundJobRunner.TimeUntil(now, 2, 30);

        Assert.Equal(TimeSpan.FromHours(24), result);
    }

    [Fact]
    public void TimeUntil_OneMinuteBeforeTarget_IsAlmostZero()
    {
        var now = new DateTime(2026, 11, 1, 2, 29, 0);

        var result = BackgroundJobRunner.TimeUntil(now, 2, 30);

        Assert.Equal(TimeSpan.FromMinutes(1), result);
    }
}
