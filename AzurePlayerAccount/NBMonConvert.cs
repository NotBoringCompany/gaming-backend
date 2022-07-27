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



public static class NBMonConvert
{
    //NBMon's Moralis Data Structure
    public class NBMonMoralisData
    {
        public int nbmonId;
        public string owner;
        public int bornAt;
        public int hatchedAt;
        public bool isHatchable;
        public int transferredAt;
        public int hatchingDuration;
        public List<string> types;
        public List<string> passives;
        public string gender;
        public string rarity;
        public string mutation;
        public string mutationType;
        public string behavior;
        public string species;
        public string genus;
        public string genusDescription;
        public int fertility;
        public int healthPotential;
        public int energyPotential;
        public int attackPotential;
        public int defensePotential;
        public int spAtkPotential;
        public int spDefPotential;
        public int speedPotential;
        public bool isEgg;
        public int currentExp;
        public int level;
        public string nickname;
        public List<string> skillList;
        public int maxHpEffort;
        public int maxEnergyEffort;
        public int speedEffort;
        public int attackEffort;
        public int specialAttackEffort;
        public int defenseEffort;
        public int specialDefenseEffort;
    }

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

    [FunctionName("ConvertNBMonFromMoralis")]
    public static async Task<dynamic> NBMonMoralisConvertion([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
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

        //Get User Read Only Title Data Information
        var reqReadOnlyTitleData = await serverApi.GetUserReadOnlyDataAsync(            
            new GetUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId
            });

        //Let's Declare Variable We Want To Uses.
        List<NBMonBattleDataSave> StellaBlockChainPC = new List<NBMonBattleDataSave>();
        List<NBMonMoralisData> PlayerDataFromMoralis = new List<NBMonMoralisData>();

        //Let's Extract Data From PlayFab
        if(reqTitleData.Result.Data.ContainsKey("StellaBlockChainPC"))
            StellaBlockChainPC = JsonConvert.DeserializeObject<List<NBMonBattleDataSave>>(reqTitleData.Result.Data["StellaBlockChainPC"].Value);

        if(reqReadOnlyTitleData.Result.Data.ContainsKey("BlockChainPC"))
            PlayerDataFromMoralis = JsonConvert.DeserializeObject<List<NBMonMoralisData>>(reqReadOnlyTitleData.Result.Data["BlockChainPC"].Value);

        log.LogInformation(JsonConvert.SerializeObject(PlayerDataFromMoralis));

        //Convert Time
        ConvertNBMonDataFromMoralisToPlayFab(StellaBlockChainPC, PlayerDataFromMoralis);

        //Update StellaBlockChainPC Data Into User Title Data
        var updateUserData = serverApi.UpdateUserDataAsync(
            new UpdateUserDataRequest
            {
                PlayFabId = context.CallerEntityProfile.Lineage.MasterPlayerAccountId,
                Data = new Dictionary<string, string>{
                    {"StellaBlockChainPC", JsonConvert.SerializeObject(StellaBlockChainPC)}
                }
            }
        );

        return null;
    }

