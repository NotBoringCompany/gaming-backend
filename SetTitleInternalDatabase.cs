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
using System.Net.Http.Headers;
using System.Text;

namespace NBCompany.Setters
{
    public static class SetTitleInternalDatabase
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

        //Azure Cloud Function
        [FunctionName("SetSkillDatabase")]
        public static async Task<dynamic> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);


            string skillDatabase = args["Data"];

            SkillsDataBase.SkillInfoPlayFabList skillInfoPlayFabListObject = new SkillsDataBase.SkillInfoPlayFabList();

            //Checks if seriliazation is done correctly
            skillInfoPlayFabListObject = JsonConvert.DeserializeObject<SkillsDataBase.SkillInfoPlayFabList>(skillDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest
            {
                Key = "SkillDatabase",
                Value = skillDatabase
            });

            return request;
        }


        [FunctionName("SetElementDatabase")]
        public static async Task<dynamic> SetElementDatabase(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);


            string elementDatabase = args["Data"];

            ElementDatabase.ElementPropDatabasePlayFabList elementPropertiesPlayFabList = new ElementDatabase.ElementPropDatabasePlayFabList();

            //Checks if seriliazation is done correctly
            elementPropertiesPlayFabList = JsonConvert.DeserializeObject<ElementDatabase.ElementPropDatabasePlayFabList>(elementDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest
            {
                Key = "ElementDatabase",
                Value = elementDatabase
            });

            return request;
        }

        [FunctionName("SetItemDatabase")]
        public static async Task<dynamic> SetItemDatabase(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);


            string itemDatabase = args["Data"];

            InventoryItemsPlayFabLists inventoryItemsPlayFabLists = new InventoryItemsPlayFabLists();


            var economyApi = new PlayFabEconomyInstanceAPI(authContext);



            foreach (ItemsPlayFab inventoryItem in inventoryItemsPlayFabLists.ItemDataBasePlayFab)
            {
                var item = new PlayFab.EconomyModels.CatalogItem();
                item.Title = new Dictionary<string, string>();
                item.Title.Add("English", inventoryItem.Name);


                var request = await economyApi.CreateDraftItemAsync(new CreateDraftItemRequest()
                {
                    Item = item,
                    Publish = true
                });
            }

            return "success";
        }

        [FunctionName("SetPassiveDatabase")]
        public static async Task<dynamic> SetPassiveDatabase(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);


            string passiveDatabase = args["Data"];

            PassiveDatabase.PassiveInfosPlayFabList passiveInfosPlayFabLists = new PassiveDatabase.PassiveInfosPlayFabList();

            //Checks if seriliazation is done correctly
            passiveInfosPlayFabLists = JsonConvert.DeserializeObject<PassiveDatabase.PassiveInfosPlayFabList>(passiveDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest
            {
                Key = "passiveDatabase",
                Value = passiveDatabase
            });

            return request;
        }

        [FunctionName("SetStatusEffectIconDatabase")]
        public static async Task<dynamic> SetStatusEffectIconDatabase(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);


            string statusEffectIconDatabase = args["Data"];

            StatusEffectIconDatabase.StatusConditionDatabasePlayFabList statusConditionDatabasePlayFabList = new StatusEffectIconDatabase.StatusConditionDatabasePlayFabList();

            //Checks if seriliazation is done correctly
            statusConditionDatabasePlayFabList = JsonConvert.DeserializeObject<StatusEffectIconDatabase.StatusConditionDatabasePlayFabList>(statusEffectIconDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest
            {
                Key = "statusEffectIconDatabase",
                Value = statusEffectIconDatabase
            });

            return request;
        }

        [FunctionName("SetNBMonDatabase")]
        public static async Task<dynamic> SetNBMonDatabase(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);


            string NBMonDatabase = args["Data"];

            NBMonDatabase.MonstersPlayFabList monstersPlayFabList = new NBMonDatabase.MonstersPlayFabList();

            //Checks if seriliazation is done correctly
            monstersPlayFabList = JsonConvert.DeserializeObject<NBMonDatabase.MonstersPlayFabList>(NBMonDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest
            {
                Key = "NBMonDatabase",
                Value = NBMonDatabase
            });

            return request;
        }

        [FunctionName("SetTeamInformation")]
        public static async Task<dynamic> SetTeamInformation(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);


            string ListsTeamInformation = args["Data"];

            NBMonDataSave.ListsTeamInformation listsTeamInformation = new NBMonDataSave.ListsTeamInformation();

            //Checks if seriliazation is done correctly
            listsTeamInformation = JsonConvert.DeserializeObject<NBMonDataSave.ListsTeamInformation>(ListsTeamInformation);

            var request = await serverApi.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>(){
                    {"ListsTeamInformation", ListsTeamInformation}
                    },
                Permission = UserDataPermission.Private
            });



            return request;
        }

        [FunctionName("GrandItem")]
        public static async Task<dynamic> GrandItem([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //Setup serverApi (Server API to PlayFab)
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            dynamic args = context.FunctionArgument;
            PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

            string itemID = args["Data"];
            List<string> IDs = new List<string>();
            IDs.Add(itemID);

            var request = await serverApi.GrantItemsToUserAsync(new GrantItemsToUserRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                ItemIds = IDs,
                CatalogVersion = "InventoryTest"
            });

            return request;
        }

        [FunctionName("HerokuAddGenesisNBMons")]
        public static async Task<dynamic> HerokuAddGenesisNBMons([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            try{
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            var getUserReadOnlyDataRequest = await serverApi.GetUserInternalDataAsync(new GetUserDataRequest()
            { PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId });

            string address = getUserReadOnlyDataRequest.Result.Data["ethAddress"].Value.ToString();

            string GenesisNBMons = "";

            HttpWebRequest requestH = (HttpWebRequest)WebRequest.Create("https://api-realmhunter.herokuapp.com/genesisNBMon/getOwnerGenesisNBMons/" + address);
            requestH.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;

            using (HttpWebResponse response = (HttpWebResponse)requestH.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                GenesisNBMons = reader.ReadToEnd();
            }

            var request = await serverApi.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>(){
                    {"GenesisNBMons", GenesisNBMons}
                    },
                Permission = UserDataPermission.Private
            });

            return request;
            }
            catch(Exception e){
                return e;
            }
        }

        public class UserInfo{
            public string ethAddress {get; set;}
            public string email {get; set;}
        }

        [FunctionName("SetSessionToken")]
        public static async Task<dynamic> SetSessionToken([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            try{
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings
            {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext
            {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            string sessionToken = args["sessionToken"]; 

            var body = new Dictionary<string,string>
                {
                    {"sessionToken", sessionToken}
                };

            HttpClient client = new HttpClient();
            
            string URL = "https://nbgamingbackend.herokuapp.com/";
            client.BaseAddress = new Uri(URL);
            var api = "userCheck/retrieveUserBySessionToken";

            var bodyContent = JsonConvert.SerializeObject(body);

            var buffer = System.Text.Encoding.UTF8.GetBytes(bodyContent);
            var byteContent = new ByteArrayContent(buffer);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var task = Task.Run(() => client.PostAsync(api, byteContent));
            task.Wait();
            
            var response = task.Result;
            var contents = await response.Content.ReadAsStringAsync();
            
            client.Dispose();

            UserInfo userInfo = JsonConvert.DeserializeObject<UserInfo>(contents);


            // 2 - second request for addLoggedInUser so it doesn't override/mismatch info
            HttpClient client2 = new HttpClient();
            
            string URL2 = "https://nbgamingbackend.herokuapp.com/";
            client2.BaseAddress = new Uri(URL2);
            var api2 = "userCheck/addLoggedInUser";

            var bodyContent2 = JsonConvert.SerializeObject(body);

            var buffer2 = System.Text.Encoding.UTF8.GetBytes(bodyContent2);
            var byteContent2 = new ByteArrayContent(buffer2);
            byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var task2 = Task.Run(() => client.PostAsync(api2, byteContent2));
            task.Wait();
            
            var response2 = task.Result;
            var contents2 = await response.Content.ReadAsStringAsync();
            
            client2.Dispose();

            string ethAddress = userInfo.ethAddress;
            string email = userInfo.email;

            var request = await serverApi.UpdateUserInternalDataAsync(new UpdateUserInternalDataRequest
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    Data = new Dictionary<string, string>(){
                        {"ethAddress", ethAddress},
                        {"email", email},
                        {"sessionToken", sessionToken}
                        }
                });

                return request;

            } catch(Exception e){
                return e;
            }
        }


        [FunctionName("GetItemDrops")]
        public static async Task<dynamic> GetItemDrops([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //Setup serverApi (Server API to PlayFab)
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            dynamic args = context.FunctionArgument;
            PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

            //Get Player's Internal Title Data
            var getPlayerInternalDataRequest = await serverApi.GetUserInternalDataAsync(new GetUserDataRequest()
            { PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId });

            //Check if the ItemDrops contains string.Empty or not, if it's does, return "There's no drop rate obtained!".
            if (getPlayerInternalDataRequest.Result.Data.ContainsKey("ItemDrops"))
            {
                //Convert the Json String given from the request.
                string JsonStringData = getPlayerInternalDataRequest.Result.Data["ItemDrops"].Value;
                List<string> ListOfItemDropIDs = new List<string>();
                ListOfItemDropIDs = JsonConvert.DeserializeObject<List<String>>(JsonStringData)!;

                //Let's sent the dropped items into the player.
                var request = await serverApi.GrantItemsToUserAsync(new GrantItemsToUserRequest
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    ItemIds = ListOfItemDropIDs,
                    CatalogVersion = "InventoryTest"
                });

                //After the Item Drops given to player, let's delete this Key.
                var setPlayerInternalDataRequest = await serverApi.UpdateUserInternalDataAsync(new UpdateUserInternalDataRequest()
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    KeysToRemove = new List<string>() { "ItemDrops" }
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
                

                return "Dropped Items are sent to Player's Inventory!";
            }
            else
            {
                return "There's no Item Dropped! Good luck next time";
            }
        }

        [FunctionName("AddItemToItemDrops")]
        public static async Task<dynamic> AddItemToItemDrops([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //Setup serverApi (Server API to PlayFab)
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            dynamic args = context.FunctionArgument;
            PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

            //Declare used Variables for Testing
            var MonsterIDWeWantToUse = 11; //Roggo's ID
            NBMonDatabase.MonstersPlayFabList TempData = new NBMonDatabase.MonstersPlayFabList();
            List<string> DroppedItems = new List<string>();

            //Get Player's Internal Title Data
            var getPlayerInternalDataRequest = await serverApi.GetUserInternalDataAsync(new GetUserDataRequest()
            { PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId });

            //Check if the ItemDrops contains string.Empty or not, if it's does, add into DroppedItems string List".
            if (getPlayerInternalDataRequest.Result.Data.ContainsKey("ItemDrops"))
            {
                string JsonStringData = getPlayerInternalDataRequest.Result.Data["ItemDrops"].Value;
                DroppedItems = JsonConvert.DeserializeObject<List<String>>(JsonStringData)!;
            }

            //Let's request NBMonDatabase from Internal Title Data
            var requestNBMonDatabaseTitleData = await serverApi.GetTitleInternalDataAsync(new GetTitleDataRequest());

            //Check if the NBMonDatabase Key is exist in the Title Internal Data
            if (requestNBMonDatabaseTitleData.Result.Data.ContainsKey("NBMonDatabase"))
            {
                //Convert Json String into Class Data using NBMonDatabase.MonstersPlayFabList
                TempData = JsonConvert.DeserializeObject<NBMonDatabase.MonstersPlayFabList>(requestNBMonDatabaseTitleData.Result.Data["NBMonDatabase"]);

                //Find Monster we want to uses
                NBMonDatabase.MonsterInfoPlayFab UsedMonster = TempData.monstersPlayFab[MonsterIDWeWantToUse];

                //Let's do RNG Looping for testing purposes
                foreach (var ItemDropTable in UsedMonster.LootLists)
                {
                    Random rand = new Random();
                    if (ItemDropTable.RNGChance > rand.Next(0, 100))
                    {
                        DroppedItems.Add(ItemDropTable.ItemDrop.PlayFabItemID);
                    }
                }

                //Let's set the DroppedItems into ItemDrops inside Player Internal Title Data
                if (DroppedItems.Count != 0)
                {
                    var request = serverApi.UpdateUserInternalDataAsync(new UpdateUserInternalDataRequest()
                    {
                        PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                        Data = new Dictionary<string, string>() { { "ItemDrops", JsonConvert.SerializeObject(DroppedItems) } }
                    });

                    return "Success!";
                }
            }

            return "No Item was Dropped!";
        }

        [FunctionName("AddItemToItemDrops_MK2")]
        public static async Task<dynamic> AddItemToItemDrops_MK2([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            //Setup serverApi (Server API to PlayFab)
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
            dynamic args = context.FunctionArgument;
            PlayFabServerInstanceAPI serverApi = SetupServerAPI(args, context);

            //Declare used Variables for Testing
            var MonsterIDWeWantToUse = 11; //Roggo's ID
            NBMonDatabase.MonstersPlayFabList TempData = new NBMonDatabase.MonstersPlayFabList();
            List<string> DroppedItems = new List<string>();

            //Get Player's Internal Title Data
            var getPlayerInternalDataRequest = await serverApi.GetUserInternalDataAsync(new GetUserDataRequest()
            { PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId, Keys = new List<string>() { "ItemDrops" } });

            //Check if the ItemDrops contains string.Empty or not, if it's does, add into DroppedItems string List".
            if (getPlayerInternalDataRequest.Result.Data.ContainsKey("ItemDrops"))
            {
                string JsonStringData = getPlayerInternalDataRequest.Result.Data["ItemDrops"].Value;
                DroppedItems = JsonConvert.DeserializeObject<List<String>>(JsonStringData)!;
            }

            var MonsterJsonData = NBMonDatabaseJson.MonsterDatabaseJson;
            TempData = JsonConvert.DeserializeObject<NBMonDatabase.MonstersPlayFabList>(MonsterJsonData);

            //Find Monster we want to uses
            NBMonDatabase.MonsterInfoPlayFab UsedMonster = TempData.monstersPlayFab[MonsterIDWeWantToUse];

            //Let's do RNG Looping for testing purposes
            foreach (var ItemDropTable in UsedMonster.LootLists)
            {
                Random rand = new Random();
                if (ItemDropTable.RNGChance > rand.Next(0, 100))
                {
                    DroppedItems.Add(ItemDropTable.ItemDrop.PlayFabItemID);
                }
            }

            //Let's set the DroppedItems into ItemDrops inside Player Internal Title Data
            if (DroppedItems.Count != 0)
            {
                var request = serverApi.UpdateUserInternalDataAsync(new UpdateUserInternalDataRequest()
                {
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    Data = new Dictionary<string, string>() { { "ItemDrops", JsonConvert.SerializeObject(DroppedItems) } }
                });

                return "Success!";
            }

            return "No item was dropped.";
        }
    }
}

