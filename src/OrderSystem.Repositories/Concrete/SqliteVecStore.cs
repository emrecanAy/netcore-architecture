using System.Globalization;
using System.Text;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderSystem.Repositories.Abstract;

namespace OrderSystem.Repositories.Concrete;

/// <summary>
/// <see cref="IVectorStore"/> backed by the sqlite-vec <c>vec0</c> virtual table
/// (<c>vec_chunks</c>), which provides a real cosine-distance KNN index. Embeddings
/// are passed as JSON arrays, which sqlite-vec parses into float32 vectors. The
/// virtual table lives in the same SQLite file as the rest of the schema, created
/// by the AddHelpDeskKnowledgeBase migration.
/// </summary>
public class SqliteVecStore : IVectorStore
{
    private readonly AppDbContext _context;

    public SqliteVecStore(AppDbContext context) => _context = context;

    public async Task UpsertAsync(Guid chunkId, ReadOnlyMemory<float> embedding, CancellationToken cancellationToken = default)
    {
        var connection = await OpenAsync(cancellationToken);

        // vec0 has no UPSERT; delete-then-insert keeps re-ingestion idempotent.
        await using var command = connection.CreateCommand();
        command.CommandText =
            "DELETE FROM vec_chunks WHERE chunk_id = $id;" +
            "INSERT INTO vec_chunks(chunk_id, embedding) VALUES ($id, $embedding);";
        command.Parameters.AddWithValue("$id", chunkId.ToString());
        command.Parameters.AddWithValue("$embedding", ToJsonArray(embedding.Span));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VectorMatch>> SearchAsync(
        ReadOnlyMemory<float> query,
        int k,
        CancellationToken cancellationToken = default)
    {
        if (k <= 0)
            return Array.Empty<VectorMatch>();

        var connection = await OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT chunk_id, distance FROM vec_chunks " +
            "WHERE embedding MATCH $query AND k = $k ORDER BY distance;";
        command.Parameters.AddWithValue("$query", ToJsonArray(query.Span));
        command.Parameters.AddWithValue("$k", k);

        var matches = new List<VectorMatch>(k);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = Guid.Parse(reader.GetString(0));
            var distance = reader.GetDouble(1); // cosine distance in [0, 2]
            matches.Add(new VectorMatch(id, 1.0 - distance));
        }

        return matches;
    }

    public async Task DeleteByDocumentAsync(IEnumerable<Guid> chunkIds, CancellationToken cancellationToken = default)
    {
        var ids = chunkIds as IReadOnlyCollection<Guid> ?? chunkIds.ToList();
        if (ids.Count == 0)
            return;

        var connection = await OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        var parameters = new string[ids.Count];
        var i = 0;
        foreach (var id in ids)
        {
            var name = $"$id{i}";
            parameters[i] = name;
            command.Parameters.AddWithValue(name, id.ToString());
            i++;
        }

        command.CommandText = $"DELETE FROM vec_chunks WHERE chunk_id IN ({string.Join(", ", parameters)});";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Ensures the EF-owned connection is open. Opening through EF runs the
    /// connection interceptor that loads the sqlite-vec extension.
    /// </summary>
    private async Task<SqliteConnection> OpenAsync(CancellationToken cancellationToken)
    {
        if (_context.Database.GetDbConnection() is not SqliteConnection connection)
            throw new InvalidOperationException("The vector store requires a SQLite connection.");

        if (connection.State != System.Data.ConnectionState.Open)
            await _context.Database.OpenConnectionAsync(cancellationToken);

        return connection;
    }

    private static string ToJsonArray(ReadOnlySpan<float> vector)
    {
        var builder = new StringBuilder(vector.Length * 8 + 2);
        builder.Append('[');
        for (var i = 0; i < vector.Length; i++)
        {
            if (i > 0)
                builder.Append(',');
            builder.Append(vector[i].ToString("R", CultureInfo.InvariantCulture));
        }

        builder.Append(']');
        return builder.ToString();
    }
}
