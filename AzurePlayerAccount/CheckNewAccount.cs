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

public static class CheckNewAccount
{
    [FunctionName("IsNewAccount")]
    public static async Task<dynamic> IsNewAccount([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Get User Title Data Information
        var reqTitleData = await serverApi.GetUserDataAsync(
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Check if Data Exists or not
        if (reqTitleData.Result.Data.ContainsKey("CurrentPlayerTeam"))
        {
            //Data Found
            return "Player Team Information Found.";
        }
        else
        {
            //Data Not Found, Create new title data for team information and inventory
            var reqInternalTitleData = await serverApi.GetTitleInternalDataAsync(new GetTitleDataRequest());
            var reqPrimaryTitleData = await serverApi.GetTitleDataAsync(new GetTitleDataRequest());

            //Declare new string variable
            string TeamInformation = reqInternalTitleData.Result.Data["NewPlayerTeam"];
            string JsonStringData = reqInternalTitleData.Result.Data["NewPlayerStarterItems"];
            string defaultQuest = reqPrimaryTitleData.Result.Data["DefaultQuest"];
            string defaultQuestVar = reqPrimaryTitleData.Result.Data["DefaultVariable"];
            List<string> ListOfItemIDs = new List<string>();
            ListOfItemIDs = JsonConvert.DeserializeObject<List<String>>(JsonStringData)!;

            List<string> BundleIDs = new List<string>();

            BundleIDs.Add("DemoStarterItem");

            //Send Data Into User Title Data
            var updateUserData = serverApi.UpdateUserDataAsync(
                new UpdateUserDataRequest
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    Data = new Dictionary<string, string>{
                        {"CurrentPlayerTeam", TeamInformation},
                        {"PlayerQuestData", defaultQuest},
                        {"PlayerVariableData", defaultQuestVar},

                    }
                }
            );

            //Send Data Into User ReadOnlyData
            var updateReadOnly = serverApi.UpdateUserReadOnlyDataAsync(
              new UpdateUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    Data = new Dictionary<string, string>{
                        {"xREC", "0"},
                        {"xRES", "0"}
                    }
              }  
            );

            //Send Inventory Data
            var updateInventoryData = serverApi.GrantItemsToUserAsync(
                new GrantItemsToUserRequest
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    ItemIds = BundleIDs,
                    CatalogVersion = "InventoryTest"
                }
            );

            return "Player Team Information Not Found! Create New Title Data";
        }
    }
}