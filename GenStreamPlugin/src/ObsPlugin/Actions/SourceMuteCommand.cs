﻿namespace Loupedeck.ObsPlugin.Actions
{
    using System;

    public class SourceMuteCommand : PluginMultistateDynamicCommand
    {
        private ObsAppProxy Proxy => (this.Plugin as ObsPlugin).Proxy;

        private const String IMGSourceMuted = "Loupedeck.ObsPlugin.icons.AudioOn.png";
        private const String IMGSourceUnmuted = "Loupedeck.ObsPlugin.icons.AudioOff.png";
        private const String IMGSourceInaccessible = "Loupedeck.ObsPlugin.icons.CloseDesktop.png";
        private const String IMGOffline = "Loupedeck.ObsPlugin.icons.SoftwareNotFound.png";
        private const String SourceNameUnknown = "Offline";
        private const String SpecialSourceGroupName = "General Audio";

        public SourceMuteCommand()
        {
            this.Name = "Audio Source Mute";
            this.Description = "Mutes/Unmutes Audio Source ";
            this.GroupName = "Audio Sources";

            this.AddState("Muted", "Audio source muted");
            this.AddState("Unmuted", "Audio source unmuted");
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

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnSourceCreated(String sourceName)
        {
            this.AddSource(sourceName);
            this.ParametersChanged();
        }

        private void OnSourceDestroyed(String sourceName)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);
            if (this.TryGetParameter(key, out var param))
            {
                this.RemoveParameter(key);
                this.ParametersChanged();
            }
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSourceMuteStateChanged(OBSWebsocketDotNet.OBSWebsocket sender, String sourceName, Boolean isMuted)
        {
            var actionParameter = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);

            // FIXME: Check if this 'has parameter' check is needed.
            if (this.TryGetParameter(actionParameter, out var param))
            {
                this.SetCurrentState(actionParameter, isMuted ? 0 : 1);
                this.ActionImageChanged();
            }
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            var sourceName = SourceNameUnknown;
            var imageName = IMGOffline;
            if (SceneKey.TryParse(actionParameter, out var parsed) && this.TryGetCurrentStateIndex(actionParameter, out var currentState))
            {
                sourceName = parsed.Source;

                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection
                    ? IMGSourceInaccessible
                    : currentState == 1 ? IMGSourceMuted : IMGSourceUnmuted;
            }

            // TODO: We need to learn to cache bitmaps. Here the key can be same 3 items: image name, state # and sourceName text
            return ObsPlugin.NameOverBitmap(imageSize, imageName, sourceName);
        }

        internal void AddSource(String sourceName, Boolean isSpecialSource = false)
        {
            var key = SceneKey.Encode(this.Proxy.CurrentSceneCollection, sourceName);

            var displayName = sourceName + (isSpecialSource ? "(G)" : "") + " mute";
            this.AddParameter(key, displayName, this.GroupName);          
            this.SetCurrentState(key, this.Proxy.AppGetMute(sourceName) ? 0 : 1);

            // this.AddParameter(key, $"{sourceName} mute", isSpecialSource ? SpecialSourceGroupName : this.GroupName);
        }

        internal void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Proxy.Trace($"Adding {this.Proxy.CurrentAudioSources.Count} sources");

                foreach (var item in this.Proxy.CurrentAudioSources)
                {
                    this.AddSource(item.Key, item.Value.SpecialSource);
                }
            }

            this.ParametersChanged();
        }
    }
}