using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using NLog;
using NzbDrone.Core.Indexers.Qobuz;
using QobuzApiSharp.Models.User;
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
        // should automatically retrieve the app id and secret
        // TODO: may want to add an optional custom app id and secret setting
        _client = new();
    }

    public QobuzApiService Client => _client;

    private QobuzApiService _client;

    public Login Login => _login;

    private Login _login;

    public void PickSignInFromSettings(QobuzIndexerSettings settings, Logger logger)
    {
        bool ep = !string.IsNullOrEmpty(settings.Email) && !string.IsNullOrEmpty(settings.MD5Password);
        bool it = !string.IsNullOrEmpty(settings.UserID) && !string.IsNullOrEmpty(settings.UserAuthToken);

        try
        {
            if (ep)
                LoginWithEmail(settings.Email, settings.MD5Password);
            else if (it)
                LoginWithToken(settings.UserID, settings.UserAuthToken);
        }
        catch (Exception ex)
        {
            logger.Error($"Qobuz login failed:\n{ex}");
        }
    }

    public void LoginWithEmail(string email, string password)
    {
        _login = _client.LoginWithEmail(email, password);
    }

    public void LoginWithToken(string userId, string userAuthToken)
    {
        _login = _client.LoginWithToken(userId, userAuthToken);
    }

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
