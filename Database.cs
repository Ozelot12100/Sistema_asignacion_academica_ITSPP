using System.Data;
using MySql.Data.MySqlClient;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace AsignacionAcademica.Api
{
    public static class Database
    {
        private static string GetConnectionString()
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            IConfiguration config = builder.Build();
            return config.GetConnectionString("DefaultConnection");
        }

        public static IDbConnection GetConnection()
        {
            var conn = new MySqlConnection(GetConnectionString());
            conn.Open();
            return conn;
        }
    }
}
