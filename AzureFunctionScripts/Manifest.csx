
using System.Collections.Specialized;
using System.Globalization;
using System.Net;
using System.Net.Cache;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Net.Http.Formatting;


public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
	// return req.CreateResponse(HttpStatusCode.OK,  req.RequestUri.ToString());


	var queryString = new FormDataCollection(req.RequestUri.Query.TrimStart('?')).ReadAsNameValueCollection();
	
	string playbackUrl = queryString["playbackUrl"];
	string token = queryString["token"];
	
	if (string.IsNullOrEmpty(playbackUrl) || string.IsNullOrEmpty(token))
	{
		return req.CreateResponse(HttpStatusCode.BadRequest);
	}
	var hostPortion = req.RequestUri.GetComponents(UriComponents.HostAndPort, UriFormat.Unescaped);
	var scheme = req.RequestUri.Scheme;
	var fragmentBaseUrl = new UriBuilder(string.Format("{0}://{1}/api/Fragments", scheme, hostPortion)).Uri.ToString();
	
	var modifiedTopLeveLManifest = await GetTopLevelManifestForToken(playbackUrl, token, fragmentBaseUrl);

	
	
	var response = req.CreateResponse(HttpStatusCode.OK);
	
	var responseContent = new ByteArrayContent(Encoding.UTF8.GetBytes(modifiedTopLeveLManifest));
	responseContent.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.apple.mpegurl");
	responseContent.Headers.TryAddWithoutValidation("Access-Control-Allow-Origin", "*");
	responseContent.Headers.TryAddWithoutValidation("X-Content-Type-Options", "nosniff");
	
	response.Content = responseContent;
	return response;
}



static async Task<string> GetTopLevelManifestForToken(string topLeveLManifestUrl, string token, string fragmentBaseUrl)
{
	var httpRequest = (HttpWebRequest)WebRequest.Create(new Uri(topLeveLManifestUrl));
	httpRequest.CachePolicy = new HttpRequestCachePolicy(HttpRequestCacheLevel.NoCacheNoStore);
	httpRequest.Timeout = 30000;

	var httpResponse = httpRequest.GetResponse();
	try
	{
		var stream = await Task.Factory.StartNew(() => httpResponse.GetResponseStream());
		if (stream != null)
		{
			using (var reader = new StreamReader(stream))
			{
				const string qualityLevelRegex = @"(QualityLevels\(\d+\)/Manifest\(.+\))";
				var toplevelmanifestcontent = reader.ReadToEnd();
				var topLevelManifestBaseUrl = topLeveLManifestUrl.Substring(0, topLeveLManifestUrl.IndexOf(".ism", System.StringComparison.OrdinalIgnoreCase)) + ".ism";
				var urlEncodedTopLeveLManifestBaseUrl = WebUtility.UrlEncode(topLevelManifestBaseUrl);
				var urlEncodedToken = WebUtility.UrlEncode(token);
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
