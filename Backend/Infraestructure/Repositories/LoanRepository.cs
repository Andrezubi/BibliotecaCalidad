using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using MySqlConnector;

namespace Backend.Infrastructure.Repositories
{
    public class LoanRepository : ILoanRepository
    {
        private readonly IConfiguration _configuration;

        public LoanRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<IEnumerable<LoanedBook>> GetLoanedBooksAsync()
        {
            var loanedBooks = new List<LoanedBook>();

            var connectionString =
                _configuration.GetConnectionString("LibraryDatabase");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "La conexión a la base de datos no está configurada."
                );
            }

            using var connection = new MySqlConnection(connectionString);

            await connection.OpenAsync();

            const string query = @"
                SELECT
                    c.Id AS CopyId,
                    b.Id AS BookId,
                    b.Title,
                    c.InternalCode,
                    c.Status
                FROM Copy c
                INNER JOIN Book b
                    ON b.Id = c.BookId
                WHERE c.Status = 'Loaned'
                    AND c.IsActive = TRUE
                    AND b.IsActive = TRUE;
            ";

            using var command = new MySqlCommand(query, connection);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                loanedBooks.Add(new LoanedBook
                {
                    CopyId = reader.GetInt32("CopyId"),
                    BookId = reader.GetInt32("BookId"),
                    Title = reader.GetString("Title"),
                    InternalCode = reader.GetString("InternalCode"),
                    Status = reader.GetString("Status")
                });
            }

            return loanedBooks;
        }
    }
}