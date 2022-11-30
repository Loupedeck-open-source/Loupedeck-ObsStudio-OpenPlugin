namespace Loupedeck.ObsStudioPlugin
{
    using System;

    using Loupedeck.ObsStudioPlugin.Actions;

    internal class DynamicScenes : PluginDynamicCommand
    {
        //Note DeviceTypeNone -- so that actions is not visible in the UI' action tree.
        public DynamicScenes()
            : base(displayName: "LegacyScenesAction",
                   description: "",
                   groupName: "",
                   DeviceType.None)
        { }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;
            this.IsEnabled = false;

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
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

            if (ObsStudioPlugin.Proxy.TryGetSceneByName(actionParameter, out var _))
            {
                imageName = actionParameter.Equals(ObsStudioPlugin.Proxy.CurrentScene?.Name) ? SceneSelectCommand.IMGSceneSelected : SceneSelectCommand.IMGSceneUnselected;
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, actionParameter, imageName == SceneSelectCommand.IMGSceneSelected);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (ObsStudioPlugin.Proxy.TryGetSceneByName(actionParameter, out var _))
            {
                ObsStudioPlugin.Proxy.AppSwitchToScene(actionParameter);
                this.ActionImageChanged();
            }
        }
    }
}
