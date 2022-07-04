const Moralis = require('moralis/node');

const moralisAPINode = process.env.MORALIS_APINODE;
require('dotenv').config();

const serverUrl = process.env.MORALIS_SERVERURL;
const appId = process.env.MORALIS_APPID;
const masterKey = process.env.MORALIS_MASTERKEY;

/**
 * @dev Helper function to parse object data into a JSON string
 */
 const parseJSON = (data) => JSON.parse(JSON.stringify(data));


/**
 * `addLoggedInUser` adds a user email's eth address to the list of `loggedInUsers`.
 * @param {string} sessionToken is the session token of the currently logged in user.
 */
const addLoggedInUser = async (sessionToken) => {
    try {
        await Moralis.start({ serverUrl, appId, masterKey });
        // we query the specific session token on the _Session class instance to obtain the record with the user
        const SessionDB = new Moralis.Query("_Session");
        const sessionTokenQuery = SessionDB.equalTo("sessionToken", sessionToken);

        // we find the query result and parse it to return the object format of the record
        const queryResult = await sessionTokenQuery.find({useMasterKey: true});
        const queryResultParsed = parseJSON(queryResult);

        // here we check if the result returns something. if it does, we return the user's eth address
        if (queryResultParsed.length !== 0) {
            // queryResultParsed returns an array of objects.
            // since we only queried a single session token, we need to return the 0th index result.
            // "user" here returns the user object for the session token.
            // "objectId" returns the object id for the user to be queried in the `_User` database.
            const userObjectId = queryResultParsed[0]["user"]["objectId"];
            
            const UserDB = new Moralis.Query("_User");
            const userPipeline = [
                { match: { _id: userObjectId } },
			    { project: { _id: 0, email: 1, ethAddress: 1 } },
            ]

            // just like `queryResultParsed`, we queried only a single user, so the array of objects returned should
            // only return 1 object.
            const userInfoRaw = await UserDB.aggregate(userPipeline);
            const userInfo = userInfoRaw[0];

            // here, we check if this object is empty. if it isn't, we continue. else, we throw an error.
            if (Object.keys(userInfo).length !== 0) {
                // we at least need the user's eth address. 
                /**
                 * @dev It would be optimal to also have their email address to know who the eth address belongs to.
                 * however, it currently isn't an issue. 
                 */
                if (userInfo["ethAddress"] !== undefined) {
                    const LoggedInUsersDB = Moralis.Object.extend("LoggedInUsers");
                    const loggedInUsersDB = new LoggedInUsersDB();

                    loggedInUsersDB.set("ETH_Address", userInfo["ethAddress"]);

                    if (userInfo["email"] !== undefined) {
                        loggedInUsersDB.set("Email", userInfo["email"]);
                    }

                    loggedInUsersDB.save(null, {useMasterKey: true}).then(
                        () => {
                            return `User ${userInfo} logged in successfully.`
                        }, (err) => {
                            throw new Error(`Error adding user to Moralis DB. ${err.stack}`);
                        }
                    )
                } else {
                    throw new Error("User's eth address is empty. Cannot log user into the database if empty.");
                }
            } else {
                // if object is completely empty
                throw new Error("Both user email and eth address are empty for the user. Please alert them.");
            }
        } else {
            throw new Error("Session token is not found.");
        }
    } catch (err) {
        throw new Error(err.stack);
    }
    // try {
    //     //Starts moralis globally with masterKey
    //     await Moralis.start({ serverUrl, appId, masterKey });
    //     // if user is logged into the game, we will fetch their eth address from the `_User` class in Moralis DB
    //     if (isLoggedIn === true) {
    //         // gets an instance of the user database and queries through the records
    //         const UserDB = new Moralis.Query("_User");

    //         // here, we set up the pipeline to filter the specific user and return their eth address
    //         const userPipeline = [
                // { match: { email: userEmail } },
                // { project: { _id: 0, ethAddress: 1 } },
    //         ]

    //         // aggregation using the specified pipeline to retrieve the address
    //         const userAddressRaw = await UserDB.aggregate(userPipeline);

    //         // userAddress will return an array of objects which looks like this:
    //         // [ { ethAddress: '0xa123123' } ]
    //         // but since there is only 1 object due to the behavior of the pipeline, we need to return the 0th index (the only result)
    //         // and return the 'ethAddress' field of that object to get the userAddress.
    //         const userAddress = userAddressRaw[0]["ethAddress"];

    //         const LoggedInUsersDB = Moralis.Object.extend("LoggedInUsers");
    //         const loggedInUsersDB = new LoggedInUsersDB();

    //         loggedInUsersDB.set("Email", userEmail);
    //         loggedInUsersDB.set("ETH_Address", userAddress);

    //         if (adminPassword === "kontol") {
    //             // we check if the user has logged in already (meaning that the user exists in the DB)
    //             // if yes, we will return an error.
    //             const userQuery = new Moralis.Query(LoggedInUsersDB);




    //             loggedInUsersDB.save(null, {useMasterKey: true}).then(
    //                 (user) => {
    //                     console.log(`${user} has logged in. Their address ${userAddress} has been noted.`);
    //                 }, (err) => {
    //                     throw new Error(err.stack);
    //                 }
    //             );
    //         } else {
    //             throw new Error("Password is wrong. User is unauthenticated");
    //         }
    //     }
    // } catch (err) {
    //     throw new Error(err.stack);
    // }
}
addLoggedInUser("r:ac3a98aca3acefabf713c04fa57e9f48");
