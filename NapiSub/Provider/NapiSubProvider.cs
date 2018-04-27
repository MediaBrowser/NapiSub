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

namespace NapiSub.Provider
{
    public class NapiSubProvider : ISubtitleProvider, IHasOrder
    {
        private readonly IHttpClient _httpClient;
        private readonly ILogger _logger;
        private readonly IFileSystem _fileSystem;

        public NapiSubProvider(ILogger logger, IFileSystem fileSystem, IHttpClient httpClient)
        {
            _logger = logger;
            _fileSystem = fileSystem;
            _httpClient = httpClient;
        }

        public async Task<SubtitleResponse> GetSubtitles(string id, CancellationToken cancellationToken)
        {
            var opts = NapiCore.CreateRequest(id);
            _logger.Info("Requesting {0}", opts.Url);

            try
            {
                using (var response = await _httpClient.Post(opts).ConfigureAwait(false))
                {
                    using (var reader = new StreamReader(response.Content))
                    {
                        var xml = await reader.ReadToEndAsync().ConfigureAwait(false);
                        var status = XmlParser.GetStatusResult(xml);

                        if (status != null && status == "success")
                        {
                            var subtitlesBase64 = XmlParser.GetSubtitlesBase64(xml);
                            var stream = XmlParser.GetSubtitlesStream(subtitlesBase64);
                            var subRip = SubtitlesConverter.ConvertToSubRipStream(stream);

                            if (subRip != null)
                            {
                                return new SubtitleResponse
                                {
                                    Format = "srt",
                                    Language = "PL",
                                    Stream = subRip
                                };
                            }
                        }
                    }
                }

                _logger.Info("No subtitles downloaded");
                return new SubtitleResponse();
            }
            catch (HttpException ex)
            {
                if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound) throw;
                _logger.Debug("ERROR");
                return new SubtitleResponse();
            }
        }

        public async Task<IEnumerable<RemoteSubtitleInfo>> Search(SubtitleSearchRequest request,
            CancellationToken cancellationToken)
        {
            if (request.TwoLetterISOLanguageName != "pl") return new List<RemoteSubtitleInfo>();

            var hash = await NapiCore.GetHash(request.MediaPath, cancellationToken, _fileSystem, _logger);
            var opts = NapiCore.CreateRequest(hash);

            try
            {
                using (var response = await _httpClient.Post(opts).ConfigureAwait(false))
                {
                    using (var reader = new StreamReader(response.Content))
                    {
                        var result = await reader.ReadToEndAsync().ConfigureAwait(false);
                        var status = XmlParser.GetStatusResult(result);

                        if (status != null && status == "success")
                        {
                            _logger.Info("Subtitles found by NapiSub");

                            return new List<RemoteSubtitleInfo>
                            {
                                new RemoteSubtitleInfo
                                {
                                    IsHashMatch = true,
                                    ProviderName = Name,
                                    Id = hash,
                                    Name = "A subtitle matched by hash"
                                }
                            };
                        }
                    }

                    _logger.Info("No subtitles found by NapiSub");
                    return new List<RemoteSubtitleInfo>();
                }
            }
            catch (HttpException ex)
            {
                if (!ex.StatusCode.HasValue || ex.StatusCode.Value != HttpStatusCode.NotFound) throw;
                _logger.Debug("ERROR");
                return new List<RemoteSubtitleInfo>();
            }
        }

        public string Name => "NapiSub";

        public IEnumerable<VideoContentType> SupportedMediaTypes =>
            new List<VideoContentType> { VideoContentType.Episode, VideoContentType.Movie };

        public int Order => 1;
    }
}