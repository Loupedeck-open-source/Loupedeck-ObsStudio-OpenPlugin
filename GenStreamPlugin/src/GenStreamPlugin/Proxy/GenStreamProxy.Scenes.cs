namespace Loupedeck.GenStreamPlugin
{
    using System;
    using System.Collections.Generic;
    using OBSWebsocketDotNet;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    public partial class GenStreamProxy
    {

        public event EventHandler<EventArgs> AppEvtSceneListChanged;
        public event EventHandler<EventArgs> AppEvtCurrentSceneChanged;

        public OBSWebsocketDotNet.Types.OBSScene CurrentScene { get; private set; }
        public List<OBSWebsocketDotNet.Types.OBSScene> Scenes { get; private set; }

        public Boolean SceneInCurrentCollection(String scene) => Helpers.TryExecuteFunc(() => this.Scenes.Find(x => x.Name == scene), out var item) && item != null;

        private void OnObsSceneListChanged(Object sender, EventArgs e)
        {
            //Rescan the scene list   
            if (this.IsAppConnected && Helpers.TryExecuteFunc(() => this.GetSceneList(), out var listInfo))
            {
                this.Scenes = (listInfo as OBSWebsocketDotNet.Types.GetSceneListInfo).Scenes;

                this.Trace($"OBS Rescanned scene list. Currently {this.Scenes.Count} scenes in collection {this.CurrentSceneCollection} ");

                //Retreiving properties for all scenes
                this.OnObsSceneCollectionChange_FetchSceneItems();

                if(Helpers.TryExecuteFunc( ()=> { return this.GetCurrentScene(); }, out var scene))
                {
                    if(!scene.Name.Equals(this.CurrentScene?.Name))
                    {
                        this.OnObsSceneChanged(e, scene.Name);
                    }
                }
                else
                {
                    this.Trace("OBS Warning: SceneListChanged: cannot fetch current scene");
                }

                this.OnObsSceneCollectionChanged_RetreiveAudioSources();
                this.AppEvtSceneListChanged?.Invoke(sender, e);
            }
            else
            {
                this.Trace("OBS Warning: Cannot handle SceneListChanged event");
            }
        }

        private void OnObsSceneChanged(Object sender, String newScene)
        {
            try
            {
                var item = this.Scenes.Find(x => x.Name == newScene);
                if (item != null)
                {
                    this.Trace($"OBS - Current scene changed from {this.CurrentScene?.Name} to {newScene}");

                    this.CurrentScene = item;
                    this.AppEvtCurrentSceneChanged?.Invoke(sender, null);
                }
                else
                {
                    this.Trace($"Warning: Cannot find scene {newScene} in current collection {this.CurrentSceneCollection}");
                }

                //Updating Mute status for sources
                //this.SyncAudioStateWithOBS();
            }
            catch (Exception ex)
            {
                this.Trace($"Warning: Exception {ex.Message} while changing scene");
            }

        }

        public void AppSwitchToScene(String newScene)
        {
            if (this.IsAppConnected && this.SceneInCurrentCollection(newScene))
            {
                this.Trace($"Switching to scene {newScene}");

                Helpers.TryExecuteSafe(() =>
                {
                    this.SetCurrentScene(newScene);
                });
            }

        }
    }
}
