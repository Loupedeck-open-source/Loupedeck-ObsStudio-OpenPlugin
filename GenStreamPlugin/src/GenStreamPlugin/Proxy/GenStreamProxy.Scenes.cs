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

        void OnObsSceneListChanged(Object sender, EventArgs e)
        {
            //Rescan the scene list   
            if (this.IsAppConnected && Helpers.TryExecuteFunc(() => this.GetSceneList(), out var listInfo))
            {

                this.Scenes = (listInfo as OBSWebsocketDotNet.Types.GetSceneListInfo).Scenes;
                this.AppEvtSceneListChanged?.Invoke(sender, e);
            }
        }

        void OnObsSceneChanged(Object sender, String newScene)
        {
            try
            {
                var item = this.Scenes.Find(x => x.Name == newScene);
                if (item != null)
                {
                    this.CurrentScene = item;
                    this.AppEvtCurrentSceneChanged?.Invoke(sender, null);

                }
            }
            catch (Exception ex)
            {
                Tracer.Warning($"Exception {ex.Message} while changing scene");
            }

        }

        public void AppSwitchToScene(String newScene)
        {
            if (this.IsAppConnected && this.SceneInCurrentCollection(newScene))
            {
                Helpers.TryExecuteSafe(() =>
                {
                    this.SetCurrentScene(newScene);
                });
            }

        }
    }
}
