using System.Globalization;
using System.Text;
using Npgsql;

// Простая утилита экспорта одной таблицы PostgreSQL в CSV.
// Конфигурация берётся из переменных окружения:
//  PGHOST, PGPORT, PGDATABASE, PGUSER, PGPASSWORD, TABLE, OUTPUT_PATH

static string GetEnv(string key, string fallback = "")
{
    var value = Environment.GetEnvironmentVariable(key);
    if (string.IsNullOrWhiteSpace(value))
    {
        return fallback;
    }
    return value;
}

var host = GetEnv("PGHOST", "postgres");
var port = GetEnv("PGPORT", "5432");
var database = GetEnv("PGDATABASE", "appdb");
var user = GetEnv("PGUSER", "app");
var password = GetEnv("PGPASSWORD", "app");
var table = GetEnv("TABLE", "shipments");
var outputPath = GetEnv("OUTPUT_PATH", "/data/export.csv");

Console.WriteLine($"Starting export: table={table}, output={outputPath}");

var connString = $"Host={host};Port={port};Database={database};Username={user};Password={password};Pooling=true";

await using var conn = new NpgsqlConnection(connString);
await conn.OpenAsync();

// COPY TO STDOUT CSV через текстовый экспорт (правильный API Npgsql)
var sql = $"COPY (SELECT * FROM {table}) TO STDOUT WITH (FORMAT CSV, HEADER TRUE, ENCODING 'UTF8')";

Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
await using var writer = new StreamWriter(outputPath, false, new UTF8Encoding(false));

using (var copyReader = await conn.BeginTextExportAsync(sql))
{
    char[] buffer = new char[8192];
    int read;
    while ((read = await copyReader.ReadAsync(buffer, 0, buffer.Length)) > 0)
    {
        await writer.WriteAsync(buffer, 0, read);
    }
}

Console.WriteLine("Export finished successfully");


