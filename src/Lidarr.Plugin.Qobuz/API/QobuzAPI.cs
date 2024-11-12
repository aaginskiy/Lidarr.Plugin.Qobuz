using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NLog;
using QobuzApiSharp.Service;

namespace NzbDrone.Plugin.Qobuz.API;

public class QobuzAPI
{
    public static QobuzAPI Instance { get; private set; }

    public static void Initialize(Logger logger)
    {
        if (Instance != null)
            return;
        Instance = new QobuzAPI();
    }

    private QobuzAPI()
    {
        Instance = this;
        _client = new();
    }

    public QobuzApiService Client => _client;

    private QobuzApiService _client;

    public string GetAPIUrl(string method, Dictionary<string, string> parameters = null)
    {
        /*parameters ??= new();
        parameters["sessionId"] = _client.ActiveUser?.SessionID ?? "";
        parameters["countryCode"] = _client.ActiveUser?.CountryCode ?? "";
        if (!parameters.ContainsKey("limit"))
            parameters["limit"] = "1000";

        StringBuilder stringBuilder = new("https://api.qobuz.com/v1/");
        stringBuilder.Append(method);
        for (var i = 0; i < parameters.Count; i++)
        {
            var start = i == 0 ? "?" : "&";
            var key = WebUtility.UrlEncode(parameters.ElementAt(i).Key);
            var value = WebUtility.UrlEncode(parameters.ElementAt(i).Value);
            stringBuilder.Append(start + key + "=" + value);
        }
        return stringBuilder.ToString();*/

        // TODO: getApiUrl
        return "";
    }
}

public enum AudioQuality
{
    MP3320 = 5,
    FLACLossless = 6,
    FLACHiRes24Bit96kHz = 7,
    FLACHiRes24Bit192Khz = 27,
}
