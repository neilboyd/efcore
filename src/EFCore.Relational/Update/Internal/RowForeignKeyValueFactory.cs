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
public class RowForeignKeyValueFactory<TKey> : IRowForeignKeyValueFactory
{
    private readonly IForeignKeyConstraint _foreignKey;
    private readonly IRowKeyValueFactory<TKey> _principalKeyValueFactory;
    private readonly IRowForeignKeyValueFactory<TKey> _rowDependentKeyValueFactory;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public RowForeignKeyValueFactory(IForeignKeyConstraint foreignKey)
    {
        _foreignKey = foreignKey;
        _principalKeyValueFactory = ((UniqueConstraint)foreignKey.PrincipalUniqueConstraint).GetRowKeyValueFactory<TKey>();
        _rowDependentKeyValueFactory = CreateRowDependentKeyValueFactory(foreignKey);
    }

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual object CreatePrincipalKeyValueIndex(IModificationCommand command, bool fromOriginalValues = false)
        => new KeyValueIndex<TKey>(
            _foreignKey,
            _principalKeyValueFactory.CreateKeyValue(command, fromOriginalValues),
            _principalKeyValueFactory.EqualityComparer,
            fromOriginalValues);

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual object? CreateDependentKeyValueIndex(IModificationCommand command, bool fromOriginalValues = false)
        => ((ForeignKeyConstraint)_foreignKey).GetRowForeignKeyValueFactory<TKey>()..TryCreateFromCurrentValues(entry, out var keyValue)
            ? new KeyValueIndex<TKey>(foreignKey, keyValue, _principalKeyValueFactory.EqualityComparer, fromOriginalValues: false)
            : null;

    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IKeyValueIndex? CreateDependentKeyValueFromOriginalValues(IUpdateEntry entry, IForeignKey foreignKey)
        => foreignKey.GetDependentKeyValueFactory<TKey>()!.TryCreateFromOriginalValues(entry, out var keyValue)
            ? new KeyValueIndex<TKey>(foreignKey, keyValue, _principalKeyValueFactory.EqualityComparer, fromOriginalValues: true)
            : null;

    private IRowForeignKeyValueFactory<TKey> CreateRowDependentKeyValueFactory(IForeignKeyConstraint foreignKey)
        => foreignKey.Columns.Count == 1
            ? CreateSimple(foreignKey)
            : new CompositeValueFactory(foreignKey.Columns);

    private IDependentKeyValueFactory<TKey> CreateSimple(IForeignKeyConstraint foreignKey)
    {
        var dependentProperty = foreignKey.Properties.Single();
        var principalType = foreignKey.PrincipalKey.Properties.Single().ClrType;
        var propertyAccessors = dependentProperty.GetPropertyAccessors();

        if (dependentProperty.ClrType.IsNullableType()
            && principalType.IsNullableType())
        {
            return new SimpleFullyNullableDependentKeyValueFactory(dependentProperty, propertyAccessors);
        }

        if (dependentProperty.ClrType.IsNullableType())
        {
            return (IDependentKeyValueFactory<TKey>)Activator.CreateInstance(
                typeof(SimpleNullableDependentKeyValueFactory), dependentProperty, propertyAccessors)!;
        }

        return principalType.IsNullableType()
            ? (IDependentKeyValueFactory<TKey>)Activator.CreateInstance(
                typeof(SimpleNullablePrincipalDependentKeyValueFactory<>).MakeGenericType(
                    typeof(TKey).UnwrapNullableType()), dependentProperty, propertyAccessors)!
            : new SimpleNonNullableDependentKeyValueFactory(dependentProperty, propertyAccessors);
    }

