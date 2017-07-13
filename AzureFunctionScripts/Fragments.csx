using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Net.Http.Formatting;

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req /*, TraceWriter log*/)
{
	var queryString = new FormDataCollection(req.RequestUri.Query.TrimStart('?')).ReadAsNameValueCollection();
	
		string playbackUrl = queryString["playbackUrl"];
            string token = queryString["token"];
            if (string.IsNullOrEmpty(playbackUrl) || string.IsNullOrEmpty(token))
            {
                return req.CreateResponse(HttpStatusCode.BadRequest);
            }
			
            if (token.StartsWith("Bearer", StringComparison.OrdinalIgnoreCase))
            {
                token = token.Substring("Bearer".Length).Trim();
            }
            var collection = new FormDataCollection(token).ReadAsNameValueCollection();
            var authToken = Extensions.ToQueryString(collection).TrimStart('=').TrimEnd('=');
            string armoredAuthToken = WebUtility.UrlEncode(authToken);

	var transformedFragments = await GetTopLevelManifestForToken(playbackUrl, token);
	var response = req.CreateResponse(HttpStatusCode.OK);

	var responseContent = new ByteArrayContent(Encoding.UTF8.GetBytes(transformedFragments));
	responseContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.apple.mpegurl");
	responseContent.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", "*");
	responseContent.Headers.TryAddWithoutValidation("X-Content-Type-Options", "nosniff");

	response.Content = responseContent;
	return response;
}



static async Task<string> GetTopLevelManifestForToken(string playbackUrl, string token)
{
	var httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(playbackUrl));
	httpRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
	httpRequest.Timeout = 30000;

	var sourceManifestResponse = httpRequest.GetResponse();

	try
	{
		var stream = await Task.Factory.StartNew(() => sourceManifestResponse.GetResponseStream());
		if (stream == null)
		{
			return null;
		}

		using (var reader = new StreamReader(stream))
		{
			const string qualityLevelRegex = @"(QualityLevels\(\d+\))";
			const string fragmentsRegex = @"(Fragments\([\w\d=-]+,[\w\d=-]+\))";
			const string urlRegex = @"("")(https?:\/\/[\da-z\.-]+\.[a-z\.]{2,6}[\/\w \.-]*\/?[\?&][^&=]+=[^&=#]*)("")";
			var baseUrl = playbackUrl.Substring(0, playbackUrl.IndexOf(".ism", System.StringComparison.OrdinalIgnoreCase)) + ".ism";
			var toplevelManifest = reader.ReadToEnd();
			var secondlevelManifest = Regex.Replace(toplevelManifest, urlRegex, string.Format(CultureInfo.InvariantCulture, "$1$2&token={0}$3", token));
			var match = Regex.Match(playbackUrl, qualityLevelRegex);
			if (match.Success)
			{
				var qualityLevel = match.Groups[0].Value;
				secondlevelManifest = Regex.Replace(secondlevelManifest, fragmentsRegex, m => string.Format(CultureInfo.InvariantCulture, baseUrl + "/" + qualityLevel + "/" + m.Value));
			}
			return secondlevelManifest;
		}
	}
	finally
	{
		sourceManifestResponse.Close();
	}
}

public static class Extensions
{
	public static string ToQueryString(NameValueCollection collection)
	{
		IEnumerable<string> segments = from key in collection.AllKeys
									   from value in collection.GetValues(key)
									   select string.Format(CultureInfo.InvariantCulture, "{0}={1}", WebUtility.UrlEncode(key), WebUtility.UrlEncode(value));
		return string.Join("&", segments);
	}
}