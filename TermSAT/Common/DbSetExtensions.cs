using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace TermSAT.Common;

/// <summary>
/// originally from https://medium.com/@eduardosilva_94960/entity-framework-core-first-level-cache-f074f73aad14
/// </summary>
public static class DbSetExtensions
{
    /// <summary>
    /// Gets an entity from the local cache or attaches it to the collection if not found.
    /// </summary>
    /// <typeparam name="T">The entity type.</typeparam>
    /// <param name="collection">The DbSet collection to search and attach to.</param>
    /// <param name="searchLocalQuery">A predicate to search for the entity in the local cache.</param>
    /// <param name="getAttachItem">A function to create and return the entity if not found in the local cache.</param>
    /// <returns>The entity found in the local cache or attached to the collection.</returns>
    /// <remarks>
    /// This method searches for an entity in the local cache of a DbSet collection based on a provided predicate.
    /// If the entity is found locally, it is returned; otherwise, the provided function is used to create the entity,
    /// attach it to the collection, and return it.
    /// </remarks>
    /// <example>
    /// var country = Context.Countries.GetLocalOrAttach(c => c.Id == CountryId, () => new Country { Id = CountryId });
    /// </example>
    public static T GetLocalOrAttach<T>(this DbSet<T> collection, Func<T, bool> searchLocalQuery, Func<T> getAttachItem) where T : class
    {
        T localEntity = collection.Local.FirstOrDefault(searchLocalQuery);

        if (localEntity == null)
        {
            localEntity = getAttachItem();
            collection.Attach(localEntity);
        }

        return localEntity;
    }
}
