const Moralis = require('moralis/node');

//NOTE: SECURITY IMPLEMENTATION NOT ADDED YET. NOW ANYONE CAN ADD DATA TO MORALIS.
// NEEDS TO BE FIXED LATER ON.

/**
 * `updateGenesisNBMonData` updates the NBMon's game data and updates it to Moralis DB.
 * @param {string} sessionId the Moralis Session ID of the logged in user
 * @param {number} nbmonId the nbmon ID to be updated
 */
// const updateGenesisNBMonData = async (
//     sessionId,
//     nbmonId
// ) => {
//     try {
//         // we are trying to query for the respective nbmon from Genesis_NBMons_GameData
//         // by querying the nbmon via `nbmonId` in the Genesis_NBMons class and then
//         // using the query result to obtain the nbmon in Genesis_NBMons_GameData
//         let GameData = Moralis.Object.extend("Genesis_NBMons_GameData");
//         let gameData = new GameData();
//         let gdQuery = new Moralis.Query(gameData);
        
//         let GenesisNBMons = Moralis.Object.extend("Genesis_NBMons");
//         let nbmonQuery = new Moralis.Query(GenesisNBMons);

//         nbmonQuery.equalTo("NBMon_ID", nbmonId);
//         // since Genesis_NBMons_GameData contains a pointer to the actual nbmon
//         // in Genesis_NBMons, we pass the query to obtain the respective record.
//         gdQuery.matchesQuery("NBMon_Instance", nbmonQuery);

//         // the nbmon record is obtained and we now can update its stats
//         let result = await gdQuery.first({useMasterKey: true});

//         result.set("nickName", nickName);
//         result.set("level", level);
//         result.set("currentExp", currentExp);
//         result.set("skillList", skillList);
//         result.set("maxHpEffort", maxHpEffort);
//         result.set("maxEnergyEffort", maxEnergyEffort);
//         result.set("speedEffort", speedEffort);
//         result.set("attackEffort", attackEffort);
//         result.set("specialAttackEffort", specialAttackEffort);
//         result.set("defenseEffort", defenseEffort);
//         result.set("specialDefenseEffort", specialDefenseEffort);

//         let objSaved;
//         result.save(null, {useMasterKey: true}).then((obj) => {
//             objSaved = obj;
//         });

//         return {
//             updated: "ok",
//             obj: objSaved
//         }
//     } catch (err) {
//         throw err;
//     }
// }

module.exports = {
    updateGenesisNBMonData
}