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
    public static async Task<dynamic> GenerateEnemyTeamData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
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
            GenerateWildNBMon(EnemyTeam, DataID);

        //Do Generate NPC NBMon Logic
        if(BattleCategory == 1)
            GenerateNPCTeam(EnemyTeam, DataID);

        if(BattleCategory == 2)
            GenerateBossTeam(EnemyTeam, DataID);

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
    public static void GenerateWildNBMon(List<NBMonBattleDataSave> EnemyTeam, int DataID)
    {
        NBMonBattleDatabase UsedData = RandomBattleDatabase.RandomBattleData[DataID];

        foreach(var MonsterDataFromRandomBattle in UsedData.MonsterDatas)
        {
            NBMonBattleDataSave MonsterData = new NBMonBattleDataSave();

            //Insert Data from Databse into NBMonBattleDataSave
            MonsterData.monsterId = MonsterDataFromRandomBattle.MonsterID;
            MonsterData.nickName = MonsterData.monsterId;
            MonsterData.skillList = MonsterDataFromRandomBattle.EquipSkill;
            MonsterData.uniqueSkillList = MonsterDataFromRandomBattle.InheritedSkill;
            MonsterData.passiveList = MonsterDataFromRandomBattle.Passive;

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(MonsterData.monsterId);

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

            //Once Done Processing This Monster Data, Add This Monster into Enemy Team
            EnemyTeam.Add(MonsterData);
        }
    }

    //Generate NPC Team
    public static void GenerateNPCTeam(List<NBMonBattleDataSave> EnemyTeam, int DataID)
    {
        FixedNBMonBattleDatabase UsedData = FixedBattleDatabase.NPCBattleData[DataID];

        foreach(var Monster in UsedData.MonsterTeam)
        {
            EnemyTeam.Add(Monster);
        }

        foreach(var Monster in EnemyTeam)
        {
            Monster.owner = UsedData.OwnerName;
        }
    }

    //Generate Boss Team
    public static void GenerateBossTeam(List<NBMonBattleDataSave> EnemyTeam, int DataID)
    {
        FixedNBMonBattleDatabase UsedData = FixedBattleDatabase.BossBattleData[DataID];

        foreach(var Monster in UsedData.MonsterTeam)
        {
            EnemyTeam.Add(Monster);
        }

        foreach(var Monster in EnemyTeam)
        {
            Monster.owner = "BOSS";
        }
    }
}