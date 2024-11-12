using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DocumentModel;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SAMS_IPT102.Services
{
    public class DynamoDbService
    {
        private readonly IAmazonDynamoDB _dynamoDb;
        private readonly ILogger<DynamoDbService> _logger; // Inject ILogger
        private readonly string _tableName = "SAMS_attendance_log_attempts"; // Replace with your table name

        // Constructor with ILogger injection
        public DynamoDbService(IAmazonDynamoDB dynamoDb, ILogger<DynamoDbService> logger)
        {
            _dynamoDb = dynamoDb;
            _logger = logger;
        }

        public async Task DeleteAllItemsAsync()
        {
            try
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
                        try
                        {
                            await table.DeleteItemAsync(document);
                            _logger.LogInformation($"Successfully deleted item with ID: {document["id"]}"); // Log successful deletions
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to delete item: {document}. Error: {ex.Message}"); // Log errors
                        }
                    }
                } while (!search.IsDone);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error occurred while scanning/deleting items: {ex.Message}"); // Log scan failures
                throw; // Rethrow exception after logging
            }
        }
    }
}