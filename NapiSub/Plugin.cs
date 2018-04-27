using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using NapiSub.Configuration;
using System;

namespace NapiSub
{
    public class Plugin : BasePlugin<PluginConfiguration>
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths,
            xmlSerializer)
        {
            Instance = this;
        }

        public override Guid Id => new Guid("6F9A84BF-CB2F-42C3-9F07-4037956F9A02");

        public override string Name => "NapiSub";

        public override string Description => "Download subtitles for Movies and TV Shows using napiprojekt.pl";

        public static Plugin Instance { get; private set; }
    }
}
