import { Middleware, TurnContext, Activity, ResourceResponse, ActivityTypes } from 'botbuilder';
import { TelemetryClient } from 'applicationinsights';
import { TelemetryConstants } from './telemetryConstants';

export class TelemetryLoggerMiddleware implements Middleware {
    public static readonly AppInsightsServiceKey: string = 'TelemetryLoggerMiddleware.AppInsightsContext';
    // Application Insights Custom Event name, logged when new message is received from the user
    public static readonly BotMsgReceiveEvent: string = 'BotMessageReceived';

    // Application Insights Custom Event name, logged when a message is sent out from the bot
    public static readonly BotMsgSendEvent: string = 'BotMessageSend';

    // Application Insights Custom Event name, logged when a message is updated by the bot (rare case)
    public static readonly BotMsgUpdateEvent: string = 'BotMessageUpdate';

    // Application Insights Custom Event name, logged when a message is deleted by the bot (rare case)
    public static readonly BotMsgDeleteEvent: string = 'BotMessageDelete';

    private readonly _telemetryClient: TelemetryClient;
    private readonly _logUserName: boolean;
    private readonly _logOriginalMessage: boolean;

    /**
     *
     */
    constructor(instrumentationKey: string, logUserName: boolean = false, logOriginalMessage: boolean = false, ) {
        if (!instrumentationKey) {
            throw new Error('instrumentationKey not found');
        }

        this._telemetryClient = new TelemetryClient(instrumentationKey);
        this._logUserName = logUserName;
        this._logOriginalMessage = logOriginalMessage;
    }

    public get logOriginalMessage(): boolean { return this._logOriginalMessage; }

    public get logUserName(): boolean { return this._logUserName; }

    public async onTurn(context: TurnContext, next: () => Promise<void>): Promise<void> {
        if (context === null) {
            throw new Error('context is null');
        }

        context.turnState.set(TelemetryLoggerMiddleware.AppInsightsServiceKey, this._telemetryClient);

        // log incoming activity at beginning of turn
        if (context.activity !== null) {
            const activity = context.activity;

            // Context properties for App Insights
            if (activity.conversation.id) {
                this._telemetryClient.context.keys.sessionId = activity.conversation.id;
            }

            if (activity.from.id) {
                this._telemetryClient.context.keys.userId = activity.from.id;
            }

            // Log the Application Insights Bot Message Received
            this._telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.BotMsgReceiveEvent,
                properties: this.fillSendEventProperties(activity)
            });
        }

        // hook up onSend pipeline
        context.onSendActivities(async (ctx, activities, nextSend): Promise<ResourceResponse[]> => {
            // run full pipeline
            const responses = await nextSend();

            activities.forEach(activity => this._telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.BotMsgSendEvent,
                properties: this.fillSendEventProperties(<Activity>activity)
            }));

            return responses
        });

        // hook up update activity pipeline
        context.onUpdateActivity(async (ctx, activity, nextUpdate) => {
            // run full pipeline
            const response = await nextUpdate();

            this._telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.BotMsgSendEvent,
                properties: this.fillSendEventProperties(<Activity>activity)
            })

            return response;
        });

        // hook up delete activity pipeline
        context.onDeleteActivity(async (ctx, reference, nextDelete) => {
            // run full pipeline
            await nextDelete();

            const deletedActivity: Partial<Activity> = TurnContext.applyConversationReference({
                type: ActivityTypes.MessageDelete,
                id: reference.activityId
            }, reference, false);

            this._telemetryClient.trackEvent({
                name: TelemetryLoggerMiddleware.BotMsgSendEvent,
                properties: this.fillSendEventProperties(<Activity>deletedActivity)
            });
        });

        if (next !== null) {
            await next();
        }
    }

    private fillBaseEventProperties(activity: Activity): { [key: string]: string } {
        const properties: { [key: string]: string } = {};

        properties[TelemetryConstants.ActivityIDProperty] = activity.id || '';
        properties[TelemetryConstants.ChannelProperty] = activity.channelId;
        properties[TelemetryConstants.ConversationIdProperty] = activity.conversation.id;
        properties[TelemetryConstants.ConversationNameProperty] = activity.conversation.name;
        properties[TelemetryConstants.LocaleProperty] = activity.locale || '';

        return properties;
    }

    private fillReceiveEventProperties(activity: Activity): { [key: string]: string } {        
        const properties: { [key: string]: string } = this.fillBaseEventProperties(activity);
        properties[TelemetryConstants.FromIdProperty] = activity.from.id;

        // For some customers, logging user name within Application Insights might be an issue so have provided a config setting to disable this feature
        if (this.logUserName && activity.from.name) {
            properties[TelemetryConstants.FromNameProperty] = activity.from.name;
        }

        // For some customers, logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text) {
            properties[TelemetryConstants.TextProperty] = activity.text;
        }

        return properties;
    }

    private fillSendEventProperties(activity: Activity): { [key: string]: string } {        
        const properties: { [key: string]: string } = this.fillBaseEventProperties(activity);
        properties[TelemetryConstants.RecipientIdProperty] = activity.recipient.id;

        // For some customers, logging user name within Application Insights might be an issue so have provided a config setting to disable this feature
        if (this.logUserName && activity.recipient.name) {
            properties[TelemetryConstants.RecipientNameProperty] = activity.recipient.name;
        }

        // For some customers, logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text) {
            properties[TelemetryConstants.TextProperty] = activity.text;
        }

        return properties;
    }

    private fillUpdateEventProperties(activity: Activity): { [key: string]: string } {        
        const properties: { [key: string]: string } = this.fillBaseEventProperties(activity);
        properties[TelemetryConstants.RecipientIdProperty] = activity.recipient.id;
        
        // For some customers, logging the utterances within Application Insights might be an so have provided a config setting to disable this feature
        if (this.logOriginalMessage && activity.text) {
            properties[TelemetryConstants.TextProperty] = activity.text;
        }

        return properties;
    }

    private fillDeleteEventProperties(activity: Activity): { [key: string]: string } {        
        const properties: { [key: string]: string } = this.fillBaseEventProperties(activity);
        properties[TelemetryConstants.RecipientIdProperty] = activity.recipient.id;
        return properties;
    }
}