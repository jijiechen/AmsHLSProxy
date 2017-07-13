using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Cache;
using System.Text;
using System.Text.RegularExpressions;


namespace AmsHLSProxy
{
    /// <summary>
    /// Summary description for Manifest
    /// </summary>
    public class Fragments : IHttpHandler
    {

        public void ProcessRequest(HttpContext context)
        {
            string playbackUrl = context.Request.QueryString["playbackUrl"];
            string token = context.Request.QueryString["token"];

            if (string.IsNullOrEmpty(playbackUrl) || string.IsNullOrEmpty(token))
            {
                return;
            }

            if (token.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer".Length).Trim();
            }
            var collection = HttpUtility.ParseQueryString(token);
            var authToken = collection.ToQueryString().TrimStart('=').TrimEnd('=');
            string armoredAuthToken = HttpUtility.UrlEncode(authToken);

            var httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(playbackUrl));
            httpRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
            httpRequest.Timeout = 30000;
            var sourceManifestResponse = httpRequest.GetResponse();

            
            try
            {
                var stream = sourceManifestResponse.GetResponseStream();
                if (stream == null)
                {
                    return;
                }

                using (var reader = new StreamReader(stream))
                {
                    const string qualityLevelRegex = @"(QualityLevels\(\d+\))";
                    const string fragmentsRegex = @"(Fragments\([\w\d=-]+,[\w\d=-]+\))";
                    const string urlRegex = @"("")(https?:\/\/[\da-z\.-]+\.[a-z\.]{2,6}[\/\w \.-]*\/?[\?&][^&=]+=[^&=#]*)("")";

                    var baseUrl = playbackUrl.Substring(0, playbackUrl.IndexOf(".ism", System.StringComparison.OrdinalIgnoreCase)) + ".ism";
                    var toplevelManifest = reader.ReadToEnd();

                    var secondlevelManifest = Regex.Replace(toplevelManifest, urlRegex, string.Format(CultureInfo.InvariantCulture, "$1$2&token={0}$3", armoredAuthToken));
                    var match = Regex.Match(playbackUrl, qualityLevelRegex);
                    if (match.Success)
                    {
                        var qualityLevel = match.Groups[0].Value;
                        secondlevelManifest = Regex.Replace(secondlevelManifest, fragmentsRegex, m => string.Format(CultureInfo.InvariantCulture, baseUrl + "/" + qualityLevel + "/" + m.Value));
                    }

                    context.Response.ContentType = "application/vnd.apple.mpegurl";
                    var bytes = Encoding.UTF8.GetBytes(secondlevelManifest);
                    context.Response.AppendHeader("Content-Length", bytes.Length.ToString());
                    context.Response.BinaryWrite(Encoding.UTF8.GetBytes(secondlevelManifest));
                }
            }
            finally
            {
                sourceManifestResponse.Close();
            }
        }



        public bool IsReusable
        {
            get
            {
                return true;
            }
        }

    }


    static class QueryExtensions
    {
        public static string ToQueryString(this NameValueCollection collection)
        {
            IEnumerable<string> segments = from key in collection.AllKeys
                                           from value in collection.GetValues(key)
                                           select string.Format(CultureInfo.InvariantCulture, "{0}={1}", HttpUtility.UrlEncode(key), HttpUtility.UrlEncode(value));

            return string.Join("&", segments);
        }
    }
}