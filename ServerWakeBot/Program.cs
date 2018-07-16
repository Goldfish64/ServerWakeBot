/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
* File: Program.cs
* 
* Copyright (c) 2018 John Davis
*
* Permission is hereby granted, free of charge, to any person obtaining a
* copy of this software and associated documentation files (the "Software"),
* to deal in the Software without restriction, including without limitation
* the rights to use, copy, modify, merge, publish, distribute, sublicense,
* and/or sell copies of the Software, and to permit persons to whom the
* Software is furnished to do so, subject to the following conditions:
*
* The above copyright notice and this permission notice shall be included
* in all copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
* OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL
* THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
* FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS
* IN THE SOFTWARE.
* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * */

using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ServerWakeBot {
    public class Program {
        #region Constants

        public const string TokenFileName = "token.txt";
        public const string MacsFileName = "-macs.json";
        public const string CommandPrefix = "%";

        public const string BroadcastMac = "192.168.1.255"; // Replace with broadcast address for local subnet.

        #endregion

        public static void Main(string[] args) => RunBot().GetAwaiter().GetResult();

        private static async Task RunBot() {
            // Ensure token exists.
            if (!File.Exists(TokenFileName)) {
                Console.WriteLine("Please place bot token in a file called \"token.txt\". Press any key to exit...");
                Console.ReadKey();
                return;
            }

            // Read token from file.
            var token = File.ReadAllText(TokenFileName);

            // Create Discord client.
            var client = new DiscordSocketClient();
            client.Log += (e) => {
                Console.WriteLine(e.ToString());
                return Task.CompletedTask;
            };

            // Create command service and map.
            var commands = new CommandService();
            var commandMap = new ServiceCollection();

            // Load commands from assembly.
            await commands.AddModulesAsync(Assembly.GetEntryAssembly());

            // Listen for messages.
            client.MessageReceived += async (message) => {
                // Get the message and check to see if it is a user message.
                var msg = message as IUserMessage;
                if (msg == null)
                    return;

                // Keeps track of where the command begins.
                var pos = 0;

                // Attempt to parse a command.
                if (msg.HasStringPrefixLower(CommandPrefix, ref pos)) {
                    var result = await commands.ExecuteAsync(new CommandContext(client, msg), msg.Content.Substring(pos));
                    if (!result.IsSuccess) {
                        // Is the command just unknown? If so, return.
                        if (result.Error == CommandError.UnknownCommand)
                            return;

                        await msg.Channel.SendMessageAsync($"Error: {result.ErrorReason}\n\nIf there are spaces in a parameter, make sure to surround it with quotes.");
                    }
                    return;
                }
            };

            // Login to Discord.
            await client.LoginAsync(TokenType.Bot, token);
            await client.StartAsync();

            await Task.Delay(-1);
        }
    }
}
