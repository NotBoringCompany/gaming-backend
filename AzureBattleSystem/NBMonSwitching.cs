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
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request Team Information (Player and Enemy)
        var requestTeamInformation = await serverApi.GetUserDataAsync(
            new GetUserDataRequest { 
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>{"Team1UniqueID_BF", "Team2UniqueID_BF", "SortedOrder", "HumanBattleData"}
            }
        );

        //Declare Variables we gonna need (BF means Battlefield aka Monster On Screen)
        List<string> AllMonsterUniqueID_BF = new List<string>();
        List<string> Team1UniqueID_BF = new List<string>();
        List<string> Team2UniqueID_BF = new List<string>();
        List<String> SortedOrder = new List<string>();
        string inputUniqueID = string.Empty;
        NBMonSwitchingInput Input = new NBMonSwitchingInput();
        dynamic SwitchInputValue = null;
        bool hasMonsterDied = false;
        HumanBattleData humanBattleData = new HumanBattleData();

        //Check args["SwitchInput"] if it's null or not
        if(args["SwitchInput"] != null && args["HasMonsterDied"] != null)
        {
            //Let's extract the argument value to SwitchInputValue variable.
            SwitchInputValue = args["SwitchInput"];
            hasMonsterDied = args["HasMonsterDied"];

            //Change from Dynamic to String
            string SwitchInputValueString = SwitchInputValue;

            //Convert that argument into Input variable.
            Input = JsonConvert.DeserializeObject<NBMonSwitchingInput>(SwitchInputValueString);
        }

        if(args["InputUniqueID"] != null)
        {
            inputUniqueID = args["InputUniqueID"];
        }

        //Convert from json to List<String>
        Team1UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team1UniqueID_BF"].Value);
        Team2UniqueID_BF = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["Team2UniqueID_BF"].Value);
        SortedOrder = JsonConvert.DeserializeObject<List<string>>(requestTeamInformation.Result.Data["SortedOrder"].Value);
        humanBattleData = JsonConvert.DeserializeObject<HumanBattleData>(requestTeamInformation.Result.Data["HumanBattleData"].Value);

        //Let's check the Team Credential First.
        if(Input.TeamCredential == "Team 1") //Team 1 Logic
        {
            if(!hasMonsterDied)
            {
                //Check if monster can switch (normally not ded)
                var monsterCanMove = EvaluateOrder.CheckBattleOrder(SortedOrder, inputUniqueID);
                var monsterSwitched = EvaluateOrder.CheckBattleOrder(SortedOrder, Input.MonsterUniqueID_TargetSwitched);

                if(!monsterCanMove)
                {
                    return $"No Monster in the turn order. Error Code: RH-0001";
                }
    
            }

            //Let's Delete the Existing NBMon in the Battle Field (for: Team Data and Global Data Variables)
            foreach(var MonsterID in Team1UniqueID_BF)
            {
                if(Input.MonsterUniqueID_TargetSwitched == MonsterID)
                {
                    Team1UniqueID_BF.Remove(MonsterID);
                    break;
                }
            }

            //Let's Add the NBMon the Input want to Switch with.
            if(!Team1UniqueID_BF.Contains(Input.MonsterUniqueID_Switch))
                Team1UniqueID_BF.Add(Input.MonsterUniqueID_Switch);
        }
        else //Team 2 Logic
        {
            //Let's Delete the Existing NBMon in the Battle Field (for: Team Data and Global Data Variables)
            foreach(var MonsterID in Team2UniqueID_BF)
            {
                if(Input.MonsterUniqueID_TargetSwitched == MonsterID)
                {
                    Team2UniqueID_BF.Remove(MonsterID);
                    break;
                }
            }

            //Let's Add the NBMon the Input want to Switch with.
            if(!Team2UniqueID_BF.Contains(Input.MonsterUniqueID_Switch))
                Team2UniqueID_BF.Add(Input.MonsterUniqueID_Switch);
        }

        //Let's add Both Teams again.
        AllMonsterUniqueID_BF = Team1UniqueID_BF.Concat<string>(Team2UniqueID_BF).ToList<string>();

        //Let's add Humans Unique IDs.
        AllMonsterUniqueID_BF.Add(humanBattleData.playerHumanData.uniqueId);

        if(humanBattleData.enemyHumanData != null)
            AllMonsterUniqueID_BF.Add(humanBattleData.enemyHumanData.uniqueId);

        //Update AllMonsterUniqueID_BF to Player Title Data
        var requestAllMonsterUniqueID_BF = await serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest {
             PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Data = new Dictionary<string, string>{
                 {"AllMonsterUniqueID_BF", JsonConvert.SerializeObject(AllMonsterUniqueID_BF)},
                 {"Team1UniqueID_BF", JsonConvert.SerializeObject(Team1UniqueID_BF)},
                 {"Team2UniqueID_BF", JsonConvert.SerializeObject(Team2UniqueID_BF)},
                 {"SortedOrder", JsonConvert.SerializeObject(SortedOrder)},
                }
            }
        );

        return null;
    }
}