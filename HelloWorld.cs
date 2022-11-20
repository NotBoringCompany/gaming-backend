using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab.Samples;
using System.Collections.Generic;

namespace NBCompany.HelloWorld
{
    [System.Serializable]
    public class PlayerItemSlotsData
    {
        public string ItemName;
        public int Quantity;
    }

    public static class HelloWorld
    {
        [FunctionName("HelloWorld")]
        public static async Task<dynamic> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            
            FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());

            dynamic args = context.FunctionArgument;

            var message = $"Hello {context.CallerEntityProfile.Lineage.MasterPlayerAccountId}!";
            log.LogInformation(message);

            dynamic inputValue = null;
            if (args != null && args["inputValue"] != null)
            {
                inputValue = args["inputValue"];
            }

            try{

            string PlayerItemSlotsDataString = args["inputValue"];

            PlayerItemSlotsData ourItem = JsonConvert.DeserializeObject<PlayerItemSlotsData>(PlayerItemSlotsDataString);
            string ItemName = ourItem.ItemName;

            message = $"Hello {ItemName}!";

            log.LogDebug($"HelloWorld: {new { input = inputValue} }");

            return new { messageValue = message };

            }
            catch(Exception e){
                return e.ToString();
            }
            
        }
    }
}
