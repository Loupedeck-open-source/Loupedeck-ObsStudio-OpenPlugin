using Loupedeck.ObsStudioPlugin;
using System.Collections.Generic;

namespace Loupedeck.ObsStudioPlugin
{
    using System;
    using System.Collections.Generic;
    using System.Runtime.CompilerServices;

    using OBSWebsocketDotNet;
    using OBSWebsocketDotNet.Types;
    using OBSWebsocketDotNet.Types.Events;

    internal class LDSceneItem
    {
        public String SourceName { get; private set; }
        public Int32 SceneItemId { get; private set; }
        public LDSceneItem(String sourceName, Int32 itemId)
        {
            this.SourceName = sourceName;
            this.SceneItemId = itemId;
        }
    }

    internal class Scene
    {
        public String Name { get; private set; }
        public  List<LDSceneItem> Items { get; private set; } 
        public Scene(SceneBasicInfo scene)
        {
            this.Name = scene.Name;
            this.Items = new List<LDSceneItem>(); // TEMP scene.Items.ConvertAll(x => new LDSceneItem(x.SourceName));
        }

        public Scene()
        {
            this.Name = "";
            this.Items = new List<LDSceneItem>();
        }
    }

    /// <summary>
    /// Proxy to OBS websocket server, for API reference see
    /// https://github.com/obsproject/obs-websocket/blob/4.x-compat/docs/generated/protocol.md
    /// </summary>
    internal partial class ObsAppProxy
    {
        public event EventHandler<EventArgs> AppEvtSceneListChanged;

        public event EventHandler<OldNewStringChangeEventArgs> AppEvtCurrentSceneChanged;
        public String CurrentSceneName { get; private set; } = "";

        public List<Scene> Scenes { get; private set; } = new List<Scene>();

        

        /// <summary>
        /// Attemts to get the Scene object for scene in current collection
        /// </summary>
        /// <param name="sceneName">Name of scene</param>
        /// <param name="scene">scene object</param>
        /// <returns>true if scene retreived</returns>
        public Boolean TryGetSceneByName(String sceneName, out Scene scene)
        {
            scene = null;
            if(!String.IsNullOrEmpty(sceneName))
            {
                scene = this.Scenes.Find(x => x.Name == sceneName);
            }
            return scene != null;
        }
        public void AppSwitchToScene(String newScene)
        {
            if (this.IsAppConnected && this.TryGetSceneByName(newScene, out var _))
            {
                this.Plugin.Log.Info($"Switching to scene {newScene}");

                Helpers.TryExecuteSafe(() =>
                {
                    if (this._studioMode)
                    {
                        //We only set previde scene in studio mode; program scene is set by transition
                        this.SetCurrentPreviewScene(newScene);
                    }
                    else
                    {
                        this.SetCurrentProgramScene(newScene);
                    }
                });
            }
        }

        private void OnObsSceneListChanged(Object sender, EventArgs e)
        {
            // Rescan the scene list
            if (this.IsAppConnected && Helpers.TryExecuteFunc(() => this.GetSceneList(), out var listInfo))
            {
                this.Scenes = listInfo.Scenes.ConvertAll(x => new Scene(x));

                this.Plugin.Log.Info($"OBS Rescanned scene list. Currently {this.Scenes?.Count} scenes in collection {this.CurrentSceneCollection} ");

                // Retreiving properties for all scenes
                this.OnObsSceneCollectionChange_FetchSceneItems();

                if (Helpers.TryExecuteFunc(() => this.GetCurrentProgramScene(), out var scene))
                {
                    if (!String.IsNullOrEmpty(scene) && !scene.Equals(this.CurrentSceneName))
                    {
                        this.OnObsSceneChanged(e, scene);
                    }
                }
                else
                {
                    this.Plugin.Log.Warning("SceneListChanged: cannot fetch current scene");
                }

                this.OnObsSceneCollectionChanged_RetreiveAudioSources();

                this.AppEvtSceneListChanged?.Invoke(sender, e);
            }
            else
            {
                this.Plugin.Log.Warning("Cannot handle SceneListChanged event");
            }
        }
        private void OnSceneChanged(String newScene)
        {
            if (this.TryGetSceneByName(newScene, out var scene) && this.CurrentSceneName != scene.Name)
            {
                this.Plugin.Log.Info($"OBS - Current scene changed from \"{this.CurrentSceneName}\" to \"{newScene}\"");
                var args = new OldNewStringChangeEventArgs(this.CurrentSceneName, scene.Name);
                this.CurrentSceneName = scene.Name;
                this.AppEvtCurrentSceneChanged?.Invoke(this, args);
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot find scene \"{newScene}\" in current collection {this.CurrentSceneCollection}");
            }
        }

        private void OnObsPreviewSceneChanged(Object sender, CurrentPreviewSceneChangedEventArgs args)
        {
            var newScene = args.SceneName;
            //Scenes and preview: When in the Studio mode
            // Scene A: Preview scene
            // Scene B: Program scene
            // Scene C: Some other scene
            //  On the device, scene B is scene. Scenes A and C are not selected
            //  If Scene C is pressed, it becomes new Preview scene but REMAINS UNSELECTED
            //  if Scene B is pressed, nothing happens 
            //  if Scene A is pressed, nothing happens

            // This the same as onObsSceneChanged but in previewmode
            // For Studio mode, OBS plugin will set / monitor Preview scenes, not main scene
            //  NB: the SceneChange command changes program scene, not a preview one
            this.Plugin.Log.Info($"PreviewSceneChange to {newScene}");

            if (this._studioMode)
            {
                //this.OnSceneChanged(newScene);
            }
            else
            {
                this.Plugin.Log.Info($"PreviewSceneChange to {newScene} but not in Studio mode, igrnoring");
            }
        }
        private void OnObsTransitionEnd(Object _ , SceneTransitionEndedEventArgs args)
        {
            // String transitionName, String transitionType, Int32 duration, String toScene)
            this.Plugin.Log.Info($"Transition {args.TransitionName} ended");
            if (this._studioMode)
            {
                //In new OBS the ProgramSceneChanged Event should fire normally
                //In studio mode (see above), the selected scene == Program scene
                //this.OnSceneChanged(toScene);
            }
        }
        private void OnObsSceneChanged(Object sender, ProgramSceneChangedEventArgs args) => this.OnObsSceneChanged(sender, args.SceneName);
        private void OnObsSceneChanged(Object _, String newScene)
        {
            if (!this._studioMode)
            {
                this.OnSceneChanged(newScene);
            }
            else
            {
                //This is handled in OnObsTransitionEnd
                this.Plugin.Log.Info($"OnObsSceneChanged to {newScene} ignoring in Studio mode");
            }
        }
        
    }
}
