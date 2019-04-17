// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// index.js is used to setup and configure your bot

// Import required packages
const path = require('path');
const restify = require('restify');

// Import required bot services. See https://aka.ms/bot-services to learn more about the different parts of a bot.
const { BotFrameworkAdapter } = require('botbuilder');
const { LuisService } = require('botframework-config');
const { LuisRecognizer, QnAMaker } = require('botbuilder-ai');
const { DispatchBot } = require('./bots/dispatchBot');

// Note: Ensure you have a .env file and include all necessary credentials to access services like LUIS and QnAMaker.
const ENV_FILE = path.join(__dirname, '.env');
require('dotenv').config({ path: ENV_FILE });

// Create adapter.
// See https://aka.ms/about-bot-adapter to learn more about .bot file its use and bot configuration .
const adapter = new BotFrameworkAdapter({
    appId: endpointConfig.appId || process.env.microsoftAppID,
    appPassword: endpointConfig.appPassword || process.env.microsoftAppPassword
});

const luisService = new LuisService({
    appId: process.env.nlpWithDispatchDispatchAppId,
    authoringKey: process.env.nlpWithDispatchDispatchAuthoringkey,
    region: process.env.nlpWithDispatchDispatchRegion
});

const dispatchRecognizer = new LuisRecognizer({
    applicationId: luisService.appId,
    endpoint: luisService.getEndpoint(),
    endpointKey: luisService.authoringKey,
}, {
    includeAllIntents: true,
    includeInstanceData: true
}, true);

const qnaMaker = new QnAMaker({
    knowledgeBaseId: process.sampleQnaKbId,
    endpointKey: process.sampleQnaEndpointKey,
    host: process.env.sampleQnaHostname
});

// Catch-all for errors.
adapter.onTurnError = async (context, error) => {
    // This check writes out errors to console log
    // NOTE: In production environment, you should consider logging this to Azure
    //       application insights.
    console.error(`\n [onTurnError]: ${ error }`);
    // Send a message to the user
    await context.sendActivity(`Oops. Something went wrong!`);
    // Clear out state
    await conversationState.delete(context);
};

// Pass in a logger to the bot. For this sample, the logger is the console, but alternatives such as Application Insights and Event Hub exist for storing the logs of the bot.
const logger = console;

// Create the main dialog.
let bot = new DispatchBot(dispatchRecognizer, qnaMaker, logger);

// Create HTTP server
let server = restify.createServer();
server.listen(process.env.port || process.env.PORT || 3978, function() {
    console.log(`\n${ server.name } listening to ${ server.url }`);
    console.log(`\nGet Bot Framework Emulator: https://aka.ms/botframework-emulator`);
});

// Listen for incoming activities and route them to your bot main dialog.
server.post('/api/messages', (req, res) => {
    // Route received a request to adapter for processing
    adapter.processActivity(req, res, async (turnContext) => {
        // route to bot activity handler.
        await bot.run(turnContext);
    });
});
