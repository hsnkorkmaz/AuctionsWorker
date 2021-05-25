using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Resources;
using System.Threading;
using System.Threading.Tasks;
using ArgentPonyWarcraftClient;
using MongoDB.Driver.Core.Configuration;

namespace AuctionsWorker
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private HttpClient _client;
        private WarcraftClient _wow;
        private MongoCRUD _mongo;
        private int timeout = 20 * 1000;

        public Worker(ILogger<Worker> logger)
        {
            _logger = logger;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _client = new HttpClient();
            _wow = new WarcraftClient(Properties.Resources.wowClientId, Properties.Resources.wowClientSecret);
            _mongo = new MongoCRUD("Blizzard", Properties.Resources.mongoDbConnectionString);
            return base.StartAsync(cancellationToken);
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            _client.Dispose();
            _logger.LogInformation("The service has been stopped...");
            return base.StopAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                if (CheckTime())
                {
                    _logger.LogInformation($"Worker running at: {DateTimeOffset.Now}");
                    await SaveAllAuctions();
                    _logger.LogInformation($"Worker finished at: {DateTimeOffset.Now}");
                }
                else
                {
                    //delay 20 seconds if the time is not right
                    await Task.Delay(20 * 1000, stoppingToken);
                }
            }
        }

        private static bool CheckTime()
        {
            return DateTime.Now.Minute == 3;
        }

        public async Task SaveAuction(int realmId, string dateIdentifier, Region region, string regionType, Locale lang)
        {
            _logger.LogInformation($"Start Get Auction Data: {DateTimeOffset.Now}");
            var auctions = _wow.GetAuctionsAsync(realmId, $"dynamic-{regionType}", region, lang);
            if (await Task.WhenAny(auctions, Task.Delay(timeout)) == auctions)
            {
                _logger.LogInformation($"End Get Auction Data: {DateTimeOffset.Now}");

                if (auctions.Result.Success)
                {
                    _logger.LogInformation($"Start Drop Collection: {DateTimeOffset.Now}");
                    await _mongo.DropCollection($"{dateIdentifier}-{regionType}{realmId}");
                    _logger.LogInformation($"End Drop Collection: {DateTimeOffset.Now}");

                    _logger.LogInformation($"Start InsertMany: {DateTimeOffset.Now}");
                    await _mongo.InsertManyAsync($"{dateIdentifier}-{regionType}{realmId}", auctions.Result.Value.Auctions.ToList());
                    _logger.LogInformation($"End InsertMany: {DateTimeOffset.Now}");
                }
                else
                {
                    _logger.LogInformation($"Could not get the Auction Data: {DateTimeOffset.Now}");
                    _logger.LogInformation($"Auction Error: {auctions.Result.Error.Detail}");

                    _logger.LogInformation($"TRYING AGAIN: {DateTimeOffset.Now}");
                    await SaveAuction(realmId, dateIdentifier, region, regionType, lang);
                }
            }
            else
            {
                _logger.LogInformation($"AUCTION DATA TIMEOUT TRYING AGAIN: {DateTimeOffset.Now}");
                await SaveAuction(realmId, dateIdentifier, region, regionType, lang);
            }
        }

        public async Task SaveAllAuctions()
        {
            string dateIdentifier = $"{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}-{DateTime.Now.Hour}";

            _logger.LogInformation($"Start Realm Data: {DateTimeOffset.Now}");
            var connectedRealmsEu = await _wow.GetConnectedRealmsIndexAsync("dynamic-eu", Region.Europe, Locale.en_GB);
            var connectedRealmsUs = await _wow.GetConnectedRealmsIndexAsync("dynamic-us", Region.US, Locale.en_US);
            _logger.LogInformation($"End Realm Data: {DateTimeOffset.Now}");
            
            //eu realms
            foreach (var realm in connectedRealmsEu.Value.ConnectedRealms)
            {
                var realmId = Convert.ToInt32(realm.Href.ToString().Replace("https://eu.api.blizzard.com/data/wow/connected-realm/", "").Replace("?namespace=dynamic-eu", ""));
                await SaveAuction(realmId, dateIdentifier, Region.Europe, "eu", Locale.en_GB);
            }

            //us realms
            foreach (var realm in connectedRealmsUs.Value.ConnectedRealms)
            {
                var realmId = Convert.ToInt32(realm.Href.ToString().Replace("https://us.api.blizzard.com/data/wow/connected-realm/", "").Replace("?namespace=dynamic-us", ""));
                await SaveAuction(realmId, dateIdentifier, Region.US, "us", Locale.en_US);
            }
        }
    }
}
