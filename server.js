require('dotenv').config();
const express = require('express');
const app = express();
const cors = require('cors');
const Moralis = require('moralis/node');

const serverUrl = process.env.MORALIS_SERVERURL;
const appId = process.env.MORALIS_APPID;
const masterKey = process.env.MORALIS_MASTERKEY;

const port = process.env.PORT;

app.use(cors());
app.use(express.json());

app.listen(port, async () => {
	console.log(`listening from port ${port}`);

	//Starts moralis globally with masterKey
	await Moralis.start({ serverUrl, appId, masterKey });
});