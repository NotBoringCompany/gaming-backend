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
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

public static class PlayerAccountInfo{
    

    [FunctionName("AddPlayFabId")]
    public static async Task<dynamic> AddPlayFabId([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){

        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request User Internal Data
        var reqInternalData = await serverApi.GetUserInternalDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        string moralisObjId = string.Empty;

        //Get Obj ID Data From Client
        if(args["userObjId"] != null)
            moralisObjId = args["userObjId"].ToString();
        else
            return $"Error: objId not found or not inserted!";

        //Check if User ethAddress, If not found terminate this function immediately.
        if(!reqInternalData.Result.Data.ContainsKey("ethAddress"))
            return "Address undefined!! Terminating function.";

        //Create new Body Dictionary for Andre's API
        var body = new Dictionary<string,dynamic>
        {
            {"ethAddress", reqInternalData.Result.Data["ethAddress"].Value},
            {"userObjId", moralisObjId},
            {"playfabId", context.CallerEntityProfile.Lineage.MasterPlayerAccountId }
        };
        
        //Declare new Variable
        HttpClient client = new HttpClient();
        string url = "https://api-realmhunter.herokuapp.com/account/addPlayfabId";
        string jsonBody = JsonConvert.SerializeObject(body);

        var buffer = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var task = Task.Run(() => client.PostAsync(url, byteContent));
        task.Wait();

        var response = task.Result;
        var contents = await response.Content.ReadAsStringAsync();

        log.LogInformation(jsonBody);
        log.LogInformation($"Moralis API Called");
        log.LogInformation($"Response: {response}");
        log.LogInformation("Contents:" + JsonConvert.SerializeObject(contents));
        
        return "Add PlayFab Id Success";
    }

    [FunctionName("AddUserData")]
    public static async Task<dynamic> AddUserData([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log){

        //Setup serverApi (Server API to PlayFab)
        FunctionExecutionContext<dynamic> context = JsonConvert.DeserializeObject<FunctionExecutionContext<dynamic>>(await req.ReadAsStringAsync());
        dynamic args = context.FunctionArgument;
        PlayFabServerInstanceAPI serverApi = AzureHelper.ServerAPISetup(args, context);

        //Request User Internal Data
        var reqInternalData = await serverApi.GetUserInternalDataAsync(
            new GetUserDataRequest{
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            }
        );

        string moralisObjId = string.Empty;

        //Get Obj ID Data From Client
        if(args["userObjId"] != null)
            moralisObjId = args["userObjId"].ToString();
        else
            return $"Error: objId not found or not inserted!";

        //Check if User ethAddress, If not found terminate this function immediately.
        if(!reqInternalData.Result.Data.ContainsKey("ethAddress"))
            return "Address undefined!! Terminating function.";

        //Create new Body Dictionary for Andre's API
        var body = new Dictionary<string,dynamic>
        {
            {"objId", moralisObjId},
            {"playfabId", context.CallerEntityProfile.Lineage.MasterPlayerAccountId }   
        };
        
        //Declare new Variable
        HttpClient client = new HttpClient();
        string url = "https://api-realmhunter.herokuapp.com/account/addUserData";
        string jsonBody = JsonConvert.SerializeObject(body);

        var buffer = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        var byteContent = new ByteArrayContent(buffer);
        byteContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        var task = Task.Run(() => client.PostAsync(url, byteContent));
        task.Wait();

        var response = task.Result;
        var contents = await response.Content.ReadAsStringAsync();

        log.LogInformation(jsonBody);
        log.LogInformation($"Moralis API Called addUserData");
        log.LogInformation($"Response: {response}");
        log.LogInformation("Contents:" + JsonConvert.SerializeObject(contents));
        
        return "Add User Data Success";
    }
}