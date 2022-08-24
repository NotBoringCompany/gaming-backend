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
    // [FunctionName("MongoTest")]
    // public static async Task<dynamic> TestMongoDBFunction([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    // {
    //     //Default Setting to call MongoDB.
    //     MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);

    //     //Setting for Collection
    //     var collection = MongoHelper.db.GetCollection<BsonDocument>("users");
    //     List<BsonDocument> dataObtained = new List<BsonDocument>();

    //     //Data Processing to add all obtained datas into a list of BsonDocument.
    //     using (IAsyncCursor<BsonDocument> cursor = await collection.FindAsync(new BsonDocument()))
    //     {                
    //         while (await cursor.MoveNextAsync())
    //         {
    //             IEnumerable<BsonDocument> batch = cursor.Current;
    //             foreach (BsonDocument document in batch)
    //             {
    //                 dataObtained.Add(document);
    //             }
    //         }

    //     }

    //     //Let's create a filter to query single data
    //     var filter = Builders<BsonDocument>.Filter.Eq("name", "Rozen Croize");
    //     var result = collection.Find(filter).ToList();

    //     //Declare new variable using BsonDocument
    //     BsonDocument foundUser = new BsonDocument();

    //     //result[0], means the first data obtained.
    //     if(result.Count > 0)
    //     {
    //         foundUser = result[0];
    //     }

    //     return $"All Users = {dataObtained.ToJson()}, Single Requested User = {foundUser}";
    // }

    [FunctionName("MongoItem")]
    public static dynamic ItemMongoDB([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Declare Variable
        List<ItemsPlayFab> newItem = new List<ItemsPlayFab>();

        //Default Setting to call MongoDB.
        MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        List<String> items = new List<string>() {"Small Healing Potion"};

        foreach(var item in items)
        {
            //Let's create a filter to query single data
            var filter = Builders<BsonDocument>.Filter.Eq("Name", item);
            //Setting for Collection
            var collection = MongoHelper.db.GetCollection<BsonDocument>("itemData").Find(filter).FirstOrDefault().AsEnumerable();

            //Convert the Result into desire Class
            newItem.Add(BsonSerializer.Deserialize<ItemsPlayFab>(collection.ToBsonDocument()));
        }

        return JsonConvert.SerializeObject(newItem);
    }

    [FunctionName("MongoPassive")]
    public static dynamic PassiveMongoDB([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Default Setting to call MongoDB.
        MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("name", "Overexertion");
        //Setting for Collection
        var collection = MongoHelper.db.GetCollection<BsonDocument>("passiveData").Find(filter).FirstOrDefault().AsEnumerable();

        PassiveDatabase.PassiveInfoPlayFab newData = new PassiveDatabase.PassiveInfoPlayFab();

        //Convert the Result into desire Class
        newData = BsonSerializer.Deserialize<PassiveDatabase.PassiveInfoPlayFab>(collection.ToBsonDocument());

        return JsonConvert.SerializeObject(newData);
    }

    [FunctionName("MongoSkill")]
    public static dynamic SkillMongoDB([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Default Setting to call MongoDB.
        MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("skillName", "Small Chop");
        //Setting for Collection
        var collection = MongoHelper.db.GetCollection<BsonDocument>("skillData").Find(filter).FirstOrDefault().AsEnumerable();

        SkillsDataBase.SkillInfoPlayFab newData = new SkillsDataBase.SkillInfoPlayFab();

        //Convert the Result into desire Class
        newData = BsonSerializer.Deserialize<SkillsDataBase.SkillInfoPlayFab>(collection.ToBsonDocument());

        return JsonConvert.SerializeObject(newData);
    }

    [FunctionName("MongoMonster")]
    public static dynamic MonsterMongoDB([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Default Setting to call MongoDB.
        MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("monsterName", "Lamox");
        //Setting for Collection
        var collection = MongoHelper.db.GetCollection<BsonDocument>("monsterData").Find(filter).FirstOrDefault().AsEnumerable();

        NBMonDatabase.MonsterInfoPlayFab newData = new NBMonDatabase.MonsterInfoPlayFab();

        //Convert the Result into desire Class
        newData = BsonSerializer.Deserialize<NBMonDatabase.MonsterInfoPlayFab>(collection.ToBsonDocument());

        return JsonConvert.SerializeObject(newData);
    }
    
    [FunctionName("MongoBattle")]
    public static dynamic BattleData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Default Setting to call MongoDB.
        MongoHelper.settings.ServerApi = new ServerApi(ServerApiVersion.V1);

        //Let's create a filter to query single data
        var filter = Builders<BsonDocument>.Filter.Eq("dataId", 0);
        //Setting for Collection
        var collection = MongoHelper.db.GetCollection<BsonDocument>("bossData").Find(filter).FirstOrDefault().AsEnumerable();

        //Declare Variable
        FixedNBMonBattleDatabase newData = new FixedNBMonBattleDatabase();

        //Convert the Result into desire Class
        newData = BsonSerializer.Deserialize<FixedNBMonBattleDatabase>(collection.ToBsonDocument());

        return JsonConvert.SerializeObject(newData);
    }
}

public static class MongoHelper
{    
    public static MongoClientSettings settings = MongoClientSettings.FromConnectionString(Environment.GetEnvironmentVariable("MONGO_DB_CONNECTION", EnvironmentVariableTarget.Process)); 
    public static MongoClient client = new MongoClient(settings);
    public static IMongoDatabase db = client.GetDatabase("RealmHunter");
}