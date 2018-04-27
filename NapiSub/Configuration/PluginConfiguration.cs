using MediaBrowser.Model.Plugins;

namespace NapiSub.Configuration
{
    public class PluginConfiguration : BasePluginConfiguration
    {
        public string NapiUrl = "http://napiprojekt.pl/api/api-napiprojekt3.php";
        public string UserAgent = "Mozilla/5.0";
        public string Mode = "1";
        public string ClientName = "NapiProjektPython";
        public string ClientVer = "0.1";
        public string SubtitlesAsText = "1"; //1 = text, 0 = zip (zip password = "iBlm8NTigvru0Jr0")
        public string SubtitlesLang = "PL";
    }
}
