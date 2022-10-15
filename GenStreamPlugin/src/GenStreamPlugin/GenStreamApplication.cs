namespace Loupedeck.GenStreamPlugin
{
    using System;

    // This class can be used to connect the Loupedeck plugin to an application.

    public class GenStreamApplication : ClientApplication
    {
        public GenStreamApplication()
        {
        }

        // This method can be used to link the plugin to a Windows application.
        protected override String GetProcessName() => "obs64";

        // This method can be used to link the plugin to a macOS application.
        protected override String GetBundleName() => "";

        // This method can be used to check whether the application is installed or not.
        public override ClientApplicationStatus GetApplicationStatus() => ClientApplicationStatus.Unknown;
    }
}
