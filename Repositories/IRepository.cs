using System.Linq.Expressions;

namespace HealthcareApi.Repositories;

/// <summary>
/// Generic repository interface for common data access operations.
/// Provides a standardized way to interact with data entities.
/// </summary>
/// <typeparam name="T">The entity type</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Gets all entities asynchronously.
    /// </summary>
    /// <returns>Collection of all entities</returns>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Gets entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">Filter condition</param>
    /// <returns>Collection of matching entities</returns>
    Task<IEnumerable<T>> GetAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Gets a single entity by its ID.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <returns>The entity if found, null otherwise</returns>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Gets the first entity that matches the predicate, or null if none found.
    /// </summary>
    /// <param name="predicate">Filter condition</param>
    /// <returns>The first matching entity or null</returns>
    Task<T?> GetFirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Adds a new entity to the repository.
    /// </summary>
    /// <param name="entity">Entity to add</param>
    /// <returns>The added entity</returns>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Updates an existing entity in the repository.
    /// </summary>
    /// <param name="entity">Entity to update</param>
    /// <returns>The updated entity</returns>
    Task<T> UpdateAsync(T entity);

    /// <summary>
    /// Removes an entity from the repository.
    /// </summary>
    /// <param name="entity">Entity to remove</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteAsync(T entity);

    /// <summary>
    /// Removes an entity by its ID.
    /// </summary>
    /// <param name="id">Entity identifier</param>
    /// <returns>True if successful</returns>
    Task<bool> DeleteByIdAsync(string id);

    /// <summary>
    /// Counts entities that match the specified predicate.
    /// </summary>
    /// <param name="predicate">Filter condition (optional)</param>
    /// <returns>Number of matching entities</returns>
    Task<int> CountAsync(Expression<Func<T, bool>>? predicate = null);

    /// <summary>
    /// Checks if any entities match the specified predicate.
    /// </summary>
    /// <param name="predicate">Filter condition</param>
    /// <returns>True if any entities match</returns>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
}
