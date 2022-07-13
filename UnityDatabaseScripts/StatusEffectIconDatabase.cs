using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class StatusEffectIconDatabase
{

    public class Sprite{
        public string url;
    }
    public enum StatusEffectCategory
    {
        HP_Energy_Related,
        Combat_Related,
        Turn_Action_Related,
        Speed_Related,
        Add_Temp_Passive_Related,
        Critical_Rate_Related
    }

    [System.Serializable]
    public class StatusConditionDataPlayFab
    {
        public NBMonProperties.StatusEffect statusConditionName;
        public StatusEffectCategory statusEffectCategory;
        public string description;
        public bool spawnCancelIcon;
        public bool durationStack = false;
        public bool stackable = false;
        public int maxStacks = 1;
        public bool elementImmunity;
        public ElementDatabase.Elements immuneAgainstElement;

        public int HPChangesInNumber, HPChangesInPercent, EnergyChangesInNumber, EnergyChangesInPercent;
        public float Attack, SP_Attack, Defense, SP_Defense;
        public float CritRate;
        public string passiveName;
        public float RNG_Chance_To_Fail_Attacking;
        public float Speed;
    }

    public List<StatusConditionDataPlayFab> statusConditionDatabasePlayFab;

    public class StatusConditionDatabasePlayFabList
    {
        public List<StatusConditionDataPlayFab> statusConditionDatabasePlayFab;
    }

    public static StatusConditionDataPlayFab FindStatusEffectIcon(NBMonProperties.StatusEffect statusEffect)
    {
        StatusEffectIconDatabase.StatusConditionDatabasePlayFabList StatusEffectDatabase = new StatusEffectIconDatabase.StatusConditionDatabasePlayFabList(); 
        var StatusEffect = StatusEffectDatabaseJson.StatusEffectDataJson;
        StatusEffectDatabase = JsonConvert.DeserializeObject<StatusEffectIconDatabase.StatusConditionDatabasePlayFabList>(StatusEffect);

        foreach (var statusData in StatusEffectDatabase.statusConditionDatabasePlayFab)
        {
            if (statusData.statusConditionName == statusEffect)
            {
                return statusData;
            }
        }

        return null;
    }

    public static int CriticalStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var TheStatusEffectParam = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var TheStackValueOfTheCurrentEffect = thisNBMon.statusEffectList[i].stacks;

            if (TheStatusEffectParam.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Critical_Rate_Related)
                return (int) (TheStatusEffectParam.CritRate * (float)TheStackValueOfTheCurrentEffect);
        }
        return 0;
    }

    //Attack Stats
    public static float AttackStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float AttackModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var TheStatusEffectParam = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var TheStackValueOfTheCurrentEffect = thisNBMon.statusEffectList[i].stacks;

            if(TheStatusEffectParam.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var BuffAttackParam = (TheStatusEffectParam.Attack / 100f) * (float)TheStackValueOfTheCurrentEffect;

                AttackModifier *= (1f + BuffAttackParam);
            }
        }

        return AttackModifier;
    }

    //SP Attack Stats
    public static float SPAttackStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float SPAttackModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var TheStatusEffectParam = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var TheStackValueOfTheCurrentEffect = thisNBMon.statusEffectList[i].stacks;
            
            if(TheStatusEffectParam.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var BuffSPAttackParam = (TheStatusEffectParam.SP_Attack / 100f) * (float)TheStackValueOfTheCurrentEffect;

                SPAttackModifier *= (1f + BuffSPAttackParam);
            }
        }

        return SPAttackModifier;
    }

    //Defense Stats
    public static float DefenseStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float DefenseModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var TheStatusEffectParam = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var TheStackValueOfTheCurrentEffect = thisNBMon.statusEffectList[i].stacks;
            
            //Normal Logic
            if(TheStatusEffectParam.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var BuffDefenseParam = (TheStatusEffectParam.Defense / 100f) * (float)TheStackValueOfTheCurrentEffect;

                DefenseModifier *= (1f + BuffDefenseParam);
            }

            //Increase Defense when Frozen
            if(TheStatusEffectParam.statusConditionName == NBMonProperties.StatusEffect.Frozen)
            {
                DefenseModifier *= 1.5f;
            }
        }

        return DefenseModifier;
    }

    public static float SPDefenseStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float SPDefenseModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var TheStatusEffectParam = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var TheStackValueOfTheCurrentEffect = thisNBMon.statusEffectList[i].stacks;

            //Normal Logic
            if (TheStatusEffectParam.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var BuffSPDefenseParam = (TheStatusEffectParam.SP_Defense / 100f) * (float)TheStackValueOfTheCurrentEffect;

                SPDefenseModifier *= (1f + BuffSPDefenseParam);
            }

            //Increase Defense when Frozen
            if(TheStatusEffectParam.statusConditionName == NBMonProperties.StatusEffect.Frozen)
            {
                SPDefenseModifier *= 1.5f;
            }
        }

        return SPDefenseModifier;
    }
}
