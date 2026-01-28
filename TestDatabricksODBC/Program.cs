using System.Data;
using System.Data.Odbc;

string connectionString = Environment.GetEnvironmentVariable("ODBC_CS") 
    ?? throw new Exception("ODBC_CS environment variable not set!");

using var connection = new OdbcConnection(connectionString);

try
{
    Console.WriteLine("Connecting to Databricks SQL Warehouse...");
    connection.Open();
    Console.WriteLine("Connected to Databricks SQL Warehouse.");
    Console.WriteLine("Type SQL and press Enter.");
    Console.WriteLine("Type 'quit' or 'exit' to close.\n");
}
catch (Exception ex)
{
    Console.WriteLine("Failed to connect:");
    Console.WriteLine(ex.Message);
    return;
}


if (args.Contains("--interactive"))
{
    while (true)
    {
        Console.Write("dbsql> ");
        string? input = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(input))
            continue;

        input = input.Trim();

        if (input.Equals("quit", StringComparison.OrdinalIgnoreCase) ||
            input.Equals("exit", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Goodbye.");
            break;
        }

        try
        {
            using var command = new OdbcCommand(input, connection);

            // Try reader first (SELECT, SHOW, DESCRIBE, etc.)
            using var reader = command.ExecuteReader();

            PrintResultSet(reader);
        }
        catch (OdbcException ex)
        {
            Console.WriteLine("ODBC Error:");
            Console.WriteLine(ex.Message);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error:");
            Console.WriteLine(ex.Message);
        }
    }
}
else
{
    Console.WriteLine("Executing test queries...\n");
    string[] testQueries = new[]
    {
        "SELECT current_database() AS current_db",
        "SHOW TABLES",
        "SELECT rand()",
        "SELECT COUNT(*) AS total_rows FROM information_schema.tables",
        "SELECT cos(rand()) AS cos_random_value, current_timestamp() as now_time"
    };

    var rnd = new Random();
    var cnt = 0;

    while (true)
    {
        cnt++;

        Console.WriteLine($"\n--- Test Query Cycle #{cnt} ---\n");

        foreach (var query in testQueries)
        {
            Console.WriteLine($">>> {query}");
            try
            {
                using var command = new OdbcCommand(query, connection);
                using var reader = command.ExecuteReader();
                PrintResultSet(reader);
            }
            catch (OdbcException ex)
            {
                Console.WriteLine("ODBC Error:");
                Console.WriteLine(ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error:");
                Console.WriteLine(ex.Message);
            }

            // Wait a random time between 1 and 3/4 second before next query 
            Thread.Sleep(rnd.Next(1, 750));
        }

        var waitTime = rnd.Next(1000, 100000);

        Console.WriteLine($"--- End of Cycle #{cnt}. Waiting {waitTime} ms before next cycle ---\n");

        // Wait a bit longer between full cycles
        Thread.Sleep(waitTime);
    }
}

Console.WriteLine("Closing connection.");

connection.Close();

Console.WriteLine("Connection closed. Program ended.");

static void PrintResultSet(OdbcDataReader reader)
{
    if (!reader.HasRows)
    {
        Console.WriteLine("(No rows returned)\n");
        return;
    }

    int fieldCount = reader.FieldCount;

    // Print column headers
    for (int i = 0; i < fieldCount; i++)
    {
        Console.Write(reader.GetName(i));
        if (i < fieldCount - 1)
            Console.Write(" | ");
    }
    Console.WriteLine();

    Console.WriteLine(new string('-', 80));

    // Print rows
    while (reader.Read())
    {
        for (int i = 0; i < fieldCount; i++)
        {
            string value = reader.IsDBNull(i)
                ? "NULL"
                : reader.GetValue(i).ToString()!;

            Console.Write(value);
            if (i < fieldCount - 1)
                Console.Write(" | ");
        }
        Console.WriteLine();
    }

    Console.WriteLine();
}
