// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class RowIdentityMap<TKey> : IRowIdentityMap
    where TKey : notnull
{
    private readonly bool _sensitiveLoggingEnabled;
    private readonly IUniqueConstraint _key;
    private readonly Dictionary<TKey, IModificationCommand> _identityMap;
    private readonly IRowKeyValueFactory<TKey> _principalKeyValueFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public RowIdentityMap(
        IUniqueConstraint key,
        bool sensitiveLoggingEnabled)
    {
        _sensitiveLoggingEnabled = sensitiveLoggingEnabled;
        _key = key;
        _principalKeyValueFactory = ((UniqueConstraint)_key).GetRowKeyValueFactory<TKey>();
        _identityMap = new Dictionary<TKey, IModificationCommand>(_principalKeyValueFactory.EqualityComparer);
    }
     
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IModificationCommand? TryGetEntry(object?[] keyValues)
    {
        var key = _principalKeyValueFactory.CreateKeyValue(keyValues);
        return key != null && _identityMap.TryGetValue(key, out var entry) ? entry : null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IModificationCommand? TryGetEntry(IDictionary<string, object?> keyPropertyValues, IEntityType entityType)
    {
        var key = _principalKeyValueFactory.CreateKeyValue(keyPropertyValues, entityType);
        return key != null && _identityMap.TryGetValue(key, out var entry) ? entry : null;
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void Add(IModificationCommand entry)
        => Add(_principalKeyValueFactory.CreateKeyValue(entry), entry);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual void Add(TKey key, IModificationCommand entry)
        => Add(key, entry, updateDuplicate: false);

    private void Add(TKey key, IModificationCommand entry, bool updateDuplicate)
    {
        if (_identityMap.TryGetValue(key, out var existingEntry))
        {
            if (!updateDuplicate)
            {
                if (existingEntry == entry)
                {
                    return;
                }

                ThrowIdentityConflict(entry);
            }
        }

        _identityMap[key] = entry;
    }

    private void ThrowIdentityConflict(IModificationCommand entry)
    {
        //if (_sensitiveLoggingEnabled)
        //{
        //    throw new InvalidOperationException(
        //        CoreStrings.IdentityConflictSensitive(
        //            entry.EntityType.DisplayName(),
        //            entry.BuildCurrentValuesString(Key.Columns)));
        //}

        //throw new InvalidOperationException(
        //    CoreStrings.IdentityConflict(
        //        entry.EntityType.DisplayName(),
        //        Key.Columns.Format()));
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual void Remove(IModificationCommand entry)
        => Remove(_principalKeyValueFactory.CreateKeyValue(entry), entry);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    protected virtual void Remove(TKey key, IModificationCommand entry)
    {
        if (_identityMap.TryGetValue(key, out var existingEntry)
            && existingEntry == entry)
        {
            _identityMap.Remove(key);
        }
    }
}
