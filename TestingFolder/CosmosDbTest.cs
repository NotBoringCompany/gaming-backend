using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;


public static class CosmosDbTest{
    public class ToDoItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("partitionKey")]
        public string PartitionKey { get; set; }

        public string Description { get; set; }
    }

    public class ToDoItemLookup
    {
        public string ToDoItemId = "1";

        public string ToDoItemPartitionKeyValue = "/category";
    }

    [FunctionName("DocsBySqlQuery")]
    public static IActionResult Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]
            HttpRequest req,
        [CosmosDB(
            databaseName: "TestDb", //The Name of Database in Cosmos DB, Case Sensitive
            collectionName: "Data", //The Collection Name of the Database in Cosmos DB, Case Sensitive
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
}