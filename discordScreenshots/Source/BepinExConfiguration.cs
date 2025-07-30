using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using Jotunn.Configs;
using Jotunn;
using Jotunn.Extensions;

namespace discordScreenshots;

public class BepinexConfiguration
{
    public static BepInEx.Configuration.ConfigFile Config;

    public static ConfigEntry<string> WebhookURL;
    public static ConfigEntry<string> WebhookUsername;
    public static ConfigEntry<string> WebhookAvatarURL;
    public static ConfigEntry<string> DeathMessage;
    public static ConfigEntry<KeyCode> ScreenshotHotkey;

    public static void GenerateConfigs(BepInEx.Configuration.ConfigFile configFile)
    {
        Config = configFile;

        ScreenshotHotkey = Config.BindConfig("Screenshot", "Hotkey", KeyCode.F12, "The hotkey to take a screenshot and send to Discord.", synced: false, order: 1);
        WebhookURL = Config.BindConfig("Webhook", "URL", "https://discord.com/api/webhooks/1399552344601002064/WJVtecbcpnWgP2folNu8GtweD9Fn72YEAAusghKOgizuj602yK_BrHjkWuy0-D3segeJ", "The URL of the Discord webhook to send messages to.", synced: true, order: 2);
        WebhookUsername = Config.BindConfig("Webhook", "Username", "Valheim Death Bot", "The username of the Discord webhook to send messages to.", synced: true, order: 3);
        WebhookAvatarURL = Config.BindConfig("Webhook", "AvatarURL", "", "The avatar URL of the Discord webhook to send messages to.", synced: true, order: 4);
        DeathMessage = Config.BindConfig("Death Screenshot", "Message", "met their demise! Final moments captured...", "The message to send with death screenshots (player name will be prepended automatically).", synced: true, order: 5);
        
    }
}