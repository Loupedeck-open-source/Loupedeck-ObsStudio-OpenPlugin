namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;
    using System.Collections.Generic;

    internal class UniversalStateSwitch : ActionEditorCommand
    {
        private static readonly SortedDictionary<String, GenericOnOffSwitch> _toggles = new SortedDictionary<String, GenericOnOffSwitch>();        

        //will be calling this method from toggle GenericOnOffSwitch' constructor.
        public static void RegisterToggle(GenericOnOffSwitch item) => _toggles[item.Name] = item;

        private Boolean TryGetToggle(String name, out GenericOnOffSwitch item)
        {
            item = !String.IsNullOrEmpty(name) && UniversalStateSwitch._toggles.ContainsKey(name) ? UniversalStateSwitch._toggles[name] : null;
            return item != null;
        }

        private const String ToggleActionSelector = "actionControlName";
        private const String ToggleStateSelector = "actionActsControlName";

        public UniversalStateSwitch()
        {

            this.DisplayName = "Set OBS Toggle On/Off";
            this.Description = "Sets a specific action toggle to go to a pre-defined state. This is particularly useful to ensure that a Toggle is set on or off in custom Multi-Action.";
            this.GroupName = "";

            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ToggleActionSelector, labelText: "OBS toggle:"));
            
            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ToggleStateSelector, labelText: "Set to state:"));
            this.ActionEditor.ListboxItemsRequested += this.OnActionEditorListboxItemsRequested;
            this.ActionEditor.ControlValueChanged += this.OnActionEditorControlValueChanged;
        }

        protected override Boolean OnLoad()
        {
            ObsStudioPlugin.Proxy.AppConnected += this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected += this.OnAppDisconnected;

            return true;
        }

        protected override Boolean OnUnload()
        {
            ObsStudioPlugin.Proxy.AppConnected -= this.OnAppConnected;
            ObsStudioPlugin.Proxy.AppDisconnected -= this.OnAppDisconnected;

            return true;
        }

        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase(ToggleActionSelector))
            {
                this.ActionEditor.ListboxItemsChanged(ToggleStateSelector);
            }
            else if (e.ControlName.EqualsNoCase(ToggleStateSelector))
            {
                e.ActionEditorState.SetDisplayName($"{e.ActionEditorState.GetControlValue(ToggleStateSelector)}");
            }
        }

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase(ToggleActionSelector))
            {
                foreach (var v in UniversalStateSwitch._toggles.Values)
                {
                    e.AddItem(v.Name, v.DisplayName, v.Description);
                    this.Plugin.Log.Info($"AE: Adding ('{v.Name}','{v.DisplayName}','{v.Description}')");
                }
            }
            else if (e.ControlName.EqualsNoCase(ToggleStateSelector))
            {
                var selectedAction = e.ActionEditorState.GetControlValue(ToggleActionSelector);

                // We get the control value from the 1st list box and generate the list accordingly
                if (!String.IsNullOrEmpty(selectedAction) && this.TryGetToggle(selectedAction, out var v))
                { 
                    e.AddItem(v.OffStateName, v.OffStateName, "Turns Off");
                    e.AddItem(v.OnStateName, v.OnStateName, "Turns On");
                }
                else
                {
                    this.Plugin.Log.Error($"AE: Cannot get toggle for control value for action {selectedAction}");
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
            return actionParameters.TryGetString(ToggleActionSelector, out var toggleName)
                && actionParameters.TryGetString(ToggleStateSelector, out var toggleValue)
                && this.TryGetToggle(toggleName, out var item)
                ? toggleValue == item.OffStateName ? item.OffStateImage.ToImage() : item.OnStateImage.ToImage()
                : null;
        }

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            if (  actionParameters.TryGetString(ToggleActionSelector, out var toggleName)
               && actionParameters.TryGetString(ToggleStateSelector, out var toggleValue)
               && this.TryGetToggle(toggleName, out var item))
            {
                this.Plugin.Log.Info($"Running command '{toggleValue}'");
                if(toggleValue == item.OffStateName)
                {
                    item.SwitchOff();
                }
                else
                {
                    item.SwitchOn();
                }
            }
            return true;
        }
    }
}
