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
    private static string _localWebhookURL = "";
    private static string _localWebhookUsername = "";
    private static string _localWebhookAvatarURL = "";
    private static string _localHotkeyScreenshotWebhookURL = "";
    private static string _localHotkeyScreenshotWebhookUsername = "";
    private static string _localHotkeyScreenshotWebhookAvatarURL = "";

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

    public static string GetWebhookURL()
    {
        return GetSyncedValueOrLocalFallback(WebhookURL, _localWebhookURL);
    }

    public static string GetWebhookUsername()
    {
        if (ShouldUseLocalFallback(WebhookURL, _localWebhookURL))
        {
            return _localWebhookUsername;
        }

        return GetSyncedValueOrLocalFallback(WebhookUsername, _localWebhookUsername);
    }

    public static string GetWebhookAvatarURL()
    {
        if (ShouldUseLocalFallback(WebhookURL, _localWebhookURL))
        {
            return _localWebhookAvatarURL;
        }

        return GetSyncedValueOrLocalFallback(WebhookAvatarURL, _localWebhookAvatarURL);
    }

    public static string GetHotkeyScreenshotWebhookURL()
    {
        return GetSyncedValueOrLocalFallback(HotkeyScreenshotWebhookURL, _localHotkeyScreenshotWebhookURL);
    }

    public static string GetHotkeyScreenshotWebhookUsername()
    {
        if (ShouldUseLocalFallback(HotkeyScreenshotWebhookURL, _localHotkeyScreenshotWebhookURL))
        {
            return _localHotkeyScreenshotWebhookUsername;
        }

        return GetSyncedValueOrLocalFallback(HotkeyScreenshotWebhookUsername, _localHotkeyScreenshotWebhookUsername);
    }

    public static string GetHotkeyScreenshotWebhookAvatarURL()
    {
        if (ShouldUseLocalFallback(HotkeyScreenshotWebhookURL, _localHotkeyScreenshotWebhookURL))
        {
            return _localHotkeyScreenshotWebhookAvatarURL;
        }

        return GetSyncedValueOrLocalFallback(HotkeyScreenshotWebhookAvatarURL, _localHotkeyScreenshotWebhookAvatarURL);
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

        _localWebhookURL = Normalize(WebhookURL.Value);
        _localWebhookUsername = Normalize(WebhookUsername.Value);
        _localWebhookAvatarURL = Normalize(WebhookAvatarURL.Value);
        _localHotkeyScreenshotWebhookURL = Normalize(HotkeyScreenshotWebhookURL.Value);
        _localHotkeyScreenshotWebhookUsername = Normalize(HotkeyScreenshotWebhookUsername.Value);
        _localHotkeyScreenshotWebhookAvatarURL = Normalize(HotkeyScreenshotWebhookAvatarURL.Value);
    }

    private static string GetSyncedValueOrLocalFallback(ConfigEntry<string> entry, string localFallback)
    {
        string syncedValue = Normalize(entry.Value);
        return syncedValue.Length > 0 ? syncedValue : localFallback;
    }

    private static bool ShouldUseLocalFallback(ConfigEntry<string> entry, string localFallback)
    {
        return Normalize(entry.Value).Length == 0 && localFallback.Length > 0;
    }

    private static string Normalize(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }
}
