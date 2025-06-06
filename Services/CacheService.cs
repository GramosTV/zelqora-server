using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;

namespace HealthcareApi.Services;

/// <summary>
/// Interface for caching operations
/// </summary>
public interface ICacheService
{
    /// <summary>
    /// Get cached value by key
    /// </summary>
    Task<T?> GetAsync<T>(string key) where T : class;

    /// <summary>
    /// Set cached value with expiration
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class;

    /// <summary>
    /// Remove cached value by key
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    /// Remove cached values by pattern
    /// </summary>
    Task RemoveByPatternAsync(string pattern);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key);
}

/// <summary>
/// Memory cache implementation
/// </summary>
public class MemoryCacheService : ICacheService
{
    private readonly IMemoryCache _memoryCache;
    private readonly ILogger<MemoryCacheService> _logger;
    private readonly HashSet<string> _cacheKeys = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public MemoryCacheService(IMemoryCache memoryCache, ILogger<MemoryCacheService> logger)
    {
        _memoryCache = memoryCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            await _semaphore.WaitAsync();
            var cached = _memoryCache.Get(key);
            if (cached is string jsonString)
            {
                return JsonSerializer.Deserialize<T>(jsonString);
            }
            return cached as T;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key {Key}", key);
            return null;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            await _semaphore.WaitAsync();

            var options = new MemoryCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.SetSlidingExpiration(expiration.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromMinutes(30)); // Default 30 minutes
            }

            // Add removal callback to track keys
            options.RegisterPostEvictionCallback((k, v, reason, state) =>
            {
                _cacheKeys.Remove(k.ToString()!);
            });

            var jsonString = JsonSerializer.Serialize(value);
            _memoryCache.Set(key, jsonString, options);
            _cacheKeys.Add(key);

            _logger.LogDebug("Cached value for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key {Key}", key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _semaphore.WaitAsync();
            _memoryCache.Remove(key);
            _cacheKeys.Remove(key);
            _logger.LogDebug("Removed cached value for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key {Key}", key);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        try
        {
            await _semaphore.WaitAsync();
            var keysToRemove = _cacheKeys.Where(k => k.Contains(pattern)).ToList();
            foreach (var key in keysToRemove)
            {
                _memoryCache.Remove(key);
                _cacheKeys.Remove(key);
            }
            _logger.LogDebug("Removed {Count} cached values matching pattern {Pattern}", keysToRemove.Count, pattern);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached values for pattern {Pattern}", pattern);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            await _semaphore.WaitAsync();
            return _memoryCache.TryGetValue(key, out _);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}

/// <summary>
/// Distributed cache implementation (Redis)
/// </summary>
public class DistributedCacheService : ICacheService
{
    private readonly IDistributedCache _distributedCache;
    private readonly ILogger<DistributedCacheService> _logger;

    public DistributedCacheService(IDistributedCache distributedCache, ILogger<DistributedCacheService> logger)
    {
        _distributedCache = distributedCache;
        _logger = logger;
    }

    public async Task<T?> GetAsync<T>(string key) where T : class
    {
        try
        {
            var cached = await _distributedCache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(cached))
            {
                return JsonSerializer.Deserialize<T>(cached);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting cached value for key {Key}", key);
            return null;
        }
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null) where T : class
    {
        try
        {
            var options = new DistributedCacheEntryOptions();
            if (expiration.HasValue)
            {
                options.SetSlidingExpiration(expiration.Value);
            }
            else
            {
                options.SetSlidingExpiration(TimeSpan.FromMinutes(30)); // Default 30 minutes
            }

            var jsonString = JsonSerializer.Serialize(value);
            await _distributedCache.SetStringAsync(key, jsonString, options);

            _logger.LogDebug("Cached value for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting cached value for key {Key}", key);
        }
    }

    public async Task RemoveAsync(string key)
    {
        try
        {
            await _distributedCache.RemoveAsync(key);
            _logger.LogDebug("Removed cached value for key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error removing cached value for key {Key}", key);
        }
    }

    public async Task RemoveByPatternAsync(string pattern)
    {
        // Note: Redis pattern removal requires additional Redis-specific implementation
        _logger.LogWarning("Pattern removal not implemented for distributed cache. Use specific keys instead.");
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(string key)
    {
        try
        {
            var cached = await _distributedCache.GetStringAsync(key);
            return !string.IsNullOrEmpty(cached);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking if key exists {Key}", key);
            return false;
        }
    }
}
