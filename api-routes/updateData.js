const express = require("express");
const { updateGenesisNBMonData } = require("../api-logic/updateData");
const router = express.Router();

router.post("/updateData", async (req, res) => {
    try {
        const { 
            nbmonId,
            nickName,
            level,
            currentExp,
            skillList,
            maxHpEffort,
            maxEnergyEffort,
            speedEffort,
            attackEffort,
            specialAttackEffort,
            defenseEffort,
            specialDefenseEffort
        } = req.body;
        let updateData = await updateGenesisNBMonData(
            nbmonId,
            nickName,
            level,
            currentExp,
            skillList,
            maxHpEffort,
            maxEnergyEffort,
            speedEffort,
            attackEffort,
            specialAttackEffort,
            defenseEffort,
            specialDefenseEffort
        );
        res.json(updateData);
    } catch (err) {
        res.status(400).json({error: err.message});
    }
});

module.exports = router;