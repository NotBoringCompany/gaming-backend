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

public static class InitialTeamSetup
{
    public class HumanBattleData
    {
        public NBMonBattleDataSave playerHumanData;
        public NBMonBattleDataSave enemyHumanData; 
    }


    public static void GenerateHumanData(HumanBattleData humanBattleData, int battleCategory, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, string playerDisplayName, string playerPlayFabId)
    {
        //Let's generate NPC Human Data if the battle is an NPC Battle
        if(battleCategory == 1 || battleCategory == 3) //Indicating NPC Battle or PvP Battle
        {
            humanBattleData.enemyHumanData = HumanBattleBaseData.defaultHumanBattleData();
            humanBattleData.enemyHumanData.maxHp = GenerateHumanHP(enemyTeam);
            humanBattleData.enemyHumanData.hp = GenerateHumanHP(enemyTeam);
            humanBattleData.enemyHumanData.owner = enemyTeam[0].owner;
            humanBattleData.enemyHumanData.nickName = enemyTeam[0].owner;
            humanBattleData.enemyHumanData.uniqueId = playerPlayFabId + enemyTeam[0].owner;
        }

        //Let's generate Player Human Data first.
        humanBattleData.playerHumanData = HumanBattleBaseData.defaultHumanBattleData();
        humanBattleData.playerHumanData.maxHp = GenerateHumanHP(playerTeam);
        humanBattleData.playerHumanData.hp = GenerateHumanHP(playerTeam);
        humanBattleData.playerHumanData.owner = playerDisplayName;
        humanBattleData.playerHumanData.nickName = playerDisplayName;
        humanBattleData.playerHumanData.uniqueId = playerPlayFabId + playerDisplayName;
    }

    public static int GenerateHumanHP(List<NBMonBattleDataSave> team)
    {
        int maxAvgHP = 0;

        foreach(var monster in team)
        {
            maxAvgHP += monster.maxHp;
        }

        var newHp = maxAvgHP/team.Count;
        return newHp;
    }

