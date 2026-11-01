using Udc.Client;
using Udc.Client.Models;
using Xunit;

namespace Udc.Client.Tests;

public class SessionRestoreTests
{
    [Fact]
    public void FromMe_MapsUserFieldsAndToken()
    {
        var me = new MeResult
        {
            User = new MeUser { Id = 7, Username = "admin", FullName = "Admin User", Email = "a@example.com", RoleId = 1, RoleName = "Super Admin" },
            Store = new MeStoreInfo { FixedStore = new StoreRef { Id = 2, Name = "Main Branch", Type = "Store" }, RequiresStoreSelection = false },
            Permissions = new List<string> { "dashboard.view", "orders.view" },
        };

        var result = SessionRestore.FromMe("saved-token-123", me);

        Assert.Equal("saved-token-123", result.Token);
        Assert.Equal(7, result.User.Id);
        Assert.Equal("Admin User", result.User.FullName);
        Assert.Equal("Super Admin", result.User.RoleName);
        Assert.Equal(2, result.User.StoreId);
        Assert.Contains("orders.view", result.User.Permissions);
    }

    [Fact]
    public void FromMe_NoFixedStore_LeavesStoreIdNull()
    {
        var me = new MeResult
        {
            User = new MeUser { Id = 1, Username = "owner", FullName = "Owner", RoleId = 1, RoleName = "Super Admin" },
            Store = new MeStoreInfo { FixedStore = null, RequiresStoreSelection = true, AvailableStores = new List<StoreRef> { new() { Id = 1, Name = "A", Type = "Store" } } },
            Permissions = new List<string>(),
        };

        var result = SessionRestore.FromMe("tok", me);

        Assert.Null(result.User.StoreId);
    }
}
