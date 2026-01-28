using System.Data;
using System.Data.Odbc;

string connectionString = Environment.GetEnvironmentVariable("ODBC_CS") 
    ?? throw new Exception("ODBC_CS environment variable not set!");

using var connection = new OdbcConnection(connectionString);

try
{
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

connection.Close();

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
