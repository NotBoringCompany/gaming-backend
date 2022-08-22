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
using MongoDB.Driver;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

///Please download MongoDB Nuget Package.

public static class MongoDBTest
{
    //setting for the MongoDB.
    public static MongoClientSettings settings = MongoClientSettings.FromConnectionString(Environment.GetEnvironmentVariable("MONGO_DB_CONNECTION", EnvironmentVariableTarget.Process));

    [FunctionName("TestMongoDB")]
    public static async Task<dynamic> TestMongoDBFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Default Setting to call MongoDB.
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(settings);

        //Setting to call Database
        var database = client.GetDatabase("myFirstDatabase");

        //Setting for Collection
        var collection = database.GetCollection<BsonDocument>("users");
        List<BsonDocument> dataObtained = new List<BsonDocument>();

        //Data Processing to add all obtained datas into a list of BsonDocument.
        using (IAsyncCursor<BsonDocument> cursor = await collection.FindAsync(new BsonDocument()))
        {                
            while (await cursor.MoveNextAsync())
            {
                IEnumerable<BsonDocument> batch = cursor.Current;
                foreach (BsonDocument document in batch)
                {
                    dataObtained.Add(document);
                }
            }

        }

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("name", "Rozen Croize");
        var result = collection.Find(filter).ToList();

        //Declare new variable using BsonDocument
        BsonDocument foundUser = new BsonDocument();

        //result[0], means the first data obtained.
        if(result.Count > 0)
        {
            foundUser = result[0];
        }

        return $"All Users = {dataObtained.ToJson()}, Single Requested User = {foundUser}";
    }

    [FunctionName("ItemMongoDB")]
    public static dynamic ItemMongoDB([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Default Setting to call MongoDB.
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(settings);

        //Setting to call Database
        var database = client.GetDatabase("myFirstDatabase");

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("Name", "Small Healing Potion");
        //Setting for Collection
        var collection = database.GetCollection<BsonDocument>("itemData").Find(filter).FirstOrDefault().AsEnumerable();;

        ItemsPlayFab newItem = new ItemsPlayFab();

        //Convert the Result into desire Class
        newItem = BsonSerializer.Deserialize<ItemsPlayFab>(collection.ToBsonDocument());

        return JsonConvert.SerializeObject(newItem);
    }

    [FunctionName("PassiveMongoDB")]
    public static dynamic PassiveMongoDB([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Default Setting to call MongoDB.
        settings.ServerApi = new ServerApi(ServerApiVersion.V1);
        var client = new MongoClient(settings);

        //Setting to call Database
        var database = client.GetDatabase("myFirstDatabase");

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("name", "Overexertion");
        //Setting for Collection
        var collection = database.GetCollection<BsonDocument>("passiveData").Find(filter).FirstOrDefault().AsEnumerable();;

        PassiveDatabase.PassiveInfoPlayFab newData = new PassiveDatabase.PassiveInfoPlayFab();

        //Convert the Result into desire Class
        newData = BsonSerializer.Deserialize<PassiveDatabase.PassiveInfoPlayFab>(collection.ToBsonDocument());

        return JsonConvert.SerializeObject(newData);
    }
}