using System;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Options;

namespace Albelli.Extensions.Caching.DynamoDb
{
    public sealed class DynamoDbCacheOptions : IOptions<DynamoDbCacheOptions>
    {
        /// <summary>
        /// The name of the DynamoDb table that should be used for caching.
        /// </summary>
        public string TableName { get; set; } = "cache";
        
        /// <summary>
        /// Define the default duration policy, in case DistributedCacheEntryOptions has no set values.
        /// </summary>
        public TimeSpan DefaultDurationPolicy { get; set; } = TimeSpan.FromDays(1);
        /// <summary>
        /// The name of the string-type attribute that will be used to store the cache keys.
        /// </summary>
        public string KeyColumnName { get; set; } = "key";
        /// <summary>
        /// The name of the binary-type attribute that will be used to store the value keys in binary format.
        /// </summary>
        public string ValueColumnName { get; set; } = "value";
        /// <summary>
        /// The name of the number-type attribute that will store the sliding expiration timespan in seconds. This is
        /// to refresh the cache if sliding expiration is set.
        /// </summary>
        public string SlidingExpiryTimespanColumnName { get; set; } = "sliding_exp_timespan";
        /// <summary>
        /// The name of the number-type attribute that will store the absolute expiration date in UNIX timestamp format.
        /// </summary>
        public string AbsoluteExpiryUnixTimestampColumnName { get; set; } = "abs_exp_timestamp";
        /// <summary>
        /// The name of the number-type attribute that will store the absolute expiration date in UNIX timestamp format.
        /// DynamoDb will remove the expired entries on a regular basis.
        /// Warning: "ttl" is a reserved name and not valid for this attribute.
        /// </summary>
        public string TimeToLiveColumnName { get; set; } = "ttl_value";
        /// <summary>
        /// Since there are no sync methods in the .NET Core AWS DynamoDb client, the sync methods are
        /// implemented with blocking calls to the async ones.
        /// Enable at your own risk.
        /// </summary>
        public bool EnableSyncMethods { get; set; }
        
        /// <summary>
        /// Inject the AmazonDynamoDb client manually. If not specified, the service will look for one injected
        /// by the DI instead.
        /// </summary>
        public IAmazonDynamoDB CustomDynamoDbClient { get; set; } = null;  

        public DynamoDbCacheOptions Value => this;
    }
}