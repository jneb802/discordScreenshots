using HarmonyLib;
using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace discordScreenshots.Patches
{
    public static class PlayerDeathScreenshotPatch
    {
        // Store captured death screenshot
        internal static Texture2D? storedDeathScreenshot = null;
        internal static string? storedPlayerName = null;
        internal static DateTime storedDeathTime;

        internal static IEnumerator CaptureDeathScreenshotCoroutine(string playerName)
        {
            // Screenshot timing delay (controls visual moment)
            // - 0.0f = Capture immediately when ragdoll is created
            // - 0.1f = Capture just after ragdoll settles (good balance)
            // - 0.5f = Capture after ragdoll physics settle (more dramatic)
            // - 1.0f = Capture well after death (aftermath scene)
            yield return new WaitForSeconds(0.1f);
            
            // Quick capture and store using existing method
            storedDeathScreenshot = SimpleDiscordWebhook.CaptureScreenshot();
            storedPlayerName = playerName;
            storedDeathTime = DateTime.Now;

            Debug.Log($"Death screenshot captured and stored for {playerName}");
        }

        internal static void UploadStoredDeathScreenshot(string trigger, bool waitForUpload)
        {
            if (storedDeathScreenshot == null)
            {
                Debug.Log($"Death screenshot upload skipped on {trigger}: no stored death screenshot found");
                return;
            }

            Texture2D screenshot = storedDeathScreenshot!;
            string playerName = string.IsNullOrEmpty(storedPlayerName) ? "Unknown Player" : storedPlayerName!;
            DateTime deathTime = storedDeathTime;

            storedDeathScreenshot = null;
            storedPlayerName = null;

            try
            {
                SimpleDiscordWebhook webhook = new SimpleDiscordWebhook(
                    BepinexConfiguration.WebhookURL.Value,
                    BepinexConfiguration.WebhookUsername.Value,
                    BepinexConfiguration.WebhookAvatarURL.Value
                );

                ScreenshotUploadData uploadData = webhook.ProcessScreenshotForUpload(screenshot);
                string deathMessage = $"**{playerName}** {BepinexConfiguration.GetRandomDeathMessage()}";
                string filename = SimpleDiscordWebhook.CreateScreenshotFilename($"{playerName}_death", deathTime);

                Task uploadTask = Task.Run(async () =>
                {
                    await webhook.SendFileAsync(uploadData.Data, filename, deathMessage, uploadData.ContentType);
                });

                if (waitForUpload)
                {
                    uploadTask.GetAwaiter().GetResult();
                    Debug.Log($"Death screenshot uploaded successfully for {playerName} on {trigger}");
                }
                else
                {
                    _ = LogUploadResultAsync(uploadTask, playerName, trigger);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing stored death screenshot on {trigger}: {ex.Message}");

                if (screenshot != null)
                {
                    UnityEngine.Object.DestroyImmediate(screenshot);
                }
            }
        }

        private static async Task LogUploadResultAsync(Task uploadTask, string playerName, string trigger)
        {
            try
            {
                await uploadTask;
                Debug.Log($"Death screenshot uploaded successfully for {playerName} on {trigger}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error uploading death screenshot on {trigger}: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), "OnRagdollCreated")]
    public static class PlayerRagdollScreenshotPatch
    {
        [HarmonyPostfix]
        static void Postfix(Humanoid __instance)
        {
            try
            {
                if (__instance is not Player player)
                    return;

                // Only take screenshot for local player
                if (player != Player.m_localPlayer)
                    return;

                string playerName = player.GetPlayerName();
                if (string.IsNullOrEmpty(playerName))
                    playerName = "Unknown Player";

                Debug.Log($"PlayerRagdollScreenshotPatch: {playerName} died - capturing screenshot");

                // Start coroutine for delayed screenshot capture
                player.StartCoroutine(PlayerDeathScreenshotPatch.CaptureDeathScreenshotCoroutine(playerName));
            }
            catch (Exception ex)
            {
                Debug.LogError($"PlayerRagdollScreenshotPatch: Error: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Player), "Awake")]
    public static class PlayerRespawnScreenshotPatch
    {
        [HarmonyPostfix]
        static void Postfix(Player __instance)
        {
            try
            {
                Debug.Log("PlayerRespawnScreenshotPatch: Awake");
                
                // // Only process for local player
                // if (__instance != Player.m_localPlayer)
                // {
                //     Debug.Log("PlayerRespawnScreenshotPatch: Not local player, skipping");
                //     return;
                // }
                
                Debug.Log("PlayerRespawnScreenshotPatch: Local player confirmed");
                    
                // Check if we have a stored death screenshot
                if (PlayerDeathScreenshotPatch.storedDeathScreenshot != null)
                {
                    Debug.Log($"PlayerRespawnScreenshotPatch: Processing stored death screenshot for {PlayerDeathScreenshotPatch.storedPlayerName}");
                    PlayerDeathScreenshotPatch.UploadStoredDeathScreenshot("respawn", waitForUpload: false);
                }
                else
                {
                    Debug.Log("PlayerRespawnScreenshotPatch: No stored death screenshot found");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"PlayerRespawnScreenshotPatch: Error: {ex.Message}");
            }
        }
    }

    [HarmonyPatch(typeof(Game), "Logout")]
    public static class GameLogoutDeathScreenshotPatch
    {
        [HarmonyPrefix]
        static void Prefix()
        {
            try
            {
                PlayerDeathScreenshotPatch.UploadStoredDeathScreenshot("logout", waitForUpload: false);
            }
            catch (Exception ex)
            {
                Debug.LogError($"GameLogoutDeathScreenshotPatch: Error: {ex.Message}");
            }
        }
    }
}