    private class SimpleFullyNullableDependentKeyValueFactory : IRowForeignKeyValueFactory<TKey>
    {
        private readonly PropertyAccessors _propertyAccessors;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SimpleFullyNullableDependentKeyValueFactory(
            IProperty property,
            PropertyAccessors propertyAccessors)
        {
            _propertyAccessors = propertyAccessors;
            EqualityComparer = property.CreateKeyEqualityComparer<TKey>();
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IEqualityComparer<TKey> EqualityComparer { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, [NotNullWhen(true)] out TKey? key)
        {
            key = (TKey)_propertyAccessors.ValueBufferGetter!(valueBuffer);
            return key != null;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromCurrentValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.CurrentValueGetter)(entry);
            return key != null;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.PreStoreGeneratedCurrentValueGetter)(entry);
            return key != null;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromOriginalValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.OriginalValueGetter!)(entry);
            return key != null;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromRelationshipSnapshot(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.RelationshipSnapshotGetter)(entry);
            return key != null;
        }
    }

    private class SimpleNullableDependentKeyValueFactory : IRowForeignKeyValueFactory<TKey>
    where TKey : struct
    {
        private readonly PropertyAccessors _propertyAccessors;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SimpleNullableDependentKeyValueFactory(
            IProperty property,
            PropertyAccessors propertyAccessors)
        {
            _propertyAccessors = propertyAccessors;
            EqualityComparer = property.CreateKeyEqualityComparer<TKey>();
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IEqualityComparer<TKey> EqualityComparer { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, out TKey key)
        {
            var value = _propertyAccessors.ValueBufferGetter!(valueBuffer);
            if (value == null)
            {
                key = default;
                return false;
            }

            key = (TKey)value;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromCurrentValues(IUpdateEntry entry, out TKey key)
            => HandleNullableValue(((Func<IUpdateEntry, TKey?>)_propertyAccessors.CurrentValueGetter)(entry), out key);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(IUpdateEntry entry, out TKey key)
            => HandleNullableValue(
                ((Func<IUpdateEntry, TKey?>)_propertyAccessors.PreStoreGeneratedCurrentValueGetter)(entry), out key);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromOriginalValues(IUpdateEntry entry, out TKey key)
            => HandleNullableValue(((Func<IUpdateEntry, TKey?>)_propertyAccessors.OriginalValueGetter!)(entry), out key);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromRelationshipSnapshot(IUpdateEntry entry, out TKey key)
            => HandleNullableValue(((Func<IUpdateEntry, TKey?>)_propertyAccessors.RelationshipSnapshotGetter)(entry), out key);

        private static bool HandleNullableValue(TKey? value, out TKey key)
        {
            if (value.HasValue)
            {
                key = (TKey)value;
                return true;
            }

            key = default;
            return false;
        }
    }

    private class SimpleNullablePrincipalDependentKeyValueFactory<TNonNullableKey> : IRowForeignKeyValueFactory<TKey>
    where TNonNullableKey : struct
    {
        private readonly PropertyAccessors _propertyAccessors;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SimpleNullablePrincipalDependentKeyValueFactory(
            IProperty property,
            PropertyAccessors propertyAccessors)
        {
            _propertyAccessors = propertyAccessors;
            EqualityComparer = property.CreateKeyEqualityComparer<TKey>();
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IEqualityComparer<TKey> EqualityComparer { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, [NotNullWhen(true)] out TKey? key)
        {
            var value = _propertyAccessors.ValueBufferGetter!(valueBuffer);
            if (value == null)
            {
                key = default;
                return false;
            }

            key = (TKey)value;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromCurrentValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = (TKey)(object)((Func<IUpdateEntry, TNonNullableKey>)_propertyAccessors.CurrentValueGetter)(entry)!;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = (TKey)(object)((Func<IUpdateEntry, TNonNullableKey>)_propertyAccessors.PreStoreGeneratedCurrentValueGetter)(entry)!;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromOriginalValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = (TKey)(object)((Func<IUpdateEntry, TNonNullableKey>)_propertyAccessors.OriginalValueGetter!)(entry)!;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromRelationshipSnapshot(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = (TKey)(object)((Func<IUpdateEntry, TNonNullableKey>)_propertyAccessors.RelationshipSnapshotGetter)(entry)!;
            return true;
        }
    }

    private class SimpleNonNullableDependentKeyValueFactory : IRowForeignKeyValueFactory<TKey>
    {
        private readonly PropertyAccessors _propertyAccessors;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SimpleNonNullableDependentKeyValueFactory(
            IProperty property,
            PropertyAccessors propertyAccessors)
        {
            _propertyAccessors = propertyAccessors;
            EqualityComparer = property.CreateKeyEqualityComparer<TKey>();
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IEqualityComparer<TKey> EqualityComparer { get; }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromBuffer(in ValueBuffer valueBuffer, [NotNullWhen(true)] out TKey? key)
        {
            var value = _propertyAccessors.ValueBufferGetter!(valueBuffer);
            if (value == null)
            {
                key = default;
                return false;
            }

            key = (TKey)value;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromCurrentValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.CurrentValueGetter)(entry)!;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromPreStoreGeneratedCurrentValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.PreStoreGeneratedCurrentValueGetter)(entry)!;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromOriginalValues(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.OriginalValueGetter!)(entry)!;
            return true;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual bool TryCreateFromRelationshipSnapshot(IUpdateEntry entry, [NotNullWhen(true)] out TKey? key)
        {
            key = ((Func<IUpdateEntry, TKey>)_propertyAccessors.RelationshipSnapshotGetter)(entry)!;
            return true;
        }
    }
}
