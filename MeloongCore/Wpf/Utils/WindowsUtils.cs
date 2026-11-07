using System.Security.Principal;

namespace MeloongCore;
public static class WindowsUtils {

    public static bool HasAdminRole() {
        using var id = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(id).IsInRole(WindowsBuiltInRole.Administrator);
    }

}
