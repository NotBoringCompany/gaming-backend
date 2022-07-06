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

public static class EvaluateOrder
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
    [FunctionName("EvaluateOrder")]
    public static async Task<dynamic> EvaluteOrder([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "AllMonsterUniqueID_BF"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> CurrentNBMonOnBF = new List<NBMonBattleDataSave>();
        List<string> AllMonsterUniqueID_BF = new List<string>();
        List<string> SortedOrder = new List<string>();
        int BattleCondition = new int();

        //Check args["BattleAdvantage"] if it's null or not
        if(args["BattleAdvantage"] != null)
            BattleCondition = (int)args["BattleAdvantage"];
        else
            BattleCondition = 0;

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        AllMonsterUniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["AllMonsterUniqueID_BF"].Value);

        //Let's find NBMon in BF using AllMonsterUniqueID_BF
        foreach(var MonsterID in AllMonsterUniqueID_BF)
        {
            ///BattleCondition = 0, Normal Battle
            ///BattleCondition = 1, Player Advantage
            ///BattleCondition = -1. Enemy Advantage

            if(BattleCondition == 0 || BattleCondition == 1)
            {
                var PlayerNBMonData = NBMonDatabase_Azure.FindNBMonDataUsingUniqueID(MonsterID, PlayerTeam);

                if(PlayerNBMonData != null)
                {
                    CurrentNBMonOnBF.Add(PlayerNBMonData);
                    continue;
                }
            }

            if(BattleCondition == 0 || BattleCondition == -1)
            {
                var EnemyNBMonData =  NBMonDatabase_Azure.FindNBMonDataUsingUniqueID(MonsterID, EnemyTeam);

                if(EnemyNBMonData != null)
                {
                    CurrentNBMonOnBF.Add(EnemyNBMonData);
                    continue;
                }
            }
        }

        //After getting the NBMons to be sorted, let's sort it by Battle Speed.
        CurrentNBMonOnBF = CurrentNBMonOnBF.OrderByDescending(f => f.battleSpeed).ToList();

        //After sorted by Battle Speed, add their Unique IDs into the SortOrder String List.
        foreach(var SortedMonster in CurrentNBMonOnBF)
        {
            SortedOrder.Add(SortedMonster.uniqueId);
        }

        //Once the Sorted Monster's ID has been added into SortedOrder. Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"SortedOrder", JsonConvert.SerializeObject(SortedOrder)}
                }
            }
        );
        
        var SortedOrderJsonString = JsonConvert.SerializeObject(SortedOrder);

        return SortedOrderJsonString;
    }
}