using System.Linq.Expressions;
using HealthcareApi.Data;
using Microsoft.EntityFrameworkCore;

namespace HealthcareApi.Repositories;

/// <summary>
/// Generic repository implementation for Entity Framework Core.
/// Provides common data access operations for all entities.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly DbSet<T> _dbSet;
    protected readonly ILogger<Repository<T>> _logger;

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    /// <param name="context">Database context</param>
    /// <param name="logger">Logger instance</param>
    public Repository(ApplicationDbContext context, ILogger<Repository<T>> logger)
    {
        _context = context;
        _dbSet = context.Set<T>();
        _logger = logger;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        try
        {
            _logger.LogDebug("Getting all entities of type {EntityType}", typeof(T).Name);
            return await _dbSet.AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            _logger.LogDebug("Getting entities of type {EntityType} with predicate", typeof(T).Name);
            return await _dbSet.AsNoTracking().Where(predicate).ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entities of type {EntityType} with predicate", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(string id)
    {
        try
        {
            _logger.LogDebug("Getting entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            return await _dbSet.FindAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            _logger.LogDebug("Getting first entity of type {EntityType} with predicate", typeof(T).Name);
            return await _dbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting first entity of type {EntityType} with predicate", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(T entity)
    {
        try
        {
            _logger.LogDebug("Adding entity of type {EntityType}", typeof(T).Name);
            var entry = await _dbSet.AddAsync(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully added entity of type {EntityType}", typeof(T).Name);
            return entry.Entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<T> UpdateAsync(T entity)
    {
        try
        {
            _logger.LogDebug("Updating entity of type {EntityType}", typeof(T).Name);
            _dbSet.Update(entity);
            await _context.SaveChangesAsync();
            _logger.LogInformation("Successfully updated entity of type {EntityType}", typeof(T).Name);
            return entity;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteAsync(T entity)
    {
        try
        {
            _logger.LogDebug("Deleting entity of type {EntityType}", typeof(T).Name);
            _dbSet.Remove(entity);
            var result = await _context.SaveChangesAsync();
            var success = result > 0;
            if (success)
            {
                _logger.LogInformation("Successfully deleted entity of type {EntityType}", typeof(T).Name);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteByIdAsync(string id)
    {
        try
        {
            _logger.LogDebug("Deleting entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            var entity = await GetByIdAsync(id);
            if (entity == null)
            {
                _logger.LogWarning("Entity of type {EntityType} with ID {Id} not found for deletion", typeof(T).Name, id);
                return false;
            }
            return await DeleteAsync(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting entity of type {EntityType} with ID {Id}", typeof(T).Name, id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null)
    {
        try
        {
            _logger.LogDebug("Counting entities of type {EntityType}", typeof(T).Name);
            return predicate == null
                ? await _dbSet.CountAsync()
                : await _dbSet.CountAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error counting entities of type {EntityType}", typeof(T).Name);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        try
        {
            _logger.LogDebug("Checking existence of entity of type {EntityType}", typeof(T).Name);
            return await _dbSet.AnyAsync(predicate);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking existence of entity of type {EntityType}", typeof(T).Name);
            throw;
        }
    }
}
