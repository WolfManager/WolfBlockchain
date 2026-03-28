using WolfBlockchain.Core;
using Xunit;

namespace WolfBlockchain.Tests.Core;

[Collection("UserManagerPersistence")]
public class UserManagerPersistenceTests
{
    [Fact]
    public void RegisterUser_WhenStateIsPersisted_NewManagerCanAuthenticateUser()
    {
        var dataPath = Path.Combine(Path.GetTempPath(), "wolf-user-state-tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("WOLF_DATA_PATH", dataPath);

        var manager = new UserManager("WOLFADMIN");
        manager.RegisterUser("WOLFADMIN", "owner", UserRole.Admin, "StrongPass!123");

        var reloadedManager = new UserManager("WOLFADMIN");
        var authenticated = reloadedManager.AuthenticateUser("WOLFADMIN", "StrongPass!123");

        Assert.NotNull(authenticated);
    }

    [Fact]
    public void ChangePassword_WhenStateIsPersisted_NewManagerUsesUpdatedPassword()
    {
        var dataPath = Path.Combine(Path.GetTempPath(), "wolf-user-state-tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("WOLF_DATA_PATH", dataPath);

        var manager = new UserManager("WOLFADMIN");
        manager.RegisterUser("WOLFADMIN", "owner", UserRole.Admin, "StrongPass!123");
        manager.ChangePassword("WOLFADMIN", "StrongPass!123", "NewStrongPass!123");

        var reloadedManager = new UserManager("WOLFADMIN");
        var authenticated = reloadedManager.AuthenticateUser("WOLFADMIN", "NewStrongPass!123");

        Assert.NotNull(authenticated);
    }

    [Fact]
    public void RegisterUser_WhenAnotherManagerIsRunning_SecondManagerReloadsAndAuthenticates()
    {
        var dataPath = Path.Combine(Path.GetTempPath(), "wolf-user-state-tests", Guid.NewGuid().ToString("N"));
        Environment.SetEnvironmentVariable("WOLF_DATA_PATH", dataPath);

        var firstManager = new UserManager("WOLFADMIN");
        var secondManager = new UserManager("WOLFADMIN");

        firstManager.RegisterUser("WOLFADMIN", "owner", UserRole.Admin, "StrongPass!123");

        var authenticated = secondManager.AuthenticateUser("WOLFADMIN", "StrongPass!123");

        Assert.NotNull(authenticated);
    }
}

[CollectionDefinition("UserManagerPersistence", DisableParallelization = true)]
public class UserManagerPersistenceCollectionDefinition
{
}
