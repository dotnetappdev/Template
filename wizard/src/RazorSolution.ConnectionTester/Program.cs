using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Npgsql;

// Test connection helper used by the Razor Pages Solution wizard (see the .csproj for usage).
// The connection string is read from stdin so passwords never appear in a process list.

var options = Options.Parse(args);
if (options.Error is not null)
{
    return Result.Write(false, options.Error, null, exitCode: 2);
}

var connectionString = (Console.In.ReadToEnd() ?? string.Empty).Trim();
if (connectionString.Length == 0)
{
    return Result.Write(false, "No connection string was supplied.", null, exitCode: 2);
}

try
{
    return options.Provider switch
    {
        "sqlserver" => await SqlServer.TestAsync(connectionString, options.TimeoutSeconds),
        "postgresql" => await PostgreSql.TestAsync(connectionString, options.TimeoutSeconds),
        "sqlite" => Sqlite.Test(connectionString, options.BaseDirectory),
        _ => Result.Write(false, $"Unknown provider '{options.Provider}'. Use SqlServer, PostgreSQL or Sqlite.", null, exitCode: 2),
    };
}
catch (ArgumentException ex)
{
    return Result.Write(false, "The connection string is not valid.", ex.Message);
}
catch (Exception ex)
{
    // Always answer with JSON so the wizard can show the problem.
    return Result.Write(false, "Could not connect.", $"{ex.GetType().Name}: {ex.Message}");
}

internal sealed record Options(string Provider, int TimeoutSeconds, string BaseDirectory, string? Error)
{
    public static Options Parse(string[] args)
    {
        string? provider = null;
        var timeout = 10;
        var baseDir = Directory.GetCurrentDirectory();

        for (var i = 0; i < args.Length; i++)
        {
            var value = i + 1 < args.Length ? args[i + 1] : null;
            switch (args[i])
            {
                case "--provider" when value is not null: provider = value.ToLowerInvariant(); i++; break;
                case "--timeout" when int.TryParse(value, out var t): timeout = Math.Clamp(t, 1, 120); i++; break;
                case "--base-dir" when value is not null: baseDir = value; i++; break;
                default: return new Options("", 0, "", $"Unknown or incomplete argument '{args[i]}'.");
            }
        }

        return provider is null
            ? new Options("", 0, "", "Missing --provider (SqlServer, PostgreSQL or Sqlite).")
            : new Options(provider, timeout, baseDir, null);
    }
}

internal static class Result
{
    private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static int Write(bool success, string message, string? detail, int? exitCode = null)
    {
        Console.Out.WriteLine(JsonSerializer.Serialize(new { success, message, detail }, JsonOptions));
        return exitCode ?? (success ? 0 : 1);
    }
}

internal static class SqlServer
{
    public static async Task<int> TestAsync(string connectionString, int timeoutSeconds)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { ConnectTimeout = timeoutSeconds };
        var database = builder.InitialCatalog;
        var watch = Stopwatch.StartNew();

