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

public static class DecreaseStatusEffectCounter
{
    

    //Azure Cloud Function
    //Call this method after turn has ended.
    [FunctionName("DecreaseStatusEffectCounter")]
    public static async Task<dynamic> DecreaseStatusEffect([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "AllMonsterUniqueID_BF"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        List<string> AllMonsterUniqueID_BF = new List<string>();

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        AllMonsterUniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["AllMonsterUniqueID_BF"].Value);

        //Call method for player and enemy team
        PlayerStatusEffect(PlayerTeam, AllMonsterUniqueID_BF);
        EnemyStatusEffect(EnemyTeam, AllMonsterUniqueID_BF);

        //Call playfab update method
        var updateAllTeamInformation = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)}
                }
            }
        );
        
        return "Decrease all status effect counter";
    }

    //Decrease status effect counter for player team
    private static void PlayerStatusEffect(List<NBMonBattleDataSave> MonsterTeam, List<string> AllMonsterUniqueID_BF){
        //Find on screen monster from player team
        foreach(var Monster in MonsterTeam){
            foreach(var UID in AllMonsterUniqueID_BF){
                if(Monster.uniqueId == UID)
                    DecreaseCounter(Monster);
            }
        }
    }

    //Decrease status effect counter for enemy team
    private static void EnemyStatusEffect(List<NBMonBattleDataSave> MonsterTeam, List<string> AllMonsterUniqueID_BF){
        //Find on screen monster from player team
        foreach(var Monster in MonsterTeam){
            foreach(var UID in AllMonsterUniqueID_BF){
                if(Monster.uniqueId == UID)
                    DecreaseCounter(Monster);
            }
        }
    }

    //Decrease Counter
    private static void DecreaseCounter(NBMonBattleDataSave Monster){
        for(int i = Monster.statusEffectList.Count-1 ; i >= 0; i--){
            //Decrease counter value
            Monster.statusEffectList[i].counter--;

            //Check if counter value equal 0 or below
            if(Monster.statusEffectList[i].counter <= 0)
                Monster.statusEffectList.RemoveAt(i);
        }
    }
}