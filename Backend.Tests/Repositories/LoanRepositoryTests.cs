using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backend.Infraestructure.Repositories;
using Backend.Models;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Backend.Tests.Repositories;

// Pruebas del repositorio con EF Core InMemory.
// Cada caso usa una base independiente: no accede a MySQL.
public class LoanRepositoryTests
{
    private static DbContextOptions<LibraryDbcontext> CrearOpciones()
    {
        return new DbContextOptionsBuilder<LibraryDbcontext>()
            .UseInMemoryDatabase($"loan_tests_{Guid.NewGuid():N}",
                configuracion => configuracion.EnableNullChecks(false))
            .Options;
    }

    // 1. AddLoanAsync
    [Fact]
    public async Task AddLoanAsync_GuardaPrestamo()
    {
        var opciones = CrearOpciones();
        await using (var db = new LibraryDbcontext(opciones))
        {
            var repo = new LoanRepository(db);
            await repo.AddLoanAsync(new Loan
            {
                Id = 101,
                Status = "Active",
                LoanDate = new DateTime(2026, 10, 1),
                DueDate = new DateTime(2026, 10, 15)
            });
        }

        await using var verificacion = new LibraryDbcontext(opciones);
        Assert.True(await verificacion.Loans.AnyAsync(l => l.Id == 101));
    }

    // 2. GetLoanedBooksAsync
    [Fact]
    public async Task GetLoanedBooksAsync_DevuelveSoloCopiasPrestadasDeLibrosActivos()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var prestado = new Copystatus { Id = 2, Name = "Prestado" };
        var disponible = new Copystatus { Id = 1, Name = "Disponible" };
        var libroActivo = new Book { Id = 201, Title = "Libro activo", IsActive = true };
        var libroInactivo = new Book { Id = 202, Title = "Libro inactivo", IsActive = false };

        db.Copies.AddRange(
            new Copy
            {
                Id = 211,
                Book = libroActivo,
                Status = prestado,
                StatusId = 2,
                InternalCode = "CP-211",
                IsActive = true
            },
            new Copy
            {
                Id = 212,
                Book = libroInactivo,
                Status = prestado,
                StatusId = 2,
                InternalCode = "CP-212",
                IsActive = true
            },
            new Copy
            {
                Id = 213,
                Book = libroActivo,
                Status = disponible,
                StatusId = 1,
                InternalCode = "CP-213",
                IsActive = true
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = (await new LoanRepository(db).GetLoanedBooksAsync()).ToList();

        Assert.Single(resultado);
        Assert.Equal("Libro activo", resultado[0].Title);
        Assert.Equal("CP-211", resultado[0].InternalCode);
        Assert.Equal("Prestado", resultado[0].Status);
    }

    [Fact]
    public async Task GetLoanedBooksAsync_SinPrestados_RetornaListaVacia()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var resultado = await new LoanRepository(db).GetLoanedBooksAsync();
        Assert.Empty(resultado);
    }

    // 3. GetCopyByIdAsync
    [Fact]
    public async Task GetCopyByIdAsync_Existe_RetornaCopia()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var copia = new Copy
        {
            Id = 301,
            BookId = 10,
            StatusId = 1,
            InternalCode = "CP-301",
            IsActive = true
        };
        db.Copies.Add(copia);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await new LoanRepository(db).GetCopyByIdAsync(301);

