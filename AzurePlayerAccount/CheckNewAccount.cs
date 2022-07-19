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

    [FunctionName("IsNewAccount")]
    public static async Task<dynamic> IsNewAccount([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {

        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

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

            //Declare new string variable
            string TeamInformation = reqInternalTitleData.Result.Data["NewPlayerTeam"];
            string JsonStringData = reqInternalTitleData.Result.Data["NewPlayerStarterItems"];
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
                        {"CurrentPlayerTeam", TeamInformation}
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