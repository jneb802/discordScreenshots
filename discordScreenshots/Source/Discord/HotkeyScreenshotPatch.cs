using HarmonyLib;
using System;
using System.Threading.Tasks;
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

                    // Use player capture webhook if set, otherwise fall back to main webhook
                    bool usePlayerCaptureWebhook = !string.IsNullOrEmpty(BepinexConfiguration.HotkeyScreenshotWebhookURL.Value);

                    string webhookUrl = usePlayerCaptureWebhook
                        ? BepinexConfiguration.HotkeyScreenshotWebhookURL.Value
                        : BepinexConfiguration.WebhookURL.Value;
                    string webhookUsername = usePlayerCaptureWebhook
                        ? BepinexConfiguration.HotkeyScreenshotWebhookUsername.Value
                        : BepinexConfiguration.WebhookUsername.Value;
                    string webhookAvatarUrl = usePlayerCaptureWebhook
                        ? BepinexConfiguration.HotkeyScreenshotWebhookAvatarURL.Value
                        : BepinexConfiguration.WebhookAvatarURL.Value;
                    string messageText = usePlayerCaptureWebhook
                        ? BepinexConfiguration.HotkeyScreenshotMessage.Value
                        : "captured this screenshot!";

                    string screenshotMessage = $"📸 **{playerName}** {messageText}";
                    string filename = SimpleDiscordWebhook.CreateScreenshotFilename(
                        $"{playerName}_screenshot",
                        DateTime.Now
                    );

                    _ = SendHotkeyScreenshotAsync(
                        webhookUrl,
                        screenshotMessage,
                        webhookUsername,
                        webhookAvatarUrl,
                        filename
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotkeyScreenshotPatch: Error: {ex.Message}");
            }
        }

        private static async Task SendHotkeyScreenshotAsync(
            string webhookUrl,
            string screenshotMessage,
            string webhookUsername,
            string webhookAvatarUrl,
            string filename)
        {
            try
            {
                await SimpleDiscordWebhook.SendQuickScreenshotAsync(
                    webhookUrl,
                    screenshotMessage,
                    webhookUsername,
                    webhookAvatarUrl,
                    filename
                );
            }
            catch (Exception ex)
            {
                Debug.LogError($"HotkeyScreenshotPatch: Failed to upload screenshot: {ex.Message}");
            }
        }
    }
}
