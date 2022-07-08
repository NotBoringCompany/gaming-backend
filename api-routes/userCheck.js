const express = require("express");
const router = express.Router();

const {addLoggedInUser, removeLoggedInUser, removeSessionToken, retrieveUserBySessionToken} = require("../api-logic/userCheckLogic");

router.post("/addLoggedInUser", async (req, res) => {
    const { sessionToken } = req.body;

    try {
        let result = await addLoggedInUser(sessionToken);
        res.json(result);
    } catch (err) {
        res.status(400).json({error: err.message});
    }
});

router.post("/removeLoggedInUser", async (req, res) => {
    const { ethAddress } = req.body;

    try {
        let result = await removeLoggedInUser(ethAddress);
        res.json(result);
    } catch (err) {
        res.status(400).json({error: err.message});
    }
});

router.post("/removeSessionToken", async (req, res) => {
    const { sessionToken } = req.body;

    try {
        let result = await removeSessionToken(sessionToken);
        res.json(result);
    } catch (err) {
        res.status(400).json({error: err.message});
    }
});

router.post("/retrieveUserBySessionToken", async (req, res) => {
    const { sessionToken } = req.body;

    try {
        let result = await retrieveUserBySessionToken(sessionToken);
        res.json(result);
    } catch (err) {
        res.status(400).json({error: err.message});
    }
});

module.exports = router;