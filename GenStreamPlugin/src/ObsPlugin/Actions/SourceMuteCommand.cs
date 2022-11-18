namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    public class SourceMuteCommand : PluginMultistateDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsStudioPlugin).Proxy;

        private const String IMGSourceMuted = "AudioOn.png";
        private const String IMGSourceUnmuted = "AudioOff.png";
        private const String IMGSourceInaccessible = "AudioOff.png";
        private const String SourceNameUnknown = "Offline";

        public SourceMuteCommand()
        {
            this.Name = "Audio Source Mute";
            this.Description = "Mutes/Unmutes Audio Source ";
            this.GroupName = "Audio Sources";
            
            this.IsEnabled = false;

            _ = this.AddState("Muted", "Audio source muted");
            _ = this.AddState("Unmuted", "Audio source unmuted");
        }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            this.Proxy.AppEvtSourceMuteStateChanged += this.OnSourceMuteStateChanged;

            this.Proxy.AppEvtSourceCreated += this.OnSourceCreated;
            this.Proxy.AppEvtSourceDestroyed += this.OnSourceDestroyed;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            this.Proxy.AppEvtSourceMuteStateChanged -= this.OnSourceMuteStateChanged;

            this.Proxy.AppEvtSourceCreated -= this.OnSourceCreated;
            this.Proxy.AppEvtSourceDestroyed -= this.OnSourceDestroyed;

            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (SceneKey.TryParse(actionParameter, out var key) && key.Collection.Equals(this.Proxy.CurrentSceneCollection))
            {
                this.Proxy.AppToggleMute(key.Source);
            }
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) =>
            //TODO: Do ActionImageChanged (ActionParam) for new  and old scene
            this.ActionImageChanged();

        private void OnSourceCreated(String sourceName)
        {
            this.AddSource(sourceName);
            this.ParametersChanged();
        }

        private void OnSourceDestroyed(String sourceName)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);
            if (this.TryGetParameter(key, out _))
            {
                this.RemoveParameter(key);
                this.ParametersChanged();
            }
        }

        private void OnAppConnected(Object sender, EventArgs e) => this.IsEnabled = true;

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.IsEnabled = false;
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSourceMuteStateChanged(OBSWebsocketDotNet.OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            var actionParameter = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);

            // FIXME: Check if this 'has parameter' check is needed.
            if (this.TryGetParameter(actionParameter, out _))
            {
                _ = this.SetCurrentState(actionParameter, isMuted ? 0 : 1);
                this.ActionImageChanged(actionParameter);
            }
        }
        protected override BitmapImage GetCommandImage(String actionParameter, Int32 stateIndex, PluginImageSize imageSize)
        {
            var sourceName = SourceNameUnknown;
            var imageName = IMGSourceInaccessible;
            if (SceneKey.TryParse(actionParameter, out var parsed))
            {
                sourceName = parsed.Source;

                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection
                    ? IMGSourceInaccessible
                    : stateIndex == 1 ? IMGSourceMuted : IMGSourceUnmuted;
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, stateIndex == 1);
        }

        internal void AddSource(String sourceName, Boolean isSpecialSource = false)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);

            var displayName = sourceName + (isSpecialSource ? "(G)" : "") + " mute";
            this.AddParameter(key, displayName, this.GroupName);
            _ = this.SetCurrentState(key, this.Proxy.AppGetMute(sourceName) ? 0 : 1);
        }

        internal void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                ObsStudioPlugin.Trace($"Adding {this.Proxy.CurrentAudioSources.Count} sources");

                foreach (var item in this.Proxy.CurrentAudioSources)
                {
                    this.AddSource(item.Key, item.Value.SpecialSource);
                }
            }

            this.ParametersChanged();
        }
    }
}
