/* * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * * *
* File: Commands.cs
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

using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ServerWakeBot {
    public sealed class CommandModule : ModuleBase {
        private bool TestMac(string macAddress) {
            return Regex.IsMatch(macAddress, "^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$");
        }

        /// <summary>
        /// Tests a MAC address.
        /// </summary>
        [Command("testmac")]
        private async Task TestMacAsync(string macAddress) {
            if (TestMac(macAddress))
                await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` is valid!");
            else
                await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` is not valid.");
        }

        /// <summary>
        /// Adds a MAC address.
        /// </summary>
        [Command("addmac")]
        private async Task AddMacAsync(string name, string macAddress) {
            if (!TestMac(macAddress)) {
                await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` is not valid.");
                return;
            }

            // Get dictionary for guild.
            var dict = new Dictionary<string, string>();
            if (File.Exists(Context.Guild.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.Guild.Id.ToString() + Program.MacsFileName));

            // Add to dictionary.
            dict[name] = macAddress.ToUpperInvariant();

            // Save settings.
            File.WriteAllText(Context.Guild.Id.ToString() + Program.MacsFileName, JsonConvert.SerializeObject(dict));
            await ReplyAsync($"MAC address `{macAddress.ToUpperInvariant()}` added for `{name}`.");
        }

        /// <summary>
        /// Deletes a MAC address.
        /// </summary>
        [Command("delmac")]
        private async Task DelMacAsync(string name) {
            // Get dictionary for guild.
            var dict = new Dictionary<string, string>();
            if (File.Exists(Context.Guild.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.Guild.Id.ToString() + Program.MacsFileName));

            // Delete from dictionary.
            dict.Remove(name);

            // Save settings.
            File.WriteAllText(Context.Guild.Id.ToString() + Program.MacsFileName, JsonConvert.SerializeObject(dict));
            await ReplyAsync($"MAC address for `{name}` is deleted.");
        }

        /// <summary>
        /// Gets a MAC address.
        /// </summary>
        [Command("getmac")]
        private async Task GetMacAsync(string name) {
            // Get dictionary for guild.
            var dict = new Dictionary<string, string>();
            if (File.Exists(Context.Guild.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.Guild.Id.ToString() + Program.MacsFileName));

            // Get from dictionary.
            string macAddress;
            if (!dict.TryGetValue(name, out macAddress)) {
                await ReplyAsync($"No MAC exists for `{name}`.");
                return;
            }

            await ReplyAsync($"MAC address for `{name}` is `{macAddress.ToUpperInvariant()}`.");
        }

        /// <summary>
        /// Wakes a MAC address.
        /// </summary>
        [Command("wakemac")]
        private async Task WakeMacAsync(string name) {
            // Get dictionary for guild.
            var dict = new Dictionary<string, string>();
            if (File.Exists(Context.Guild.Id.ToString() + Program.MacsFileName))
                dict = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(Context.Guild.Id.ToString() + Program.MacsFileName));

            // Get from dictionary.
            string macAddress;
            if (!dict.TryGetValue(name, out macAddress)) {
                await ReplyAsync($"No MAC exists for `{name}`.");
                return;
            }

            await ReplyAsync($"Waking up host `{macAddress.ToUpperInvariant()}`...");

            // https://www.codeproject.com/Articles/5315/Wake-On-Lan-sample-for-C.
            // Create UdpClient.
            var wolClient = new UdpClient();

            // Create buffer.
            int counter = 0;
            var bytes = new byte[1024];

            // First 6 bytes are 0xFF.
            for (int i = 0; i < 6; i++)
                bytes[counter++] = 0xFF;

            // Repeate MAC 16 times.
            for (int i = 0; i < 16; i++) {
                int z = 0;
                for (int j = 0; j < 6; j++) {
                    bytes[counter++] = byte.Parse(macAddress.Substring(z, 2), NumberStyles.HexNumber);
                    z += 3;
                }
            }

            // Print to console.
            int zz = 0;
            for (int i = 0; i < 200; i++) {
                Console.Write(bytes[i] + " ");
                zz++;
                if (zz >= 6) {
                    Console.WriteLine();
                    zz = 0;
                }
            }

            wolClient.Send(bytes, bytes.Length, new IPEndPoint(IPAddress.Parse(Program.BroadcastMac), 0));
        }
    }
}
