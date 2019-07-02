using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Serialization;
using NapiSub.Configuration;
using System;
using System.IO;
using MediaBrowser.Model.Drawing;

namespace NapiSub
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasThumbImage
    {
        public Plugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer) : base(applicationPaths,
            xmlSerializer)
        {
            Instance = this;
        }

        public override Guid Id => new Guid("C657D302-D19A-40F3-A639-C5F382D43A62");

        public override string Name => "NapiSub";

        public override string Description => "Download subtitles for Movies and TV Shows using napiprojekt.pl";

        public static Plugin Instance { get; private set; }

        public Stream GetThumbImage()
        {
            var type = GetType();
            return type.Assembly.GetManifestResourceStream(type.Namespace + ".thumb.png");
        }

        public ImageFormat ThumbImageFormat
        {
            get
            {
                return ImageFormat.Png;
            }
        }
    }
}
