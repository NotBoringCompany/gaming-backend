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
using static InitialTeamSetup;

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
        var requestTeamInformation = await serverApi.GetUserDataAsync(new GetUserDataRequest { PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam", "EnemyTeam", "SortedOrder", "HumanBattleData"} });

        //Convert from json to NBmonBattleDataSave
        List<NBMonBattleDataSave> playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        List<NBMonBattleDataSave> enemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["EnemyTeam"].Value);
        List<String> sortedOrder = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["SortedOrder"].Value);
        HumanBattleData humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestTeamInformation.Result.Data["HumanBattleData"].Value);

        //Check args["ThisMonsterUniqueID"] if it's null or not
        string thisMonsterUniqueID = args["ThisMonsterUniqueID"]?.ToString();

        //Let's Recover this Monster's Energy by 50 points.
        var monster = UseItem.FindMonster(thisMonsterUniqueID, playerTeam, humanBattleData) ?? UseItem.FindMonster(thisMonsterUniqueID, enemyTeam, humanBattleData);

        //If monster found in the player team, let's check its turn order.
        if (monster != null)
        {
            //Check if monster can attack
            var monsterCanMove = EvaluateOrder.CheckBattleOrder(sortedOrder, thisMonsterUniqueID);
            if (!monsterCanMove)
            {
                return $"No Monster in the turn order. Error Code: RH-0001";
            }
        }

        var energyRecoveryResting = (int)System.Math.Ceiling((0.23f * monster.maxEnergy) + 1);

        //Recover Energy by 50 points.
        NBMonTeamData.StatsValueChange(monster, NBMonProperties.StatsType.Energy, energyRecoveryResting);

        //Once the Sorted Monster's ID has been added into SortedOrder. Let's convert it into Json String and Send it into PlayFab (Player Title Data).
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
            Data = new Dictionary<string, string>{
                {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)},
                {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)},
                {"SortedOrder", JsonConvert.SerializeObject(sortedOrder)},
                {"HumanBattleData", JsonConvert.SerializeObject(humanBattleData)}
            }
        });

        return $"{monster?.nickName} / {monster?.uniqueId} has recovered 50 Energy successfully!";
    }

}