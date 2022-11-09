namespace Loupedeck.GenStreamPlugin.Actions
{
    using System;

    class SourceVolumeAdjustment : PluginDynamicAdjustment
    {

        private GenStreamProxy Proxy => (this.Plugin as GenStreamPlugin).Proxy;
    
        private const String IMG_SceneSelected = "Loupedeck.GenStreamPlugin.icons.SourceOn.png";
        private const String IMG_SceneUnselected = "Loupedeck.GenStreamPlugin.icons.SourceOff.png";
        private const String IMG_SceneInaccessible = "Loupedeck.GenStreamPlugin.icons.CloseDesktop.png";
        private const String IMG_Offline = "Loupedeck.GenStreamPlugin.icons.SoftwareNotFound.png";
        private const String SourceNameUnknown = "Offline";

        public SourceVolumeAdjustment() : base(true)
        {
            this.Name = "Audio Soruce Volume Mixer";
            this.DisplayName = "Volume Mixer"; 
            this.Description = "Controls Audio Source Volume";
            this.GroupName = "Audio Sources";
        }

        protected override Boolean OnLoad()
        {
            this.Proxy.EvtAppConnected += this.OnAppConnected;
            this.Proxy.EvtAppDisconnected += this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged += this.OnCurrentSceneChanged;

            this.Proxy.AppEvtSourceVolumeChanged += this.OnSourceVolumeChanged;

            this.Proxy.AppEvtSceneItemAdded += this.OnSceneItemAdded;
            this.Proxy.AppEvtSceneItemRemoved += this.OnSceneItemRemoved;

            this.OnAppDisconnected(this, null);
            
            return true;
        }

        protected override Boolean OnUnload()
        {
            this.Proxy.EvtAppConnected -= this.OnAppConnected;
            this.Proxy.EvtAppDisconnected -= this.OnAppDisconnected;

            this.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            this.Proxy.AppEvtCurrentSceneChanged -= this.OnCurrentSceneChanged;

            this.Proxy.AppEvtSourceVolumeChanged -= this.OnSourceVolumeChanged;

            this.Proxy.AppEvtSceneItemAdded -= this.OnSceneItemAdded;
            this.Proxy.AppEvtSceneItemRemoved -= this.OnSceneItemRemoved;

            return true;
        }

        protected override String GetAdjustmentDisplayName(String actionParameter, PluginImageSize imageSize)
        {
            if (SceneItemKey.TryParse(actionParameter, out var key))
            {
                return key.Source;
            }
            return SourceNameUnknown;
        }


        protected override void ApplyAdjustment(String actionParameter, Int32 diff)
        {
            ///         protected override void ApplyAdjustment(String actionParameter, Int32 diff)
            if (SceneItemKey.TryParse(actionParameter, out var key))
            {

                this.Proxy.AppSetVolume(key.Scene, key.Source, this.Proxy.allSceneItems[actionParameter].AdjustVolume(diff));

                if (!key.Scene.Equals(this.Proxy.CurrentScene.Name))
                {
                    //For non-current scene, we just adjust the state 
                    //It'll be sent once key.scene will become current and Proxy.Synchronize... will execute
                    
                    //this.AdjustmentValueChanged();
                }
            }

            this.AdjustmentValueChanged();
        }


        protected override void RunCommand(String actionParameter)
        {
            if( SceneItemKey.TryParse(actionParameter, out var key) )
            {
                //Pressing the button toggles mute
                this.Proxy.AppToggleMute(key.Scene, key.Source);
                if(!key.Scene.Equals(this.Proxy.CurrentScene.Name))
                {
                    //For non-current scene, we just flip the state without waiting for the 'on volume changed signal'.
                    //It'll be sent once key.scene will become current and Proxy.SynchronizeMuteWithOBS will execute
                    this.ActionImageChanged(actionParameter);
                }
            }
            else
            {
                this.Proxy.Trace($"Warning: Cannot  parse actionParameter {actionParameter}");
            }
        }

        protected override String GetAdjustmentValue(String actionParameter)
        {
            var volume = this.Proxy.allSceneItems.ContainsKey(actionParameter) ? this.Proxy.allSceneItems[actionParameter].VolumeByLD : 0;
            return $"{volume}";
        }


        private void OnSceneListChanged(Object sender, EventArgs e) => this.ResetParameters(true);

        private void OnCurrentSceneChanged(Object sender, EventArgs e) => this.ActionImageChanged();

        private void OnSceneItemAdded(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName)
        {
            this.AddSceneItemParameter(sceneName, itemName);
            this.ParametersChanged();
        }

        private void OnSceneItemRemoved(OBSWebsocketDotNet.OBSWebsocket sender, String sceneName, String itemName)
        {
            this.RemoveParameter(SceneItemKey.Encode(this.Proxy?.CurrentSceneCollection, sceneName, itemName));
            this.ParametersChanged();
        }

        private void OnAppConnected(Object sender, EventArgs e)
        { 
            //We expect to get SceneCollectionChange so doin' nothin' here. 
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
            this.ResetParameters(false);
            this.ActionImageChanged();
        }

        protected void OnSourceVolumeChanged(OBSWebsocketDotNet.OBSWebsocket sender, OBSWebsocketDotNet.Types.SourceVolume volume)
        {
            var actionParameter = SceneItemKey.Encode(this.Proxy?.CurrentSceneCollection, this.Proxy?.CurrentScene.Name, volume.SourceName);
            /*
            if(!this.TryApplyAdjustment(actionParameter, (Int32)(volume.Volume * 100.0)))
            {
                Tracer.Warning($"Error setting volume{volume}, {volume.SourceName}");
            }
            */
            this.AdjustmentValueChanged(actionParameter);
        }

        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            //FIXME: Need proper images etc

            var sourceName = SourceNameUnknown;
            var imageName = IMG_Offline;
            if ( SceneItemKey.TryParse(actionParameter, out var parsed))
            {
                sourceName = parsed.Source; 

                imageName = parsed.Collection != this.Proxy.CurrentSceneCollection ? IMG_SceneInaccessible :  IMG_SceneSelected;
            }

            return GenStreamPlugin.NameOverBitmap(imageSize, imageName, sourceName);
        }

        internal void AddSceneItemParameter(String sceneName, String itemName)
        {
            var key = SceneItemKey.Encode(this.Proxy.CurrentSceneCollection, sceneName, itemName);
            this.AddParameter(key, $"{itemName} ({sceneName})", this.GroupName);
            
        }

        internal void ResetParameters(Boolean readContent)
        {
            this.RemoveAllParameters();

            if (readContent)
            {
                this.Proxy.Trace($"Adding {this.Proxy.allSceneItems?.Count} sources");

                foreach (var item in this.Proxy.allSceneItems)
                {
                    this.AddSceneItemParameter(item.Value.SceneName, item.Value.SourceName);
                }
            }

            this.ParametersChanged();
        }

    }
}
