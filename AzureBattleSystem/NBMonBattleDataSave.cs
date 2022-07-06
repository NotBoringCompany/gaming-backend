using System.Collections.Generic;

public class NBMonBattleDataSave
{
    public string owner { get; set; }
    public string nickName { get; set; }
    public string monsterId { get; set; }
    public string uniqueId { get; set; }
    public int gender { get; set; }
    public int level { get; set; }
    public int fertility { get; set; }
    public bool selectSkillsByHP { get; set; }
    public List<SkillByHP> setSkillByHPBoundaries { get; set; }
    public int Quality { get; set; }
    public bool MutationAcquired { get; set; }
    public int MutationType { get; set; }
    public bool NBMonLevelUp { get; set; }
    public int expMemoryStorage { get; set; }
    public int currentExp { get; set; }
    public int nextLevelExpRequired { get; set; }
    public bool fainted { get; set; }
    public List<StatusEffectList> statusEffectList { get; set; }
    public List<string> skillList { get; set; }
    public List<string> uniqueSkillList { get; set; }
    public List<string> passiveList { get; set; }
    public List<string> temporaryPassives { get; set; }
    public int hp { get; set; }
    public int energy { get; set; }
    public int maxHp { get; set; }
    public int maxEnergy { get; set; }
    public int speed { get; set; }
    public int battleSpeed { get; set; }
    public int attack { get; set; }
    public int specialAttack { get; set; }
    public int defense { get; set; }
    public int specialDefense { get; set; }
    public int criticalHit { get; set; }
    public float attackBuff, specialAttackBuff, defenseBuff, specialDefenseBuff, criticalBuff, ignoreDefenses, damageReduction, energyShieldValue;
    public int energyShield { get; set; }
    public int surviveLethalBlow { get; set; }
    public int totalIgnoreDefense { get; set; }
    public int mustCritical { get; set; }
    public int immuneCritical { get; set; }
    public int elementDamageReduction { get; set; }
    public int maxHpPotential { get; set; }
    public int maxEnergyPotential { get; set; }
    public int speedPotential { get; set; }
    public int attackPotential { get; set; }
    public int specialAttackPotential { get; set; }
    public int defensePotential { get; set; }
    public int specialDefensePotential { get; set; }
    public int maxHpEffort { get; set; }
    public int maxEnergyEffort { get; set; }
    public int speedEffort { get; set; }
    public int attackEffort { get; set; }
    public int specialAttackEffort { get; set; }
    public int defenseEffort { get; set; }
    public int specialDefenseEffort { get; set; }
}

public class StatusEffectList
{
    public int statusEffect { get; set; }
    public int counter { get; set; }
    public int stacks { get; set; }
}

public class SkillByHP
{
    public int HPHasToBeLowerThan;
    public int HPHasToBeBiggerThan;
    public string SkillName;
}

public class NBMonDatabase_Azure
{
    public static NBMonBattleDataSave FindNBMonDataUsingUniqueID(string UniqueID, List<NBMonBattleDataSave> Database)
    {
        foreach(var Monster in Database)
        {
            if(Monster.uniqueId == UniqueID)
            {
                return Monster;
            }
        }

        return null;
    }
}