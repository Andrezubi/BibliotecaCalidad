using Backend.Domain.Interfaces;
using Backend.Domain.Models;
using MySqlConnector;

namespace Backend.Infrastructure.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly IConfiguration _configuration;

        public UserRepository(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        private string GetConnectionString()
        {
            var connectionString =
                _configuration.GetConnectionString("LibraryDatabase");

            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "La conexión a la base de datos no está configurada."
                );
            }

            return connectionString;
        }

        public async Task<bool> ExistsByUsernameAsync(string username)
        {
            await using var connection =
                new MySqlConnection(GetConnectionString());

            await connection.OpenAsync();

            const string query = @"
                SELECT COUNT(*)
                FROM `User`
                WHERE Username = @Username;
            ";

            await using var command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@Username",
                username
            );

            var result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }

        public async Task<bool> ExistsByCIAsync(
            int ci,
            string? complement)
        {
            await using var connection =
                new MySqlConnection(GetConnectionString());

            await connection.OpenAsync();

            const string query = @"
                SELECT COUNT(*)
                FROM `User`
                WHERE CI = @CI
                AND COALESCE(Complement, '') = @Complement;
            ";

            await using var command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@CI",
                ci
            );

            command.Parameters.AddWithValue(
                "@Complement",
                complement ?? string.Empty
            );

            var result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result) > 0;
        }

        public async Task<int?> GetRoleIdByNameAsync(string roleName)
        {
            await using var connection =
                new MySqlConnection(GetConnectionString());

            await connection.OpenAsync();

            const string query = @"
                SELECT Id
                FROM Role
                WHERE Name = @RoleName
                LIMIT 1;
            ";

            await using var command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@RoleName",
                roleName
            );

            var result = await command.ExecuteScalarAsync();

            if (result == null || result == DBNull.Value)
            {
                return null;
            }

            return Convert.ToInt32(result);
        }

        public async Task<int> CreateAsync(User user)
        {
            await using var connection =
                new MySqlConnection(GetConnectionString());

            await connection.OpenAsync();

            const string query = @"
                INSERT INTO `User`
                (
                    CI,
                    Complement,
                    FirstName,
                    LastName,
                    Phone,
                    Username,
                    PasswordHash,
                    RoleId,
                    Status,
                    IsActive
                )
                VALUES
                (
                    @CI,
                    @Complement,
                    @FirstName,
                    @LastName,
                    @Phone,
                    @Username,
                    @PasswordHash,
                    @RoleId,
                    @Status,
                    @IsActive
                );

                SELECT LAST_INSERT_ID();
            ";

            await using var command =
                new MySqlCommand(query, connection);

            command.Parameters.AddWithValue(
                "@CI",
                user.CI
            );

            command.Parameters.AddWithValue(
                "@Complement",
                user.Complement ?? string.Empty
            );

            command.Parameters.AddWithValue(
                "@FirstName",
                user.FirstName
            );

            command.Parameters.AddWithValue(
                "@LastName",
                user.LastName
            );

            command.Parameters.AddWithValue(
                "@Phone",
                (object?)user.Phone ?? DBNull.Value
            );

            command.Parameters.AddWithValue(
                "@Username",
                user.Username
            );

            command.Parameters.AddWithValue(
                "@PasswordHash",
                user.PasswordHash
            );

            command.Parameters.AddWithValue(
                "@RoleId",
                user.RoleId
            );

            command.Parameters.AddWithValue(
                "@Status",
                user.Status
            );

            command.Parameters.AddWithValue(
                "@IsActive",
                user.IsActive
            );

            var result = await command.ExecuteScalarAsync();

            return Convert.ToInt32(result);
        }
    }
}