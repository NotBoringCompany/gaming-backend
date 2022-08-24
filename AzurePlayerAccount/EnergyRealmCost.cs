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

public static class EnergyRealmCost{
    

    //Azure Function
    [FunctionName("RealmEnergyCost")]
    public static async Task<dynamic> RealmEnergyCost([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Declare Variable
        List<string> itemIdList = new List<string>();
        int playerStamina = new int();
        int energyCost= new int();

        //Get ID Data From Client
        if(args["EnergyCost"] != null)
            energyCost = args["EnergyCost"];
        else
            energyCost = 0;

        //Request User Inventory Item
        var reqUserInventory = await serverApi.GetUserInventoryAsync(
            new GetUserInventoryRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Get player's current stamina
        if(reqUserInventory.Result.VirtualCurrency.ContainsKey("ST"))
            playerStamina = reqUserInventory.Result.VirtualCurrency["ST"];
        else
            playerStamina = 0;

        //Check if the player has enough stamina
        if(playerStamina >= energyCost)
        {
            var reqUserUpdateInventory = await serverApi.SubtractUserVirtualCurrencyAsync(
                new SubtractUserVirtualCurrencyRequest {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    VirtualCurrency = "ST", 
                    Amount = energyCost
                });

            return null;
        }
        else
        {
            return $"Error: Player has not enough stamina left!";
        }
    }
}