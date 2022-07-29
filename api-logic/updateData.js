require('dotenv').config();
const Moralis = require('moralis/node');
const axios = require('axios').default;

let titleId = process.env.TITLE_ID;
const serverUrl = process.env.MORALIS_SERVERURL;
const appId = process.env.MORALIS_APPID;
const masterKey = process.env.MORALIS_MASTERKEY;
/**
 * `updateGenesisNBMonData` updates the NBMon's game data and updates it to Moralis DB.
 * @param {string} sessionId the Moralis Session ID of the logged in user
 * @param {number} nbmonId the nbmon ID to be updated
 */
const updateGenesisNBMonData = async (
    sessionId,
    nbmonId,
    playfabId,
    xSecretKey
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

        // the nbmon record is obtained in `result`
        let result = await gdQuery.first({useMasterKey: true});

        // now we obtain the data from playfab so we can set the updated data to the moralis instance of the GameData class
        // in Moralis from `result`
        const headers = {
            'X-SecretKey': xSecretKey
        };

        const data = {
            'playFabId': playfabId
        };

        axios.post(`https://${titleId}.playfabapi.com/Server/GetUserData`, data, {
            headers: headers
        }).then((response) => {
            let nbmonData = response.data.data["Data"];
            console.log(nbmonData);

            // let parsedData = JSON.parse(nbmonData);
            // console.log(parsedData);
        }).catch((err) => {
            if (err.response) {
                console.log(err.response.data);
            } else if (err.request) {
                console.log(err.request.data);
            } else {
                console.log("err", err.message);
            }
        });

        // result.set("nickName", nickName);
        // result.set("level", level);
        // result.set("currentExp", currentExp);
        // result.set("skillList", skillList);
        // result.set("maxHpEffort", maxHpEffort);
        // result.set("maxEnergyEffort", maxEnergyEffort);
        // result.set("speedEffort", speedEffort);
        // result.set("attackEffort", attackEffort);
        // result.set("specialAttackEffort", specialAttackEffort);
        // result.set("defenseEffort", defenseEffort);
        // result.set("specialDefenseEffort", specialDefenseEffort);

        // let objSaved;
        // result.save(null, {useMasterKey: true}).then((obj) => {
        //     objSaved = obj;
        // });

        // return {
        //     updated: "ok",
        //     obj: objSaved
        // }
    } catch (err) {
        throw err;
    }
}

updateGenesisNBMonData("asdasd", 1, "6A5EF3AB2ADF3DE", "Q1XAQDB4TQ8R8QD6Z9IUMJI15W1GSSE1OQXQPO3UPRP6BWABFN");

module.exports = {
    updateGenesisNBMonData
}