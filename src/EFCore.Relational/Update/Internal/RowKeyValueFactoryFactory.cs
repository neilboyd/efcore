// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using JetBrains.Annotations;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Microsoft.EntityFrameworkCore.Update.Internal;

/// <summary>
///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
///     the same compatibility standards as public APIs. It may be changed or removed without notice in
///     any release. You should only use it directly in your code with extreme caution and knowing that
///     doing so can result in application failures when updating to a new Entity Framework Core release.
/// </summary>
public class RowKeyValueFactoryFactory : IRowKeyValueFactoryFactory
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IRowKeyValueFactory Create(IUniqueConstraint key)
        => (IRowKeyValueFactory)_createMethod
                .MakeGenericMethod(((UniqueConstraint)key).GetKeyType())
                .Invoke(null, new object[] { key })!;

    private readonly static MethodInfo _createMethod = typeof(RowKeyValueFactoryFactory).GetTypeInfo()
        .GetDeclaredMethod(nameof(CreateFactory))!;

    [UsedImplicitly]
    private static IRowKeyValueFactory<TKey> CreateFactory<TKey>(IUniqueConstraint key)
        => key.Columns.Count == 1
            ? new SimpleRowKeyValueFactory<TKey>(key.Columns.Single())
            : new CompositeRowKeyValueFactory<TKey>(key);

    private class SimpleRowKeyValueFactory<TKey> : IRowKeyValueFactory<TKey>
    {
        private readonly IColumn _column;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public SimpleRowKeyValueFactory(IColumn column)
        {
            _column = column;

            var comparer = column.PropertyMappings.First().TypeMapping.ProviderComparer;

            EqualityComparer
                = comparer != null
                    ? new NoNullsCustomEqualityComparer(comparer)
                    : typeof(IStructuralEquatable).IsAssignableFrom(typeof(TKey))
                        ? new NoNullsStructuralEqualityComparer()
                        : EqualityComparer<TKey>.Default;
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual object? CreateFromKeyValues(object?[] keyValues)
            => keyValues[0];

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual object? CreateFromBuffer(ValueBuffer valueBuffer)
            => _propertyAccessors.ValueBufferGetter!(valueBuffer);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IProperty FindNullPropertyInKeyValues(object?[] keyValues)
            => _column;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual TKey CreateFromCurrentValues(IUpdateEntry entry)
            => ((Func<IUpdateEntry, TKey>)_propertyAccessors.CurrentValueGetter)(entry);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IProperty FindNullPropertyInCurrentValues(IUpdateEntry entry)
            => _column;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual TKey CreateFromOriginalValues(IUpdateEntry entry)
            => ((Func<IUpdateEntry, TKey>)_propertyAccessors.OriginalValueGetter!)(entry);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual TKey CreateFromRelationshipSnapshot(IUpdateEntry entry)
            => ((Func<IUpdateEntry, TKey>)_propertyAccessors.RelationshipSnapshotGetter)(entry);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IEqualityComparer<TKey> EqualityComparer { get; }

        private sealed class NoNullsStructuralEqualityComparer : IEqualityComparer<TKey>
        {
            private readonly IEqualityComparer _comparer
                = StructuralComparisons.StructuralEqualityComparer;

            public bool Equals(TKey? x, TKey? y)
                => _comparer.Equals(x, y);

            public int GetHashCode([DisallowNull] TKey obj)
                => _comparer.GetHashCode(obj);
        }

        private sealed class NoNullsCustomEqualityComparer : IEqualityComparer<TKey>
        {
            private readonly Func<TKey?, TKey?, bool> _equals;
            private readonly Func<TKey, int> _hashCode;

            public NoNullsCustomEqualityComparer(ValueComparer comparer)
            {
                if (comparer.Type != typeof(TKey)
                    && comparer.Type == typeof(TKey).UnwrapNullableType())
                {
                    comparer = comparer.ToNonNullNullableComparer();
                }

                _equals = (Func<TKey?, TKey?, bool>)comparer.EqualsExpression.Compile();
                _hashCode = (Func<TKey, int>)comparer.HashCodeExpression.Compile();
            }

            public bool Equals(TKey? x, TKey? y)
                => _equals(x, y);

            public int GetHashCode([DisallowNull] TKey obj)
                => _hashCode(obj);
        }
    }

    private class CompositeRowKeyValueFactory<TKey> : CompositeRowValueFactory, IRowKeyValueFactory<TKey>
    {
        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public CompositeRowKeyValueFactory(IUniqueConstraint key)
            : base(key.Columns)
        {
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual object? CreateFromKeyValues(object?[] keyValues)
            => keyValues.Any(v => v == null) ? null : keyValues;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual object? CreateFromBuffer(ValueBuffer valueBuffer)
            => TryCreateFromBuffer(valueBuffer, out var values) ? values : null;

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IProperty FindNullPropertyInKeyValues(object?[] keyValues)
        {
            var index = -1;
            for (var i = 0; i < keyValues.Length; i++)
            {
                if (keyValues[i] == null)
                {
                    index = i;
                    break;
                }
            }

            return Columns[index];
        }

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual object[] CreateFromCurrentValues(IUpdateEntry entry)
            => CreateFromEntry(entry, (e, p) => e.GetCurrentValue(p));

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual IProperty? FindNullPropertyInCurrentValues(IUpdateEntry entry)
            => Columns.FirstOrDefault(p => entry.GetCurrentValue(p) == null);

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual object[] CreateFromOriginalValues(IUpdateEntry entry)
            => CreateFromEntry(entry, (e, p) => e.GetOriginalValue(p));

        /// <summary>
        ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
        ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
        ///     any release. You should only use it directly in your code with extreme caution and knowing that
        ///     doing so can result in application failures when updating to a new Entity Framework Core release.
        /// </summary>
        public virtual object[] CreateFromRelationshipSnapshot(IUpdateEntry entry)
            => CreateFromEntry(entry, (e, p) => e.GetRelationshipSnapshotValue(p));

        private object[] CreateFromEntry(
            IUpdateEntry entry,
            Func<IUpdateEntry, IProperty, object?> getValue)
        {
            var values = new object[Columns.Count];
            var index = 0;

            foreach (var property in Columns)
            {
                var value = getValue(entry, property);
                if (value == null)
                {
                    return default!;
                }

                values[index++] = value;
            }

            return values;
        }
    }
}
