using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace discordScreenshots;

/// <summary>
/// A simplified Discord webhook client for sending basic messages to Discord.
/// Based on the DiscordConnector plugin but stripped down to essentials.
/// </summary>
public class SimpleDiscordWebhook
{
    private readonly string _webhookUrl;
    private readonly string? _username;
    private readonly string? _avatarUrl;

    /// <summary>
    /// Create a new SimpleDiscordWebhook instance.
    /// </summary>
    /// <param name="webhookUrl">The Discord webhook URL</param>
    /// <param name="username">Optional username override for the webhook</param>
    /// <param name="avatarUrl">Optional avatar URL for the webhook</param>
    public SimpleDiscordWebhook(string webhookUrl, string? username = null, string? avatarUrl = null)
    {
        if (string.IsNullOrEmpty(webhookUrl))
        {
            throw new ArgumentException("Webhook URL cannot be null or empty", nameof(webhookUrl));
        }

        _webhookUrl = webhookUrl;
        _username = username;
        _avatarUrl = avatarUrl;
    }

    /// <summary>
    /// Send a simple text message to the Discord webhook.
    /// </summary>
    /// <param name="message">The message content to send</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            throw new ArgumentException("Message cannot be null or empty", nameof(message));
        }

        var payload = new SimpleWebhookPayload
        {
            content = message,
            username = _username,
            avatar_url = _avatarUrl
        };

        string jsonPayload = JsonConvert.SerializeObject(payload);
        await SendPayloadAsync(jsonPayload);
    }

    /// <summary>
    /// Send a simple text message to the Discord webhook (synchronous version).
    /// </summary>
    /// <param name="message">The message content to send</param>
    public void SendMessage(string message)
    {
        SendMessageAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Capture a screenshot and send it to the Discord webhook.
    /// Must be called from Unity's main thread for screenshot capture.
    /// </summary>
    /// <param name="message">Optional message to send with the screenshot</param>
    /// <param name="filename">Optional filename for the screenshot (defaults to timestamp)</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SendScreenshotAsync(string? message = null, string? filename = null)
    {
        try
        {
            // Generate filename if not provided
            if (string.IsNullOrEmpty(filename))
            {
                filename = $"valheim_screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png";
            }
            else if (!filename!.EndsWith(".png"))
            {
                filename += ".png";
            }

            // Step 1: Capture screenshot on main thread (fast ~5ms)
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            
            if (screenshot == null)
            {
                throw new Exception("Failed to capture screenshot - returned null texture");
            }

            // Step 2: Encode to PNG on main thread (Unity requirement - but still relatively fast)
            byte[] pngData = screenshot.EncodeToPNG();
            
            // Step 3: Clean up texture immediately (main thread requirement)
            UnityEngine.Object.DestroyImmediate(screenshot);
            
            if (pngData == null || pngData.Length == 0)
            {
                throw new Exception("Failed to encode screenshot to PNG");
            }

            UnityEngine.Debug.Log($"Screenshot captured and encoded - {pngData.Length} bytes, uploading...");

            // Step 4: Upload to Discord on background thread (network operation)
            await Task.Run(async () =>
            {
                await SendFileAsync(pngData, filename, message);
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error capturing and sending screenshot: {ex.Message}", ex);
        }
    }

    public Texture2D CaptureScreenshot()
    {
            // Step 1: Capture screenshot on main thread (fast ~5ms)
            var screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            
            if (screenshot == null)
            {
                throw new Exception("Failed to capture screenshot - returned null texture");
            }

            return screenshot;
    }

    public byte[] ProcessScreenshot(Texture2D screenshot)
    {
        // Step 2: Encode to PNG on main thread (Unity requirement - but still relatively fast)
        byte[] pngData = screenshot.EncodeToPNG();

        // Step 3: Clean up texture immediately (main thread requirement)
        UnityEngine.Object.DestroyImmediate(screenshot);

        return pngData;
    }

    /// <summary>
    /// Send a file (like a screenshot) to the Discord webhook.
    /// </summary>
    /// <param name="fileData">The file data as byte array</param>
    /// <param name="filename">The filename for the attachment</param>
    /// <param name="message">Optional message to send with the file</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SendFileAsync(byte[] fileData, string filename, string? message = null)
    {
        if (fileData == null || fileData.Length == 0)
        {
            throw new ArgumentException("File data cannot be null or empty", nameof(fileData));
        }

        if (string.IsNullOrEmpty(filename))
        {
            throw new ArgumentException("Filename cannot be null or empty", nameof(filename));
        }

        // Create multipart form data for Discord webhook with file attachment
        string boundary = "----formdata-discord-" + DateTime.Now.Ticks.ToString("x");
        
        using (var memoryStream = new MemoryStream())
        {
            // Build multipart form data
            await WriteMultipartFormDataAsync(memoryStream, boundary, fileData, filename, message);
            
            byte[] formData = memoryStream.ToArray();
            
            // Send the multipart request
            await SendMultipartPayloadAsync(formData, boundary);
        }
    }

    /// <summary>
    /// Send the JSON payload to the Discord webhook.
    /// </summary>
    /// <param name="jsonPayload">The JSON payload to send</param>
    private async Task SendPayloadAsync(string jsonPayload)
    {
        try
        {
            byte[] byteArray = Encoding.UTF8.GetBytes(jsonPayload);

            // Create the web request
            WebRequest request = WebRequest.Create(_webhookUrl);
            request.Method = "POST";
            request.ContentType = "application/json";
            request.ContentLength = byteArray.Length;

            // Write the data to the request stream
            using (Stream dataStream = request.GetRequestStream())
            {
                await dataStream.WriteAsync(byteArray, 0, byteArray.Length);
            }

            // Get the response
            using (WebResponse response = request.GetResponse())
            {
                // Discord webhooks typically return 204 No Content on success
                if (response is HttpWebResponse httpResponse)
                {
                    if (httpResponse.StatusCode != HttpStatusCode.NoContent && 
                        httpResponse.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Discord webhook returned status: {httpResponse.StatusCode}");
                    }
                }

                // Read response if there is one
                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string responseText = await reader.ReadToEndAsync();
                            // Log response if needed for debugging
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                UnityEngine.Debug.Log($"Discord response: {responseText}");
                            }
                        }
                    }
                }
            }
        }
        catch (WebException ex)
        {
            string errorMessage = "Failed to send webhook message";
            
            if (ex.Response is HttpWebResponse errorResponse)
            {
                using (Stream errorStream = errorResponse.GetResponseStream())
                {
                    if (errorStream != null)
                    {
                        using (StreamReader reader = new StreamReader(errorStream))
                        {
                            string errorDetails = await reader.ReadToEndAsync();
                            errorMessage += $": {errorResponse.StatusCode} - {errorDetails}";
                        }
                    }
                }
            }
            
            throw new Exception(errorMessage, ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending Discord webhook: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Send multipart form data to the Discord webhook (for file uploads).
    /// </summary>
    /// <param name="formData">The multipart form data</param>
    /// <param name="boundary">The multipart boundary string</param>
    private async Task SendMultipartPayloadAsync(byte[] formData, string boundary)
    {
        try
        {
            // Create the web request for multipart form data
            WebRequest request = WebRequest.Create(_webhookUrl);
            request.Method = "POST";
            request.ContentType = $"multipart/form-data; boundary={boundary}";
            request.ContentLength = formData.Length;

            // Write the form data to the request stream
            using (Stream dataStream = request.GetRequestStream())
            {
                await dataStream.WriteAsync(formData, 0, formData.Length);
            }

            // Get the response
            using (WebResponse response = request.GetResponse())
            {
                // Discord webhooks typically return 204 No Content or 200 OK on success
                if (response is HttpWebResponse httpResponse)
                {
                    if (httpResponse.StatusCode != HttpStatusCode.NoContent && 
                        httpResponse.StatusCode != HttpStatusCode.OK)
                    {
                        throw new Exception($"Discord webhook returned status: {httpResponse.StatusCode}");
                    }
                }

                // Read response if there is one
                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            string responseText = await reader.ReadToEndAsync();
                            if (!string.IsNullOrEmpty(responseText))
                            {
                                UnityEngine.Debug.Log($"Discord file upload response: {responseText}");
                            }
                        }
                    }
                }
            }
        }
        catch (WebException ex)
        {
            string errorMessage = "Failed to send webhook file";
            
            if (ex.Response is HttpWebResponse errorResponse)
            {
                using (Stream errorStream = errorResponse.GetResponseStream())
                {
                    if (errorStream != null)
                    {
                        using (StreamReader reader = new StreamReader(errorStream))
                        {
                            string errorDetails = await reader.ReadToEndAsync();
                            errorMessage += $": {errorResponse.StatusCode} - {errorDetails}";
                        }
                    }
                }
            }
            
            throw new Exception(errorMessage, ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"Error sending Discord webhook file: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Write multipart form data for Discord webhook file upload.
    /// </summary>
    /// <param name="stream">The stream to write to</param>
    /// <param name="boundary">The multipart boundary</param>
    /// <param name="fileData">The file data</param>
    /// <param name="filename">The filename</param>
    /// <param name="message">Optional message</param>
    private async Task WriteMultipartFormDataAsync(Stream stream, string boundary, byte[] fileData, string filename, string? message)
    {
        string newLine = "\r\n";
        byte[] boundaryBytes = Encoding.UTF8.GetBytes($"--{boundary}{newLine}");
        
        // Write file part
        await stream.WriteAsync(boundaryBytes, 0, boundaryBytes.Length);
        
        string fileHeader = $"Content-Disposition: form-data; name=\"files[0]\"; filename=\"{filename}\"{newLine}" +
                           $"Content-Type: image/png{newLine}{newLine}";
        byte[] fileHeaderBytes = Encoding.UTF8.GetBytes(fileHeader);
        await stream.WriteAsync(fileHeaderBytes, 0, fileHeaderBytes.Length);
        
        // Write file data
        await stream.WriteAsync(fileData, 0, fileData.Length);
        
        byte[] newLineBytes = Encoding.UTF8.GetBytes(newLine);
        await stream.WriteAsync(newLineBytes, 0, newLineBytes.Length);
        
        // Write JSON payload part (if there's a message)
        if (!string.IsNullOrEmpty(message))
        {
            await stream.WriteAsync(boundaryBytes, 0, boundaryBytes.Length);
            
            var payload = new SimpleWebhookPayload
            {
                content = message,
                username = _username,
                avatar_url = _avatarUrl
            };
            
            string jsonPayload = JsonConvert.SerializeObject(payload);
            string jsonHeader = $"Content-Disposition: form-data; name=\"payload_json\"{newLine}" +
                               $"Content-Type: application/json{newLine}{newLine}";
            
            byte[] jsonHeaderBytes = Encoding.UTF8.GetBytes(jsonHeader);
            await stream.WriteAsync(jsonHeaderBytes, 0, jsonHeaderBytes.Length);
            
            byte[] jsonBytes = Encoding.UTF8.GetBytes(jsonPayload);
            await stream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            
            await stream.WriteAsync(newLineBytes, 0, newLineBytes.Length);
        }
        
        // Write closing boundary
        byte[] closingBoundary = Encoding.UTF8.GetBytes($"--{boundary}--{newLine}");
        await stream.WriteAsync(closingBoundary, 0, closingBoundary.Length);
    }

    /// <summary>
    /// Create a webhook instance for quick one-off messages.
    /// </summary>
    /// <param name="webhookUrl">The Discord webhook URL</param>
    /// <param name="message">The message to send</param>
    /// <param name="username">Optional username override</param>
    /// <param name="avatarUrl">Optional avatar URL</param>
    public static async Task SendQuickMessageAsync(string webhookUrl, string message, 
        string? username = null, string? avatarUrl = null)
    {
        var webhook = new SimpleDiscordWebhook(webhookUrl, username, avatarUrl);
        await webhook.SendMessageAsync(message);
    }

    /// <summary>
    /// Create a webhook instance for quick one-off messages (synchronous version).
    /// </summary>
    /// <param name="webhookUrl">The Discord webhook URL</param>
    /// <param name="message">The message to send</param>
    /// <param name="username">Optional username override</param>
    /// <param name="avatarUrl">Optional avatar URL</param>
    public static void SendQuickMessage(string webhookUrl, string message, 
        string? username = null, string? avatarUrl = null)
    {
        SendQuickMessageAsync(webhookUrl, message, username, avatarUrl)
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Capture and send a screenshot with a quick one-off webhook instance.
    /// </summary>
    /// <param name="webhookUrl">The Discord webhook URL</param>
    /// <param name="message">Optional message to send with the screenshot</param>
    /// <param name="username">Optional username override</param>
    /// <param name="avatarUrl">Optional avatar URL</param>
    /// <param name="filename">Optional filename for the screenshot</param>
    public static async Task SendQuickScreenshotAsync(string webhookUrl, string? message = null, 
        string? username = null, string? avatarUrl = null, string? filename = null)
    {
        var webhook = new SimpleDiscordWebhook(webhookUrl, username, avatarUrl);
        await webhook.SendScreenshotAsync(message, filename);
    }

    /// <summary>
    /// Capture and send a screenshot with a quick one-off webhook instance (synchronous version).
    /// </summary>
    /// <param name="webhookUrl">The Discord webhook URL</param>
    /// <param name="message">Optional message to send with the screenshot</param>
    /// <param name="username">Optional username override</param>
    /// <param name="avatarUrl">Optional avatar URL</param>
    /// <param name="filename">Optional filename for the screenshot</param>
    public static void SendQuickScreenshot(string webhookUrl, string? message = null, 
        string? username = null, string? avatarUrl = null, string? filename = null)
    {
        SendQuickScreenshotAsync(webhookUrl, message, username, avatarUrl, filename)
            .ConfigureAwait(false).GetAwaiter().GetResult();
    }
}

/// <summary>
/// Simple payload structure for Discord webhook messages.
/// </summary>
internal class SimpleWebhookPayload
{
    /// <summary>
    /// The message content (up to 2000 characters).
    /// </summary>
    public string? content { get; set; }

    /// <summary>
    /// Override the default username of the webhook.
    /// </summary>
    public string? username { get; set; }

    /// <summary>
    /// Override the default avatar of the webhook.
    /// </summary>
    public string? avatar_url { get; set; }
} 