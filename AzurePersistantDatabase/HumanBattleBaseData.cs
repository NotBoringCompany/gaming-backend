using System.Collections.Generic;

public static class HumanBattleBaseData
{

    public static NBMonBattleDataSave defaultHumanBattleData()
    {
        NBMonBattleDataSave baseHumanData = new NBMonBattleDataSave() {
        owner = string.Empty,
        nickName = string.Empty,
        monsterId = "Human",
        uniqueId = string.Empty,
        selectSkillsByHP = false,
        setSkillByHPBoundaries = new System.Collections.Generic.List<NBMonBattleDataSave.SkillByHP>(),
        Quality = (int)NBMonDataSave.MonsterQuality.Legend,
        MutationAcquired = false,
        MutationType = 0,
        NBMonLevelUp = false,
        expMemoryStorage = 0,
        currentExp = 0,
        nextLevelExpRequired = 0,
        fainted = false,
        statusEffectList = new List<StatusEffectList>(),
        skillList = new List<string>() {"Slap"},
        uniqueSkillList = new List<string>(),
        passiveList = new List<string>(),
        temporaryPassives = new List<string>(),

        //Normal Stats Related
        level = 10,
        hp = 100,
        maxHp = 100,
        energy = 999,
        maxEnergy = 999,
        speed = 20,
        battleSpeed = 20,
        attack = 20,
        specialAttack = 20,
        defense = 20,
        specialDefense = 20,
        criticalHit = 200,

        //Buff Related
        attackBuff = 0, specialAttackBuff = 0, defenseBuff = 0, specialDefenseBuff = 0, criticalBuff = 0, ignoreDefenses = 0, damageReduction = 0, energyShieldValue = 0, absorbDamageValue = 0,

        //Damage Related
        energyShield = 0, surviveLethalBlow = 0, totalIgnoreDefense = 0, mustCritical = 0, immuneCritical = 0, elementDamageReduction = 0,

        //Potential Stats
        maxHpPotential = 50,
        maxEnergyPotential = 50,
        speedPotential = 50,
        attackPotential = 50,
        defensePotential = 50,
        specialAttackPotential = 50,
        specialDefensePotential = 50,

        //Effort Stats
        maxHpEffort = 0,
        maxEnergyEffort = 0,
        speedEffort = 0,
        attackEffort = 0,
        defenseEffort = 0,
        specialAttackEffort = 0,
        specialDefenseEffort = 0
    };

        return baseHumanData;
    }
}