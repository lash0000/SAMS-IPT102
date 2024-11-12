using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using System.Threading.Tasks;

namespace SAMS_IPT102.Services
{
    public class DynamoDbService
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly string _tableName = "SAMS_attendance_log_attempts"; // Replace with your table name

        public DynamoDbService(IAmazonDynamoDB dynamoDb)
        {
            _dynamoDb = dynamoDb;
        }

        public async Task DeleteAllItemsAsync()
        {
            var table = Table.LoadTable(_dynamoDb, _tableName);
            var scanFilter = new ScanFilter();
            var search = table.Scan(scanFilter);

            List<Document> documentList = new List<Document>();
            do
            {
                documentList = await search.GetNextSetAsync();
                foreach (var document in documentList)
                {
                    await table.DeleteItemAsync(document);
                }
            } while (!search.IsDone);
        }
    }
}
