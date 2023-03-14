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

public static class NBMonSwitching
{
    

    public class NBMonSwitchingInput
    {
        public string MonsterUniqueID_Switch;
        public string MonsterUniqueID_TargetSwitched;
        public string TeamCredential;
    }

    //Cloud Methods
    [FunctionName("NBMonSwitching")]
    public static async Task<dynamic> SwitchingAzure([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        // Read the request body
        var requestBody = await req.ReadAsStringAsync();

        // Parse the input arguments
        var inputArgs = JsonConvert.DeserializeObject<dynamic>(requestBody);
        var inputUniqueID = inputArgs.inputUniqueID;
        var switchInputValue = inputArgs.SwitchInput;
        var hasMonsterDied = inputArgs.HasMonsterDied;

        // Retrieve some data from a server API
        var serverApi = AzureHelper.ServerAPISetup(inputArgs, null);
        var teamData = await serverApi.GetUserDataAsync(new GetUserDataRequest
        {
            PlayFabId = inputArgs.PlayerUniqueID,
            Keys = new List<string>{"Team1UniqueID_BF", "Team2UniqueID_BF", "SortedOrder", "HumanBattleData"}
        });

        var team1 = JsonConvert.DeserializeObject<List<string>>(teamData.Result.Data["Team1UniqueID_BF"].Value);
        var team2 = JsonConvert.DeserializeObject<List<string>>(teamData.Result.Data["Team2UniqueID_BF"].Value);
        var sortedOrder = JsonConvert.DeserializeObject<List<string>>(teamData.Result.Data["SortedOrder"].Value);
        var humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(teamData.Result.Data["HumanBattleData"].Value);

        // Check if the monster can switch
        if (!hasMonsterDied)
        {
            var monsterCanMove = EvaluateOrder.CheckBattleOrder(sortedOrder, inputUniqueID);
            var monsterSwitched = EvaluateOrder.CheckBattleOrder(sortedOrder, switchInputValue.MonsterUniqueID_TargetSwitched);

            if (!monsterCanMove)
            {
                return $"No monster in the turn order. Error Code: RH-0001";
            }
        }

        // Switch the monsters
        var team = inputArgs.TeamCredential == "Team 1" ? team1 : team2;
        team.Remove(switchInputValue.MonsterUniqueID_TargetSwitched);
        if (!team.Contains(switchInputValue.MonsterUniqueID_Switch))
        {
            team.Add(switchInputValue.MonsterUniqueID_Switch);
        }

        // Update the server API with the new data
        var allMonsters = team1.Concat(team2).ToList();
        allMonsters.Add(humanBattleData.playerHumanData.uniqueId);

        if (humanBattleData.enemyHumanData != null)
        {
            allMonsters.Add(humanBattleData.enemyHumanData.uniqueId);
        }

        var updateRequest = new UpdateUserDataRequest
        {
            PlayFabId = inputArgs.PlayerUniqueID,
            Data = new Dictionary<string, string>
            {
                {"AllMonsterUniqueID_BF", JsonConvert.SerializeObject(allMonsters)},
                {"Team1UniqueID_BF", JsonConvert.SerializeObject(team1)},
                {"Team2UniqueID_BF", JsonConvert.SerializeObject(team2)},
                {"SortedOrder", JsonConvert.SerializeObject(sortedOrder)}
            }
        };

        var updateResult = await serverApi.UpdateUserDataAsync(updateRequest);
        return null;
    }

}