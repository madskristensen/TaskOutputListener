using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TaskOutputListener
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", Vsix.Version, IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.guidTaskOuputPackageString)]
    public sealed class ClearErrorListPackage : Package
    {
        protected override void Initialize()
        {
            ClearErrorListCommand.Initialize(this);
            base.Initialize();
        }
    }
}
