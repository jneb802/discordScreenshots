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
        internal static Texture2D storedDeathScreenshot = null;
        internal static string storedPlayerName = null;
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
            var webhook = new SimpleDiscordWebhook(BepinexConfiguration.WebhookURL.Value);
            storedDeathScreenshot = webhook.CaptureScreenshot();
            storedPlayerName = playerName;
            storedDeathTime = DateTime.Now;

            Debug.Log($"Death screenshot captured and stored for {playerName}");
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
                    ProcessStoredDeathScreenshot();
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
        
        private static void ProcessStoredDeathScreenshot()
        {
            try
            {
                // Create webhook instance with proper config
                var webhook = new SimpleDiscordWebhook(
                    BepinexConfiguration.WebhookURL.Value,
                    BepinexConfiguration.WebhookUsername.Value,
                    BepinexConfiguration.WebhookAvatarURL.Value
                );
                
                // Process screenshot using existing method
                ScreenshotUploadData uploadData = webhook.ProcessScreenshotForUpload(PlayerDeathScreenshotPatch.storedDeathScreenshot);
                
                // Prepare upload data
                string deathMessage = $"**{PlayerDeathScreenshotPatch.storedPlayerName}** {BepinexConfiguration.GetRandomDeathMessage()}";
                string filename = SimpleDiscordWebhook.CreateScreenshotFilename(
                    $"{PlayerDeathScreenshotPatch.storedPlayerName}_death",
                    PlayerDeathScreenshotPatch.storedDeathTime
                );
                
                // Upload to Discord using existing method (fire and forget)
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await webhook.SendFileAsync(uploadData.Data, filename, deathMessage, uploadData.ContentType);
                        Debug.Log($"Death screenshot uploaded successfully for {PlayerDeathScreenshotPatch.storedPlayerName}");
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error uploading death screenshot: {ex.Message}");
                    }
                });
                
                // Cleanup
                PlayerDeathScreenshotPatch.storedDeathScreenshot = null;
                PlayerDeathScreenshotPatch.storedPlayerName = null;
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error processing stored death screenshot: {ex.Message}");
                
                // Cleanup on error
                if (PlayerDeathScreenshotPatch.storedDeathScreenshot != null)
                {
                    UnityEngine.Object.DestroyImmediate(PlayerDeathScreenshotPatch.storedDeathScreenshot);
                    PlayerDeathScreenshotPatch.storedDeathScreenshot = null;
                    PlayerDeathScreenshotPatch.storedPlayerName = null;
                }
            }
        }
    }
}
