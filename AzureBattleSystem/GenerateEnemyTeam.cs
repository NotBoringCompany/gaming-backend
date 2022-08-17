using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.Samples;
using System.Collections.Generic;
using PlayFab.EconomyModels;
using PlayFab.ServerModels;
using System.Net.Http;
using System.Net;
using System.Linq;
using Microsoft.Azure.Documents.Client;

public static class GenerateEnemyTeam
{
    //Helper Methods
    public static PlayFabServerInstanceAPI SetupServerAPI(dynamic args, FunctionExecutionContext<dynamic> context)
    {
        var apiSettings = new PlayFabApiSettings
        {
            TitleId = context.TitleAuthenticationContext.Id,
            DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
        };

        var authContext = new PlayFabAuthenticationContext
        {
            EntityId = context.TitleAuthenticationContext.EntityToken
        };

        return new PlayFabServerInstanceAPI(apiSettings, authContext);
    }

    //Cloud Method
    [FunctionName("GenerateEnemyTeamData")]
    public static async Task<dynamic> GenerateEnemyTeamData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, 
    [CosmosDB(ConnectionStringSetting = "CosmosDBConnection")] DocumentClient client, ILogger log)
    {   
        //Convert JsonString into a Class
        RandomBattleDatabase.GetData();
        FixedBattleDatabase.GetData();

        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        int DataID = new int();

        //Battle Category Consist of 0 which is a Wild NBMon Battle, 1 is an NPC Battle, and 2 is a Boss Battle.
        int BattleCategory = new int();

        //Check args["ThisMonsterUniqueID"] if it's null or not
        if(args["DataID"] != null)
            DataID = (int)args["DataID"];

        if(args["BattleCategory"] != null)
            BattleCategory = (int)args["BattleCategory"];
        
        //Do Generate Wild NBMon Logic
        if(BattleCategory == 0)
            GenerateWildNBMon(EnemyTeam, DataID, client);

        //Do Generate NPC NBMon Logic
        if(BattleCategory == 1)
            GenerateNPCTeam(EnemyTeam, DataID, client);

        if(BattleCategory == 2)
            GenerateBossTeam(EnemyTeam, DataID, client);

        //Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)}
                }
            }
        );

        //In Client, After This Function Finished, Pls Get Enemy Team Data from PlayFab Directly
        return null;
    }

    //Generate Wild NBMon
    public static void GenerateWildNBMon(List<NBMonBattleDataSave> EnemyTeam, int DataID, DocumentClient client)
    {
        //Declare Variable For Cosmos Usage
        var option = new FeedOptions(){ EnableCrossPartitionQuery = true };
        Uri wildUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "WildBattle");
        Uri monsterUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "NBMonData");

        // NBMonBattleDatabase UsedData = RandomBattleDatabase.RandomBattleData[DataID];
        NBMonBattleDatabase UsedData = client.CreateDocumentQuery<NBMonBattleDatabase>(wildUri, 
        $"SELECT * FROM db WHERE db.id = '{DataID}'",
        option).AsEnumerable().FirstOrDefault();

        foreach(var MonsterDataFromRandomBattle in UsedData.MonsterDatas)
        {
            NBMonBattleDataSave MonsterData = new NBMonBattleDataSave();

            //Insert Data from Databse into NBMonBattleDataSave
            MonsterData.owner = "WILD";
            MonsterData.monsterId = MonsterDataFromRandomBattle.MonsterID;
            MonsterData.nickName = MonsterData.monsterId;
            MonsterData.skillList = MonsterDataFromRandomBattle.EquipSkill;
            MonsterData.uniqueSkillList = MonsterDataFromRandomBattle.InheritedSkill;
            MonsterData.passiveList = MonsterDataFromRandomBattle.Passive;

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            // NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(MonsterData.monsterId);
            NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = client.CreateDocumentQuery<NBMonDatabase.MonsterInfoPlayFab>(monsterUri, 
            $"SELECT * FROM db WHERE db.monsterName = '{MonsterData.monsterId}'",
            option).AsEnumerable().FirstOrDefault();

            //Let's Generate This Monster's Level
            NBMonStatsCalculation.GenerateRandomLevel(MonsterData, UsedData.LevelRange);

            //Let's Generate This Monster UniqueID
            NBMonStatsCalculation.GenerateWildMonsterCredential(MonsterData);

            //Let's Generate This Monster's Quality
            NBMonStatsCalculation.GenerateThisMonsterQuality(MonsterData);

            //Let's Generate It's Potential Stats
            NBMonStatsCalculation.GenerateRandomPotentialValue(MonsterData, MonsterFromDatabase);

            //Generate This Monster Base Stats
            NBMonStatsCalculation.StatsCalculation(MonsterData, MonsterFromDatabase);

            //Fully Recovery HP and Energy
            NBMonTeamData.StatsPercentageChange(MonsterData, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(MonsterData, NBMonProperties.StatsType.Energy, 100);

            //Make Other Data not null.
            MonsterData.statusEffectList = new List<StatusEffectList>();
            MonsterData.temporaryPassives = new List<string>();
            MonsterData.setSkillByHPBoundaries = new List<NBMonBattleDataSave.SkillByHP>();

            //Once Done Processing This Monster Data, Add This Monster into Enemy Team
            EnemyTeam.Add(MonsterData);
        }
    }

    //Generate NPC Team
    public static void GenerateNPCTeam(List<NBMonBattleDataSave> EnemyTeam, int DataID, DocumentClient client)
    {
        //Declare Variable For Cosmos Usage
        var option = new FeedOptions(){ EnableCrossPartitionQuery = true };
        Uri npcUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "NPCBattle");

        // FixedNBMonBattleDatabase UsedData = FixedBattleDatabase.NPCBattleData[DataID];
        FixedNBMonBattleDatabase UsedData = client.CreateDocumentQuery<FixedNBMonBattleDatabase>(npcUri, 
        $"SELECT * FROM db WHERE db.id = '{DataID}'",
        option).AsEnumerable().FirstOrDefault();


        foreach(var Monster in UsedData.MonsterTeam)
        {
            EnemyTeam.Add(Monster);
        }

        foreach(var Monster in EnemyTeam)
        {
            NBMonTeamData.StatsPercentageChange(Monster, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(Monster, NBMonProperties.StatsType.Energy, 100);

            Monster.owner = UsedData.OwnerName;
        }
    }

    //Generate Boss Team
    public static void GenerateBossTeam(List<NBMonBattleDataSave> EnemyTeam, int DataID, DocumentClient client)
    {
        //Declare Variable For Cosmos Usage
        var option = new FeedOptions(){ EnableCrossPartitionQuery = true };
        Uri bossUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "BossBattle");
        
        // FixedNBMonBattleDatabase UsedData = FixedBattleDatabase.BossBattleData[DataID];
        FixedNBMonBattleDatabase UsedData = client.CreateDocumentQuery<FixedNBMonBattleDatabase>(bossUri, 
        $"SELECT * FROM db WHERE db.id = '{DataID}'",
        option).AsEnumerable().FirstOrDefault();

        foreach(var Monster in UsedData.MonsterTeam)
        {
            EnemyTeam.Add(Monster);
        }

        foreach(var Monster in EnemyTeam)
        {
            NBMonTeamData.StatsPercentageChange(Monster, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(Monster, NBMonProperties.StatsType.Energy, 100);

            Monster.owner = "BOSS";
        }
    }
}