using MKS.DeepWater.Web.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MKS.DeepWater.Web.Tests;

/// <summary>
/// Baza na czas jednego testu: SQLite w pamięci, ze schematem zbudowanym z migracji.
/// <para>
/// Świadomie <b>nie</b> provider "EF Core InMemory": tamten nie wymusza unikalnych indeksów
/// i nie zwraca <see cref="SqliteException"/> z kodem 19, więc testy unikalności numeru
/// członkowskiego i telefonu byłyby zielone niezależnie od tego, czy indeks istnieje.
/// </para>
/// <para>
/// Baza w pamięci żyje tak długo, jak otwarte połączenie - dlatego klasa trzyma je u siebie
/// i zamyka razem z kontekstem.
/// </para>
/// </summary>
public sealed class TestDb : IDisposable
{
    private readonly SqliteConnection _connection;

    private TestDb(SqliteConnection connection, ApplicationDbContext context)
    {
        _connection = connection;
        Context = context;
    }

    /// <summary>Kontekst do pracy w teście.</summary>
    public ApplicationDbContext Context { get; }

    public static TestDb Create()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var context = NewContext(connection);
        context.Database.Migrate();

        return new TestDb(connection, context);
    }

    /// <summary>
    /// Drugi kontekst na tej samej bazie - do sprawdzania, co faktycznie trafiło do bazy,
    /// bez podpowiedzi z pamięci zmian pierwszego kontekstu.
    /// </summary>
    public ApplicationDbContext NewContext() => NewContext(_connection);

    /// <summary>Wykonuje surowy SQL na bazie testowej (np. zdjęcie indeksu w teście kontrolnym).</summary>
    public void Execute(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    /// <summary>Odczytuje pojedynczą wartość tekstową (null, gdy zapytanie nic nie zwróci).</summary>
    public string? QuerySingleText(string sql)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = sql;
        return command.ExecuteScalar() as string;
    }

    public void Dispose()
    {
        Context.Dispose();
        _connection.Dispose();
    }

    private static ApplicationDbContext NewContext(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options);
}
