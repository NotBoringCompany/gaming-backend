using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using Microsoft.Azure.Documents;

public static class CosmosDbTest
{
    public class ToDoItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public string Description { get; set; }
    }

    public class ToDoItemLookup
    {
        public string ToDoItemId = "1";

        public string ToDoItemPartitionKeyValue = "/category";
    }

    [FunctionName("DocsBySqlQuery")]
    public static IActionResult Run1(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req,
        [CosmosDB(
            databaseName: "TestDb",
            collectionName: "Data",
            ConnectionStringSetting = "CosmosDBConnection",
            SqlQuery = "SELECT * FROM c")]
            IEnumerable<ToDoItem> toDoItems,
        ILogger log)
    {
        log.LogInformation("C# HTTP trigger function processed a request.");
        foreach (ToDoItem toDoItem in toDoItems)
        {
            log.LogInformation(toDoItem.Description);
        }
        return new OkResult();
    }

    [FunctionName("DocsByUsingDocumentClient")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post",
                Route = null)]HttpRequest req,
            [CosmosDB(
                databaseName: "TestDb",
                collectionName: "Data",
                ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            
            List<ToDoItem> toDoItems = new List<ToDoItem>();

            var option = new FeedOptions(){ EnableCrossPartitionQuery = true };

            Uri collectionUri = UriFactory.CreateDocumentCollectionUri("TestDb", "Data");

            // List<ToDoItem> query = client.CreateDocumentQuery<ToDoItem>(collectionUri, "SELECT * FROM database db WHERE db.description = 'This is work description'",option).ToList();
            List<ToDoItem> query = client.CreateDocumentQuery<ToDoItem>(collectionUri, "SELECT * FROM database db",option).ToList();

            // log.LogInformation(query.HasMoreResults.ToString());

            // while (query.HasMoreResults)
            // {
            //     foreach (ToDoItem result in await query.ExecuteNextAsync())
            //     {
            //         toDoItems.Add(result);
            //     }
            // }

            log.LogInformation(JsonConvert.SerializeObject(query));
            
            return new OkResult();
        }
}
