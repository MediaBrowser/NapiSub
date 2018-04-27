using MediaBrowser.Common.Net;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Logging;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NapiSub.Core
{
    public static class NapiCore
    {
        public static async Task<string> GetHash(string path, CancellationToken cancellationToken, IFileSystem fileSystem, ILogger logger)
        {
            var buffer = new byte[10485760];
            logger.Info("Reading {0}", path);

            using (var stream =
                fileSystem.GetFileStream(path, FileOpenMode.Open, FileAccessMode.Read, FileShareMode.Read))
            {
                await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
            }

            string hash;
            using (var md5 = MD5.Create())
            {
                hash = ToHex(md5.ComputeHash(buffer));
            }

            logger.Info("Computed hash {0} of {1} for NapiSub", hash, path);
            return hash;
        }

        public static string ToHex(IReadOnlyCollection<byte> bytes)
        {
            var result = new StringBuilder(bytes.Count * 2);
            foreach (var t in bytes)
                result.Append(t.ToString("x2"));
            return result.ToString();
        }

        public static string GetSecondHash(string input)
        {
            if (input.Length != 32) return "";
            int[] idx = { 0xe, 0x3, 0x6, 0x8, 0x2 };
            int[] mul = { 2, 2, 5, 4, 3 };
            int[] add = { 0x0, 0xd, 0x10, 0xb, 0x5 };
            var b = "";
            for (var j = 0; j <= 4; j++)
            {
                var a = add[j];
                var m = mul[j];
                var i = idx[j];
                var t = a + int.Parse(input[i] + "", NumberStyles.HexNumber);
                var v = int.Parse(t == 31 ? input.Substring(t, 1) : input.Substring(t, 2), NumberStyles.HexNumber);
                var x = v * m % 0x10;
                b += x.ToString("x");
            }
            return b;
        }

        public static HttpRequestOptions CreateRequest(string hash)
        {
            if (hash == null) return null;

            var opts = new HttpRequestOptions
            {
                Url = Plugin.Instance.Configuration.NapiUrl,
                UserAgent = Plugin.Instance.Configuration.UserAgent,
                TimeoutMs = 10000, //10 seconds timeout
            };

            var dic = new Dictionary<string, string>
            {
                {
                    "mode", Plugin.Instance.Configuration.Mode
                },
                {
                    "client", Plugin.Instance.Configuration.ClientName
                },
                {
                    "client_ver", Plugin.Instance.Configuration.ClientVer
                },
                {
                    "downloaded_subtitles_id", hash
                },
                {
                    "downloaded_subtitles_txt", Plugin.Instance.Configuration.SubtitlesAsText
                },
                {
                    "downloaded_subtitles_lang", Plugin.Instance.Configuration.SubtitlesLang
                }
            };

            opts.SetPostData(dic);

            return opts;
        }
    }
}
