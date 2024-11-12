using System;
using NLog;
using NzbDrone.Common.Cache;
using NzbDrone.Common.Http;
using NzbDrone.Core.Configuration;
using NzbDrone.Core.Download.Clients.Qobuz;
using NzbDrone.Core.Parser;
using NzbDrone.Plugin.Qobuz.API;

namespace NzbDrone.Core.Indexers.Qobuz
{
    public class Qobuz : HttpIndexerBase<QobuzIndexerSettings>
    {
        public override string Name => "Qobuz";
        public override string Protocol => nameof(QobuzDownloadProtocol);
        public override bool SupportsRss => false;
        public override bool SupportsSearch => true;
        public override int PageSize => 100;
        public override TimeSpan RateLimit => new TimeSpan(0);

        private readonly IQobuzProxy _qobuzProxy;

        public Qobuz(IQobuzProxy qobuzProxy,
            IHttpClient httpClient,
            IIndexerStatusService indexerStatusService,
            IConfigService configService,
            IParsingService parsingService,
            Logger logger)
            : base(httpClient, indexerStatusService, configService, parsingService, logger)
        {
            _qobuzProxy = qobuzProxy;
        }

        public override IIndexerRequestGenerator GetRequestGenerator()
        {
            if (!string.IsNullOrEmpty(Settings.Email) && !string.IsNullOrEmpty(Settings.MD5Password))
            {
                QobuzAPI.Initialize(_logger);
                try
                {
                    QobuzAPI.Instance.LoginWithEmail(Settings.Email, Settings.MD5Password);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Qobuz login failed:\n{ex}");
                }
            }
            else
                return null;

            return new QobuzRequestGenerator()
            {
                Settings = Settings,
                Logger = _logger
            };
        }

        public override IParseIndexerResponse GetParser()
        {
            return new QobuzParser()
            {
                Settings = Settings
            };
        }
    }
}
