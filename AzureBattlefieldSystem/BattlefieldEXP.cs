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

public static class BattlefieldEXP
{
    //Cloud Function
    [FunctionName("BattlefieldGetEXP")]
    public static async Task<dynamic> BattlefieldGetEXP([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
        new GetUserDataRequest { 
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam"}
        } );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<NBMonBattleDataSave> enemyTeam = new List<NBMonBattleDataSave>();
        string CurrentMonsterUniqueID = string.Empty;
        int wildBattleIndex = new int();

        //Check args["ThisMonsterUniqueID"] if it's null or not
        if(args["CurrentMonsterUniqueID"] != null)
            CurrentMonsterUniqueID = (string) args["CurrentMonsterUniqueID"];

        if(args["WildBattleIndex"] != null)
            wildBattleIndex = (int)args["WildBattleIndex"];
        else
            return $"Error: WildBattleIndex variable not registered!";

        GenerateEnemyTeam.GenerateWildNBMon(enemyTeam, wildBattleIndex);
        
        //Convert from json to NBmonBattleDataSave
        var playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        var currentMonsterData = UseItem.FindMonster(CurrentMonsterUniqueID, playerTeam);

        if(currentMonsterData != null)
        {
            var enemyData = enemyTeam[0];

            //Add EXP, but the dataFromAzureToClient is null because we don't need it to appear in UI
            AttackFunction.AddEXP(enemyData, currentMonsterData, null);

            //Put the EXP into Monster and level up it if the requirement meet.
            BattleFinished.GetEXPLogicIndividual(currentMonsterData);

            //Let's Save Player Team Data and Enemy Team Data into PlayFab again.
            var updateReq = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)}
                }});

            return $"{currentMonsterData.nickName} got EXP and updated to PlayFab!";
        }
        else
        {
            return $"Error: Unique ID of {CurrentMonsterUniqueID} not found!";
        }
    }
}