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
public class RowForeignKeyValueFactoryFactory : IRowForeignKeyValueFactoryFactory
{
    /// <summary>
    ///     This is an internal API that supports the Entity Framework Core infrastructure and not subject to
    ///     the same compatibility standards as public APIs. It may be changed or removed without notice in
    ///     any release. You should only use it directly in your code with extreme caution and knowing that
    ///     doing so can result in application failures when updating to a new Entity Framework Core release.
    /// </summary>
    public virtual IRowForeignKeyValueFactory Create(IForeignKeyConstraint foreignKey)
        => (IRowForeignKeyValueFactory)_createMethod
                .MakeGenericMethod(((UniqueConstraint)foreignKey.PrincipalUniqueConstraint).GetKeyType())
                .Invoke(null, new object[] { foreignKey })!;

    private readonly static MethodInfo _createMethod = typeof(RowForeignKeyValueFactoryFactory).GetTypeInfo()
        .GetDeclaredMethod(nameof(CreateFactory))!;

    [UsedImplicitly]
    private static IRowForeignKeyValueFactory CreateFactory<TKey>(IForeignKeyConstraint foreignKey)
        where TKey : notnull
        => new RowForeignKeyValueFactory<TKey>(foreignKey);
}
