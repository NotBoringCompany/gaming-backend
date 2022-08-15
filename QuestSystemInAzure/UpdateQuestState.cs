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

public static class UpdateQuest
{
    //Data Structure of PlayFabQuestData
    public class PlayFabQuestData
    {
        public int QuestID;
        public string QuestName;
        public string QuestState;
    }
    
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

    [FunctionName("UpdatePlayerQuestData")]
    public static async Task<dynamic> UpdatePlayerQuestData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {            
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Get Player PlayerQuestData Data from PlayFab.
        var reqUserData = await serverApi.GetUserDataAsync(new GetUserDataRequest{
            PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
        });

        //Declare Variables
        string questStateReq = string.Empty;
        string questNameReq = string.Empty;
        List<PlayFabQuestData> playerQuestData = new List<PlayFabQuestData>();

        log.LogInformation($"{playerQuestData}");

        //Let's extract questStateReq and questNameReq from Function Argument
        if(args["QuestName"] != null)
        {
            questNameReq = args["QuestName"].ToString();
        }
        else
            return "Error: QuestName variable argument is not inserted!";

        if(args["QuestState"] != null)
        {
            questStateReq = args["QuestState"].ToString();
        }
        else
            return "Error: QuestState variable argument is not inserted!";

        //check PlayerQuestData exist or not.
        if(reqUserData.Result.Data.ContainsKey("PlayerQuestData"))
        {
            playerQuestData = JsonConvert.DeserializeObject<List<PlayFabQuestData>>(reqUserData.Result.Data["PlayerQuestData"].Value);
        }
        else
            return "Error: Player Quest Data not found!";

        //let's get questDataChange which will be used to change the quest state.
        PlayFabQuestData questDataChange = FindQuestData(playerQuestData, questNameReq);

        if(questDataChange == null)
        {
            return "Error: Player Quest Data not found, possibly trying to insert new quest data not exist in the Database!";
        }
        else
        {
            //change the Quest State
            questDataChange.QuestState = questStateReq;

            var reqUpdateUserData = await serverApi.UpdateUserDataAsync(new UpdateUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>() {{"PlayerQuestData",JsonConvert.SerializeObject(playerQuestData)}}
            });
        }

        //return Quest Data to Client for UI.
        return "Quest State Update Success!";
    }

    public static PlayFabQuestData FindQuestData(List<PlayFabQuestData> playerQuestData, string questNameReq)
    {
        foreach(var quest in playerQuestData)
        {
            if(quest.QuestName == questNameReq)
            return quest;
        }

        return null;
    }
}