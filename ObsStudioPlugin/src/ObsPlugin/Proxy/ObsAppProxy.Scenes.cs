namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;

    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types;

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtSceneListChanged;

        public event EventHandler<OldNewStringChangeEventArgs> AppEvtCurrentSceneChanged;

        public OBSScene CurrentScene { get; private set; } = new OBSScene();

        public List<OBSScene> Scenes { get; private set; } = new List<OBSScene>();

        private void OnObsSceneListChanged(Object sender, EventArgs e)
        {
            // Rescan the scene list
            if (this.IsAppConnected && Helpers.TryExecuteFunc(() => this.GetSceneList(), out var listInfo))
            {
                this.Scenes = (listInfo as GetSceneListInfo).Scenes;

                ObsStudioPlugin.Trace($"OBS Rescanned scene list. Currently {this.Scenes.Count} scenes in collection {this.CurrentSceneCollection} ");

                // Retreiving properties for all scenes
                this.OnObsSceneCollectionChange_FetchSceneItems();

                if (Helpers.TryExecuteFunc(() => this.GetCurrentScene(), out var scene))
                {
                    if (!scene.Name.Equals(this.CurrentScene?.Name))
                    {
                        this.OnObsSceneChanged(e, scene.Name);
                    }
                }
                else
                {
                    ObsStudioPlugin.Trace("OBS Warning: SceneListChanged: cannot fetch current scene");
                }

                this.OnObsSceneCollectionChanged_RetreiveAudioSources();
                this.AppEvtSceneListChanged?.Invoke(sender, e);
            }
            else
            {
                ObsStudioPlugin.Trace("OBS Warning: Cannot handle SceneListChanged event");
            }
        }

        /// <summary>
        /// Attemts to get the Scene object for scene in current collection
        /// </summary>
        /// <param name="sceneName">Name of scene</param>
        /// <param name="scene">scene object</param>
        /// <returns>true if scene retreived</returns>
        public Boolean TryGetSceneByName(String sceneName, out OBSScene scene)
        {
            scene = null;
            if(!String.IsNullOrEmpty(sceneName))
            {
                scene = this.Scenes.Find(x => x.Name == sceneName);
            }
            return scene != null;
        }

        private void OnObsSceneChanged(Object sender, String newScene)
        {
            if( this.TryGetSceneByName(newScene, out var scene)  && this.CurrentScene != scene)
            {
                ObsStudioPlugin.Trace($"OBS - Current scene changed from {this.CurrentScene?.Name} to {newScene}");
                var args = new OldNewStringChangeEventArgs(this.CurrentScene?.Name, scene.Name);
                this.CurrentScene = scene;
                this.AppEvtCurrentSceneChanged?.Invoke(sender,args);
            }
            else
            {
                ObsStudioPlugin.Trace($"Warning: Cannot find scene {newScene} in current collection {this.CurrentSceneCollection}");
            }
        }

        public void AppSwitchToScene(String newScene)
        {
            if (this.IsAppConnected && this.TryGetSceneByName(newScene, out var _ ))
            {
                ObsStudioPlugin.Trace($"Switching to scene {newScene}");

                _ = Helpers.TryExecuteSafe(() => this.SetCurrentScene(newScene));
            }
        }
    }
}
