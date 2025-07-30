using HarmonyLib;
using System;
using UnityEngine;

namespace discordScreenshots.Patches
{
    [HarmonyPatch(typeof(Player), "Update")]
    public static class HotkeyScreenshotPatch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                // Only check hotkey for local player
                if (__instance != Player.m_localPlayer)
                    return;

                // Check if screenshot hotkey is pressed
                if (Input.GetKeyDown(BepinexConfiguration.ScreenshotHotkey.Value))
                {
                    string playerName = __instance.GetPlayerName();
                    if (string.IsNullOrEmpty(playerName))
                        playerName = "Unknown Player";

                    Debug.Log($"HotkeyScreenshotPatch: {playerName} pressed screenshot hotkey");

                    // Take screenshot using existing infrastructure
                    string screenshotMessage = $"📸 **{playerName}** captured this screenshot!";
                    string filename = $"{playerName}_screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";

                    // Fire and forget
                    _ = SimpleDiscordWebhook.SendQuickScreenshotAsync(
                        BepinexConfiguration.WebhookURL.Value,
                        screenshotMessage,
                        BepinexConfiguration.WebhookUsername.Value,
                        BepinexConfiguration.WebhookAvatarURL.Value,
                        filename
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotkeyScreenshotPatch: Error: {ex.Message}");
            }
        }
    }
} 