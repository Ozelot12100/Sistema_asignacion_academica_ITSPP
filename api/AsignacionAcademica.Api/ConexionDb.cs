using System.Data;
using MySql.Data.MySqlClient;

namespace AsignacionAcademica.Api
{
    public class ConexionDb(IConfiguration config)
    {
        private readonly string _cadena = config.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'ConnectionStrings:DefaultConnection'. " +
                "Configúrala con 'dotnet user-secrets' o con la variable de entorno ConnectionStrings__DefaultConnection.");

        public IDbConnection Abrir()
        {
            var conn = new MySqlConnection(_cadena);
            conn.Open();
            return conn;
        }
    }
}
