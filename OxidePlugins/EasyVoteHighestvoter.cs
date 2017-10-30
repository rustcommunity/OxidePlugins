﻿using Facepunch.Extend;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("EasyVote-HighestVoter", "Exel80", "1.0.0")]
    class EasyVoteHighestvoter : RustPlugin
    {
        // EasyVote is life and <3
        [PluginReference] private Plugin EasyVote;

        // Just make sure im no using other name...
        private const string StoredDataName = "HighestVoter";

        #region Hooks
        void onUserReceiveHighestVoterReward(Dictionary<string, string> RewardData)
        {
            // Convert to bool
            bool ReceivedReward = RewardData["ReceivedReward"] == "true";

            // Logging data to oxide/logs/EasyVoteHighestvoter
            if (config.logEnabled)
            {
                switch (RewardData["RewardType"])
                {
                    case "item":
                        {
                            LogToFile("Highestvoter",
                                $"[{DateTime.UtcNow.ToString()}] [HighestPlayer: {RewardData["HighestPlayerName"]} Id: {RewardData["HighestPlayerID"]}] " +
                                $"Voter received his reward item(s) => {RewardData["Reward"]}", this);
                        }
                        break;
                    default:
                    case "group":
                        {
                            // New
                            LogToFile("Highestvoter",
                                $"[{DateTime.UtcNow.ToString()}] [HighestPlayer: {RewardData["HighestPlayerName"]} Id: {RewardData["HighestPlayerID"]}] " +
                                $"Voter has been added to his reward group => {RewardData["Reward"]}", this);
                            // Old
                            if (!string.IsNullOrEmpty(RewardData["OldHighestPlayerID"]))
                            {
                                LogToFile("Highestvoter",
                                $"[{DateTime.UtcNow.ToString()}] [OldHighestPlayerID: {RewardData["OldHighestPlayerID"]}] " +
                                $"Removed from his reward group => {RewardData["Reward"]}", this);
                            }
                        }
                        break;
                }
            }

            // Mark that player received his reward
            _storedData.hasReceivedReward = ReceivedReward;
            Interface.GetMod().DataFileSystem.WriteObject(StoredDataName, _storedData);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            // If Highest voter havent received reward.
            if (_storedData.hasReceivedReward != true)
            {
                // If match happen
                if (player.UserIDString == _storedData.highestVoterID)
                {
                    GaveRewards(player.UserIDString);
                }
            }
        }
        #endregion

        #region Initializing
        void Init()
        {
            // Load data
            _storedData = Interface.GetMod().DataFileSystem.ReadObject<StoredData>(StoredDataName);

            // Start timer
            timer.Repeat(config.checkTime, 0, nextMonth);
        }

        // Now?!
        private void nextMonth()
        {
            // When new month arravie, then this protect spamming.
            bool triggered = false;

            // If month doesnt match
            if (_storedData.Month != DateTime.UtcNow.Month)
            {
                string HighestPlayer = EasyVote?.Call("getHighestvoter").ToString();

                if (string.IsNullOrEmpty(HighestPlayer))
                {
                    PrintWarning("HighestPlayer is NULL !!! No one have voted your server past month, updated month number.");

                    _storedData.Month = DateTime.UtcNow.Month;
                    Interface.GetMod().DataFileSystem.WriteObject(StoredDataName, _storedData);

                    return;
                }

                // TRIGGERED!
                triggered = true;

                // Gave reward + Hook
                GaveRewards(HighestPlayer);

                // Reset
                EasyVote?.Call("resetData");
            }

            // Triggered?
            if (!triggered)
                Announce();
        }
        #endregion

        #region Localization
        string _lang(string key, string id = null, params object[] args) => string.Format(lang.GetMessage(key, this, id), args);

        void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                ["HighestGroup"] = "<color=cyan>The player with the highest number of votes per month gets a free</color> <color=yellow>{0}</color> " +
                "<color=cyan>rank for 1 month.</color> <color=yellow>/vote</color> <color=cyan>Vote now to get free rank!</color>",
                ["HighestGroupCongrats"] = "<color=yellow>{0}</color> <color=cyan>was highest voter past month.</color> <color=cyan>He earned free</color> " +
                "<color=yellow>{1}</color> <color=cyan>rank for 1 month. Vote now to earn it next month!</color>",
                ["HighestItems"] = "<color=cyan>The player with the highest number of votes per month gets </color> <color=yellow>{0}</color> " +
                "<color=cyan>to his inventory.</color> <color=yellow>/vote</color> <color=cyan>Vote now to get free stuff!</color>",
                ["HighestItemsCongrats"] = "<color=yellow>{0}</color> <color=cyan>was highest voter past month.</color> <color=cyan>He earned</color> " +
                "<color=yellow>{1}</color> <color=cyan>items. Vote now to earn it next month!</color>"
            }, this);
        }
        #endregion

        #region Storing
        class StoredData
        {
            public int Month = DateTime.UtcNow.Month;
            public string highestVoterID = string.Empty;
            public bool hasReceivedReward = false;

            public StoredData() { }
        }
        StoredData _storedData;
        #endregion

        #region Configuration
        private Configuration config;

        public class Configuration
        {
            [JsonProperty(PropertyName = "Enable logging, save to oxide/logs/EasyVoteHighestvoter (true / false)")]
            public bool logEnabled;

            [JsonProperty(PropertyName = "Interval timer (seconds)")]
            public int checkTime;

            [JsonProperty(PropertyName = "Highest voter reward (item or group)")]
            public string rewardIs;

            [JsonProperty(PropertyName = "Highest voter reward group (group name)")]
            public string group;

            [JsonProperty(PropertyName = "Highest voter reward item(s) (Item.Shortname:Amount => http://docs.oxidemod.org/rust/#item-list)")]
            public string item;

            public static Configuration DefaultConfig()
            {
                return new Configuration
                {
                    logEnabled = true,
                    checkTime = 1800,
                    rewardIs = "group",
                    group = "hero",
                    item = "wood:1000,supply.signal:1"
                };
            }
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                config = Config.ReadObject<Configuration>();
            }
            catch
            {
                PrintWarning($"Could not read oxide/config/{Name}.json, creating new config file");
                LoadDefaultConfig();
            }
            SaveConfig();
        }

        protected override void LoadDefaultConfig() => config = Configuration.DefaultConfig();

        protected override void SaveConfig() => Config.WriteObject(config);
        #endregion

        #region Helper 
        private void GaveRewards(string HighestPlayer)
        {
            // For callhooks
            Dictionary<string, string> RewardData = new Dictionary<string, string>();
            RewardData.Add("HighestPlayerName", string.Empty);
            RewardData.Add("HighestPlayerID", HighestPlayer);

            // Check last month highest.
            string OldHighestPlayer = _storedData.highestVoterID;

            // Change this month highestID
            _storedData.highestVoterID = HighestPlayer;
            _storedData.Month = DateTime.UtcNow.Month;
            Interface.GetMod().DataFileSystem.WriteObject(StoredDataName, _storedData);

            // For callhooks
            RewardData.Add("OldHighestPlayerID", OldHighestPlayer);
            RewardData.Add("RewardType", (config.rewardIs.ToLower() != "item" ? "group" : "item"));
            RewardData.Add("Reward", string.Empty);
            RewardData.Add("ReceivedReward", "false");

            // Try found player
            BasePlayer player = FindPlayer(HighestPlayer).FirstOrDefault();

            // If make sure that player isnt null <3
            if (player != null)
            {
                // Added for callhooks
                RewardData["HighestPlayerName"] = player.displayName;

                // Item reward
                if (config.rewardIs.ToLower() == "item")
                {
                    // Also make sure player is connected.
                    if (player.IsConnected)
                    {
                        // Check if multiple items
                        if (config.item.Contains(','))
                        {
                            StringBuilder tempItems = new StringBuilder();
                            string[] RawItems = config.item.Replace(" ", "").Split(',');
                            for (int i = 0; i < RawItems.Count(); i++)
                            {
                                string[] Items = RawItems[i].Split(':');

                                string weapon = Items[0];
                                int amount = Convert.ToInt32(Items[1]);

                                try
                                {
                                    Item itemToReceive = ItemManager.CreateByName(weapon, amount);

                                    tempItems.Append($"{amount}x {itemToReceive.info.displayName.translated}, ");

                                    RewardData["ReceivedReward"] = "true";

                                    //If the item does not end up in the inventory
                                    //Drop it on the ground for them
                                    if (!player.inventory.GiveItem(itemToReceive, player.inventory.containerMain))
                                        itemToReceive.Drop(player.GetDropPosition(), player.GetDropVelocity());
                                }
                                catch (Exception e) { PrintWarning($"{e}"); }
                            }
                            RewardData["Reward"] = tempItems.ToString().Substring(0, tempItems.Length - 2);
                        }
                        else
                        {
                            string[] Item = config.item.Split(':');

                            string weapon = Item[0];
                            int amount = Convert.ToInt32(Item[1]);

                            try
                            {
                                Item itemToReceive = ItemManager.CreateByName(weapon, amount);

                                RewardData["Reward"] = $"{amount}x {itemToReceive.info.displayName.translated}";

                                RewardData["ReceivedReward"] = "true";

                                //If the item does not end up in the inventory
                                //Drop it on the ground for them
                                if (!player.inventory.GiveItem(itemToReceive, player.inventory.containerMain))
                                    itemToReceive.Drop(player.GetDropPosition(), player.GetDropVelocity());
                            }
                            catch (Exception e) { PrintWarning($"{e}"); }
                        }
                    }
                }
                // Group reward
                else if (config.rewardIs.ToLower() == "group")
                {
                    // Execute console command
                    rust.RunServerCommand($"oxide.usergroup add {HighestPlayer} {config.group}");

                    RewardData["Reward"] = config.group;

                    RewardData["ReceivedReward"] = "true";

                    if (!string.IsNullOrEmpty(OldHighestPlayer))
                        rust.RunServerCommand($"oxide.usergroup remove {OldHighestPlayer} {config.group}");
                }
                else
                    PrintWarning($"{config.rewardIs.ToLower()} cant be detected. Please, use \"group\" or \"item\" only!");

                // Congrats msg <3
                Congrats(player.displayName, player.UserIDString);
            }
            // Group reward
            else if (config.rewardIs.ToLower() == "group")
            {
                // Execute console command
                rust.RunServerCommand($"oxide.usergroup add {HighestPlayer} {config.group}");

                RewardData["Reward"] = config.group;

                RewardData["ReceivedReward"] = "true";

                // Congrats msg <3
                Congrats(HighestPlayer);

                if (!string.IsNullOrEmpty(OldHighestPlayer))
                    rust.RunServerCommand($"oxide.usergroup remove {OldHighestPlayer} {config.group}");
            }
            else
                PrintWarning($"{config.rewardIs.ToLower()} cant be detected. Please, use \"group\" or \"item\" only!");

            // Hook => void onUserReceiveHighestVoterReward(Dictionary<string, string> RewardData)
            Interface.CallHook("onUserReceiveHighestVoterReward", RewardData);
        }

        private void Announce()
        {
            if (config.rewardIs.ToLower() == "item")
                PrintToChat(_lang("HighestItems", null, $"\"{config.item.Replace(",", ", ")}\""));
            else
                PrintToChat(_lang("HighestGroup", null, config.group));
        }

        private void Congrats(string name, string id = null)
        {
            if (config.rewardIs.ToLower() == "item")
                PrintToChat(_lang("HighestItemsCongrats", id, name, $"\"{config.item.Trim().Replace(",", ", ")}\""));
            else
                PrintToChat(_lang("HighestGroupCongrats", id, name, config.group));
        }

        private static HashSet<BasePlayer> FindPlayer(string nameOrIdOrIp)
        {
            var players = new HashSet<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(sleepingPlayer);
                else if (!string.IsNullOrEmpty(sleepingPlayer.displayName) && sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(sleepingPlayer);
            }
            return players;
        }
        #endregion
    }
}
