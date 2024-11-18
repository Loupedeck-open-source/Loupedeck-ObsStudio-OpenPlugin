namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.Design;
    using System.Linq;

    internal class SourceTransformSwitch : ActionEditorCommand
    {


        private const String ControlSceneSelector = "sceneSelector";
        private const String ControlSourceSelector = "sourceSelector";
        private const String ControlTransformType  = "transformType";
        private const String ControlApplyToAllScenes = "useInAllScenes";
        private readonly String TransformType_Left = "move left";
        private readonly String TransformType_Right = "move right";
        private readonly String TransformType_Top = "move top";
        private readonly String TransformType_bottom = "move bottom";
        private readonly String TransformType_zoomIn = "zoomIn";
        private readonly String TransformType_zoomOut = "zoomOut";
        

        public SourceTransformSwitch()
        {
            this.DisplayName = "Source Transform";
            this.Description = "aaaaaaaaSets the visibility of a specific source to the ON or OFF state. This is particularly useful when creating Multi-Actions (can be found in Custom category) and trying to target a specific state of visibility.";
            this.GroupName = "";
            
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ControlSceneSelector, labelText: "Scene:"/*,"Select Scene name"*/)
                    .SetRequired()
                );
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ControlSourceSelector, labelText: "Source:"/*, "Select Source name"*/)
                .SetRequired()
                );
            this.ActionEditor.AddControlEx(parameterControl:
                new ActionEditorListbox(name: ControlTransformType, labelText: "Type:"/*, "Controls, what state source needs to be in"*/)
                .SetRequired()
                );

            
            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;
        }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            ObsStudioPlugin.Proxy.AppEvtSceneListChanged += this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged += this.OnSceneListChanged;

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;
            ObsStudioPlugin.Proxy.AppEvtSceneListChanged -= this.OnSceneListChanged;
            ObsStudioPlugin.Proxy.AppEvtSceneNameChanged -= this.OnSceneListChanged;

            return true;
        }

        private void OnSceneListChanged(Object sender, EventArgs e) => this.ActionEditor.ListboxItemsChanged(ControlSceneSelector);

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {

            if (e.ControlName.EqualsNoCase(ControlSceneSelector))
            {
                this.ActionEditor.ListboxItemsChanged(ControlSourceSelector);
                this.Plugin.Log.Info($"Regenerating sources list");
            }
            //We set display name ONLY if all controls are set!
            else if( 
                    (
                        e.ControlName.EqualsNoCase(ControlSourceSelector)
                        || e.ControlName.EqualsNoCase(ControlTransformType)
                        || e.ControlName.EqualsNoCase(ControlApplyToAllScenes)
                    )
                   &&
                   (
                    !(e.ActionEditorState.GetControlValue(ControlSceneSelector).IsNullOrEmpty()
                   || e.ActionEditorState.GetControlValue(ControlSourceSelector).IsNullOrEmpty())
                   )
               )
            {
                
                var sourceName = "Unknown";
                var sceneName = "Unknown";
                if (SceneItemKey.TryParse(e.ActionEditorState.GetControlValue(ControlSourceSelector), out var parsed))
                {
                    sourceName = parsed.SourceName;
                    sceneName = parsed.Scene;
                }
                e.ActionEditorState.SetDisplayName($"{sourceName} ({sceneName})");
            }
            
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            /*
             * This does not work (yet)
             * e.ActionEditorState.SetEnabled(ControlSceneSelector, ObsStudioPlugin.Proxy.IsAppConnected);
             * e.ActionEditorState.SetEnabled(ControlSourceSelector, ObsStudioPlugin.Proxy.IsAppConnected);
             * e.ActionEditorState.SetEnabled(ControlIsSourceVisible, ObsStudioPlugin.Proxy.IsAppConnected);
            */

            if (e.ControlName.EqualsNoCase(ControlTransformType))
            {
                e.AddItem(this.TransformType_Left, this.TransformType_Left, $"Moves Source to left");
                e.AddItem(this.TransformType_Right, this.TransformType_Right, $"Moves Source to right");
                e.AddItem(this.TransformType_Top, this.TransformType_Top, $"Moves Source to top");
                e.AddItem(this.TransformType_bottom, this.TransformType_bottom, $"Moves Source to bottom");
                e.AddItem(this.TransformType_zoomIn, this.TransformType_zoomIn, $"Zooms_in");
                e.AddItem(this.TransformType_zoomOut, this.TransformType_zoomOut, $"Zooms_out");
                
            }
            else if (e.ControlName.EqualsNoCase(ControlSceneSelector))
            {
                if (!ObsStudioPlugin.Proxy.IsAppConnected)
                {
                    e.AddItem("N/A", "No data", "Not connected to OBS");
                }
                else
                {
                    this.Plugin.Log.Info($"SVS: Adding scenes to {ControlSceneSelector} control");

                    //Unique scenes (those that have sources)
                    var unique_scenes = new HashSet<String>();

                    foreach (var item in ObsStudioPlugin.Proxy.AllSceneItems)
                    {
                        if( ObsStudioPlugin.Proxy.CurrentSceneCollection.EqualsNoCase(item.Value.CollectionName)
                        && !unique_scenes.Contains(item.Value.SceneName))
                        {
                            unique_scenes.Add(item.Value.SceneName);
                            e.AddItem(item.Value.SceneName, item.Value.SceneName, $"Scene {item.Value.SceneName}");
                        }
                    }
                }
            }
            else if (e.ControlName.EqualsNoCase(ControlSourceSelector))
            {
                if (!ObsStudioPlugin.Proxy.IsAppConnected)
                {
                    //To ensure sources list is empty so that control cannot be saved.
                    return; 
                }
                var selectedScene = e.ActionEditorState.GetControlValue(ControlSceneSelector);

                if (!String.IsNullOrEmpty(selectedScene))
                {
                    this.Plugin.Log.Info($"SVS: Adding sources for {selectedScene}");
                    foreach(var item in ObsStudioPlugin.Proxy.AllSceneItems)
                    {
                        if (ObsStudioPlugin.Proxy.CurrentSceneCollection.EqualsNoCase(item.Value.CollectionName)
                            && selectedScene.EqualsNoCase(item.Value.SceneName))
                        {
                            e.AddItem(item.Key, item.Value.SourceName, $"Source {item.Value.SourceName}");
                        }
                    }
                }
                else
                {
                    e.AddItem("N/A", "No Scene Selected", "Select Scene first");
                }
            }
            else
            {
                this.Plugin.Log.Error($"Unexpected control name '{e.ControlName}'");
            }
        }

        private void OnAppConnected(Object sender, EventArgs e)
        {
        }

        private void OnAppDisconnected(Object sender, EventArgs e)
        {
        }

        protected override BitmapImage GetCommandImage(ActionEditorActionParameters actionParameters, Int32 imageWidth, Int32 imageHeight)
        {
            var imageName = SourceVisibilityCommand.IMGSceneInaccessible;

            if (actionParameters.TryGetString(ControlSourceSelector, out var key) && SceneItemKey.TryParse(key, out var parsed))
            {

                // TODO: FIX ME: Provide propper images!
                //var sourceVisible = actionParameters.TryGetString(ControlTransformType, out var vis) && vis == this.Visibility_Show;
                var sourceVisible = actionParameters.TryGetString(ControlTransformType, out var vis) && vis == "show";

                imageName = parsed.Collection != ObsStudioPlugin.Proxy.CurrentSceneCollection || !ObsStudioPlugin.Proxy.AllSceneItems.ContainsKey(key)
                    ? SourceVisibilityCommand.IMGSceneInaccessible
                    : sourceVisible 
                        ? SourceVisibilityCommand.IMGSceneSelected 
                        : SourceVisibilityCommand.IMGSceneUnselected;
            }
            else
            {
                this.Plugin.Log.Warning($"Cannot retreive selected source name from '{actionParameters}'");
            }

            return EmbeddedResources.ReadBinaryFile(ObsStudioPlugin.ImageResPrefix + imageName).ToImage();
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (actionParameters.TryGetString(ControlSourceSelector, out var key))
            {
                actionParameters.TryGetString(ControlTransformType, out var action);
                ObsStudioPlugin.Proxy.TransformSoure(key, action);
                //ObsStudioPlugin.Proxy.AppSceneItemVisibilityToggle(key, true, setVisible, actionParameters.TryGetBoolean(ControlApplyToAllScenes, out var applyToAllScenes) && applyToAllScenes);

                return true;
            }
            else
            {
                this.Plugin.Log.Warning($"Run: Cannot retreive selected source name from '{actionParameters}'");
                return false;
            }
        }
    }
}
