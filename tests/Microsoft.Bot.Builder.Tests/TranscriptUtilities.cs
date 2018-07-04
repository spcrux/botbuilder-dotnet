﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Bot.Schema;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Tests
{
    /// <summary>
    /// Helpers to get activities from trancript files
    /// </summary>
    public static class TranscriptUtilities
    {
        /// <summary>
        /// Loads a list of activities from a transcript file.
        /// Use the context of the test to find the transcript file
        /// </summary>
        /// <param name="context">Test context</param>
        /// <returns>A list of activities to test</returns>
        public static IEnumerable<IActivity> GetFromTestContext(TestContext context)
        {
            var relativePath = Path.Combine(context.FullyQualifiedTestClassName.Split('.').Last(), $"{context.TestName}.chat");
            return GetActivities(relativePath);
        }

        public static IEnumerable<IActivity> GetActivities(string relativePath)
        {
            var transcriptsRootFolder = TestUtilities.GetKey("TranscriptsRootFolder") ?? @"..\..\..\..\..\transcripts";
            var path = Path.Combine(transcriptsRootFolder, relativePath);
            if (!File.Exists(path))
            {
                path = Path.Combine(transcriptsRootFolder, relativePath.Replace(".chat", ".transcript", StringComparison.InvariantCultureIgnoreCase));
            }
            if (!File.Exists(path))
            {
                Assert.Fail($"Required transcript file '{path}' does not exists in '{transcriptsRootFolder}' folder. Review the 'TranscriptsRootFolder' environment variable value.");
            }

            string content;
            if (string.Equals(path.Split('.').Last(), "chat", StringComparison.InvariantCultureIgnoreCase))
            {
                content = Chatdown(path);
            }
            else
            {
                content = File.ReadAllText(path);
            }

            var activities = JsonConvert.DeserializeObject<List<Activity>>(content);

            var lastActivity = activities.Last();
            if (lastActivity.Text.Last() == '\n')
            {
                lastActivity.Text = lastActivity.Text.Remove(lastActivity.Text.Length - 1);
            }

            return activities.Take(activities.Count - 1).Append(lastActivity);
        }

        public static string Chatdown(string path)
        {
            var file = new FileInfo(path);
            var chatdown = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "chatdown_gen.cmd",
                Arguments = file.FullName,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };
            var chatdownProcess = System.Diagnostics.Process.Start(chatdown);
            var content = chatdownProcess.StandardOutput.ReadToEnd();
            chatdownProcess.WaitForExit();
            if (string.IsNullOrEmpty(content))
            {
                throw new Exception("Chatdown error. Please check if chatdown is correctly installed or install it with \"npm i -g chatdown\"");
            }
            return content;
        }
        
        /// <summary>
        /// Get a conversation reference.
        /// This method can be used to set the conversation reference needed to create a <see cref="Adapters.TestAdapter"/>
        /// </summary>
        /// <param name="activity"></param>
        /// <returns>A valid conversation reference to the activity provides</returns>
        public static ConversationReference GetConversationReference(this IActivity activity)
        {
            bool IsReply(IActivity act) => string.Equals("bot", act.From?.Role, StringComparison.InvariantCultureIgnoreCase);
            var bot = IsReply(activity) ? activity.From : activity.Recipient;
            var user = IsReply(activity) ? activity.Recipient : activity.From;
            return new ConversationReference
            {
                User = user,
                Bot = bot,
                Conversation = activity.Conversation,
                ChannelId = activity.ChannelId,
                ServiceUrl = activity.ServiceUrl
            };
        }
    }
}
