using MySql.Data.MySqlClient;

namespace SAMS_IPT102
{
    public class DBConnection
    {
        public string ConnectionString { get; private set; }

        // Constructor to initialize the connection string
        public DBConnection(string databaseName)
        {
            ConnectionString = $"Server=localhost;Database={databaseName};Uid=root;Pwd=;";
        }

        // Method to get a MySqlConnection object
        public MySqlConnection GetConnection()
        {
            return new MySqlConnection(ConnectionString);
        }
    }
}
