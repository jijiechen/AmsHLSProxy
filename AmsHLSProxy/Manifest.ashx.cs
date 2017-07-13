using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace AmsHLSProxy
{
    /// <summary>
    /// Summary description for Manifest1
    /// </summary>
    public class Manifest : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string playbackUrl = context.Request.QueryString["playbackUrl"];
            string token = context.Request.QueryString["token"];
            if (string.IsNullOrEmpty(playbackUrl) || string.IsNullOrEmpty(token))
            {
                return;
            }


            var hostPortion = context.Request.Url.GetComponents( UriComponents.HostAndPort, UriFormat.Unescaped);
            var scheme = context.Request.Url.Scheme;
            var fragmentBaseUrl = String.Format("{0}://{1}/Fragments.ashx", scheme, hostPortion);

            var modifiedTopLeveLManifest = GetTopLevelManifestForToken(playbackUrl, token, fragmentBaseUrl);
            
            context.Response.ContentType = "application/vnd.apple.mpegurl";
            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");
            context.Response.AppendHeader("X-Content-Type-Options", "nosniff");
           // context.Response.AppendHeader("Cache-Control", "max-age=86400");

            var bytes = Encoding.UTF8.GetBytes(modifiedTopLeveLManifest);
            context.Response.AppendHeader("Content-Length", bytes.Length.ToString());
            context.Response.BinaryWrite(bytes);
        }


        public string GetTopLevelManifestForToken(string topLeveLManifestUrl, string token, string fragmentBaseUrl)
        {
            var httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(topLeveLManifestUrl));
            httpRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            httpRequest.Timeout = 30000;
            var httpResponse = httpRequest.GetResponse();

            try
            {
                var stream = httpResponse.GetResponseStream();
                if (stream != null)
                {
                    using (var reader = new StreamReader(stream))
                    {
                        const string qualityLevelRegex = @"(QualityLevels\(\d+\)/Manifest\(.+\))";

                        var toplevelmanifestcontent = reader.ReadToEnd();

                        var topLevelManifestBaseUrl = topLeveLManifestUrl.Substring(0, topLeveLManifestUrl.IndexOf(".ism", System.StringComparison.OrdinalIgnoreCase)) + ".ism";
                        var urlEncodedTopLeveLManifestBaseUrl = HttpUtility.UrlEncode(topLevelManifestBaseUrl);
                        var urlEncodedToken = HttpUtility.UrlEncode(token);

                        var newContent = Regex.Replace(toplevelmanifestcontent,
                                                      qualityLevelRegex,
                                                      string.Format(CultureInfo.InvariantCulture,
                                                           "{0}?playbackUrl={1}/$1&token={2}",
                                                           fragmentBaseUrl,
                                                           urlEncodedTopLeveLManifestBaseUrl,
                                                           urlEncodedToken));

                        return newContent;
                    }
                }
            }
            finally
            {
                httpResponse.Close();
            }
            return null;
        }

        public bool IsReusable
        {
            get
            {
                return true;
            }
        }
    }
}