using System;
using System.Threading.Tasks;
using Albelli.Extensions.Caching.DynamoDb;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Caching.Distributed;
using Xunit;

namespace Albelli.Extensions.Caching.Integration
{
    /// <summary>
    /// TO DO: Make it CI/CD friendly
    /// </summary>
    public class DynamoDbCacheIntegrationTests
    {
        private readonly IAmazonDynamoDB _dynamoDb = new AmazonDynamoDBClient();
        private readonly ISystemClock _systemClock = new DefaultSystemClock();
        private readonly DynamoDbCacheOptions _options = new DynamoDbCacheOptions()
        {
            TableName = "cachetestingv2"
        };
        private readonly IDistributedCache _cache;
        public DynamoDbCacheIntegrationTests() => _cache = new DynamoDbCache(_options, _systemClock, _dynamoDb);

        [Fact(Skip = "Local only.")]
        public async Task Should_Successfully_Create_And_Retrieve_Keys()
        {
            var dummyKey =  $"TestKey{Guid.NewGuid():N}";
            var dummyValue = "Hi, there!";
            await _cache.SetStringAsync(dummyKey, dummyValue);
            var result = await _cache.GetStringAsync(dummyKey);
            Assert.Equal(dummyValue, result);
        }

        [Fact(Skip = "Local only.")]        
        public async Task Should_recalculate_sliding_expiration_properly()
        {
            var dummyKey =  $"TestKey{Guid.NewGuid():N}";
            var dummyValue = "Hi, there!";
            await _cache.SetStringAsync(dummyKey, dummyValue, new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
                SlidingExpiration = TimeSpan.FromSeconds(5)
            });
            for (var i = 0; i <= 5; i++)
            {
                await Task.Delay(3000);
                var result = await _cache.GetStringAsync(dummyKey);
                Assert.Equal(dummyValue, result);
            }
            await Task.Delay(7000);
            var finalResult = await _cache.GetStringAsync(dummyKey);
            Assert.Null(finalResult);
        }
        
        [Fact(Skip = "Local only.")]
        public async Task Should_expire_after_five_seconds()
        {
            var dummyKey =  $"TestKey{Guid.NewGuid():N}";
            var dummyValue = "Hi, there!";
            await _cache.SetStringAsync(dummyKey, dummyValue, new DistributedCacheEntryOptions()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(5)
            });
            var result = await _cache.GetStringAsync(dummyKey);
            Assert.Equal(dummyValue, result);
            await Task.Delay(5500);
            var finalResult = await _cache.GetStringAsync(dummyKey);
            Assert.Null(finalResult);
        }

        [Fact(Skip = "Local only.")]
        public async Task Should_delete_the_key_properly()
        {
            var dummyKey =  $"TestKey{Guid.NewGuid():N}";
            var dummyValue = "Hi, there!";
            await _cache.SetStringAsync(dummyKey, dummyValue);
            var result = await _cache.GetStringAsync(dummyKey);
            Assert.Equal(dummyValue, result);
            await _cache.RemoveAsync(dummyKey);
            result = await _cache.GetStringAsync(dummyKey);
            Assert.Null(result);
        }
    }
}