        try
        {
            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            var version = await ScalarAsync(connection, "SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128)) + N' ' + CAST(SERVERPROPERTY('Edition') AS nvarchar(128))");
            return Result.Write(true, $"Connected to '{builder.DataSource}', database '{Display(database)}' in {watch.ElapsedMilliseconds} ms.", $"SQL Server {version}");
        }
        catch (SqlException ex) when (ex.Number == 4060 && !string.IsNullOrEmpty(database))
        {
            // Cannot open database: the server and credentials work, the database just doesn't exist yet.
            builder.InitialCatalog = "master";
            try
            {
                await using var connection = new SqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                var version = await ScalarAsync(connection, "SELECT CAST(SERVERPROPERTY('ProductVersion') AS nvarchar(128))");
                return Result.Write(true,
                    $"Connected to '{builder.DataSource}'. Database '{database}' does not exist yet; the API creates it on first run.",
                    $"SQL Server {version}");
            }
            catch (SqlException inner)
            {
                return Result.Write(false, Describe(inner), inner.Message);
            }
        }
        catch (SqlException ex)
        {
            return Result.Write(false, Describe(ex), ex.Message);
        }
        catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException)
        {
            return Result.Write(false, "Could not connect.", ex.Message);
        }
    }

    private static async Task<string?> ScalarAsync(SqlConnection connection, string sql)
    {
        await using var command = new SqlCommand(sql, connection);
        return (await command.ExecuteScalarAsync())?.ToString();
    }

    private static string Describe(SqlException ex) => ex.Number switch
    {
        18456 => "Login failed: check the user name and password (or that Windows authentication is allowed).",
        -2 => "The server did not answer in time.",
        53 or -1 or 0 or 2 or 26 or 40 or 11001 => "The server was not found or is not reachable. Check the server name, that SQL Server is running and that TCP/IP is enabled.",
        _ => $"SQL Server error {ex.Number}.",
    };

    private static string Display(string db) => string.IsNullOrEmpty(db) ? "(default)" : db;
}

internal static class PostgreSql
{
    public static async Task<int> TestAsync(string connectionString, int timeoutSeconds)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Timeout = timeoutSeconds, Pooling = false };
        var database = builder.Database;
        var watch = Stopwatch.StartNew();

        try
        {
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            return Result.Write(true, $"Connected to '{builder.Host}', database '{database}' in {watch.ElapsedMilliseconds} ms.", $"PostgreSQL {connection.PostgreSqlVersion}");
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.InvalidCatalogName)
        {
            // Database does not exist (3D000): try the maintenance database to prove the server and credentials work.
            builder.Database = "postgres";
            try
            {
                await using var connection = new NpgsqlConnection(builder.ConnectionString);
                await connection.OpenAsync();
                return Result.Write(true,
                    $"Connected to '{builder.Host}'. Database '{database}' does not exist yet; the API creates it on first run.",
                    $"PostgreSQL {connection.PostgreSqlVersion}");
            }
            catch (Exception inner) when (inner is NpgsqlException or TimeoutException)
            {
                return Result.Write(false, Describe(inner), inner.Message);
            }
        }
        catch (Exception ex) when (ex is NpgsqlException or TimeoutException or InvalidOperationException)
        {
            return Result.Write(false, Describe(ex), ex.Message);
        }
    }

    private static string Describe(Exception ex) => ex switch
    {
        PostgresException { SqlState: PostgresErrorCodes.InvalidPassword } => "Login failed: wrong password.",
        PostgresException { SqlState: PostgresErrorCodes.InvalidAuthorizationSpecification } => "Login failed: the user is not allowed to connect.",
        PostgresException pg => $"PostgreSQL error {pg.SqlState}: {pg.MessageText}",
        TimeoutException => "The server did not answer in time.",
        _ when ex.InnerException is System.Net.Sockets.SocketException => "The server was not found or refused the connection. Check the host, the port and that PostgreSQL is running.",
        _ => "Could not connect.",
    };
}

internal static class Sqlite
{
    // SQLite needs no server: check that the database file's folder exists and is writable.
    public static int Test(string connectionString, string baseDirectory)
    {
        const string key = "Data Source=";
        var start = connectionString.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (start < 0)
        {
            return Result.Write(false, "The SQLite connection string has no 'Data Source='.", null);
        }

        var file = connectionString[(start + key.Length)..].Split(';')[0].Trim();
        var path = Path.GetFullPath(Path.Combine(baseDirectory, file));
        var folder = Path.GetDirectoryName(path)!;

        try
        {
            Directory.CreateDirectory(folder);
            var probe = Path.Combine(folder, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllText(probe, "ok");
            File.Delete(probe);
            return Result.Write(true, File.Exists(path)
                ? $"SQLite database '{file}' exists and its folder is writable."
                : $"SQLite database '{file}' will be created by the API on first run (folder is writable).", path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Result.Write(false, $"The folder for '{file}' is not writable.", ex.Message);
        }
    }
}
