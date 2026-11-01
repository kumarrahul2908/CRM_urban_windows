using Udc.Client.Models;

namespace Udc.Client;

/// <summary>Turns a validated <c>GET /api/mobile/me</c> response plus the token that validated
/// it into the same <see cref="LoginResult"/> shape a fresh login produces, so the rest of the
/// app (MainWindow etc.) doesn't need to know whether a session came from the Login window or
/// from a saved <see cref="TokenStore"/> token on startup.</summary>
public static class SessionRestore
{
    public static LoginResult FromMe(string token, MeResult me) => new()
    {
        Token = token,
        User = new UserContext
        {
            Id = me.User.Id,
            Username = me.User.Username,
            FullName = me.User.FullName,
            Email = me.User.Email,
            RoleId = me.User.RoleId,
            RoleName = me.User.RoleName,
            StoreId = me.Store.FixedStore?.Id,
            Permissions = me.Permissions,
        },
    };
}
