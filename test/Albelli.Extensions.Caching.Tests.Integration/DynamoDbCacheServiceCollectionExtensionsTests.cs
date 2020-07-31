using System;
using System.Linq;
using Albelli.Extensions.Caching.DynamoDb;
using Amazon.DynamoDBv2;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Albelli.Extensions.Caching.Integration
{
    public class DynamoDbCacheServiceCollectionExtensionsTests
    {
        [Fact]
        public void DynamoDb_Is_Registered_Correctly()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDynamoDbCache(o =>
            {
                o.TableName = "test";
            });
            var distributedCache = serviceCollection.FirstOrDefault(desc => desc.ServiceType == typeof(IDistributedCache));
            Assert.NotNull(distributedCache);
            Assert.Equal(ServiceLifetime.Singleton, distributedCache.Lifetime);
            var systemClock = serviceCollection.FirstOrDefault(desc => desc.ServiceType == typeof(ISystemClock));
            Assert.NotNull(systemClock);
            Assert.Equal(ServiceLifetime.Singleton, systemClock.Lifetime);
        }
        
        [Fact]
        public void ServiceProvider_Throws_Without_A_DynamoDbClient_Registered_In_the_DI()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDynamoDbCache(o =>
            {
                o.TableName = "test";
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.Throws<ArgumentNullException>(() => serviceProvider.GetService(typeof(IDistributedCache)));
        }
        
        [Fact]
        public void ServiceProvider_Is_Retrieved_Successfully_With_A_DynamoDbClient_Registered_In_the_DI()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDynamoDbCache(o =>
            {
                o.TableName = "test";
            });
            serviceCollection.AddSingleton<IAmazonDynamoDB>(m => new Mock<IAmazonDynamoDB>().Object);
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetService(typeof(IDistributedCache)));
        }
        
        [Fact]
        public void ServiceProvider_Is_Retrieved_Successfully_With_A_Manually_Provided_DynamoDBClient()
        {
            var serviceCollection = new ServiceCollection();
            serviceCollection.AddDynamoDbCache(o =>
            {
                o.TableName = "test";
                o.CustomDynamoDbClient = new Mock<IAmazonDynamoDB>().Object;
            });
            var serviceProvider = serviceCollection.BuildServiceProvider();
            Assert.NotNull(serviceProvider.GetService(typeof(IDistributedCache)));
        }
    }
}