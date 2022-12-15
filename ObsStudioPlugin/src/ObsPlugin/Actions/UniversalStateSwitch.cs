namespace Loupedeck.ObsStudioPlugin.Actions
{
    using System;

    internal class UniversalStateSwitch : ActionEditorCommand
    {
        private const String ActionNameControlName = "ActionControlName";
        private const String ActionActsControlName = "ActionActsControlName";

        public UniversalStateSwitch()
        {

            this.DisplayName = "Universal switch";
            this.Description = "Sets whatever to whatever state you want";
            this.GroupName = "";


            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ActionNameControlName, labelText: "What to switch:"));
            
            this.ActionEditor.AddControl(
                new ActionEditorListbox(name: ActionActsControlName, labelText: "Select to what state to switch:"));
/*
            this.ActionEditor.AddControl(
                new ActionEditorCheckbox(name: ActionActsControlName, labelText: "Select to what state to switch:")
                    .SetDefaultValue(true)
                );
*/
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

        protected override String GetCommandDisplayName(ActionEditorActionParameters actionParameters)
        {
            if( !actionParameters.TryGetString(ActionNameControlName, out var p1val))
            {
                p1val = "Unknown";
            }

            if (!actionParameters.TryGetString(ActionActsControlName, out var p2val))
            {
                p2val = "Unknown";
            }

            return "Ctrl -" + p1val + "  " + p2val;


        }


        private void OnActionEditorControlValueChanged(Object sender, ActionEditorControlValueChangedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase(ActionNameControlName))
            {
                this.ActionEditor.ListboxItemsChanged(ActionActsControlName);
            }
            else if (e.ControlName.EqualsNoCase(ActionActsControlName))
            {
                //var listbox2ControlValue = e.ActionEditorState.GetControlValue(ActionActsControlName);
                e.ActionEditorState.SetDisplayName($"What- {e.ActionEditorState.GetControlValue(ActionNameControlName)} - {e.ActionEditorState.GetControlValue(ActionActsControlName)} ");
            }
        }

        class ToggleActionDescription
        {
            public String ToggleName;
            public String ToggleDescription;
            public String OnCommand;
            public String OffCommand;
            public ToggleActionDescription(String name, String description, String onCmd, String offCmd)
            {
                this.ToggleName = name;
                this.ToggleDescription = description;
                this.OnCommand = onCmd;
                this.OffCommand = offCmd;
            }
            //TODO: Add run command
        };

        private static readonly ToggleActionDescription[] allActions =
                new ToggleActionDescription[] {
                        new ToggleActionDescription( "a1", "b1", "c1", "d1" ),
                        new ToggleActionDescription( "a2", "b2", "c2", "d2" ),
                        new ToggleActionDescription( "a3", "b3", "c3", "d3" ),
                        new ToggleActionDescription( "a4", "b4", "c4", "d4" ) };

        private void OnActionEditorListboxItemsRequested(Object sender, ActionEditorListboxItemsRequestedEventArgs e)
        {
            if (e.ControlName.EqualsNoCase(ActionNameControlName))
            {
                foreach (var v in allActions)
                {
                    e.AddItem(v.ToggleName, v.ToggleName, v.ToggleDescription);
                }
            }
            else if (e.ControlName.EqualsNoCase(ActionActsControlName))
            {
                // We get the control value from the 1st list box and generate the list accordingly
                //FIXME REPLACE ALLACTIONS WITH THE PROPER CODE ON THE PROXY LEVEL, with events and on-and off- commands
                var x = e.ActionEditorState.GetControlValue(ActionNameControlName);
                foreach (var v in allActions)
                {
                    if (v.ToggleName == x)
                    {
                        e.AddItem(v.OffCommand, v.OffCommand, "Turns it off");
                        e.AddItem(v.OnCommand, v.OnCommand, "Turns it on");

                    }
                }

            }


        }



        private void OnAppConnected(Object sender, EventArgs e)
        {
        }
        private void OnAppDisconnected(Object sender, EventArgs e)
        {
        }

        //protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize) => (this.Plugin as ObsStudioPlugin).GetPluginCommandImage(imageSize, IMGAction);

        protected override Boolean RunCommand(ActionEditorActionParameters actionParameters)
        {
            this.Plugin.Log.Info($"Run command for {actionParameters}");
            return false;
        }

    }
}