    //Cloud Methods
    [FunctionName("InitialTeamSetupAzure")]
    public static async Task<dynamic> InitialTeamSetupAzure([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "MoraleGaugeData", "BattleCategory"}
            }
        );

        var requestUserProfile = await serverApi.GetPlayerProfileAsync(
            new GetPlayerProfileRequest {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        BattleMoraleGauge.MoraleData moraleData = new BattleMoraleGauge.MoraleData();
        List<NBMonBattleDataSave> playerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> enemyTeam = new List<NBMonBattleDataSave>();
        List<string> allMonsterUniqueID_BF = new List<string>();
        List<string> team1UniqueID_BF = new List<string>();
        List<string> team2UniqueID_BF = new List<string>();
        HumanBattleData humanBattleData = new HumanBattleData();
        int battleCategory = 0;

        //Convert from json to NBmonBattleDataSave
        playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        enemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);

        if(requestTeamInformation.Result.Data.ContainsKey("BattleCategory"))
            battleCategory = int.Parse(requestTeamInformation.Result.Data["BattleCategory"].Value);

        //Check if the player has the MoraleGaugeData, if no. Create a new one
        if(requestTeamInformation.Result.Data.ContainsKey("MoraleGaugeData"))
        {
            moraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestTeamInformation.Result.Data["MoraleGaugeData"].Value);
        }

        string displayName = requestUserProfile.Result.PlayerProfile.DisplayName;
        string PlayFabID = context.CallerEntityProfile.Lineage.MasterPlayerAccountId;

        //Genrate Human Battle Data
        GenerateHumanData(humanBattleData, battleCategory, playerTeam, enemyTeam, displayName, PlayFabID);

        // Loop through Team 1 and add monsters to respective lists
        for (int i = 0; i < Math.Min(playerTeam.Count, 2); i++)
        {
            var monster = playerTeam[i];
            allMonsterUniqueID_BF.Add(monster.uniqueId);
            team1UniqueID_BF.Add(monster.uniqueId);
        }

        // Add Player Unique ID
        allMonsterUniqueID_BF.Add(humanBattleData.playerHumanData.uniqueId);

        // Loop through Team 2 and add monsters to respective lists
        for (int i = 0; i < Math.Min(enemyTeam.Count, 2); i++)
        {
            var monster = enemyTeam[i];
            allMonsterUniqueID_BF.Add(monster.uniqueId);
            team2UniqueID_BF.Add(monster.uniqueId);
        }

        // Add NPC if battleCategory is 1 or higher (2), which mean vs NPC or PvP
        if (battleCategory == 1 || battleCategory == 3)
            allMonsterUniqueID_BF.Add(humanBattleData.enemyHumanData.uniqueId);

        // Purge NBMon Level Up Bool, Status Effect List, and Temporary Stats
        foreach (var monster in playerTeam)
        {
            monster.NBMonLevelUp = false;
            monster.newSkillLearned = false;

            if (monster.fainted)
                monster.hp = 1;

            monster.fainted = false;
            // monster.statusEffectList.Clear(); // This line is commented out
            monster.temporaryPassives.Clear();
        }

        // Reset Enemy Team HP and Energy back to Full Health
        foreach (var monster in enemyTeam)
        {
            monster.hp = monster.maxHp;
            monster.energy = monster.maxEnergy;
            monster.NBMonLevelUp = false;
            monster.fainted = false;
            monster.statusEffectList.Clear();
            monster.temporaryPassives.Clear();
        }

        // Reset Enemy Morale Gauge
        moraleData.playerMoraleGauge = 0;
        moraleData.enemyMoraleGauge = 0;

        //Update AllMonsterUniqueID_BF to Player Title Data
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"AllMonsterUniqueID_BF", JsonConvert.SerializeObject(allMonsterUniqueID_BF)},
                 {"Team1UniqueID_BF", JsonConvert.SerializeObject(team1UniqueID_BF)},
                 {"Team2UniqueID_BF", JsonConvert.SerializeObject(team2UniqueID_BF)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)},
                 {"MoraleGaugeData", JsonConvert.SerializeObject(moraleData)},
                 {"HumanBattleData", JsonConvert.SerializeObject(humanBattleData)}
                }
            }
        );

        return $"{allMonsterUniqueID_BF.Count}";
    }

    //Cloud Methods
    [FunctionName("InitialTeamSetupAzureUsingOpponentData")]
    public static async Task<dynamic> InitialTeamSetupAzureUsingOpponentData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Opponent PlayFabID
        string opponentPlayFabID = string.Empty;

        //Check args["SwitchInput"] if it's null or not
        if(args["OpponentPlayFabID"] != null)
        {
            //Let's extract the argument value to SwitchInputValue variable.
            opponentPlayFabID = args["OpponentPlayFabID"];
        }
        else return $"Error: Missing OpponentPlayFabID!";

        //Request Team Information
        var requestUserData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest {}
        );

        //Request Team Information (Opponent Player Data)
        var requestOpponentData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = opponentPlayFabID
            }
        );

        //Process Morale Data from Opponent to This Player
        BattleMoraleGauge.MoraleData opponentMoraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestOpponentData.Result.Data["MoraleGaugeData"].Value);
        BattleMoraleGauge.MoraleData userMoraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestUserData.Result.Data["MoraleGaugeData"].Value);
        userMoraleData.playerMoraleGauge = opponentMoraleData.enemyMoraleGauge;
        userMoraleData.enemyMoraleGauge = opponentMoraleData.playerMoraleGauge;

        //Process Human Player Data from Opponent to This Player
        HumanBattleData opponentHumanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestOpponentData.Result.Data["MoraleGaugeData"].Value);
        HumanBattleData userHumanBattleData = new HumanBattleData();
        userHumanBattleData.playerHumanData = opponentHumanBattleData.enemyHumanData;
        userHumanBattleData.enemyHumanData = opponentHumanBattleData.playerHumanData;

        //Update AllMonsterUniqueID_BF to Player Title Data
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"AllMonsterUniqueID_BF", requestOpponentData.Result.Data["AllMonsterUniqueID_BF"].Value}, //Stay Intact
                 {"Team1UniqueID_BF",  requestOpponentData.Result.Data["Team2UniqueID_BF"].Value}, //Reversed for Team 1 Turn Order
                 {"Team2UniqueID_BF", requestOpponentData.Result.Data["Team1UniqueID_BF"].Value}, //Reversed for Team 2 Turn Order
                 {"CurrentPlayerTeam",requestOpponentData.Result.Data["EnemyTeam"].Value}, // Use EnemyTeam of Opponent to be this player CurrentPlayerTeam
                 {"EnemyTeam", requestOpponentData.Result.Data["CurrentPlayerTeam"].Value}, //Use CurrentPlayerTeam of Opponent to be an Enemy Team.
                 {"MoraleGaugeData", JsonConvert.SerializeObject(userMoraleData)}, //Reversed Morale Gauge
                 {"HumanBattleData", JsonConvert.SerializeObject(userHumanBattleData)}, //Reversed HumanBattleData
                 {"RNGSeeds",  requestOpponentData.Result.Data["RNGSeeds"].Value}, //Get RNGSeeds from Opponent. Important
                }
            }
        );

        return $"Get Opponent Data Success";
    }
}