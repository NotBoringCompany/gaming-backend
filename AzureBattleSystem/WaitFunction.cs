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

public static class WaitFunction
{
    

    //Cloud Method
    [FunctionName("WaitTurnAndRecoverEnergy")]
    public static async Task<dynamic> WaitTurnAndRecoverEnergy([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> PlayerTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> EnemyTeam = new List<NBMonBattleDataSave>();
        List<NBMonBattleDataSave> CurrentNBMonOnBF = new List<NBMonBattleDataSave>();
        string ThisMonsterUniqueID = string.Empty;

        //Check args["ThisMonsterUniqueID"] if it's null or not
        if(args["ThisMonsterUniqueID"] != null)
        {
            dynamic Val = args["ThisMonsterUniqueID"];

            ThisMonsterUniqueID = Val.ToString();
        }

        //Convert from json to NBmonBattleDataSave
        PlayerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        EnemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        
        //Let's Recover this Monster's Energy by 50 points.
        var Monster = UseItem.FindMonster(ThisMonsterUniqueID, PlayerTeam);

        //If the Monster Variable still Null, let's find it using Enemy Team.
        if(Monster == null)
        {
            Monster = UseItem.FindMonster(ThisMonsterUniqueID, EnemyTeam);
        }

        //Recover Energy by 50 points.
        NBMonTeamData.StatsValueChange(Monster, NBMonProperties.StatsType.Energy, 50);

        //Once the Sorted Monster's ID has been added into SortedOrder. Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"CurrentPlayerTeam", JsonConvert.SerializeObject(PlayerTeam)},
                 {"EnemyTeam", JsonConvert.SerializeObject(EnemyTeam)}
                }
            }
        );

        return $"{Monster.nickName} / {Monster.uniqueId} has recovered 50 Energy successfully!";
    }
}