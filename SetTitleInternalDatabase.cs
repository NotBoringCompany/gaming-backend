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

namespace NBCompany.Setters
{
    public static class SetTitleInternalDatabase
    {
        [FunctionName("SetSkillDatabase")]
        public static async Task<dynamic> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var apiSettings = new PlayFabApiSettings {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            
            string skillDatabase = args["Data"];

            SkillsDataBase.SkillInfoPlayFabList skillInfoPlayFabListObject = new SkillsDataBase.SkillInfoPlayFabList();

            //Checks if seriliazation is done correctly
            skillInfoPlayFabListObject = JsonConvert.DeserializeObject<SkillsDataBase.SkillInfoPlayFabList>(skillDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest{
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

            var apiSettings = new PlayFabApiSettings {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            
            string elementDatabase = args["Data"];

            ElementDatabase.ElementPropDatabasePlayFabList elementPropertiesPlayFabList = new ElementDatabase.ElementPropDatabasePlayFabList();

            //Checks if seriliazation is done correctly
            elementPropertiesPlayFabList = JsonConvert.DeserializeObject<ElementDatabase.ElementPropDatabasePlayFabList>(elementDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest{
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

            var apiSettings = new PlayFabApiSettings {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            
            string itemDatabase = args["Data"];

            InventoryItemsPlayFabLists inventoryItemsPlayFabLists = new InventoryItemsPlayFabLists();


            var economyApi = new PlayFabEconomyInstanceAPI(authContext);

            

            foreach(ItemsPlayFab inventoryItem in inventoryItemsPlayFabLists.ItemDataBasePlayFab){
                var item = new PlayFab.EconomyModels.CatalogItem();
                item.Title = new Dictionary<string, string>();
                item.Title.Add("English", inventoryItem.Name);


                var request = await economyApi.CreateDraftItemAsync(new CreateDraftItemRequest(){
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

            var apiSettings = new PlayFabApiSettings {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            
            string passiveDatabase = args["Data"];

            PassiveDatabase.PassiveInfosPlayFabList passiveInfosPlayFabLists = new PassiveDatabase.PassiveInfosPlayFabList();

            //Checks if seriliazation is done correctly
            passiveInfosPlayFabLists = JsonConvert.DeserializeObject<PassiveDatabase.PassiveInfosPlayFabList>(passiveDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest{
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

            var apiSettings = new PlayFabApiSettings {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            
            string statusEffectIconDatabase = args["Data"];

            StatusEffectIconDatabase.StatusConditionDatabasePlayFabList statusConditionDatabasePlayFabList = new StatusEffectIconDatabase.StatusConditionDatabasePlayFabList();

            //Checks if seriliazation is done correctly
            statusConditionDatabasePlayFabList = JsonConvert.DeserializeObject<StatusEffectIconDatabase.StatusConditionDatabasePlayFabList>(statusEffectIconDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest{
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

            var apiSettings = new PlayFabApiSettings {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            
            string NBMonDatabase = args["Data"];

            NBMonDatabase.MonstersPlayFabList monstersPlayFabList = new NBMonDatabase.MonstersPlayFabList();

            //Checks if seriliazation is done correctly
            monstersPlayFabList = JsonConvert.DeserializeObject<NBMonDatabase.MonstersPlayFabList>(NBMonDatabase);

            var request = await serverApi.SetTitleInternalDataAsync(new SetTitleDataRequest{
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

            var apiSettings = new PlayFabApiSettings {
                TitleId = context.TitleAuthenticationContext.Id,
                DeveloperSecretKey = Environment.GetEnvironmentVariable("PLAYFAB_DEV_SECRET_KEY", EnvironmentVariableTarget.Process)
            };

            var authContext = new PlayFabAuthenticationContext {
                EntityId = context.TitleAuthenticationContext.EntityToken
            };

            var serverApi = new PlayFabServerInstanceAPI(apiSettings, authContext);

            
            string ListsTeamInformation = args["Data"];

            NBMonDataSave.ListsTeamInformation listsTeamInformation = new NBMonDataSave.ListsTeamInformation();

            //Checks if seriliazation is done correctly
            listsTeamInformation = JsonConvert.DeserializeObject<NBMonDataSave.ListsTeamInformation>(ListsTeamInformation);

            var request = await serverApi.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>(){
                    {"ListsTeamInformation", ListsTeamInformation}
                    },
                Permission = UserDataPermission.Private
            });



            return request;
            }
        }
}

