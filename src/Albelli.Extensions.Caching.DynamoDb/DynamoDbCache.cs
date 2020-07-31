using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace Albelli.Extensions.Caching.DynamoDb
{
    public sealed class DynamoDbCache : IDistributedCache
    {
        private readonly DynamoDbCacheOptions _options;
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly ISystemClock _systemClock;

        public DynamoDbCache(IOptions<DynamoDbCacheOptions> options, ISystemClock systemClock, IAmazonDynamoDB dynamoDb = null)
        {
            _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            if (_options.CustomDynamoDbClient != null)
            {
                _dynamoDb = _options.CustomDynamoDbClient;
            }
            else
            {
                _dynamoDb = dynamoDb ?? throw new ArgumentNullException(nameof(dynamoDb),
                    "This library depends on AWSSDK.DynamoDBv2. Please inject IAmazonDynamoDB or provide it manually through the configuration.");
            }
            _systemClock = systemClock ?? throw new ArgumentNullException(nameof(systemClock));
        }

        public byte[] Get(string key)
        {
            if (!_options.EnableSyncMethods)
            {
                throw new NotSupportedException("Sync methods are disabled.");
            }

            return GetAsync(key).GetAwaiter().GetResult();
        }

        public async Task<byte[]> GetAsync(string key, CancellationToken token = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
            token.ThrowIfCancellationRequested();
            var result = await _dynamoDb.GetItemAsync(_options.TableName,
                    new Dictionary<string, AttributeValue>() {[_options.KeyColumnName] = new AttributeValue(key)},
                    token)
                .ConfigureAwait(false);
            if (!result.IsItemSet)
            {
                return null;
            }
            var currentTtl = long.Parse(result.Item[_options.TimeToLiveColumnName].N);
            if (currentTtl < _systemClock.UtcNow.ToUnixTimeSeconds())
            {
                return null;
            }
            if (result.Item.ContainsKey(_options.SlidingExpiryTimespanColumnName))
            {
                var slidingExpiry = long.Parse(result.Item[_options.SlidingExpiryTimespanColumnName].N);
                var currentTime = _systemClock.UtcNow.ToUnixTimeSeconds();
                if (currentTtl - currentTime <= slidingExpiry / 2)
                {
                    await RefreshAsync(key, token).ConfigureAwait(false);
                }
            }
            var valueAttr = result.Item[_options.ValueColumnName];
            var stream = valueAttr.B;
            return stream.ToArray();
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (!_options.EnableSyncMethods)
            {
                throw new NotSupportedException("Sync methods are disabled.");
            }

            SetAsync(key, value, options).GetAwaiter().GetResult();
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options,
            CancellationToken token = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
            token.ThrowIfCancellationRequested();
            if (options?.AbsoluteExpiration == null && options?.SlidingExpiration == null &&
                options?.AbsoluteExpirationRelativeToNow == null)
            {
                options = new DistributedCacheEntryOptions()
                    {AbsoluteExpirationRelativeToNow = _options.DefaultDurationPolicy};
            }
            var dynamoDbCacheEntry = BuildDynamoDbCacheEntry(key, value, options);
            await _dynamoDb.PutItemAsync(_options.TableName, dynamoDbCacheEntry, token).ConfigureAwait(false);
        }

        public void Refresh(string key)
        {
            if (!_options.EnableSyncMethods)
            {
                throw new NotSupportedException("Sync methods are disabled.");
            }

            RefreshAsync(key).GetAwaiter().GetResult();
        }

        public async Task RefreshAsync(string key, CancellationToken token = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
            token.ThrowIfCancellationRequested();
            await _dynamoDb.UpdateItemAsync(
                    new UpdateItemRequest()
                    {
                        TableName = _options.TableName,
                        Key = new Dictionary<string, AttributeValue>()
                            {[_options.KeyColumnName] = new AttributeValue(key)},
                        ConditionExpression =
                            $"attribute_exists({_options.SlidingExpiryTimespanColumnName}) and (attribute_not_exists({_options.AbsoluteExpiryUnixTimestampColumnName}) or ({_options.TimeToLiveColumnName} < {_options.AbsoluteExpiryUnixTimestampColumnName}))",
                        UpdateExpression = $"SET #ttlName = #ttlName + #sldExpr",
                        ExpressionAttributeNames = new Dictionary<string, string>(
                        )
                        {
                            ["#ttlName"] = _options.TimeToLiveColumnName,
                            ["#sldExpr"] = _options.SlidingExpiryTimespanColumnName
                        }
                    },
                    token)
                .ConfigureAwait(false);
        }

        public void Remove(string key)
        {
            if (!_options.EnableSyncMethods)
            {
                throw new NotSupportedException("Sync methods are disabled.");
            }

            RemoveAsync(key).GetAwaiter().GetResult();
        }

        public async Task RemoveAsync(string key, CancellationToken token = new CancellationToken())
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Value cannot be null or whitespace.", nameof(key));
            token.ThrowIfCancellationRequested();
            await _dynamoDb.DeleteItemAsync(_options.TableName,
                    new Dictionary<string, AttributeValue>() {[_options.KeyColumnName] = new AttributeValue(key)},
                    token)
                .ConfigureAwait(false);
        }
        
        private Dictionary<string, AttributeValue> BuildDynamoDbCacheEntry(string key, byte[] value,
            DistributedCacheEntryOptions options)
        {
            var table = new Dictionary<string, AttributeValue>()
            {
                [_options.KeyColumnName] = new AttributeValue(key),
                [_options.ValueColumnName] = new AttributeValue() {B = new MemoryStream(value)},
            };
            var slidingExpirationTicks = options.SlidingExpiration.HasValue
                ? Convert.ToInt64(Math.Round(options.SlidingExpiration.Value.TotalSeconds))
                : (long?) null;
            var absoluteExpirationTimestamp =
                GetAbsoluteExpirationUnixTimestamp(options.AbsoluteExpiration, options.AbsoluteExpirationRelativeToNow);
            if (slidingExpirationTicks.HasValue)
            {
                table.Add(_options.SlidingExpiryTimespanColumnName,
                    new AttributeValue() {N = slidingExpirationTicks.Value.ToString()});
            }

            if (absoluteExpirationTimestamp.HasValue)
            {
                table.Add(_options.AbsoluteExpiryUnixTimestampColumnName,
                    new AttributeValue() {N = absoluteExpirationTimestamp.Value.ToString()});
            }

            table.Add(_options.TimeToLiveColumnName,
                new AttributeValue()
                    {N = GetInitialTtlTimestamp(slidingExpirationTicks, absoluteExpirationTimestamp).ToString()});
            return table;
        }

        private long GetInitialTtlTimestamp(long? slidingExpirationTicks, long? absoluteExpirationTimestamp)
        {
            switch (slidingExpirationTicks)
            {
                case null when absoluteExpirationTimestamp == null:
                    throw new InvalidOperationException("Cannot calculate initial TTL if both values are null");
                case null:
                    return absoluteExpirationTimestamp.Value;
                default:
                    return _systemClock.UtcNow.ToUnixTimeSeconds() + slidingExpirationTicks.Value;
            }
        }

        private long? GetAbsoluteExpirationUnixTimestamp(DateTimeOffset? absoluteExpiration,
            TimeSpan? relativeToNowAbsoluteExpiration)
        {
            DateTimeOffset? result = null;
            if (absoluteExpiration != null && relativeToNowAbsoluteExpiration != null)
            {
                throw new NotSupportedException(
                    "AbsoluteExpiration and AbsoluteExpirationRelativeToNow cannot be set at the same time.");
            }

            if (absoluteExpiration.HasValue)
            {
                result = absoluteExpiration.Value;
            }
            else if (relativeToNowAbsoluteExpiration.HasValue)
            {
                result = _systemClock.UtcNow.Add(relativeToNowAbsoluteExpiration.Value);
            }

            return result?.ToUnixTimeSeconds();
        }
    }
}