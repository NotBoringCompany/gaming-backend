using System.Collections;
using System.Collections.Generic;
using Microsoft.Azure.Documents.Client;
using MongoDB.Bson;
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
        public ObjectId _id { get; set; }
        public NBMonProperties.StatusEffect statusConditionName;
        public StatusEffectCategory statusEffectCategory;
        public SkillsDataBase.RemoveStatusEffectType statusEffectType;
        public string description;
        public object icon;
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

    public static void ApplyStatusEffectImmediately(NBMonBattleDataSave thisNBMon)
    {
        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var theStatusEffectParam = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var thisNBMonTempPassive = thisNBMon.temporaryPassives;

            if(theStatusEffectParam.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Add_Temp_Passive_Related)
            {
                if (!thisNBMonTempPassive.Contains(theStatusEffectParam.passiveName))
                    thisNBMonTempPassive.Add(theStatusEffectParam.passiveName);
            }
        }
    }

    public static int CriticalStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var theStatusEffectParam = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var theStackValueOfTheCurrentEffect = thisNBMon.statusEffectList[i].stacks;

            if (theStatusEffectParam.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Critical_Rate_Related)
                return (int) (theStatusEffectParam.CritRate * (float)theStackValueOfTheCurrentEffect);
        }
        return 0;
    }

    //Attack Stats
    public static float AttackStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float attackModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var statusEffectFromMonster = thisNBMon.statusEffectList[i];
            var statusEffectFromDatabase = UseItem.FindStatusEffectFromDatabase(statusEffectFromMonster.statusEffect);

            if(statusEffectFromDatabase.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var buffAttackParam = (statusEffectFromDatabase.Attack / 100f) * (float)statusEffectFromMonster.stacks;

                attackModifier *= (1f + buffAttackParam);
            }

            //Burn reduce Attack by 50%.
            if(statusEffectFromDatabase.statusConditionName == NBMonProperties.StatusEffect.Burned)
            {
                attackModifier *= 0.5f;
            }
        }

        return attackModifier;
    }

    //SP Attack Stats
    public static float SPAttackStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float spAttackModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var statusEffectFromMonster = thisNBMon.statusEffectList[i];
            var statusEffectFromDatabase = UseItem.FindStatusEffectFromDatabase(statusEffectFromMonster.statusEffect);
            
            if(statusEffectFromDatabase.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var buffSPAttackParam = (statusEffectFromDatabase.SP_Attack / 100f) * (float)statusEffectFromMonster.stacks;

                spAttackModifier *= (1f + buffSPAttackParam);
            }
        }

        return spAttackModifier;
    }

    //Defense Stats
    public static float DefenseStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float defenseModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var statusEffectFromDatabase = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var stacks = thisNBMon.statusEffectList[i].stacks;
            
            //Normal Logic
            if(statusEffectFromDatabase.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var buffDefenseParam = (statusEffectFromDatabase.Defense / 100f) * (float)stacks;

                defenseModifier *= (1f + buffDefenseParam);
            }

            //Increase Defense when Frozen
            if(statusEffectFromDatabase.statusConditionName == NBMonProperties.StatusEffect.Frozen)
            {
                defenseModifier *= 1.5f;
            }
        }

        return defenseModifier;
    }

    public static float SPDefenseStatusEffectLogic(NBMonBattleDataSave thisNBMon)
    {
        float spDefenseModifier = 1f;

        for (int i = 0; i < thisNBMon.statusEffectList.Count; i++)
        {
            var statusEffectFromDatabase = UseItem.FindStatusEffectFromDatabase(thisNBMon.statusEffectList[i].statusEffect);
            var stacks = thisNBMon.statusEffectList[i].stacks;

            //Normal Logic
            if (statusEffectFromDatabase.statusEffectCategory == StatusEffectIconDatabase.StatusEffectCategory.Combat_Related)
            {
                var buffSPDefenseParam = (statusEffectFromDatabase.SP_Defense / 100f) * (float)stacks;

                spDefenseModifier *= (1f + buffSPDefenseParam);
            }

            //Increase Defense when Frozen
            if(statusEffectFromDatabase.statusConditionName == NBMonProperties.StatusEffect.Frozen)
            {
                spDefenseModifier *= 1.5f;
            }
        }

        return spDefenseModifier;
    }
}
