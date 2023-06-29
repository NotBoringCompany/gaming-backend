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

public static class PvP_GetOpponentData
{
    [FunctionName("GetOpponentDataMasterClient")]
    public static async Task<dynamic> GetOpponentDataMasterClient([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Opponent PlayFabID
        string opponentPlayFabID = string.Empty;

        //Check args["SwitchInput"] if it's null or not
        if(args["OpponentPlayFabID"] != null)
        {
            //Let's extract the argument value to SwitchInputValue variable.
            opponentPlayFabID = args["OpponentPlayFabID"];
        }
        else return $"Error: Missing OpponentPlayFabID!";

        //Request Team Information (Opponent Player Data)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = opponentPlayFabID
            }
        );

        //Get Opponent Monster Data.
        var enemyTeam = new List<NBMonBattleDataSave>();

        if(requestTeamInformation.Result.Data.ContainsKey("CurrentPlayerTeam"))
        {
            enemyTeam = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(requestTeamInformation.Result.Data["CurrentPlayerTeam"].Value);
        }
        else return "Error: Opponent don't have CurrentPlayerTeam!";

        //Check if opponent has zero Monster
        if(enemyTeam.Count == 0)
            return "Error: Opponent don't have any monsters!";

        // Reset Enemy Team HP and Energy back to Full Health
        foreach (var monster in enemyTeam)
        {
            monster.hp = monster.maxHp;
            monster.energy = monster.maxEnergy;
            monster.NBMonLevelUp = false;
            monster.fainted = false;
            monster.statusEffectList.Clear();
            monster.temporaryPassives.Clear();
        }

        //Let's grab that enemyTeam into PhotonNetwork's Master Client PlayFab Data.
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"EnemyTeam", JsonConvert.SerializeObject(enemyTeam)}
                }
            }
        );

        return $"Get Opponent Data Success! Please Inform your opponent (non PhotonNetwork.MasterClient) to get Data from this player's PlayFab Data";
    }
}