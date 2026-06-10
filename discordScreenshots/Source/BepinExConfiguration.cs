using System.Linq;
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

    public static ConfigEntry<string> HotkeyScreenshotWebhookURL;
    public static ConfigEntry<string> HotkeyScreenshotWebhookUsername;
    public static ConfigEntry<string> HotkeyScreenshotWebhookAvatarURL;
    public static ConfigEntry<string> HotkeyScreenshotMessage;

    private static readonly System.Random _random = new System.Random();

    public static string GetRandomDeathMessage()
    {
        string raw = DeathMessage.Value;
        if (string.IsNullOrEmpty(raw)) return "met their demise!";

        string[] trimmed = raw.Split(';')
            .Select(m => m.Trim())
            .Where(m => !string.IsNullOrEmpty(m))
            .ToArray();

        if (trimmed.Length == 0) return "met their demise!";
        return trimmed[_random.Next(trimmed.Length)];
    }

    public static void GenerateConfigs(BepInEx.Configuration.ConfigFile configFile)
    {
        Config = configFile;

        ScreenshotHotkey = Config.BindConfig("Screenshot", "Hotkey", KeyCode.F12, "The hotkey to take a screenshot and send to Discord.", synced: false, order: 1);
        WebhookURL = Config.BindConfig("Webhook", "URL", "", "The URL of the Discord webhook to send messages to.", synced: true, order: 2);
        WebhookUsername = Config.BindConfig("Webhook", "Username", "Valheim Death Bot", "The username of the Discord webhook to send messages to.", synced: true, order: 3);
        WebhookAvatarURL = Config.BindConfig("Webhook", "AvatarURL", "", "The avatar URL of the Discord webhook to send messages to.", synced: true, order: 4);
        DeathMessage = Config.BindConfig("Death Screenshot", "Message", "met their demise! Final moments captured...", "The message to send with death screenshots (player name will be prepended automatically).", synced: true, order: 5);

        HotkeyScreenshotWebhookURL = Config.BindConfig("Player Capture Webhook", "URL", "", "Optional separate webhook URL for player capture (F12) screenshots. If empty, uses the main Webhook URL.", synced: true, order: 6);
        HotkeyScreenshotWebhookUsername = Config.BindConfig("Player Capture Webhook", "Username", "Valheim Screenshot Bot", "The username for the player capture webhook.", synced: true, order: 7);
        HotkeyScreenshotWebhookAvatarURL = Config.BindConfig("Player Capture Webhook", "AvatarURL", "", "The avatar URL for the player capture webhook.", synced: true, order: 8);
        HotkeyScreenshotMessage = Config.BindConfig("Player Capture Webhook", "Message", "captured this screenshot!", "The message to send with player capture screenshots (player name will be prepended automatically).", synced: true, order: 9);
    }
}
