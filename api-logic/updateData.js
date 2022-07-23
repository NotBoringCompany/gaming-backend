const Moralis = require('moralis/node');

//NOTE: SECURITY IMPLEMENTATION NOT ADDED YET. NOW ANYONE CAN ADD DATA TO MORALIS.
// NEEDS TO BE FIXED LATER ON.

/**
 * `updateGenesisNBMonData` updates the NBMon's game data and updates it to Moralis DB.
 */
const updateGenesisNBMonData = async (
    nbmonId,
    nickname,
    monsterId,
    uniqueId,
    level,
    selectSkillsByHP,
    setSkillByHPBoundaries,
    Quality,
    NBMonLevelUp,
    expMemoryStorage,
    currentExp,
    nextLevelExpRequired,
    fainted,
    statusEffectList,
    skillList,
    uniqueSkillList,
    temporaryPassives,
    hp,
    energy,
    maxHp,
    maxEnergy,
    speed,
    battleSpeed,
    attack,
    specialAttack,
    defense,
    specialDefense,
    criticalHit,
    attackBuff,
    specialAttackBuff,
    defenseBuff,
    specialDefenseBuff,
    criticalBuff,
    ignoreDefenses,
    damageReduction,
    energyShieldValue,
    energyShield,
    surviveLethalBlow,
    totalIgnoreDefense,
    mustCritical,
    immuneCritical,
    elementDamageReduction,
    maxHpEffort,
    maxEnergyEffort,
    speedEffort,
    attackEffort,
    specialAttackEffort,
    defenseEffort,
    specialDefenseEffort
) => {
    try {
        await Moralis.start({ serverUrl, appId, masterKey });

        // we are trying to query for the respective nbmon from Genesis_NBMons_GameData
        // by querying the nbmon via `nbmonId` in the Genesis_NBMons class and then
        // using the query result to obtain the nbmon in Genesis_NBMons_GameData
        let GameData = Moralis.Object.extend("Genesis_NBMons_GameData");
        let gameData = new GameData();
        let gdQuery = new Moralis.Query(gameData);
        
        let GenesisNBMons = Moralis.Object.extend("Genesis_NBMons");
        let nbmonQuery = new Moralis.Query(GenesisNBMons);

        nbmonQuery.equalTo("NBMon_ID", nbmonId);
        // since Genesis_NBMons_GameData contains a pointer to the actual nbmon
        // in Genesis_NBMons, we pass the query to obtain the respective record.
        gdQuery.matchesQuery("NBMon_Instance", nbmonQuery);

        // the nbmon record is obtained and we now can update its stats
        let result = await gdQuery.first({useMasterKey: true});

        result.set("nickname", nickname);
        result.set("monsterId", monsterId);
        result.set("uniqueId", uniqueId);
        result.set("level", level);
        result.set("selectSkillsByHP", selectSkillsByHP);
        result.set("setSkillByHPBoundaries", setSkillByHPBoundaries);
        result.set("Quality", Quality);
        result.set("NBMonLevelUp", NBMonLevelUp);
        result.set("expMemoryStorage", expMemoryStorage);
        result.set("currentExp", currentExp);
        result.set("nextLevelExpRequired", nextLevelExpRequired);
        result.set("fainted", fainted);
        result.set("statusEffectList", statusEffectList);
        result.set("skillList", skillList);
        result.set("uniqueSkillList", uniqueSkillList);
        result.set("temporaryPassives", temporaryPassives);
        result.set("hp", hp);
        result.set("energy", energy);
        result.set("maxHp", maxHp);
        result.set("maxEnergy", maxEnergy);
        result.set("speed", speed);
        result.set("battleSpeed", battleSpeed);
        result.set("attack", attack);
        result.set("specialAttack", specialAttack);
        result.set("defense", defense);
        result.set("specialDefense", specialDefense);
        result.set("criticalHit", criticalHit);
        result.set("attackBuff", attackBuff);
        result.set("specialAttackBuff", specialAttackBuff);
        result.set("defenseBuff", defenseBuff);
        result.set("specialDefenseBuff", specialDefenseBuff);
        result.set("criticalBuff", criticalBuff);
        result.set("ignoreDefenses", ignoreDefenses);
        result.set("damageReduction", damageReduction);
        result.set("energyShieldValue", energyShieldValue);
        result.set("energyShield", energyShield);
        result.set("surviveLethalBlow", surviveLethalBlow);
        result.set("totalIgnoreDefense", totalIgnoreDefense);
        result.set("mustCritical", mustCritical);
        result.set("immuneCritical", immuneCritical);
        result.set("elementDamageReduction", elementDamageReduction);
        result.set("maxHpEffort", maxHpEffort);
        result.set("maxEnergyEffort", maxEnergyEffort);
        result.set("speedEffort", speedEffort);
        result.set("attackEffort", attackEffort);
        result.set("specialAttackEffort", specialAttackEffort);
        result.set("defenseEffort", defenseEffort);
        result.set("specialDefenseEffort", specialDefenseEffort);

        result.save(null, {useMasterKey: true}).then((obj) => {
            return {
                updated: "ok",
                object: obj
            }
        });
    } catch (err) {
        throw err;
    }
}

module.exports = {
    updateGenesisNBMonData
}