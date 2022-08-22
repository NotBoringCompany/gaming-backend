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
    ILogger log)
    {   
        //Convert JsonString into a Class
        RandomBattleDatabase.GetData();
        FixedBattleDatabase.GetData();

        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> enemyTeam = new List<NBMonBattleDataSave>();
        int dataId = new int();

        //Battle Category Consist of 0 which is a Wild NBMon Battle, 1 is an NPC Battle, and 2 is a Boss Battle.
        int battleCategory = new int();

        //Check args["ThisMonsterUniqueID"] if it's null or not
        if(args["DataID"] != null)
            dataId = (int)args["DataID"];

        if(args["BattleCategory"] != null)
            battleCategory = (int)args["BattleCategory"];
        
        //Do Generate Wild NBMon Logic
        if(battleCategory == 0)
            GenerateWildNBMon(enemyTeam, dataId);

        //Do Generate NPC NBMon Logic
        if(battleCategory == 1)
            GenerateNPCTeam(enemyTeam, dataId);

        if(battleCategory == 2)
            GenerateBossTeam(enemyTeam, dataId);

        //Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)}
                }
            }
        );

        //In Client, After This Function Finished, Pls Get Enemy Team Data from PlayFab Directly
        return null;
    }

    //Generate Wild NBMon
    public static void GenerateWildNBMon(List<NBMonBattleDataSave> enemyTeam, int dataId)
    {
        NBMonBattleDatabase usedData = RandomBattleDatabase.RandomBattleData[dataId];

        foreach(var randomMonsterData in usedData.MonsterDatas)
        {
            NBMonBattleDataSave monsterData = new NBMonBattleDataSave();

            //Insert Data from Databse into NBMonBattleDataSave
            monsterData.owner = "WILD";
            monsterData.monsterId = randomMonsterData.MonsterID;
            monsterData.nickName = monsterData.monsterId;
            monsterData.skillList = randomMonsterData.EquipSkill;
            monsterData.uniqueSkillList = randomMonsterData.InheritedSkill;
            monsterData.passiveList = randomMonsterData.Passive;

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab monsterFromDatabase = NBMonDatabase.FindMonster(monsterData.monsterId);
            
            /*
            NBMonDatabase.MonsterInfoPlayFab monsterFromDatabase = client.CreateDocumentQuery<NBMonDatabase.MonsterInfoPlayFab>(monsterUri, 
            $"SELECT * FROM db WHERE db.monsterName = '{monsterData.monsterId}'",
            option).AsEnumerable().FirstOrDefault();
            */

            //Let's Generate This Monster's Level
            NBMonStatsCalculation.GenerateRandomLevel(monsterData, usedData.LevelRange);

            //Let's Generate This Monster UniqueID
            NBMonStatsCalculation.GenerateWildMonsterCredential(monsterData);

            //Let's Generate This Monster's Quality
            NBMonStatsCalculation.GenerateThisMonsterQuality(monsterData);

            //Let's Generate It's Potential Stats
            NBMonStatsCalculation.GenerateRandomPotentialValue(monsterData, monsterFromDatabase);

            //Generate This Monster Base Stats
            NBMonStatsCalculation.StatsCalculation(monsterData, monsterFromDatabase);

            //Fully Recovery HP and Energy
            NBMonTeamData.StatsPercentageChange(monsterData, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(monsterData, NBMonProperties.StatsType.Energy, 100);

            //Make Other Data not null.
            monsterData.statusEffectList = new List<StatusEffectList>();
            monsterData.temporaryPassives = new List<string>();
            monsterData.setSkillByHPBoundaries = new List<NBMonBattleDataSave.SkillByHP>();

            //Once Done Processing This Monster Data, Add This Monster into Enemy Team
            enemyTeam.Add(monsterData);
        }
    }

    //Generate NPC Team
    public static void GenerateNPCTeam(List<NBMonBattleDataSave> enemyTeam, int dataID)
    {
        FixedNBMonBattleDatabase usedData = FixedBattleDatabase.NPCBattleData[dataID];
       
        foreach(var monster in usedData.MonsterTeam)
        {
            enemyTeam.Add(monster);
        }

        foreach(var monster in enemyTeam)
        {
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Energy, 100);

            monster.owner = usedData.OwnerName;
        }
    }

    //Generate Boss Team
    public static void GenerateBossTeam(List<NBMonBattleDataSave> enemyTeam, int dataId)
    {   
        FixedNBMonBattleDatabase usedData = FixedBattleDatabase.BossBattleData[dataId];

        foreach(var monster in usedData.MonsterTeam)
        {
            enemyTeam.Add(monster);
        }

        foreach(var monster in enemyTeam)
        {
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Energy, 100);

            monster.owner = "BOSS";
        }
    }
}