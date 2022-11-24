namespace Loupedeck.ObsStudioPlugin.DynamicActions
{
    using System;

    using Loupedeck.ObsStudioPlugin.Actions;

    public class MuteAudioProfileActions: PluginDynamicCommand
    {
        private const String IMGAction = "STREAM_Transition.png";

        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;


        //Note DeviceTypeNone -- so that actions is not visible in the UI' action tree.
        public MuteAudioProfileActions()
            : base(displayName: "LegacyMuteAction", 
                   description: "",
                   groupName: "",
                   DeviceType.None)
        {
        }

        protected override Boolean OnLoad()
        {
            this.Proxy.AppConnected += this.OnAppConnected;
            this.Proxy.AppDisconnected += this.OnAppDisconnected;
            this.Proxy.AppEvtSourceMuteStateChanged += this.OnSourceMuteStateChanged;
            this.IsEnabled = false;

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.AppConnected -= this.OnAppConnected;
            this.Proxy.AppDisconnected -= this.OnAppDisconnected;
            this.Proxy.AppEvtSourceMuteStateChanged -= this.OnSourceMuteStateChanged;
            return true;
        }
        protected void OnSourceMuteStateChanged(Object sender, MuteEventArgs args)
        {
            ObsStudioPlugin.Trace($"OnSourceMuteStateChanged: Mute signal for '{args.SourceName}' is '{args.isMuted}'");
            this.ActionImageChanged();
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;
        private void OnAppDisconnected(Object sender, EventArgs e) => this.IsEnabled = false;

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var imageName = SourceMuteCommand.IMGSourceInaccessible;
            var sourceName = "Unknown";

            if (this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key_struct))
            {
                sourceName = key_struct.Source;

                var key = key_struct.Stringize();
                if (this.Proxy.CurrentAudioSources.ContainsKey(sourceName))
                {
                    // FIXME: Find how to cache 'muted' state
                    imageName = this.Proxy.CurrentAudioSources[sourceName].Muted ?
                                     SourceMuteCommand.IMGSourceMuted
                                     : SourceMuteCommand.IMGSourceUnmuted;
                }
            }

            ObsStudioPlugin.Trace($"MuteAudioProfileActions: Mute image for '{actionParameter}' is '{imageName}'");

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, imageName == SourceMuteCommand.IMGSourceUnmuted);
        }

        protected override void RunCommand(String actionParameter)
        {
            if (this.Proxy.TryConvertLegacyActionParamToKey(actionParameter, out var key_struct) && this.Proxy.CurrentAudioSources.ContainsKey(key_struct.Source))
            {
                this.Proxy.AppToggleMute(key_struct.Source);
            }
        }
    }
}
