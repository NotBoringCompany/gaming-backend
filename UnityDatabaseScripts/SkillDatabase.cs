using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using PlayFab.Json;
using PlayFab.ServerModels;
using PlayFab;
//using PlayFab.DataModels;
using PlayFab.Samples;
//using PlayFab.AuthenticationModels;
using PlayFab.Plugins.CloudScript;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Drawing;
using Microsoft.Azure.Documents.Client;
using System.Linq;

public class SkillsDataBase
{
    public enum ActionType
    {
        Damage,
        StatsRecovery,
    }

    public enum AITargetType
    {
        ToMyself,
        ToTeamMate,
        ToEnemy
    }

    public enum TargetType
    {
        One,
        AllEnemy,
        All,
        EveryoneExceptMe,
        AllTeam
    }


    public enum TechniqueType
    {
        Attack,
        SpecialAttack
    }
    [System.Serializable]
    public class SkillInfoPlayFab
    {
        public string skillName;
        public string skillDescription;

        public int energyRequired = 0;
        public int criticalRate = 5;

        public ElementDatabase.Elements skillElement;
        public ActionType actionType;
        public TargetType targetType;
        public List<AITargetType> aiTargetType;
        public TechniqueType techniqueType;

        public bool DrainAbility;
        public float HPDrainInPercent, EnergyDrainInPercent;

        public int attack = 0;
        public int specialAttack = 0;

        public int hp = 0;
        public int energy = 0;
        public float hpPercent = 0;
        public float energyPercent = 0;
        public string StatusEffectDescription;

        public List<NBMonProperties.StatusEffectInfo> statusEffectList;
        public List<NBMonProperties.StatusEffectInfo> removeStatusEffectList;
        public List<NBMonProperties.StatusEffectInfo> statusEffectListSelf;
        public List<NBMonProperties.StatusEffectInfo> removeStatusEffectListSelf;

        public List<string> additionalSkillEffect;

        public List<string> allAvailableCutscene;

    }

    public List<SkillInfoPlayFab> skillInfosPlayFab;

    public static SkillInfoPlayFab FindSkill(string skillName, DocumentClient client)
    {
        // var SkillDatabaseJsonString = SkillDatabaseJson.SkillDataJson;
        // SkillInfoPlayFabList SkillDatabase = JsonConvert.DeserializeObject<SkillInfoPlayFabList>(SkillDatabaseJsonString);

        // foreach (var skill in SkillDatabase.skillInfosPlayFab)
        // {
        //     if(skillName == skill.skillName)
        //     {
        //         return skill;
        //     }
        // }

        //Declare Variable For Cosmos Usage
        var option = new FeedOptions(){ EnableCrossPartitionQuery = true };
        Uri collectionUri = UriFactory.CreateDocumentCollectionUri("RealmDb", "Skilldata");

        SkillInfoPlayFab usedData = client.CreateDocumentQuery<SkillInfoPlayFab>(collectionUri, $"SELECT * FROM db WHERE db.skillName = '{skillName}'",option).AsEnumerable().FirstOrDefault();
        
        //If usedData exists, return value
        if(usedData != null)
            return usedData;

        //else return null
        return null;
    }

    public class SkillInfoPlayFabList
    {
        public List<SkillInfoPlayFab> skillInfosPlayFab;
    
    }

}
