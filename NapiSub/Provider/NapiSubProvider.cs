using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using NapiSub.Core;
using NapiSub.Helpers;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System;
using MediaBrowser.Model.Globalization;

namespace NapiSub.Provider
{
    public class NapiSubProvider : ISubtitleProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;
        private ILocalizationManager _localizationManager;

        public NapiSubProvider(ILogger logger, IFileSystem fileSystem, IHttpClient httpClient, ILocalizationManager localizationManager)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
            _localizationManager = localizationManager;
        }

        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var index = id.IndexOf('_');
            var language = id.Substring(0, index);
            var hash = id.Substring(index + 1);

            var opts = NapiCore.CreateRequest(hash, language.ToUpperInvariant());
            _logger.Info($"Requesting {opts.Url}");

            using (var response = await _httpClient.Post(opts).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(response.Content))
                {
                    var xml = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var status = XmlParser.GetStatusFromXml(xml);

                    if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                    {
                        var subtitlesBase64 = XmlParser.GetSubtitlesBase64(xml);
                        var stream = XmlParser.GetSubtitlesStream(subtitlesBase64);
                        var subRip = SubtitlesConverter.ConvertToSubRipStream(stream);

                        if (subRip != null)
                        {
                            return new SubtitleResponse
                            {
                                Format = "srt",
                                Language = language,
                                Stream = subRip
                            };
                        }
                    }
                    else
                    {
                        throw new Exception("Error downloading subtitles: " + status);
                    }
                }
            }

            throw new Exception("No subtitles downloaded");
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            var language = _localizationManager.FindLanguageInfo(request.Language.AsSpan())?.TwoLetterISOLanguageName ?? request.Language;

            if (!string.Equals(language, "pl", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<RemoteSubtitleInfo>();
            }

            var hash = await NapiCore.GetHash(request.MediaPath, cancellationToken, _fileSystem, _logger).ConfigureAwait(false);
            var opts = NapiCore.CreateRequest(hash, language);

            using (var response = await _httpClient.Post(opts).ConfigureAwait(false))
            {
                using (var reader = new StreamReader(response.Content))
                {
                    var xml = await reader.ReadToEndAsync().ConfigureAwait(false);
                    var status = XmlParser.GetStatusFromXml(xml);

                    if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Info("Subtitles found by NapiSub");

                        return new List<RemoteSubtitleInfo>
                            {
                                new RemoteSubtitleInfo
                                {
                                    IsHashMatch = true,
                                    ProviderName = Name,
                                    Id = language + "_" + hash,
                                    Name = "A subtitle matched by hash",
                                    Language = language,
                                    Format = "srt"
                                }
                            };
                    }
                }

                _logger.Info("No subtitles found by NapiSub");
                return new List<RemoteSubtitleInfo>();
            }
        }

        public string Name => "NapiSub";

        public IEnumerable<VideoContentType> SupportedMediaTypes =>
            new List<VideoContentType> { VideoContentType.Episode, VideoContentType.Movie };

        public int Order => 1;
    }
}