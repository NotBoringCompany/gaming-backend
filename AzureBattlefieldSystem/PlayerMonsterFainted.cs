using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab;
using PlayFab.Samples;
using PlayFab.ServerModels;
using System.Collections.Generic;


public static class PlayerMonsterFainted{
    //Azure Function
    [FunctionName("PlayerMonsterFaint")]
    public static async Task<dynamic> PlayerMonsterFaint([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);
        
        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(new GetUserDataRequest { 
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"CurrentPlayerTeam"}
        } );

        //Declare new variable
        string monsterUid = string.Empty;
        if(args["monsterUid"] != null)
            monsterUid = args["monsterUid"];
        else
            return "Monster Unique ID is null, stopping the process";

        //Convert from json to NBmonBattleDataSave
        var playerTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        var currentMonsterData = UseItem.FindMonster(monsterUid, playerTeam, null);
        //Apply the penalty on fainted monster
        FaintedPenalty(currentMonsterData);
        //Update the result back into Playfab
        var updateReq = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest {
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
            {"CurrentPlayerTeam", JsonConvert.SerializeObject(playerTeam)}
            }});

        return $"Player {currentMonsterData.nickName} fainted, applying penalty!!";
    }

    private static void FaintedPenalty(NBMonBattleDataSave faintMonster){
        //Reduce current HP into 25% of maxHp.
        faintMonster.hp = (int)(faintMonster.maxHp*0.25f);
        //Loss a percentage of currentExp
        faintMonster.currentExp -= (int)(faintMonster.currentExp * 0.1f);
    }
}