# Albelli.Extensions.Caching.DynamoDb
This library offers an implementation of IDistributedCache with a DynamoDb back-end.
It makes use of the TTL functionality to get remove the stale cache keys.
IDistributedCache is a low-level interface used by cache-related consumers, such as Polly. Microsoft also provides a set of extension methods that simplify common workflows.

## Considerations
 - There is no optimistic locking(or locking of any kind). If this is a big problem, using a different provider(such as Redis with WATCH) might be better for your use case.
 - Eventual consistency is used.
 - Deleting DynamoDB items with an expired TTL takes up to 48 hours. There is a soft invalidation in the code to check whether the item has expired or not.
 - Sliding expiration is WIP and experimental. It works in such a way that the TTL is refreshed(by the amount of the sliding expiration) when you do a GetCacheKey and the TTL is about to expire in (SlidingExpirationDuration / 2) seconds.
	Example:
	Sliding expiration is set to 30 mins. 
	The TTL is CurrentTime + 14 mins( < SlidingExpirationDuration / 2).
	Doing a GET will refresh the TTL to CurrentTime + 14 mins + 30 mins.
	A subsequent GET will not refresh the key until the condition is met again.
 - Sync methods are disabled by default due to the fact that Amazon's DynamoDb client uses HttpClient internally, which lacks sync methods. There is an override that will enable them, but doing so is not recommended.
 - The library is still **quite raw**. Any pull requests, ideas and feedback are encouraged.

 
