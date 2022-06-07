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
using PlayFab.ServerModels;

namespace NBCompany.Setters
{
    public static class SetTitleInternalDatabase
    {
        [FunctionName("SetTitleInternalDatabase")]
        public static async Task<dynamic> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try{
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

                var request = await serverApi.UpdateUserReadOnlyDataAsync(new UpdateUserDataRequest(){
                    PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                    Data = new Dictionary<string, string>(){
                        {"SkillDatabase", skillDatabase}
                    }
                });

                return request.Result.ToString();
            }
            catch(Exception e){
                return e;
            }
        }
    }
}
