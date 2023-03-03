﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure;
using Azure.AI.OpenAI;

namespace Microsoft.BotBuilderSamples
{
    /// <summary>
    /// https://github.com/Azure/azure-sdk-for-net/tree/main/sdk/openai/Azure.AI.OpenAI
    /// </summary>
    public class AzureOpenAIClient : ICompletion
    {
        private readonly global::Azure.AI.OpenAI.OpenAIClient openAIClient;
        private readonly string deploymentId;

        public AzureOpenAIClient(string apiKey, string endpoint, string deploymentId)
        {
            openAIClient = new(new Uri(endpoint), new AzureKeyCredential(apiKey));
            this.deploymentId = deploymentId;
        }

        public async Task<string> GenerateCompletionAsync(string prompt)
        {
            var completionsOptions = new CompletionsOptions();
            completionsOptions.Prompt.Add(prompt);
            completionsOptions.MaxTokens = 2048;

            var completionsResponse = await openAIClient.GetCompletionsAsync(deploymentId, completionsOptions);
            var completion = completionsResponse?.Value?.Choices[0]?.Text ?? "no result";
            return completion;
        }
    }
}
