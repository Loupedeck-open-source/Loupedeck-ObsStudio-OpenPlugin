namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class SourceMuteCommand : PluginMultistateDynamicCommand
    {
        public const String IMGSourceMuted = "AudioOff.png";
        public const String IMGSourceUnmuted = "AudioOn.png";
        public const String IMGSourceInaccessible = "AudioInaccessible.png";
        public const String SourceNameUnknown = "Offline";

        private const Int32 State_Muted = 1;
        private const Int32 State_Unmuted = 0;

        public SourceMuteCommand()
        {
            
            this.Description = "Mutes/Unmutes Audio Source";
            this.GroupName = "3. Audio";

            _ = this.AddState("Unmuted", "Audio source unmuted");
            _ = this.AddState("Muted", "Audio source muted");

        }

        protected override Boolean OnLoad()
        {
            this.IsEnabled = false;

            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            ObsStudioPlugin.Proxy.AppEvtSourceMuteStateChanged += this.OnSourceMuteStateChanged;

            ObsStudioPlugin.Proxy.AppSourceCreated += this.OnSourceCreated;
            ObsStudioPlugin.Proxy.AppSourceDestroyed += this.OnSourceDestroyed;

            this.OnAppDisconnected(this, null);

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;
            ObsStudioPlugin.Proxy.AppEvtSourceMuteStateChanged -= this.OnSourceMuteStateChanged;

            ObsStudioPlugin.Proxy.AppSourceCreated -= this.OnSourceCreated;
            ObsStudioPlugin.Proxy.AppSourceDestroyed -= this.OnSourceDestroyed;

            return true;
        }

        protected override void RunCommand(String actionParameter)
        {
            if (SceneKey.TryParse(actionParameter, out var key) && key.Collection.Equals(ObsStudioPlugin.Proxy.CurrentSceneCollection))
            {
                ObsStudioPlugin.Proxy.AppToggleMute(key.Source);
            }
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) =>

            // TODO: Do ActionImageChanged (ActionParam) for new  and old scene
            this.ActionImageChanged();

        private void OnSourceCreated(Object sender, SourceNameEventArgs args)
        {
            this.AddSource(args.SourceName);
            this.ParametersChanged();
        }

        private void OnSourceDestroyed(Object sender, SourceNameEventArgs args)
        {
            var key = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName);

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

        protected void OnSourceMuteStateChanged(Object sender, MuteEventArgs args)
        {
            var actionParameter = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, args.SourceName);

            // FIXME: Check if this 'has parameter' check is needed.
            if (this.TryGetParameter(actionParameter, out _))
            {
                _ = this.SetCurrentState(actionParameter, args.isMuted ? State_Muted : State_Unmuted);
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

                imageName = parsed.Collection != ObsStudioPlugin.Proxy.CurrentSceneCollection
                    ? IMGSourceInaccessible
                    : stateIndex == State_Muted ? IMGSourceMuted : IMGSourceUnmuted;
            }

            return (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, imageName, sourceName, imageName == IMGSourceUnmuted);
        }

        private void AddSource(String sourceName, Boolean isSpecialSource = false)
        {
            var key = SceneKey.Encode(ObsStudioPlugin.Proxy.CurrentSceneCollection, sourceName);

            var displayName = sourceName + (isSpecialSource ? "(G)" : "") + " mute";
            this.AddParameter(key, displayName, this.GroupName).Description = 
                    (ObsStudioPlugin.Proxy.AppGetMute(sourceName) ? "Mute" : "Unmute") + $" audio source \"{sourceName}\"";
            this.SetCurrentState(key, ObsStudioPlugin.Proxy.AppGetMute(sourceName) ? State_Muted : State_Unmuted);
        }

        private void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Plugin.Log.Info($"Adding {ObsStudioPlugin.Proxy.CurrentAudioSources.Count} sources");

                foreach (var item in ObsStudioPlugin.Proxy.CurrentAudioSources)
                {
                    this.AddSource(item.Key, item.Value.SpecialSource);
                }
            }

            this.ParametersChanged();
        }
    }
}
