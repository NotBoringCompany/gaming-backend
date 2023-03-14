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
using Azure;
using static InitialTeamSetup;

public static class GambitFunction
{
    public class GambitInput
    {
        public string team;
        public string gamebitFunction;
    }

    //Cloud Function
    [FunctionName("MoraleBoost")]
    public static async Task<dynamic> MoraleBoost([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req, ILogger log)
    {
        // Setup serverApi (Server API to PlayFab)
        var context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        var args = context.FunctionArgument;
        var serverApi = AzureHelper.ServerAPISetup(args, context);

        // Get player and enemy team information
        var requestTeamInformation = await serverApi.GetUserDataAsync(new GetUserDataRequest
        {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
        });

        // Deserialize saved data
        var moraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestTeamInformation.Result.Data["MoraleGaugeData"].Value);
        var humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestTeamInformation.Result.Data["HumanBattleData"].Value);
        var sortedOrder = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["SortedOrder"].Value);

        // Get input from client
        var uniqueId = args["InputFromClient"]?.ToString();
        if (string.IsNullOrEmpty(uniqueId))
        {
            return "Error: RH-0005, no Unique ID specified.";
        }

        // Check if the monster can move
        var monsterCanMove = EvaluateOrder.CheckBattleOrder(sortedOrder, uniqueId);
        if (!monsterCanMove)
        {
            return $"Error: RH-0006, no such Unique ID of {uniqueId} exist in the SortedOrder.";
        }

        // Change morale gauge based on team
        BattleMoraleGauge.ChangeMoraleGauge(moraleData, 20, uniqueId == humanBattleData.playerHumanData.uniqueId);

        
        // Save updated data
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest
        {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
            Data = new Dictionary<string, string>
            {
                {"SortedOrder", JsonConvert.SerializeObject(sortedOrder)},
                {"MoraleGaugeData", JsonConvert.SerializeObject(moraleData)},
                {"HumanBattleData", JsonConvert.SerializeObject(humanBattleData)},
            },
        });

        return string.Empty;
    }


    //Cloud Function
    [FunctionName("GambitLogic")]
    public static async Task<dynamic> GambitLogic([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
    ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{
                    "CurrentPlayerTeam", "EnemyTeam", "Team1UniqueID_BF", "Team2UniqueID_BF", 
                    "BattleEnvironment", "MoraleGaugeData", "RNGSeeds", "HumanBattleData"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> playerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> enemyTeam = new List<NBMonBattleDataSave>();
        List<string> team1UniqueID_BF = new List<string>();
        List<string> team2UniqueID_BF = new List<string>();
        BattleMoraleGauge.MoraleData moraleData = new BattleMoraleGauge.MoraleData();
        GambitInput gambitInput = new GambitInput();
        RNGSeedClass seedClass = new RNGSeedClass();
        HumanBattleData humanBattleData = new HumanBattleData();

        //Convert from json to NBmonBattleDataSave and Other Type Data (String for Battle Environment).
        playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        enemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        team1UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team1UniqueID_BF"].Value);
        team2UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team2UniqueID_BF"].Value);
        moraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestTeamInformation.Result.Data["MoraleGaugeData"].Value);
        seedClass = JsonConvert.DeserializeObject<RNGSeedClass>(requestTeamInformation.Result.Data["RNGSeeds"].Value);
        humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestTeamInformation.Result.Data["HumanBattleData"].Value);

        //Insert Battle Environment Value into Static Variable from Attack Function.
        AttackFunction.BattleEnvironment = requestTeamInformation.Result.Data["BattleEnvironment"].Value;

        //Get Data From Unity and Convert it back to Class.
        if(args["GambitInput"] != null)
        {
            string ArgumentString = args["GambitInput"];

            gambitInput = JsonConvert.DeserializeObject<GambitInput>(ArgumentString);
        }
        else
        {
            //Return Error Exception to Client
            return $"Error: RH-00002, Gambit Input is NULL.";
        }

        if (gambitInput.team == "Team 1")
        {
            if (moraleData.playerMoraleGauge != 100)
            {
                return "Error: RH-00003, Player Morale Gauge is not 100.";
            }
            
            moraleData.playerMoraleGauge = 0;
            moraleData.playerMoraleUsageCount++;
        }
        else
        {
            if (moraleData.enemyMoraleGauge != 100)
            {
                return "Error: RH-00003, Enemy Morale Gauge is not 100.";
            }
            
            moraleData.enemyMoraleGauge = 0;
        }

        //Let's do Gambit Function
        switch(gambitInput.gamebitFunction){
            case "Dance Rupture": DanceRuptureFunction(gambitInput.team, team1UniqueID_BF, team2UniqueID_BF, playerTeam, enemyTeam, humanBattleData); break;
            case "Revitalize": RevitalizeFunction(gambitInput.team, team1UniqueID_BF, team2UniqueID_BF, playerTeam, enemyTeam, humanBattleData); break;
            case "Guardian": GuardianFunction(gambitInput.team, team1UniqueID_BF, team2UniqueID_BF, playerTeam, enemyTeam, seedClass, humanBattleData); break;
        }


        //Let's Save Player Team Data and Enemy Team Data into PlayFab again.
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)},
                 {"MoraleGaugeData", JsonConvert.SerializeObject(moraleData)},
                 {"HumanBattleData", JsonConvert.SerializeObject(humanBattleData)}
                }
            }
        );

        return null;
    }

    public static void DanceRuptureFunction(string team, List<string> team1UniqueID_BF, List<string> team2UniqueID_BF, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData)
    {
        // Collect the monsters to be used and the target monsters
        var usedMonsters = new List<NBMonBattleDataSave>();
        var targetMonsters = new List<NBMonBattleDataSave>();
        CheckWhichTeamInAction(team, team1UniqueID_BF, team2UniqueID_BF, playerTeam, enemyTeam, humanBattleData, usedMonsters, targetMonsters);

        // Calculate the attack power
        var defaultDamagePercent = 10;
        var damage = usedMonsters.Count * defaultDamagePercent;

        // Increase the damage per monster if the opposing team only has one monster
        if (targetMonsters.Count == 1)
        {
            damage *= 2;
        }
        damage = (int)Math.Floor((float)damage / targetMonsters.Count);

        // Apply the damage to the target monsters
        foreach (var target in targetMonsters)
        {
            NBMonTeamData.StatsPercentageChange(target, NBMonProperties.StatsType.Hp, -damage);

            // Check if the target monster has fainted
            AttackFunction.CheckTargetDied(target, usedMonsters[0], null, playerTeam, enemyTeam, team1UniqueID_BF, null);
        }
    }

    private static void CheckWhichTeamInAction(string team, List<string> team1UniqueID_BF, List<string> team2UniqueID_BF, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData, List<NBMonBattleDataSave> usedMonsters, List<NBMonBattleDataSave> targetMonsters)
    {
        var team1Monsters = (team == "Team 1") ? team1UniqueID_BF : team2UniqueID_BF;
        var team2Monsters = (team == "Team 1") ? team2UniqueID_BF : team1UniqueID_BF;

        foreach (var monsterId in team1Monsters)
        {
            var monster = UseItem.FindMonster(monsterId, playerTeam, humanBattleData);

            if (!monster.fainted)
            {
                if (team == "Team 1")
                    usedMonsters.Add(monster);
                else
                    targetMonsters.Add(monster);
            }
        }

        foreach (var monsterId in team2Monsters)
        {
            var monster = UseItem.FindMonster(monsterId, enemyTeam, humanBattleData);

            if (!monster.fainted)
            {
                if (team == "Team 1")
                    targetMonsters.Add(monster);
                else
                    usedMonsters.Add(monster);
            }
        }
    }


    public static void RevitalizeFunction(string team, List<string> team1UniqueID_BF, List<string> team2UniqueID_BF, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, HumanBattleData humanBattleData)
    {
        List<NBMonBattleDataSave> usedMonsters = new List<NBMonBattleDataSave>();
        int recoveryPercentage = 15;

        // Determine which team to apply the function to and collect the active monsters
        if (team == "Team 1")
        {
            usedMonsters = team1UniqueID_BF.Select(id => UseItem.FindMonster(id, playerTeam, humanBattleData)).Where(monster => !monster.fainted).ToList();
        }
        else
        {
            usedMonsters = team2UniqueID_BF.Select(id => UseItem.FindMonster(id, enemyTeam, humanBattleData)).Where(monster => !monster.fainted).ToList();
        }

        // Recover HP and Energy of active monsters
        usedMonsters.ForEach(monster => {
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Hp, recoveryPercentage);
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Energy, recoveryPercentage);
        });
    }

    public static void GuardianFunction(string team, List<string> team1UniqueID_BF, List<string> team2UniqueID_BF, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam, RNGSeedClass seedClass, HumanBattleData humanBattleData)
    {
        //Declared New Variable.
        List<NBMonBattleDataSave> usedMonsters = new List<NBMonBattleDataSave>();
        List<NBMonProperties.StatusEffectInfo> statusEffectsInfoList = new List<NBMonProperties.StatusEffectInfo>();
        var guardGambit = new NBMonProperties.StatusEffectInfo();
        guardGambit.statusEffect = NBMonProperties.StatusEffect.Guard;
        guardGambit.triggerChance = 100;
        guardGambit.countAmmount = 3;
        statusEffectsInfoList.Add(guardGambit);

        if(team == "Team 1")
        {
            foreach(var monsterId in team1UniqueID_BF)
            {
                usedMonsters.Add(UseItem.FindMonster(monsterId, playerTeam, humanBattleData));
            }
        }
        else
        {
            foreach(var monsterId in team2UniqueID_BF)
            {
                usedMonsters.Add(UseItem.FindMonster(monsterId, enemyTeam, humanBattleData));
            }
        }

        //Receive Guard Buff for all active member party in the field.
        foreach(var monster in usedMonsters)
        {
            UseItem.ApplyStatusEffect(monster, statusEffectsInfoList, null, false, seedClass);
        }
    }
}