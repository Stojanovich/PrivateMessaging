using Rocket.API;
using Rocket.Core.Plugins;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace PrivateMessagePlugin
{
    public class PrivateMessagePlugin : RocketPlugin<PrivateMessageConfiguration>
    {
        public static PrivateMessagePlugin Instance { get; private set; }

        protected override void Load()
        {
            Instance = this;
            Say(null, "Private Message Plugin loaded successfully!");
        }

        protected override void Unload()
        {
            Say(null, "Private Message Plugin unloaded!");
        }

        public void Say(UnturnedPlayer player, string message)
        {
            if (string.IsNullOrEmpty(Configuration.Instance.MessageIcon))
            {
                if (player == null)
                    UnturnedChat.Say(message, UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor, Color.yellow));
                else
                    UnturnedChat.Say(player, message, UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor, Color.yellow));
            }
            else
            {
                ChatManager.serverSendMessage(message,
                    UnturnedChat.GetColorFromName(Configuration.Instance.MessageColor, Color.yellow),
                    null,
                    player?.Player.channel.owner,
                    EChatMode.SAY,
                    Configuration.Instance.MessageIcon,
                    true);
            }
        }
    }

    public class PrivateMessageConfiguration : IRocketPluginConfiguration
    {
        public string MessageColor { get; set; }
        public string MessageIcon { get; set; }

        public void LoadDefaults()
        {
            MessageColor = "yellow";
            MessageIcon = "https://i.imgur.com/JZjQEHV.png";
        }
    }

    public class PrivateMessageCommand : IRocketCommand
    {
        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "pm";
        public string Help => "Send a private message to another player";
        public string Syntax => "/pm <player> <message>";
        public List<string> Aliases => new List<string>();
        public List<string> Permissions => new List<string> { "pm" };

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer sender = caller as UnturnedPlayer;
            if (sender == null) return;

            if (command.Length < 2)
            {
                PrivateMessagePlugin.Instance.Say(sender, "Usage: /pm <player> <message>");
                return;
            }

            string targetName = command[0];
            string message = string.Join(" ", command, 1, command.Length - 1);

            UnturnedPlayer target = UnturnedPlayer.FromName(targetName);
            if (target == null)
            {
                PrivateMessagePlugin.Instance.Say(sender, $"Player '{targetName}' not found!");
                return;
            }

            if (target.CSteamID == sender.CSteamID)
            {
                PrivateMessagePlugin.Instance.Say(sender, "You cannot send a private message to yourself!");
                return;
            }

            // Send message to recipient
            PrivateMessagePlugin.Instance.Say(target, $"[PM from {sender.DisplayName}] {message}");
            // Confirm to sender
            PrivateMessagePlugin.Instance.Say(sender, $"[PM to {target.DisplayName}] {message}");
        }
    }

    public class PMReplyCommand : IRocketCommand
    {
        private static Dictionary<ulong, ulong> lastMessageFrom = new Dictionary<ulong, ulong>();

        public AllowedCaller AllowedCaller => AllowedCaller.Player;
        public string Name => "r";
        public string Help => "Reply to the last private message";
        public string Syntax => "/r <message>";
        public List<string> Aliases => new List<string> { "reply" };
        public List<string> Permissions => new List<string> { "pm" };

        public static void SetLastMessageFrom(UnturnedPlayer recipient, UnturnedPlayer sender)
        {
            lastMessageFrom[recipient.CSteamID.m_SteamID] = sender.CSteamID.m_SteamID;
        }

        public void Execute(IRocketPlayer caller, string[] command)
        {
            UnturnedPlayer sender = caller as UnturnedPlayer;
            if (sender == null) return;

            if (command.Length < 1)
            {
                PrivateMessagePlugin.Instance.Say(sender, "Usage: /r <message>");
                return;
            }

            if (!lastMessageFrom.ContainsKey(sender.CSteamID.m_SteamID))
            {
                PrivateMessagePlugin.Instance.Say(sender, "You have no one to reply to!");
                return;
            }

            ulong lastSenderID = lastMessageFrom[sender.CSteamID.m_SteamID];
            UnturnedPlayer target = UnturnedPlayer.FromCSteamID(new Steamworks.CSteamID(lastSenderID));

            if (target == null)
            {
                PrivateMessagePlugin.Instance.Say(sender, "The player you're trying to reply to is no longer online!");
                lastMessageFrom.Remove(sender.CSteamID.m_SteamID);
                return;
            }

            string message = string.Join(" ", command);

            // Send message to recipient
            PrivateMessagePlugin.Instance.Say(target, $"[PM from {sender.DisplayName}] {message}");
            // Confirm to sender
            PrivateMessagePlugin.Instance.Say(sender, $"[PM to {target.DisplayName}] {message}");

            // Update last message tracking
            SetLastMessageFrom(target, sender);
        }
    }
}