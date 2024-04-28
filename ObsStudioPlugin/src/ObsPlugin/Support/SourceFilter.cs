using System;

namespace Loupedeck.ObsStudioPlugin
{

    internal class SourceFilter
    {
        public String FilterName;
        public Boolean Enabled;
        public SourceFilter(String name, Boolean enabled)
        {
            this.FilterName = name;
            this.Enabled = enabled;
        }
    }

}
