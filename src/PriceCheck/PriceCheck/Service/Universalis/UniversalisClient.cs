﻿using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using DalamudPluginCommon;
using Newtonsoft.Json;

namespace PriceCheck
{
    /// <inheritdoc />
    public class UniversalisClient : IUniversalisClient
    {
        private const string Endpoint = "https://staging.universalis.app/api/";
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="UniversalisClient"/> class.
        /// </summary>
        /// <param name="priceCheckPlugin">price check plugin.</param>
        public UniversalisClient(IPriceCheckPlugin priceCheckPlugin)
        {
            this.httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromMilliseconds(priceCheckPlugin.Configuration
                    .RequestTimeout),
            };
        }

        /// <inheritdoc />
        public MarketBoardData? GetMarketBoard(uint worldId, ulong itemId)
        {
            var marketBoardFromAPI = this.GetMarketBoardData(worldId, itemId);
            return marketBoardFromAPI;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            this.httpClient.Dispose();
        }

        private MarketBoardData? GetMarketBoardData(uint worldId, ulong itemId)
        {
            HttpResponseMessage result;
            try
            {
                result = this.GetMarketBoardDataAsync(worldId, itemId).Result;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Failed to retrieve data from Universalis for itemId {0} / worldId {1}.",
                    itemId,
                    worldId);

                return null;
            }

            if (result.StatusCode != HttpStatusCode.OK)
            {
                Logger.LogError(
                    "Failed to retrieve data from Universalis for itemId {0} / worldId {1} with HttpStatusCode {2}.",
                    itemId,
                    worldId,
                    result.StatusCode);
                return null;
            }

            var json = JsonConvert.DeserializeObject<dynamic>(result.Content.ReadAsStringAsync().Result);
            if (json == null)
            {
                Logger.LogError(
                    "Failed to deserialize Universalis response for itemId {0} / worldId {1}.",
                    itemId,
                    worldId);
                return null;
            }

            try
            {
                var marketBoardData = new MarketBoardData
                {
                    LastCheckTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    LastUploadTime = json.lastUploadTime?.Value,
                    AveragePriceNQ = json.averagePriceNQ?.Value,
                    AveragePriceHQ = json.averagePriceHQ?.Value,
                    CurrentAveragePriceNQ = json.currentAveragePriceNQ?.Value,
                    CurrentAveragePriceHQ = json.currentAveragePriceHQ?.Value,
                    MinimumPriceNQ = json.minPriceNQ?.Value,
                    MinimumPriceHQ = json.minPriceHQ?.Value,
                    MaximumPriceNQ = json.maxPriceNQ?.Value,
                    MaximumPriceHQ = json.maxPriceHQ?.Value,
                    CurrentMinimumPrice = json.listings[0]?.pricePerUnit?.Value,
                };
                return marketBoardData;
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    ex,
                    "Failed to parse marketBoard data for itemId {0} / worldId {1}.",
                    itemId,
                    worldId);
                return null;
            }
        }

        private async Task<HttpResponseMessage> GetMarketBoardDataAsync(uint? worldId, ulong itemId)
        {
            return await this.httpClient.GetAsync(new Uri(Endpoint + "/" + worldId + "/" + itemId));
        }
    }
}
