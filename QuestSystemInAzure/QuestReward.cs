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

public static class QuestReward
{
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

    [FunctionName("GetQuestReward")]
    public static async Task<dynamic> GetQuestReward([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
    {            
        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

        //Get Player Read Only Data.
        var reqUserReadOnly = await serverApi.GetUserReadOnlyDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            });

        //Declare Vairables
        int questID = new int();
        double xResUser = new double();
        List<string> itemIds = new List<string>();

        //Extract Quest ID from Client to Azure.
        if(args["QuestID"] != null)
            questID = (int)args["QuestID"];
        else
            return $"Error: Missing QuestID Input!";

        //Get Player's xRES from Read Only Player Data
        if(reqUserReadOnly.Result.Data.ContainsKey("xRES"))
            xResUser = double.Parse(reqUserReadOnly.Result.Data["xRES"].Value);
        else
            xResUser = 0;

        //Find Quest Data
        var questData = QuestRewardDatabase.FindQuestUsingID(questID);

        //Check Quest Data is null or not.
        if(questData == null)
        {
            return $"No such Quest Reward Database exist!";
        }
        else //Quest Data is not Null
        {
            //Let's add realmShard Data
            if(questData.shardReward > 0)
            {
                xResUser += questData.shardReward;
                xResUser = Math.Round(xResUser, 2);

                //Update xRes into PlayFab
                var updateUserReadOnly = await serverApi.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    Data = new Dictionary<string, string>{{"xRES", xResUser.ToString()}}
                });
            }

            //Let's add Player's Item
            if(questData.itemRewards.Count > 0)
            {
                foreach(var itemData in questData.itemRewards)
                {
                    //find Player item Data
                    string playfabItemId = UseItem.FindPlayFabItemID(itemData.itemName);

                    //Let's add this item into the itemIds based on Quantity
                    if(!string.IsNullOrEmpty(playfabItemId))
                    {
                        for(int i = 0; i < itemData.itemQuantity; i++)
                        {
                            itemIds.Add(playfabItemId);
                        }
                    }
                }

                //Let's call PlayFab Function to Add Item.
                var reqGrantItem = await serverApi.GrantItemsToUserAsync( new GrantItemsToUserRequest
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    ItemIds = itemIds,
                    CatalogVersion = "InventoryTest" 
                });

                //Check Item Data
                var requestPlayerInventory = await serverApi.GetUserInventoryAsync(new GetUserInventoryRequest()
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                });

                List<ItemInstance> PlayerItemData = requestPlayerInventory.Result.Inventory;

                //Let's update Player Inventory if the Player's item exceeded maximum item or not.
                foreach (var IndividualItemInstance in PlayerItemData)
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
            }
        }

        //return Quest Data to Client for UI.
        return JsonConvert.SerializeObject(questData);
    }
}