        Assert.NotNull(resultado);
        Assert.Equal(copia.Id, resultado!.Id);
        Assert.Equal("CP-301", resultado.InternalCode);
    }

    [Fact]
    public async Task GetCopyByIdAsync_NoExiste_RetornaNull()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var resultado = await new LoanRepository(db).GetCopyByIdAsync(99999);
        Assert.Null(resultado);
    }

    // 4. GetActiveLoanByCopyIdAsync
    [Theory]
    [InlineData("Active", true)]
    [InlineData("Overdue", true)]
    [InlineData("Returned", false)]
    [InlineData("Lost", false)]
    public async Task GetActiveLoanByCopyIdAsync_FiltraEstadoDeLoanitem(
        string estadoItem, bool debeEncontrar)
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var libro = new Book { Id = 401, Title = "Libro prestado", IsActive = true };
        var copia = new Copy
        {
            Id = 402,
            Book = libro,
            StatusId = 2,
            InternalCode = "CP-402",
            IsActive = true
        };
        var lector = new User { Id = 41, FirstName = "Ana", LastName = "Paz" };
        var prestamo = new Loan
        {
            Id = 403,
            Borrower = lector,
            Status = "Active",
            LoanDate = new DateTime(2026, 10, 1)
        };
        var item = new Loanitem { Loan = prestamo, Copy = copia, Status = estadoItem };

        db.Loanitems.Add(item);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await new LoanRepository(db).GetActiveLoanByCopyIdAsync(402);

        Assert.Equal(debeEncontrar, resultado is not null);
        if (debeEncontrar)
        {
            Assert.Equal(prestamo.Id, resultado!.Id);
            Assert.Equal("Ana", resultado.Borrower.FirstName);
            Assert.Equal("Libro prestado", resultado.Loanitems.Single().Copy.Book.Title);
        }
    }

    [Fact]
    public async Task GetActiveLoanByCopyIdAsync_VariosCoincidentes_RetornaMasReciente()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        // Completar las relaciones requeridas por los Include del repositorio.
        var libro = new Book { Id = 419, Title = "Libro de prueba", IsActive = true };
        var lector = new User { Id = 42, FirstName = "Ana", LastName = "Paz" };
        var copia = new Copy
        {
            Id = 420,
            Book = libro,
            StatusId = 2,
            InternalCode = "CP-420",
            IsActive = true
        };
        var anterior = new Loan
        {
            Id = 421,
            Borrower = lector,
            Status = "Active",
            LoanDate = new DateTime(2026, 9, 1)
        };
        var reciente = new Loan
        {
            Id = 422,
            Borrower = lector,
            Status = "Active",
            LoanDate = new DateTime(2026, 10, 1)
        };

        db.Loanitems.AddRange(
            new Loanitem { Loan = anterior, Copy = copia, Status = "Active" },
            new Loanitem { Loan = reciente, Copy = copia, Status = "Overdue" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        Assert.Equal(2, await db.Loanitems.CountAsync(li => li.CopyId == 420));
        var resultado = await new LoanRepository(db).GetActiveLoanByCopyIdAsync(420);

        Assert.NotNull(resultado);
        Assert.Equal(reciente.Id, resultado!.Id);
    }

    // 5. SaveChangesAsync
    [Fact]
    public async Task SaveChangesAsync_PersisteCambiosPendientes()
    {
        var opciones = CrearOpciones();
        await using (var db = new LibraryDbcontext(opciones))
        {
            db.Copies.Add(new Copy
            {
                Id = 501,
                BookId = 1,
                StatusId = 1,
                InternalCode = "CP-501",
                IsActive = true
            });
            await new LoanRepository(db).SaveChangesAsync();
        }

        await using var verificacion = new LibraryDbcontext(opciones);
        Assert.True(await verificacion.Copies.AnyAsync(c => c.Id == 501));
    }

    // 6. AddFineAsync
    [Fact]
    public async Task AddFineAsync_GuardaMulta()
    {
        var opciones = CrearOpciones();
        await using (var db = new LibraryDbcontext(opciones))
        {
            await new LoanRepository(db).AddFineAsync(new Fine
            {
                Id = 601,
                LoanId = 101,
                CopyId = 201,
                DailyRate = 10m,
                OverdueDays = 2,
                Amount = 20m,
                Status = "Pending"
            });
        }

        await using var verificacion = new LibraryDbcontext(opciones);
        var resultado = await verificacion.Fines.SingleOrDefaultAsync(f => f.Id == 601);
        Assert.NotNull(resultado);
        Assert.Equal(20m, resultado!.Amount);
    }

    // 7. GetFineByLoanCopyAsync
    [Fact]
    public async Task GetFineByLoanCopyAsync_Existe_RetornaMultaCorrecta()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var multa = new Fine
        {
            Id = 701,
            LoanId = 111,
            CopyId = 222,
            Amount = 30m,
            Status = "Pending"
        };
        db.Fines.Add(multa);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await new LoanRepository(db).GetFineByLoanCopyAsync(111, 222);

        Assert.NotNull(resultado);
        Assert.Equal(multa.Id, resultado!.Id);
    }

    [Fact]
    public async Task GetFineByLoanCopyAsync_NoCoincide_RetornaNull()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        db.Fines.Add(new Fine
        {
            Id = 702,
            LoanId = 111,
            CopyId = 222,
            Amount = 30m,
            Status = "Pending"
        });
        await db.SaveChangesAsync();

        var resultado = await new LoanRepository(db).GetFineByLoanCopyAsync(111, 999);

        Assert.Null(resultado);
    }

    // 8. GetFirstAvailableCopyByBookIdAsync
    [Fact]
    public async Task GetFirstAvailableCopyByBookIdAsync_DevuelveDisponibleConMenorId()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var libro = new Book { Id = 801, Title = "Libro activo", IsActive = true };
        var menor = new Copy
        {
            Id = 802,
            Book = libro,
            StatusId = 1,
            InternalCode = "CP-802",
            IsActive = true
        };
        db.Copies.AddRange(
            new Copy
            {
                Id = 803,
                Book = libro,
                StatusId = 1,
                InternalCode = "CP-803",
                IsActive = true
            },
            menor,
            new Copy
            {
                Id = 800,
                Book = libro,
                StatusId = 2,
                InternalCode = "CP-800",
                IsActive = true
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await new LoanRepository(db).GetFirstAvailableCopyByBookIdAsync(801);

        Assert.NotNull(resultado);
        Assert.Equal(menor.Id, resultado!.Id);
    }

    [Fact]
    public async Task GetFirstAvailableCopyByBookIdAsync_LibroInactivo_RetornaNull()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var libro = new Book { Id = 811, Title = "Libro inactivo", IsActive = false };
        db.Copies.Add(new Copy
        {
            Id = 812,
            Book = libro,
            StatusId = 1,
            InternalCode = "CP-812",
            IsActive = true
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await new LoanRepository(db).GetFirstAvailableCopyByBookIdAsync(811);

        Assert.Null(resultado);
    }

    [Fact]
    public async Task GetFirstAvailableCopyByBookIdAsync_SinDisponibles_RetornaNull()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var libro = new Book { Id = 821, Title = "Libro prestado", IsActive = true };
        db.Copies.Add(new Copy
        {
            Id = 822,
            Book = libro,
            StatusId = 2,
            InternalCode = "CP-822",
            IsActive = true
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await new LoanRepository(db).GetFirstAvailableCopyByBookIdAsync(821);

        Assert.Null(resultado);
    }

    // 9. GetAllLoansAsync (tambien cubre GetLoanStatusDisplayName)
    [Theory]
    [InlineData("Active", "Activo")]
    [InlineData("Completed", "Completado")]
    [InlineData("Overdue", "Vencido")]
    [InlineData("Lost", "Perdido")]
    [InlineData("Other", "Other")]
    public async Task GetAllLoansAsync_ConvierteEstadoParaMostrar(
        string estado, string esperado)
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var prestamo = new Loan
        {
            Id = 901,
            Status = estado,
            LoanDate = new DateTime(2026, 10, 1),
            DueDate = new DateTime(2026, 10, 15),
            Borrower = new User { Id = 91, FirstName = "Ana", LastName = "Paz" },
            Librarian = new User { Id = 92, FirstName = "Luis", LastName = "Rojas" }
        };
        db.Loans.Add(prestamo);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = (await new LoanRepository(db).GetAllLoansAsync()).Single();

        Assert.Equal(esperado, resultado.StatusDisplayName);
        Assert.Equal("Ana Paz", resultado.BorrowerName);
        Assert.Equal("Luis Rojas", resultado.LibrarianName);
    }

    [Fact]
    public async Task GetAllLoansAsync_CuentaItemsYDevueltos()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var prestamo = new Loan
        {
            Id = 911,
            Status = "Active",
            Borrower = new User { Id = 93, FirstName = "Ana", LastName = "Paz" },
            Librarian = new User { Id = 94, FirstName = "Luis", LastName = "Rojas" }
        };
        db.Loanitems.AddRange(
            new Loanitem { Loan = prestamo, CopyId = 912, Status = "Returned" },
            new Loanitem { Loan = prestamo, CopyId = 913, Status = "Active" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = (await new LoanRepository(db).GetAllLoansAsync()).Single();

        Assert.Equal(2, resultado.ItemsCount);
        Assert.Equal(1, resultado.ReturnedItemsCount);
    }

    [Fact]
    public async Task GetAllLoansAsync_OrdenaPorFechaDescendente()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var lector = new User { Id = 95, FirstName = "Ana", LastName = "Paz" };
        var bibliotecario = new User { Id = 96, FirstName = "Luis", LastName = "Rojas" };
        db.Loans.AddRange(
            new Loan
            {
                Id = 921,
                Status = "Active",
                Borrower = lector,
                Librarian = bibliotecario,
                LoanDate = new DateTime(2026, 9, 1)
            },
            new Loan
            {
                Id = 922,
                Status = "Active",
                Borrower = lector,
                Librarian = bibliotecario,
                LoanDate = new DateTime(2026, 10, 1)
            });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = (await new LoanRepository(db).GetAllLoansAsync()).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Equal(922, resultado[0].Id);
        Assert.Equal(921, resultado[1].Id);
    }

    // 10. GetLoanWithItemsByIdAsync
    [Fact]
    public async Task GetLoanWithItemsByIdAsync_Existe_IncluyeRelaciones()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var prestamo = new Loan
        {
            Id = 1001,
            Status = "Active",
            Borrower = new User { Id = 101, FirstName = "Ana", LastName = "Paz" },
            Librarian = new User { Id = 102, FirstName = "Luis", LastName = "Rojas" }
        };
        var copia = new Copy
        {
            Id = 1002,
            InternalCode = "CP-1002",
            StatusId = 2,
            Book = new Book { Id = 1003, Title = "Libro incluido", IsActive = true },
            Status = new Copystatus { Id = 2, Name = "Prestado" }
        };
        db.Loanitems.Add(new Loanitem { Loan = prestamo, Copy = copia, Status = "Active" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = await new LoanRepository(db).GetLoanWithItemsByIdAsync(1001);

        Assert.NotNull(resultado);
        Assert.Equal("Ana", resultado!.Borrower.FirstName);
        Assert.Equal("Luis", resultado.Librarian.FirstName);
        Assert.Single(resultado.Loanitems);
        var item = resultado.Loanitems.Single();
        Assert.Equal("Libro incluido", item.Copy.Book.Title);
        Assert.Equal("Prestado", item.Copy.Status.Name);
    }

    [Fact]
    public async Task GetLoanWithItemsByIdAsync_NoExiste_RetornaNull()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var resultado = await new LoanRepository(db).GetLoanWithItemsByIdAsync(99999);
        Assert.Null(resultado);
    }

    // 11. GetLoansForStatusUpdateAsync
    [Fact]
    public async Task GetLoansForStatusUpdateAsync_ExcluyeCompletedYLost()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        db.Loans.AddRange(
            new Loan { Id = 1101, Status = "Active" },
            new Loan { Id = 1102, Status = "Overdue" },
            new Loan { Id = 1103, Status = "Completed" },
            new Loan { Id = 1104, Status = "Lost" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = (await new LoanRepository(db).GetLoansForStatusUpdateAsync()).ToList();

        Assert.Equal(2, resultado.Count);
        Assert.Contains(resultado, l => l.Status == "Active");
        Assert.Contains(resultado, l => l.Status == "Overdue");
        Assert.DoesNotContain(resultado, l => l.Status == "Completed");
        Assert.DoesNotContain(resultado, l => l.Status == "Lost");
    }

    [Fact]
    public async Task GetLoansForStatusUpdateAsync_IncluyeLoanitems()
    {
        await using var db = new LibraryDbcontext(CrearOpciones());
        var prestamo = new Loan { Id = 1111, Status = "Active" };
        db.Loanitems.Add(new Loanitem
        {
            Loan = prestamo,
            CopyId = 1112,
            Status = "Active"
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        var resultado = (await new LoanRepository(db).GetLoansForStatusUpdateAsync()).Single();

        Assert.Single(resultado.Loanitems);
    }
}

// Estas pruebas usan MySQL real de PRUEBAS (no InMemory).
// Estan deshabilitadas hasta crear biblioteca_test, cargar el esquema y
// configurar TEST_DB_CONNECTION. Para ejecutarlas, quitar Skip de cada Fact.
public class LoanRepositoryMySqlTests
{
    private static LibraryDbcontext CrearContextoMySql()
    {
        var conexion = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION");
        if (string.IsNullOrWhiteSpace(conexion))
            throw new InvalidOperationException("Configura TEST_DB_CONNECTION.");

        var datos = new MySqlConnector.MySqlConnectionStringBuilder(conexion);
        if (!string.Equals(datos.Database, "biblioteca_test",
                StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo se permite biblioteca_test.");

        var opciones = new DbContextOptionsBuilder<LibraryDbcontext>()
            .UseMySql(conexion, ServerVersion.AutoDetect(conexion))
            .Options;
        var contexto = new LibraryDbcontext(opciones);
        if (!string.Equals(contexto.Database.GetDbConnection().Database,
                "biblioteca_test", StringComparison.OrdinalIgnoreCase))
        {
            contexto.Dispose();
            throw new InvalidOperationException("Conexion no apunta a biblioteca_test.");
        }
        return contexto;
    }

    // 12. BeginTransactionAsync
    [Fact]
    [Trait("Category", "MySql")]
    public async Task BeginTransactionAsync_AbreTransaccion()
    {
        await using var db = CrearContextoMySql();
        var repo = new LoanRepository(db);
        await using var transaccion = await repo.BeginTransactionAsync();
        Assert.NotNull(transaccion);
        await transaccion.RollbackAsync();
    }

    // 13. GetFirstAvailableCopyByBookIdForUpdateAsync - sin resultados
    [Fact]
    [Trait("Category", "MySql")]
    public async Task GetFirstAvailableCopyByBookIdForUpdateAsync_SinCopia_RetornaNull()
    {
        await using var db = CrearContextoMySql();
        const uint libroInexistente = uint.MaxValue;
        Assert.False(await db.Copies.AnyAsync(c => c.BookId == libroInexistente));

        var repo = new LoanRepository(db);
        await using var transaccion = await repo.BeginTransactionAsync();
        var resultado = await repo.GetFirstAvailableCopyByBookIdForUpdateAsync(libroInexistente);
        Assert.Null(resultado);
        await transaccion.RollbackAsync();
    }

    // 13. GetFirstAvailableCopyByBookIdForUpdateAsync - copia disponible
    [Fact]
    [Trait("Category", "MySql")]
    public async Task GetFirstAvailableCopyByBookIdForUpdateAsync_ConCopia_RetornaMenorId()
    {
        await using var db = CrearContextoMySql();
        var candidata = await db.Copies.AsNoTracking()
            .Where(c => c.StatusId == 1 && c.IsActive == true)
            .OrderBy(c => c.Id).FirstOrDefaultAsync();
        Assert.NotNull(candidata); // Preparar al menos una copia en biblioteca_test.

        var esperado = await db.Copies.AsNoTracking()
            .Where(c => c.BookId == candidata!.BookId && c.StatusId == 1 && c.IsActive == true)
            .OrderBy(c => c.Id).FirstAsync();

        var repo = new LoanRepository(db);
        await using var transaccion = await repo.BeginTransactionAsync();
        var resultado = await repo.GetFirstAvailableCopyByBookIdForUpdateAsync(candidata.BookId);

        Assert.NotNull(resultado);
        Assert.Equal(esperado.Id, resultado!.Id);
        await transaccion.RollbackAsync();
    }
}
