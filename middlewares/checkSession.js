const Moralis = require('moralis/node');
const ethers = require('ethers');
const fs = require('fs');
const path = require('path');

const nodeURL = process.env.RPC_URL;

const customHttpProvider = new ethers.providers.JsonRpcProvider(nodeURL);

const genesisNBMonABI = fs.readFileSync(
	path.resolve(__dirname, "../abi/genesisNBMon.json")
);
const genesisABI = JSON.parse(genesisNBMonABI);
const genesisContract = new ethers.Contract(
	process.env.CONTRACT_ADDRESS,
	genesisABI,
	customHttpProvider
);

/**
 * @dev Helper function to parse object data into a JSON string
 */
const parseJSON = (data) => JSON.parse(JSON.stringify(data));

/**
 * 
 * @param {string} sessionToken is the session token of the user
 * @param {number} nbmonId is the nbmon id that needs to be updated.
 * `updateGenesisNBMonCheck` checks if the user is logged in and the session token belongs to the user.
 * if it does, it checks if the nbmon ID that is being updated/to be updated belongs to this user.
 */
const updateGenesisNBMonCheck = async (req, res, next) => {
    try {
        const {sessionToken, nbmonId} = req.body;

        // we get an instance of the Session DB for querying
        const SessionDB = new Moralis.Query("_Session");
        // we get an instance of the User DB for querying
        const UserDB = new Moralis.Query("_User");

        // we get all the data of the record that contains the session token
        const sessionTokenQuery = SessionDB.equalTo("sessionToken", sessionToken);
        const queryResult = await sessionTokenQuery.first({useMasterKey: true});

        // if the query is undefined, the session token isn't found in the database.
        if (queryResult === undefined) {
            throw new Error("Session token not found. Please try again.");
        }
        const queryResultParsed = parseJSON(queryResult);
        // we obtain the pointer (obj ID) to the user from the _Session class.
        const userObjId = queryResultParsed["user"]["objectId"];

        // now we query for the user using the object ID from the _Session class.
        const userQuery = UserDB.equalTo("objectId", userObjId);
        const userQueryResult = await userQuery.first({useMasterKey: true});

        // if somehow the user can't be found, this error will be thrown.
        if (userQueryResult === undefined) {
            throw new Error("User not found. Please try again.");
        }

        const userQueryResultParsed = parseJSON(userQueryResult);

        // we now obtain the eth address of the user
        const ethAddress = userQueryResultParsed["ethAddress"];

        // if somehow the user doesn't have an eth address, this error will be thrown.
        if (ethAddress === undefined || ethAddress.length === 0) {
            throw new Error("User does not have an ETH address.");
        }

        // now we obtain the IDs owned by this user from the genesis contract.
        const ownerIds = await genesisContract.getOwnerNFTIds(ethAddress);

        // if the user doesn't own any nbmons, then this error is thrown since there is no need
        // for further checks.
        if (ownerIds.length === 0) {
            throw new Error("User does not own any NBMons");
        }
        
        // by default, the result returned from Solidity's uints are always in BigNumber.
        // this will convert them into numbers.
        let convertedArray = [];
        for (let i = 0; i < ownerIds.length; i++) {
            let converted = parseInt(Number(ownerIds[i]));
            convertedArray.push(converted);
        }

        // this is a boolean value that will return true if the user does own the specified `nbmonId`.
        let isOwner = false;

        // now we check if the nbmonId that is being updated is owned by the owner.
        for (let j = 0; j < convertedArray.length; j++) {
            if (convertedArray[j] === parseInt(nbmonId)) {
                isOwner = true;
            }
        }

        // if isOwner is still false, then we know the user doesn't own the nbmon. an error will be thrown.
        if (isOwner === false) {
            res.status(403).json({
                errorMessage: "User doesn't own specified NBMon"
            })
        } else {
            next();
        }
    } catch (err) {
        throw err;
    }   
}

module.exports = { updateGenesisNBMonCheck }