// using System;
// using System.Threading.Tasks;

// namespace discordScreenshots;

// /// <summary>
// /// Example usage of the SimpleDiscordWebhook class.
// /// This file demonstrates how to send messages to Discord using your actual webhook.
// /// </summary>
// public static class WebhookExample
// {
//     // Your actual Discord webhook URL for "Spidey Bot"
//     public static string WEBHOOK_URL = BepinexConfiguration.WebhookURL.Value;

//     /// <summary>
//     /// Example of sending a simple message using instance methods.
//     /// </summary>
//     public static async Task SendSimpleMessageExample()
//     {
//         try
//         {
//             // Create a webhook instance
//             var webhook = new SimpleDiscordWebhook(
//                 webhookUrl: BepinexConfiguration.WebhookURL.Value,
//                 username: BepinexConfiguration.WebhookUsername.Value,
//                 avatarUrl: BepinexConfiguration.WebhookAvatarURL.Value
//             );

//             // Send a message
//             await webhook.SendMessageAsync("Hello from Valheim! A player just joined the server.");

//             UnityEngine.Debug.Log("Message sent successfully!");
//         }
//         catch (Exception ex)
//         {
//             UnityEngine.Debug.LogError($"Error sending message: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Example of sending a quick one-off message using static methods.
//     /// </summary>
//     public static async Task SendQuickMessageExample()
//     {
//         try
//         {
//             // Send a quick message without creating an instance
//             await SimpleDiscordWebhook.SendQuickMessageAsync(
//                 webhookUrl: WEBHOOK_URL,
//                 message: "🚀 Valheim server is starting up...",
//                 username: "Valheim Bot"
//             );

//             UnityEngine.Debug.Log("Quick message sent successfully!");
//         }
//         catch (Exception ex)
//         {
//             UnityEngine.Debug.LogError($"Error sending quick message: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Example of capturing and sending an actual screenshot.
//     /// </summary>
//     public static async Task SendActualScreenshotExample()
//     {
//         try
//         {
//             var webhook = new SimpleDiscordWebhook(WEBHOOK_URL, "Valheim Screenshots");

//             // Capture and send actual screenshot with message
//             await webhook.SendScreenshotAsync("📸 Check out this awesome view in Valheim!");

//             UnityEngine.Debug.Log("Screenshot captured and sent successfully!");
//         }
//         catch (Exception ex)
//         {
//             UnityEngine.Debug.LogError($"Error capturing and sending screenshot: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Example of sending a quick screenshot using static method.
//     /// </summary>
//     public static async Task SendQuickScreenshotExample()
//     {
//         try
//         {
//             // Quick screenshot with static method
//             await SimpleDiscordWebhook.SendQuickScreenshotAsync(
//                 WEBHOOK_URL,
//                 "🌟 Amazing sunset in Valheim!",
//                 "Valheim Bot",
//                 null,
//                 "valheim_sunset.png"
//             );

//             UnityEngine.Debug.Log("Quick screenshot sent successfully!");
//         }
//         catch (Exception ex)
//         {
//             UnityEngine.Debug.LogError($"Error sending quick screenshot: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Example of using synchronous methods (blocking).
//     /// </summary>
//     public static void SendSynchronousMessageExample()
//     {
//         try
//         {
//             // Using synchronous method (will block until complete)
//             SimpleDiscordWebhook.SendQuickMessage(
//                 webhookUrl: WEBHOOK_URL,
//                 message: "⚡ This is a synchronous message from Valheim!",
//                 username: "Sync Bot"
//             );

//             UnityEngine.Debug.Log("Synchronous message sent successfully!");
//         }
//         catch (Exception ex)
//         {
//             UnityEngine.Debug.LogError($"Error sending synchronous message: {ex.Message}");
//         }
//     }

//     /// <summary>
//     /// Ready-to-use integration for your Valheim mod.
//     /// </summary>
//     public static class ValheimDiscordIntegration
//     {
//         private static SimpleDiscordWebhook? _webhook;

