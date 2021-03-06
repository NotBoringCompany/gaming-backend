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

    //Cloud Methods
    [FunctionName("InitialTeamSetupAzure")]
    public static async Task<dynamic> InitialTeamSetupAzure([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam"}
            }
        );
        
        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        List<string> AllMonsterUniqueID_BF = new List<string>();
        List<string> Team1UniqueID_BF = new List<string>();
        List<string> Team2UniqueID_BF = new List<string>();

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);

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

        //Purge NBMon Level Up Bool
        foreach(var Monster in PlayerTeam)
        {
            Monster.NBMonLevelUp = false;
            Monster.fainted = false;
        }

        //Purge NBMon Level Up Bool
        foreach(var Monster in EnemyTeam)
        {
            Monster.NBMonLevelUp = false;
            Monster.fainted = false;
        }

        //Update AllMonsterUniqueID_BF to Player Title Data
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"AllMonsterUniqueID_BF", JsonConvert.SerializeObject(AllMonsterUniqueID_BF)},
                 {"Team1UniqueID_BF", JsonConvert.SerializeObject(Team1UniqueID_BF)},
                 {"Team2UniqueID_BF", JsonConvert.SerializeObject(Team2UniqueID_BF)},
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)}
                }
            }
        );

        return $"{AllMonsterUniqueID_BF.Count}";
    }
}