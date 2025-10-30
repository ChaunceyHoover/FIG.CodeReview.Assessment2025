using Microsoft.Data.SqlClient;

// (Note: Anything in parentheses is me writing out-of-code-review comments - it's not something I'd normally leave in the review.)

namespace UserManagement.Services
{
    public class UserService
    {
        // Move this to config file, preferably Azure Key Vault if available, and then wrap it in a Depdenency Injected model.
        // See https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-9.0 for more info
        //
        // Also, consider using another user instead of `sa` for security purposes
        private readonly string _connectionString = "Server=prod-db01;Database=UserDB;User Id=sa;Password=MySecretPassword123!;";

        public async Task<User> GetUserByIdAsync(int userId)
        {
            try
            {
                using var connection = new SqlConnection(_connectionString);
                connection.Open();

                // `Users` table probably won't change much, but just so you always know what data is coming back,
                // it may be best to explicitly name the columns to return.
                var query = @"
SELECT
    Id,
    Username,
    Email,
    CreatedDate,
    IsActive,
    Role
FROM
    Users
WHERE Id = @UserId";

                using var command = new SqlCommand(query, connection);

                // In this case, `userId` will always be an integer, so it would be safe to interpolate as it was. 
                // However, it is good practice to always parameterize dynamic queries to avoid SQL-injection and
                // not generate false positives in a code security scan
                command.Parameters.Add(new SqlParameter("@UserId", SqlDbType.Integer) { Value = userId });

                using var reader = command.ExecuteReader();

                if (reader.Read())
                {
                    // For security purposes, omitting `Password` - best to use a separate method or provide an additional
                    // parameter to include the password, as it won't be needed in most case
                    //
                    // (Note: Normally, I'd ask the dev if `Password` was needed instead of just removing it outright without asking.
                    //        Since this is an assessment, I am going to assume best practices)
                    return new User
                    {
                        Id = (int)reader["Id"],
                        Username = reader["Username"].ToString(),
                        Email = reader["Email"].ToString(),
                        CreatedDate = (DateTime)reader["CreatedDate"],
                        IsActive = (bool)reader["IsActive"],
                        Role = reader["Role"].ToString()
                    };
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log exception somewhere
                throw ex;
            }
        }

        public async Task<bool> ValidateUserAsync(string username, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Parameterizing query as this is a potential exploit vector since direct strings
            // could be passed in here
            var query = @"
SELECT
    COUNT(*)
FROM
    Users
WHERE
    Username = @Username
    AND Password = @Password"; // (Assuming password hashes are being compared)

            using var command = new SqlCommand(query, connection);
            command.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 64) { Value = username });
            command.Parameters.Add(new SqlParameter("@Password", SqlDbType.VarChar, 256) { Value = password }); // (Adjust length based off password hash length)

            var result = (int)command.ExecuteScalar();
            return result > 0;
        }

        public async Task<User> CreateUserAsync(string username, string email, string password)
        {
            using var connection = new SqlConnection(_connectionString);
            connection.Open();

            // Combine INSERT and SELECT into one query, saving a trip to the database and getting inserted data directly
            var insertQuery = @"
INSERT INTO Users (
    Username,
    Email,
    Password,
    CreatedDate,
    IsActive
) OUTPUT
    inserted.Id,
    inserted.Username,
    inserted.Email,
    inserted.CreatedDate,
    inserted.IsActive
VALUES
    (@Username, @Email, @PasswordHash, GETDATE(), 1);";
            // (NOTE: This is assuming MSSQL - if using MySql, instead add a SELECT statement at the end and filter by LAST_INSERT_ID()
            //        or add `RETURNING Id` if PostgreSQL)

            using var command = new SqlCommand(insertQuery, connection);
            command.Parameters.Add(new SqlParameter("@Username", SqlDbType.VarChar, 64) { Value = username });
            command.Parameters.Add(new SqlParameter("@Email", SqlDbType.NVarChar, 255) { Value = email });
            command.Parameters.Add(new SqlParameter("@PasswordHash", SqlDbType.VarChar, 256) { Value = Password });
            command.ExecuteReader();

            using var reader = command.ExecuteReader();

            if (reader.Read())
            {
                // As before, omitting `Password` for security purposes
                return new User
                {
                    Id = (int)reader["Id"],
                    Username = reader["Username"].ToString(),
                    Email = reader["Email"].ToString(),
                    CreatedDate = (DateTime)reader["CreatedDate"],
                    IsActive = (bool)reader["IsActive"]
                };
            }

            return null;
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime CreatedDate { get; set; }
        public bool IsActive { get; set; }
        public string Role { get; set; }
    }
}