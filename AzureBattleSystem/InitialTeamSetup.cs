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
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "MoraleGaugeData"}
            }
        );
        
        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        BattleMoraleGauge.MoraleData moraleData = new BattleMoraleGauge.MoraleData();
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        List<string> AllMonsterUniqueID_BF = new List<string>();
        List<string> Team1UniqueID_BF = new List<string>();
        List<string> Team2UniqueID_BF = new List<string>();

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);

        //Check if the player has the MoraleGaugeData, if no. Create a new one
        if(requestTeamInformation.Result.Data.ContainsKey("MoraleGaugeData"))
        {
            moraleData = JsonConvert.DeserializeObject<BattleMoraleGauge.MoraleData>(requestTeamInformation.Result.Data["MoraleGaugeData"].Value);
        }

        //Looping Team 1
        byte P1Count = 0;
        foreach(var Monster in PlayerTeam)
        {   
            if(P1Count == 2)
                continue;

            AllMonsterUniqueID_BF.Add(Monster.uniqueId);
            Team1UniqueID_BF.Add(Monster.uniqueId);

            P1Count++;
        }

        //Looping Team 2
        byte P2Count = 0;
        foreach(var Monster in EnemyTeam)
        {
            if(P2Count == 2)
                continue;

            AllMonsterUniqueID_BF.Add(Monster.uniqueId);
            Team2UniqueID_BF.Add(Monster.uniqueId);

            P2Count++;
        }

        //Purge NBMon Level Up Bool, Status Effect List, and Temporary Stats;
        foreach(var Monster in PlayerTeam)
        {
            Monster.NBMonLevelUp = false;
            Monster.fainted = false;
            //Monster.statusEffectList.Clear();
            Monster.temporaryPassives.Clear();
        }

        //Setup Enemy Team HP and Energy back to Full Health
        foreach(var Monster in EnemyTeam)
        {
            Monster.hp = Monster.maxHp;
            Monster.energy = Monster.maxEnergy;
            Monster.NBMonLevelUp = false;
            Monster.fainted = false;
            Monster.statusEffectList.Clear();
            Monster.temporaryPassives.Clear();
        }

        //Resets Enemy Morale Gauge
        moraleData.enemyMoraleGauge = 0;

        //Update AllMonsterUniqueID_BF to Player Title Data
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"AllMonsterUniqueID_BF", JsonConvert.SerializeObject(AllMonsterUniqueID_BF)},
                 {"Team1UniqueID_BF", JsonConvert.SerializeObject(Team1UniqueID_BF)},
                 {"Team2UniqueID_BF", JsonConvert.SerializeObject(Team2UniqueID_BF)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)},
                 {"MoraleGaugeData", JsonConvert.SerializeObject(moraleData)}
                }
            }
        );

        return $"{AllMonsterUniqueID_BF.Count}";
    }
}