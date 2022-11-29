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

public static class GambitFunction
{
    public class GambitInput
    {
        public string team;
        public string gamebitFunction;
    }

    //Cloud Function
    [FunctionName("GambitLogic")]
    public static async Task<dynamic> AttackLogic([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
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
                    "BattleEnvironment", "MoraleGaugeData"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> playerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> enemyTeam = new List<NBMonBattleDataSave>();
        List<string> team1UniqueID_BF = new List<string>();
        List<string> team2UniqueID_BF = new List<string>();
        BattleMoraleGauge.MoraleData moraleData = new BattleMoraleGauge.MoraleData();
        GambitInput gambitInput = new GambitInput();

        //Convert from json to NBmonBattleDataSave and Other Type Data (String for Battle Environment).
        playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        enemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        team1UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team1UniqueID_BF"].Value);
        team2UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team2UniqueID_BF"].Value);
        moraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestTeamInformation.Result.Data["MoraleGaugeData"].Value);

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

        if(gambitInput.team == "Team 1")
        {
            if(moraleData.playerMoraleGauge != 100)
            {
                return $"Error: RH-00003, Player Moral Gauge is not 100.";   
            }
            else
            {
                moraleData.playerMoraleGauge = 0;
                moraleData.playerMoraleUsageCount ++;
            }
        }
        else
        {
            if(moraleData.enemyMoraleGauge != 100)
            {
                return $"Error: RH-00003, Enemy Moral Gauge is not 100.";   
            }
            else
            {
                moraleData.enemyMoraleGauge = 0;
            }
        }

        //Let's do Gambit Function
        switch(gambitInput.gamebitFunction){
            case "Dance Rupture": DanceRuptureFunction(gambitInput.team, team1UniqueID_BF, team2UniqueID_BF, playerTeam, enemyTeam); break;
            case "Revitalize": RevitalizeFunction(gambitInput.team, team1UniqueID_BF, team2UniqueID_BF, playerTeam, enemyTeam); break;
            case "Guardian": GuardianFunction(gambitInput.team, team1UniqueID_BF, team2UniqueID_BF, playerTeam, enemyTeam); break;
        }


        //Let's Save Player Team Data and Enemy Team Data into PlayFab again.
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)},
                 {"MoraleGaugeData", JsonConvert.SerializeObject(moraleData)}
                }
            }
        );

        return null;
    }

    public static void DanceRuptureFunction(string team, List<string> team1UniqueID_BF, List<string> team2UniqueID_BF, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam)
    {
        List<NBMonBattleDataSave> usedMonsters = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> targetMonsters = new List<NBMonBattleDataSave>(); 
        int damage = new int();

        if(team == "Team 1")
        {
            foreach(var monsterId in team1UniqueID_BF)
            {
                usedMonsters.Add(UseItem.FindMonster(monsterId, playerTeam));
            }

            foreach(var monsterId in team2UniqueID_BF)
            {
                targetMonsters.Add(UseItem.FindMonster(monsterId, enemyTeam));
            }
        }
        else
        {
            foreach(var monsterId in team2UniqueID_BF)
            {
                usedMonsters.Add(UseItem.FindMonster(monsterId, enemyTeam));
            }

            foreach(var monsterId in team1UniqueID_BF)
            {
                targetMonsters.Add(UseItem.FindMonster(monsterId, playerTeam));
            }
        }

        //Calculate their Attack Power and Special Attack Power
        foreach(var monster in usedMonsters)
        {
            damage += (monster.attack + monster.specialAttack);
        }

        damage = (int)Math.Floor((float)damage/(float)usedMonsters.Count);

        foreach(var target in targetMonsters)
        {
            NBMonTeamData.StatsValueChange(target ,NBMonProperties.StatsType.Hp, damage * -1);

            //Let's Check Target Monster if it's Fainted or not
            AttackFunction.CheckTargetDied(target, usedMonsters[0], null, playerTeam, enemyTeam, team1UniqueID_BF, null);
        }
    }

    public static void RevitalizeFunction(string team, List<string> team1UniqueID_BF, List<string> team2UniqueID_BF, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam)
    {
        List<NBMonBattleDataSave> usedMonsters = new List<NBMonBattleDataSave>();

        if(team == "Team 1")
        {
            foreach(var monsterId in team1UniqueID_BF)
            {
                usedMonsters.Add(UseItem.FindMonster(monsterId, playerTeam));
            }
        }
        else
        {
            foreach(var monsterId in team2UniqueID_BF)
            {
                usedMonsters.Add(UseItem.FindMonster(monsterId, enemyTeam));
            }
        }

        //Recover all active member party HP and Energy in the field.
        foreach(var monster in usedMonsters)
        {
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Hp, 40);
            NBMonTeamData.StatsPercentageChange(monster, NBMonProperties.StatsType.Energy, 40);
        }
    }
    
    public static void GuardianFunction(string team, List<string> team1UniqueID_BF, List<string> team2UniqueID_BF, List<NBMonBattleDataSave> playerTeam, List<NBMonBattleDataSave> enemyTeam)
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
                usedMonsters.Add(UseItem.FindMonster(monsterId, playerTeam));
            }
        }
        else
        {
            foreach(var monsterId in team2UniqueID_BF)
            {
                usedMonsters.Add(UseItem.FindMonster(monsterId, enemyTeam));
            }
        }

        //Receive Guard Buff for all active member party in the field.
        foreach(var monster in usedMonsters)
        {
            UseItem.ApplyStatusEffect(monster, statusEffectsInfoList, null, false);
        }
    }
}