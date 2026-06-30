using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace OrderSystem.Repositories.Interceptors;

/// <summary>
/// Loads the sqlite-vec native extension (<c>vec0</c>) onto every SQLite connection
/// as it opens, so the <c>vec_chunks</c> virtual table is available for both schema
/// creation and queries. Registered on the DbContext options.
/// </summary>
public sealed class VecExtensionConnectionInterceptor : DbConnectionInterceptor
{
    private readonly string _extensionPath;

    public VecExtensionConnectionInterceptor(string extensionPath) => _extensionPath = extensionPath;

    public override void ConnectionOpened(DbConnection connection, ConnectionEndEventData eventData) =>
        Load(connection);

    public override Task ConnectionOpenedAsync(
        DbConnection connection,
        ConnectionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        Load(connection);
        return Task.CompletedTask;
    }

    private void Load(DbConnection connection)
    {
        if (connection is not SqliteConnection sqlite)
            return;

        sqlite.EnableExtensions(true);
        sqlite.LoadExtension(_extensionPath);
    }
}