    public static void ConvertNBMonDataFromMoralisToPlayFab(List<NBMonBattleDataSave> StellaBlockChainPC, List<NBMonMoralisData> PlayerDataFromMoralis)
    {
        foreach(var MonsterFromMoralis in PlayerDataFromMoralis)
        {
            //Declare New NBMonBattleDataSave Variable
            NBMonBattleDataSave NewMonsterData = new NBMonBattleDataSave();

            //Check if The MonsterFromMoralis Data has Null or String Empty Data
            if(string.IsNullOrEmpty(MonsterFromMoralis.genus)) //Genus is MosnterId in the game
                continue;

            if(string.IsNullOrEmpty(MonsterFromMoralis.rarity)) //Rarity is Quality of the Monster in the game
                continue;

            //Check if this Monster is an Egg, if it's an Egg, Skip Data Processing.
            if(MonsterFromMoralis.isEgg)
                continue;

            //Convertion Begin
            NewMonsterData.owner = MonsterFromMoralis.owner;
            
            if(!string.IsNullOrEmpty(MonsterFromMoralis.nickname as string))
                NewMonsterData.nickName = MonsterFromMoralis.nickname as string;
            else    
                NewMonsterData.nickName = MonsterFromMoralis.genus;

            NewMonsterData.uniqueId = MonsterFromMoralis.nbmonId.ToString();
            NewMonsterData.monsterId = MonsterFromMoralis.genus;
            NewMonsterData.gender = DefineNBMonGender(MonsterFromMoralis.gender);
            
            //Monster Level is not Defined, will uses Default Lv. 5
            NewMonsterData.level = 5;
            NewMonsterData.fertility = MonsterFromMoralis.fertility;
            NewMonsterData.selectSkillsByHP = false;
            NewMonsterData.setSkillByHPBoundaries = new List<NBMonBattleDataSave.SkillByHP>();
            NewMonsterData.Quality = DefineNBMonQuality(MonsterFromMoralis.rarity);
            NewMonsterData.MutationAcquired = DefineMutation(MonsterFromMoralis.mutation);
            
            //Mutation Type
            if(NewMonsterData.MutationAcquired)
            {
                //To Do, Insert Mutation Type;
            }
            else
                NewMonsterData.MutationType = 0;

            //Insert Potential Stats
            NewMonsterData.maxHpPotential = MonsterFromMoralis.healthPotential;
            NewMonsterData.maxEnergyPotential = MonsterFromMoralis.energyPotential;
            NewMonsterData.speedPotential = MonsterFromMoralis.speedPotential;
            NewMonsterData.attackPotential = MonsterFromMoralis.attackPotential;
            NewMonsterData.specialAttackPotential = MonsterFromMoralis.spAtkPotential;
            NewMonsterData.defensePotential = MonsterFromMoralis.defensePotential;
            NewMonsterData.specialDefensePotential = MonsterFromMoralis.spDefPotential;

            //Effort Stats is an object Class, convert it to Null
            NewMonsterData.maxHpEffort = MonsterFromMoralis.maxHpEffort;
            NewMonsterData.maxEnergyEffort = MonsterFromMoralis.maxEnergyEffort;
            NewMonsterData.speedEffort = MonsterFromMoralis.speedEffort;
            NewMonsterData.attackEffort = MonsterFromMoralis.attackEffort;
            NewMonsterData.specialAttackEffort = MonsterFromMoralis.specialAttackEffort;
            NewMonsterData.defenseEffort = MonsterFromMoralis.defenseEffort;
            NewMonsterData.specialDefenseEffort = MonsterFromMoralis.specialDefenseEffort;

            //Get Monster Data Base using Monster's MonsterID. Not Unique ID.
            NBMonDatabase.MonsterInfoPlayFab MonsterFromDatabase = NBMonDatabase.FindMonster(NewMonsterData.monsterId);

            //After that Recalculate NBMon Stats
            NBMonStatsCalculation.StatsCalculation(NewMonsterData, MonsterFromDatabase, true);

            //insert Unique Skill List
            if(MonsterFromMoralis.skillList != null || MonsterFromMoralis.skillList.Count > 0)
            {
                NewMonsterData.uniqueSkillList = MonsterFromMoralis.skillList;
            }
            else
                NewMonsterData.uniqueSkillList = new List<string>();

            //insert Equipped Skill List
            if(NewMonsterData.uniqueSkillList.Count == 0)
            {
                NewMonsterData.skillList.Add(MonsterFromDatabase.baseSkillAndPassive[0].allBaseSkill[0]);

                if(MonsterFromDatabase.baseSkillAndPassive[0].allBaseSkill.Count > 1)
                    NewMonsterData.skillList.Add(MonsterFromDatabase.baseSkillAndPassive[0].allBaseSkill[1]);
            }
            else
            {
                NewMonsterData.skillList = NewMonsterData.uniqueSkillList;
            }

            //Passives
            NewMonsterData.passiveList = MonsterFromMoralis.passives;
            NewMonsterData.temporaryPassives = new List<string>();

            //Once convertion is done, let's add into StellaBlockChainPC.
            StellaBlockChainPC.Add(NewMonsterData);
        }
    }

    public static int DefineNBMonGender(string Gender)
    {
        if(Gender == "Male")
            return 0;

        if(Gender == "Female")
            return 1;

        //Default Male
        return 0;
    }

    public static int DefineNBMonQuality(string Rarity)
    {
        switch (Rarity)
        {
            case "Common":
                return 0;
            case "Uncommon":
                return 1;
            case "Rare":
                return 2;
            case "Elite":
                return 3;
            case "Legend":
                return 4;
            default:
                return 0;
        }
    }

    public static bool DefineMutation(string MutationInfo)
    {
        if(MutationInfo == "Not mutated")
            return false;
        else
            return true;
    }
}