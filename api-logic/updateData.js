require('dotenv').config();
const Moralis = require('moralis/node');
const axios = require('axios').default;

let titleId = process.env.TITLE_ID;
// const serverUrl = process.env.MORALIS_SERVERURL;
// const appId = process.env.MORALIS_APPID;
// const masterKey = process.env.MORALIS_MASTERKEY;

const updateGenesisNBMonData = async (nbmonIds, playfabId, xSecretKey) => {
    try {
        // await Moralis.start({ serverUrl, appId, masterKey });

        // we need to first check that `nbmonIds` is not empty.
        if (nbmonIds.length === 0) {
            throw new Error("Please specify at least 1 NBMon ID");
        }

        // querying `Genesis_NBMons_GameData` to obtain the game data class
        let GameData = Moralis.Object.extend("Genesis_NBMons_GameData");
        let gameData = new GameData();
        let gdQuery = new Moralis.Query(gameData);
        
        // querying `Genesis_NBMons` to obtain the blockchain data class
        let GenesisNBMons = Moralis.Object.extend("Genesis_NBMons");
        let nbmonQuery = new Moralis.Query(GenesisNBMons);

        // now we obtain the data from playfab so we can set the updated data to the moralis instance of the GameData class
        // in Moralis from `result` later on
        const headers = {
            'X-SecretKey': xSecretKey
        };

        const data = {
            'playFabId': playfabId
        };

        // this will return the nbmons from `StellaBlockChainPC` from Playfab
        const playfabData = await axios.post(`https://${titleId}.playfabapi.com/Server/GetUserData`, data, {
            headers: headers
        }).then((response) => {
            return response.data.data["Data"]["StellaBlockChainPC"]["Value"];
        }).catch((err) => {
            if (err.response) {
                throw new Error(`Error: ${err.response.data.errorMessage}`);
            } else if (err.request) {
                throw new Error(`Error: ${err.request.data.errorMessage}`);
            } else {
                throw new Error(`Error: ${err.message}`);
            }
        });

        const parsedPlayfabData = JSON.parse(playfabData);
        
        if (parsedPlayfabData.length === 0) {
            throw new Error("User doesn't have any NBMons in StellaBlockChainPC");
        }

        let res = [];

        for (let i = 0; i < parsedPlayfabData.length; i++) {
            let id = parsedPlayfabData[i]["uniqueId"];
            let currentExp = parsedPlayfabData[i]["currentExp"];
            let level = parsedPlayfabData[i]["level"];
            let nickName = parsedPlayfabData[i]["nickName"];
            let skillList = parsedPlayfabData[i]["skillList"];
            let maxHpEffort = parsedPlayfabData[i]["maxHpEffort"];
            let maxEnergyEffort = parsedPlayfabData[i]["maxEnergyEffort"];
            let speedEffort = parsedPlayfabData[i]["speedEffort"];
            let attackEffort = parsedPlayfabData[i]["attackEffort"];
            let specialAttackEffort = parsedPlayfabData[i]["specialAttackEffort"];
            let defenseEffort = parsedPlayfabData[i]["defenseEffort"];
            let specialDefenseEffort = parsedPlayfabData[i]["specialDefenseEffort"];

            nbmonQuery.equalTo("NBMon_ID", parseInt(id));
            gdQuery.matchesQuery("NBMon_Instance", nbmonQuery);
            
            const result = await gdQuery.first({useMasterKey: true});

            if (result === undefined) {
                throw new Error(`NBMon ${id} doesn't exist in the database. Please check the database.`);
            }

            result.set("currentExp", currentExp);
            result.set("level", level);
            result.set("nickName", nickName);
            result.set("skillList", skillList);
            result.set("maxHpEffort", maxHpEffort);
            result.set("maxEnergyEffort", maxEnergyEffort);
            result.set("speedEffort", speedEffort);
            result.set("attackEffort", attackEffort);
            result.set("specialAttackEffort", specialAttackEffort);
            result.set("defenseEffort", defenseEffort);
            result.set("specialDefenseEffort", specialDefenseEffort);

            result.save(null, {useMasterKey: true}).then((result) => {
                res.push(result);
            });
        }
        return {
            result: res
        }
    } catch (err) {
        throw err;
    }
}

// updateGenesisNBMonData([12, 123, 993], "74059183E65B33CF", "Q1XAQDB4TQ8R8QD6Z9IUMJI15W1GSSE1OQXQPO3UPRP6BWABFN");

module.exports = {
    updateGenesisNBMonData
}