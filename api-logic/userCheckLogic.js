const Moralis = require('moralis/node');

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

                    // here, we are querying the `LoggedInUsers` class to check if the eth address already exists
                    // (meaning that the user with this eth address has already logged in)
                    const LoggedInUsersQuery = new Moralis.Query(loggedInUsersDB);
                    const ethAddressQuery = LoggedInUsersQuery.equalTo("ETH_Address", userInfo["ethAddress"]);
                    const ethAddressQueryResult = await ethAddressQuery.find({useMasterKey: true});
                    
                    // parsing the query result into object format. should return {} if there are no results.
                    const ethAddressQueryResultParsed = parseJSON(ethAddressQueryResult);

                    // if the user hasn't logged in yet, then this check should be true
                    if (ethAddressQueryResultParsed.length === 0) {
                        // we then set the eth address with the user's eth address
                        loggedInUsersDB.set("ETH_Address", userInfo["ethAddress"]);

                        if (userInfo["email"] !== undefined) {
                            // if email exists, we also set the email here just for extra check purposes.
                            loggedInUsersDB.set("Email", userInfo["email"]);
                        }

                        // we save the record we just set (i.e. added to the database)
                        loggedInUsersDB.save(null, {useMasterKey: true}).then(
                            () => {
                                return `User ${userInfo} logged in successfully.`
                            }, (err) => {
                                throw new Error(`Error adding user to Moralis DB. ${err.stack}`);
                            }
                        )
                    // if the user has already logged in, we can't add their eth address again.
                    } else {
                        throw new Error(`User's ETH Address ${userInfo["email"]} is already logged in. Cannot add another instance.`);
                    }
                // if eth address is empty, we cannot log the user's eth address in. they need to add this address.
                } else {
                    throw new Error("User's eth address is empty. Cannot log user into the database if empty.");
                }
            // if the user has no email or eth address, we cannot log them in. they need to add at least the eth address.
            } else {
                throw new Error("Both user email and eth address are empty for the user. Please alert them.");
            }
        // if session token is invalid or not found, we throw an error.
        } else {
            throw new Error("Session token is not found.");
        }
    // catches any random or unexpected errors.
    } catch (err) {
        throw new Error(err.stack);
    }
}

module.exports = {
    addLoggedInUser
}
