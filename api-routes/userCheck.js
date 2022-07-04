const express = require("express");
const router = express.Router();

const {addLoggedInUser} = require("../api-logic/userCheckLogic");

router.post("/addLoggedInUser", async (req, res) => {
    const { sessionToken } = req.body;

    try {
        let result = await addLoggedInUser(sessionToken);
        res.json(result);
    } catch (err) {
        res.status(400).json({error: err.message});
    }
});

module.exports = router;