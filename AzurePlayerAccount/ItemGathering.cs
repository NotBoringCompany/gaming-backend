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

public static class ItemGathering{
    

    //Azure Function
    [FunctionName("PickItem")]
    public static async Task<dynamic> PickItem([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Declare Variable
        List<string> itemIdList = new List<string>();
        string itemId = "";
        int itemQuantity;

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

        //Grant User The Item List
        var reqGrantItem = await serverApi.GrantItemsToUserAsync(
            new GrantItemsToUserRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                ItemIds = itemIdList,
                CatalogVersion = "InventoryTest"
            }
        );

        //Request User Inventory Item
        var reqUserInventory = await serverApi.GetUserInventoryAsync(
            new GetUserInventoryRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
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

        return $"{itemId} Picked up, {itemQuantity}x "+JsonConvert.SerializeObject(itemIdList);
    }
}