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
    

    //Cloud Method
    [FunctionName("GenerateEnemyTeamData")]
    public static async Task<dynamic> GenerateEnemyTeamData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        // Deserialize function arguments from request body
        var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        var args = context.FunctionArgument;

        // Setup serverApi (Server API to PlayFab)
        var serverApi = AzureHelper.ServerAPISetup(args, context);

        // Get dataId and battleCategory from function arguments
        var dataId = (int?)args["DataID"] ?? 0;
        var battleCategory = (int?)args["BattleCategory"] ?? 0;

        // Generate enemy team based on battle category
        var enemyTeam = new List<NBMonBattleDataSave>();
        switch (battleCategory)
        {
            case 0:
                GenerateWildNBMon(enemyTeam, dataId);
                break;
            case 1:
                GenerateNPCTeam(enemyTeam, dataId);
                break;
            case 2:
                GenerateBossTeam(enemyTeam, dataId);
                break;
        }

        // Update user data in PlayFab
        await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
            Data = new Dictionary<string, string> {
                {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)},
                {"BattleCategory", battleCategory.ToString()}
            }
        });

        // Return null (or any desired response)
        return null;
    }


    //Generate Wild NBMon
    public static void GenerateWildNBMon(List<NBMonBattleDataSave> enemyTeam, int dataId)
    {
        // Get battle data from the database using the data ID
        NBMonBattleDatabase usedData = RandomBattleDatabase.GetWildBattleData(dataId);

        // Generate monster data for each monster in the battle data
        foreach (var randomMonsterData in usedData.MonsterDatas)
        {
            // Create a new NBMonBattleDataSave object to store the monster data
            NBMonBattleDataSave monsterData = new NBMonBattleDataSave();

            // Set the owner to "WILD", the monster ID, nickname, skills, and passives
            monsterData.owner = "WILD";
            monsterData.monsterId = randomMonsterData.MonsterID;
            monsterData.nickName = randomMonsterData.MonsterNickname;
            monsterData.skillList = randomMonsterData.EquipSkill;
            monsterData.uniqueSkillList = randomMonsterData.InheritedSkill;
            monsterData.passiveList = randomMonsterData.Passive;

            // Get the monster data from the database using the monster ID
            NBMonDatabase.MonsterInfoPlayFab monsterFromDatabase = NBMonDatabase.FindMonster(monsterData.monsterId);

            // Generate a random level for the monster based on the level range in the battle data
            NBMonStatsCalculation.GenerateRandomLevel(monsterData, usedData.LevelRange);

            // Generate a unique ID for the monster
            NBMonStatsCalculation.GenerateWildMonsterCredential(monsterData);

            // Generate a quality value for the monster
            NBMonStatsCalculation.GenerateThisMonsterQuality(monsterData);

            // Generate potential stats for the monster
            NBMonStatsCalculation.GenerateRandomPotentialValue(monsterData, monsterFromDatabase);

            // Generate base stats for the monster
            NBMonStatsCalculation.StatsCalculation(monsterData, monsterFromDatabase);

            // Set the monster's HP and energy to full
            NBMonTeamData.StatsPercentageChange(monsterData, NBMonProperties.StatsType.Hp, 100);
            NBMonTeamData.StatsPercentageChange(monsterData, NBMonProperties.StatsType.Energy, 100);

            // Initialize other data fields
            monsterData.statusEffectList = new List<StatusEffectList>();
            monsterData.temporaryPassives = new List<string>();
            monsterData.setSkillByHPBoundaries = new List<NBMonBattleDataSave.SkillByHP>();

            // Add the monster to the enemy team
            enemyTeam.Add(monsterData);
        }
    }

    //Generate NPC Team
    public static void GenerateNPCTeam(List<NBMonBattleDataSave> enemyTeam, int dataId)
    {
        FixedNBMonBattleDatabase usedData = FixedBattleDatabase.GetBattleData("npcData", dataId);
       
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
        FixedNBMonBattleDatabase usedData = FixedBattleDatabase.GetBattleData("bossData", dataId);

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