//         /// <summary>
//         /// Initialize the Discord webhook connection.
//         /// Call this once when your mod starts up.
//         /// </summary>
//         public static void Initialize()
//         {
//             _webhook = new SimpleDiscordWebhook(
//                 webhookUrl: WEBHOOK_URL,
//                 username: "Valheim Server",
//                 avatarUrl: null // You can add a Valheim icon URL here if you want
//             );
//         }

//         /// <summary>
//         /// Capture and send an actual screenshot to Discord.
//         /// </summary>
//         public static async Task CaptureAndSendScreenshot(string playerName = "Unknown", string? customMessage = null)
//         {
//             if (_webhook == null) 
//             {
//                 UnityEngine.Debug.LogWarning("Discord webhook not initialized!");
//                 return;
//             }

//             try
//             {
//                 string message = customMessage ?? $"📸 **{playerName}** captured this screenshot!";
//                 string filename = $"{playerName}_screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
                
//                 await _webhook.SendScreenshotAsync(message, filename);
//                 UnityEngine.Debug.Log($"Screenshot captured and sent to Discord for {playerName}");
//             }
//             catch (Exception ex)
//             {
//                 UnityEngine.Debug.LogError($"Failed to capture and send screenshot: {ex.Message}");
//             }
//         }

//         /// <summary>
//         /// Send a screenshot notification message (text only, no image).
//         /// </summary>
//         public static async Task NotifyScreenshotTaken(string playerName = "Unknown")
//         {
//             if (_webhook == null) 
//             {
//                 UnityEngine.Debug.LogWarning("Discord webhook not initialized!");
//                 return;
//             }

//             try
//             {
//                 await _webhook.SendMessageAsync($"📸 **{playerName}** took a screenshot!");
//             }
//             catch (Exception ex)
//             {
//                 UnityEngine.Debug.LogError($"Failed to notify Discord of screenshot: {ex.Message}");
//             }
//         }

//         /// <summary>
//         /// Send a player join notification.
//         /// </summary>
//         public static async Task NotifyPlayerJoined(string playerName)
//         {
//             if (_webhook == null) return;

//             try
//             {
//                 await _webhook.SendMessageAsync($"🟢 **{playerName}** joined the server!");
//             }
//             catch (Exception ex)
//             {
//                 UnityEngine.Debug.LogError($"Failed to notify Discord of player join: {ex.Message}");
//             }
//         }

//         /// <summary>
//         /// Send a player leave notification.
//         /// </summary>
//         public static async Task NotifyPlayerLeft(string playerName)
//         {
//             if (_webhook == null) return;

//             try
//             {
//                 await _webhook.SendMessageAsync($"🔴 **{playerName}** left the server!");
//             }
//             catch (Exception ex)
//             {
//                 UnityEngine.Debug.LogError($"Failed to notify Discord of player leave: {ex.Message}");
//             }
//         }

//         /// <summary>
//         /// Send a general server event notification.
//         /// </summary>
//         public static async Task NotifyServerEvent(string eventMessage)
//         {
//             if (_webhook == null) return;

//             try
//             {
//                 await _webhook.SendMessageAsync($"⚡ {eventMessage}");
//             }
//             catch (Exception ex)
//             {
//                 UnityEngine.Debug.LogError($"Failed to notify Discord of server event: {ex.Message}");
//             }
//         }

//         /// <summary>
//         /// Test the webhook connection by sending a test message.
//         /// </summary>
//         public static async Task TestConnection()
//         {
//             if (_webhook == null)
//             {
//                 UnityEngine.Debug.LogWarning("Discord webhook not initialized!");
//                 return;
//             }

//             try
//             {
//                 await _webhook.SendMessageAsync("🧪 Test message from Valheim mod - Discord connection working!");
//                 UnityEngine.Debug.Log("Discord test message sent successfully!");
//             }
//             catch (Exception ex)
//             {
//                 UnityEngine.Debug.LogError($"Discord test failed: {ex.Message}");
//             }
//         }
//     }
// } 