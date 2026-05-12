using HarmonyLib;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace discordScreenshots.Patches
{
    [HarmonyPatch(typeof(Terminal), "InitTerminal")]
    public static class DiscordConsoleCommandPatch
    {
        [HarmonyPostfix]
        static void Postfix()
        {
            // Add discord command to send messages to webhook
            new Terminal.ConsoleCommand("discord", "sends a message to Discord webhook. Usage: discord <message>", (Terminal.ConsoleEvent)(args =>
            {
                if (args.Length < 2)
                {
                    args.Context?.AddString("Usage: discord <message>");
                    args.Context?.AddString("Example: discord Hello from Valheim!");
                    return;
                }

                // Combine all arguments after "discord" into the message
                string message = string.Join(" ", args.Args, 1, args.Args.Length - 1);

                if (string.IsNullOrWhiteSpace(message))
                {
                    args.Context?.AddString("Message cannot be empty");
                    return;
                }

                // Get player name if available
                string playerName = "Server";
                if (Player.m_localPlayer != null && !string.IsNullOrEmpty(Player.m_localPlayer.GetPlayerName()))
                {
                    playerName = Player.m_localPlayer.GetPlayerName();
                }

                args.Context?.AddString($"Sending message to Discord...");

                // Send message asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Format message with player name
                        string formattedMessage = $"💬 **{playerName}**: {message}";

                        await SimpleDiscordWebhook.SendQuickMessageAsync(
                            BepinexConfiguration.WebhookURL.Value,
                            formattedMessage,
                            "Valheim Console"
                        );

                        // Note: Can't update console from async context easily, 
                        // so we'll just log to Unity console
                        UnityEngine.Debug.Log($"Discord message sent successfully: {formattedMessage}");
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Failed to send Discord message: {ex.Message}");
                    }
                });
            }));

            // Add discordtest command to test webhook connection
            new Terminal.ConsoleCommand("discordtest", "tests the Discord webhook connection", (Terminal.ConsoleEvent)(args =>
            {
                args.Context?.AddString("Testing Discord webhook connection...");

                // Get player name if available
                string playerName = "Server";
                if (Player.m_localPlayer != null && !string.IsNullOrEmpty(Player.m_localPlayer.GetPlayerName()))
                {
                    playerName = Player.m_localPlayer.GetPlayerName();
                }

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await SimpleDiscordWebhook.SendQuickMessageAsync(
                            BepinexConfiguration.WebhookURL.Value,
                            $"🧪 Discord webhook test from **{playerName}** - Connection working!",
                            "Valheim Test Bot"
                        );

                        UnityEngine.Debug.Log("Discord webhook test message sent successfully!");
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.LogError($"Discord webhook test failed: {ex.Message}");
                    }
                });
            }));

            // Add discordscreenshot command to capture and send actual screenshots
            new Terminal.ConsoleCommand("discordscreenshot", "captures and sends a screenshot to Discord", (Terminal.ConsoleEvent)(args =>
            {
                // Get player name if available
                string playerName = "Unknown Player";
                if (Player.m_localPlayer != null && !string.IsNullOrEmpty(Player.m_localPlayer.GetPlayerName()))
                {
                    playerName = Player.m_localPlayer.GetPlayerName();
                }

                // Parse optional message from args
                string? message = null;
                if (args.Length > 1)
                {
                    message = string.Join(" ", args.Args, 1, args.Args.Length - 1);
                }

                // Default message if none provided
                if (string.IsNullOrEmpty(message))
                {
                    message = $"📸 **{playerName}** took a screenshot!";
                }

                args.Context?.AddString($"Capturing screenshot...");

                try
                {
                    // Create webhook instance
                    var webhook = new SimpleDiscordWebhook(
                        BepinexConfiguration.WebhookURL.Value,
                        "Valheim Screenshots"
                    );

                    string filename = SimpleDiscordWebhook.CreateScreenshotFilename(
                        $"{playerName}_screenshot",
                        DateTime.Now
                    );

                    // Capture screenshot synchronously on main thread
                    var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                    if (screenshot == null)
                    {
                        throw new Exception("Failed to capture screenshot");
                    }

                    ScreenshotUploadData uploadData = webhook.ProcessScreenshotForUpload(screenshot);

                    args.Context?.AddString($"Screenshot captured, uploading to Discord...");

                    // Upload in background task (network operations are safe to run async)
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await webhook.SendFileAsync(uploadData.Data, filename, message, uploadData.ContentType);
                            UnityEngine.Debug.Log($"Screenshot uploaded to Discord for {playerName}");
                        }
                        catch (Exception ex)
                        {
                            UnityEngine.Debug.LogError($"Failed to upload screenshot: {ex.Message}");
                        }
                    });
                }
                catch (Exception ex)
                {
                    args.Context?.AddString($"Error: {ex.Message}");
                    UnityEngine.Debug.LogError($"Failed to capture screenshot: {ex.Message}");
                }
            }));
        }
    }
}
