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
using System.Numerics;

public static class CraftingItem{
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

    //Azure Function
    [FunctionName("CraftItem")]
    public static async Task<dynamic> CraftItem([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Declare Variable
        List<string> itemIdList = new List<string>();
        string itemId = "";
        int itemQuantity;
        BigInteger totalCost = new BigInteger();

        //Get ID Data From Client
        if(args["itemID"] != null)
            itemId = args["itemID"];

        //Get Quantity Data From Client
        if(args["itemQuantity"] != null)
            itemQuantity = (int) args["itemQuantity"];
        else
            itemQuantity = 1;

        //Add Item Into List Based On Quantity
        for(int i = 0; i < itemQuantity; i++){
            itemIdList.Add(itemId);
        }

        //Get total Cost From Client
        if(args["totalCost"] != null)
            totalCost = (BigInteger) args["totalCost"];

        //Grant User The Item List
        var reqGrantItem = await serverApi.GrantItemsToUserAsync(
            new GrantItemsToUserRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                ItemIds = itemIdList,
                CatalogVersion = "InventoryTest"
            }
        );

        var reqUserReadOnly = await serverApi.GetUserReadOnlyDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Request User Inventory Item
        var reqUserInventory = await serverApi.GetUserInventoryAsync(
            new GetUserInventoryRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        //Deduct xRes From User
        BigInteger xResUser = BigInteger.Parse(reqUserReadOnly.Result.Data["xRES"].Value);
        xResUser -= totalCost;

        //Update xRes into PlayFab
        var updateUserReadOnly = await serverApi.UpdateUserReadOnlyDataAsync(
            new UpdateUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>{
                    {"xRES", xResUser.ToString()}
                }
            }
        );

        List<ItemInstance> ItemData = reqUserInventory.Result.Inventory;
        //Let's update Player Inventory if the Player's item exceeded maximum item or not.
        foreach (var IndividualItemInstance in ItemData)
        {
            if (IndividualItemInstance.RemainingUses >= 99)
            {
                var requestModifyItem = await serverApi.ModifyItemUsesAsync(new ModifyItemUsesRequest()
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    ItemInstanceId = IndividualItemInstance.ItemInstanceId,
                    UsesToAdd = (int)(99 - IndividualItemInstance.RemainingUses)
                });
            }
        }

        //Return Current user xRes
        return xResUser.ToString();
    }
}