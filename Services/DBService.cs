using System.Collections.Generic;

namespace SAMS_IPT102.Services
{
    public class DBService
    {
        public List<DBConnection> Connections { get; private set; }

        public DBService()
        {
            // Initialize connections for each database
            Connections = new List<DBConnection>
            {
                new DBConnection("sams_web_portal"),
                new DBConnection("sams_desktop_portal")
            };
        }

        // Method to get a specific connection by index
        public DBConnection GetConnection(int index)
        {
            return Connections[index];
        }
    }
}
