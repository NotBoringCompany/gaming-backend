const express = require("express");
const { updateGenesisNBMonData } = require("../api-logic/updateData");
const { updateGenesisNBMonCheck } = require("../middlewares/checkSession");
const router = express.Router();

router.post("/updateData", updateGenesisNBMonCheck, async (req, res) => {
    try {
        const { 
            nbmonId,
            playFabId,
            xSecretKey
        } = req.body;
        let updateData = await updateGenesisNBMonData(
            nbmonId,
            playFabId,
            xSecretKey
        );
        res.json(updateData);
    } catch (err) {
        res.status(400).json({error: err.message});
    }
});

module.exports = router;