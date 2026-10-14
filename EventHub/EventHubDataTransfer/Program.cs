using DotNetEnv;
using Microsoft.Data.Sqlite;
using Npgsql;
using NpgsqlTypes;
using System.Globalization;

var projectRoot = Path.GetFullPath(@"..");
var sqlitePath = Path.Combine(projectRoot, "eventhub.db");
var envPath = Path.Combine(projectRoot, ".env");

if (!File.Exists(sqlitePath))
    throw new FileNotFoundException("SQLite database not found.", sqlitePath);

if (!File.Exists(envPath))
    throw new FileNotFoundException(".env file not found.", envPath);

Env.Load(envPath);

var postgresConnectionString =
    Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");

if (string.IsNullOrWhiteSpace(postgresConnectionString))
    throw new InvalidOperationException(
        "ConnectionStrings__DefaultConnection was not found in .env.");

var tables = new[]
{
    ("Categories", "CategoryId"),
    ("Users", "UserId"),
    ("Events", "EventId"),
    ("Registrations", "RegistrationId")
};

var records = new Dictionary<string, List<Dictionary<string, object?>>>();

// Read every source record without modifying SQLite.
using (var sqlite = new SqliteConnection(
    new SqliteConnectionStringBuilder
    {
        DataSource = sqlitePath,
        Mode = SqliteOpenMode.ReadOnly
    }.ToString()))
{
    sqlite.Open();

    foreach (var (table, _) in tables)
    {
        var rows = new List<Dictionary<string, object?>>();

        using var command = sqlite.CreateCommand();
        command.CommandText = $"SELECT * FROM \"{table}\"";

        using var reader = command.ExecuteReader();

        while (reader.Read())
        {
            var row = new Dictionary<string, object?>(
                StringComparer.OrdinalIgnoreCase);

            for (var i = 0; i < reader.FieldCount; i++)
            {
                row[reader.GetName(i)] =
                    reader.IsDBNull(i) ? null : reader.GetValue(i);
            }

            rows.Add(row);
        }

        records[table] = rows;
        Console.WriteLine($"SQLite {table}: {rows.Count} records read");
    }
}

// Do not risk duplicating or overwriting existing Neon data.
await using var postgres = new NpgsqlConnection(postgresConnectionString);
await postgres.OpenAsync();

await using var transaction = await postgres.BeginTransactionAsync();

try
{
    foreach (var (table, _) in tables)
    {
        await using var countCommand = new NpgsqlCommand(
            $"SELECT COUNT(*) FROM \"{table}\"",
            postgres,
            transaction);

        var count = (long)(await countCommand.ExecuteScalarAsync())!;

        if (count != 0)
        {
            throw new InvalidOperationException(
                $"Neon table {table} already contains {count} records. " +
                "Nothing was transferred. Check the target database first.");
        }
    }

    foreach (var (table, _) in tables)
    {
        foreach (var row in records[table])
        {
            var columns = row.Keys.ToArray();

            var columnSql = string.Join(
                ", ", columns.Select(c => $"\"{c}\""));

            var parameterSql = string.Join(
                ", ", columns.Select((_, i) => $"@p{i}"));

            await using var insert = new NpgsqlCommand(
                $"INSERT INTO \"{table}\" ({columnSql}) " +
                $"VALUES ({parameterSql})",
                postgres,
                transaction);

            for (var i = 0; i < columns.Length; i++)
            {
                var column = columns[i];
                var value = row[column];

                if (value is string text &&
                    (column.EndsWith("At", StringComparison.OrdinalIgnoreCase) ||
                     column.Equals("EventDate", StringComparison.OrdinalIgnoreCase)))
                {
                    if (DateTime.TryParse(
                        text,
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.RoundtripKind,
                        out var date))
                    {
                        // PostgreSQL timestamptz requires UTC DateTime values.
                        value = DateTime.SpecifyKind(date, DateTimeKind.Utc);
                        insert.Parameters.AddWithValue(
                            $"p{i}", NpgsqlDbType.TimestampTz, value);
                        continue;
                    }
                }

                insert.Parameters.AddWithValue(
                    $"p{i}", value ?? DBNull.Value);
            }

            await insert.ExecuteNonQueryAsync();
        }

        Console.WriteLine(
            $"Prepared {records[table].Count} records for {table}");
    }

    await transaction.CommitAsync();

    Console.WriteLine();
    Console.WriteLine("Transfer committed successfully.");
}
catch
{
    await transaction.RollbackAsync();
    Console.WriteLine("Transfer failed. PostgreSQL changes were rolled back.");
    throw;
}

// Verify the committed counts.
Console.WriteLine();
Console.WriteLine("Final Neon PostgreSQL counts:");

foreach (var (table, _) in tables)
{
    await using var verify = new NpgsqlCommand(
        $"SELECT COUNT(*) FROM \"{table}\"", postgres);

    Console.WriteLine($"{table}: {await verify.ExecuteScalarAsync()} records");
}





