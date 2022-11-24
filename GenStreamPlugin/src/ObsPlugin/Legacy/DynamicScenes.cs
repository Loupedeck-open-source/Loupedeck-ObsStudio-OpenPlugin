namespace Loupedeck.ObsStudioPlugin
{
    using System;

    using Loupedeck.ObsStudioPlugin.Actions;

    public class DynamicScenes : PluginDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        //Note DeviceTypeNone -- so that actions is not visible in the UI' action tree.
        public DynamicScenes()
            : base(displayName: "LegacyScenesAction",
                   description: "",
                   groupName: "",
                   DeviceType.None) => this.Name = "DynamicScenes";

        protected override Boolean OnLoad()
        {
            this.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;
            this.Proxy.AppConnected += this.OnAppConnected;
            this.Proxy.AppDisconnected += this.OnAppDisconnected;
            this.IsEnabled = false;

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            this.Proxy.AppConnected -= this.OnAppConnected;
            this.Proxy.AppDisconnected -= this.OnAppDisconnected;
            return true;
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;
        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        private void OnCurrentSceneChanged(Object sender, EventArgs e)
        { 
            var arg = e as OldNewStringChangeEventArgs;
            //unselecting old and selecting new
            if (!String.IsNullOrEmpty(arg.Old))
            {
                this.ActionImageChanged(arg.Old);
            }
            if (!String.IsNullOrEmpty(arg.New))
            {
                this.ActionImageChanged(arg.New);
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = SceneSelectCommand.IMGSceneInaccessible;

            if (this.Proxy.TryGetSceneByName(actionParameter, out var _))
            {
                imageName = actionParameter.Equals(this.Proxy.CurrentScene?.Name) ? SceneSelectCommand.IMGSceneSelected : SceneSelectCommand.IMGSceneUnselected;
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, actionParameter, imageName == SceneSelectCommand.IMGSceneSelected);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.Proxy.TryGetSceneByName(actionParameter, out var _))
            {
                this.Proxy.AppSwitchToScene(actionParameter);
                this.ActionImageChanged();
            }
        }
    }
}
