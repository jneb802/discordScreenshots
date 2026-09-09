using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
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
    private const int LargeScreenshotPixelThreshold = 4_000_000;
    private const int LargeScreenshotJpegQuality = 90;
    private static readonly HttpClient WebhookHttpClient = CreateHttpClient();

    private readonly Uri _webhookUri;
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
        if (!TryNormalizeWebhookUrl(webhookUrl, out Uri webhookUri, out string reason))
        {
            throw new ArgumentException($"Invalid webhook URL: {reason}", nameof(webhookUrl));
        }

        _webhookUri = webhookUri;
        _username = NormalizeOptionalText(username);
        _avatarUrl = NormalizeOptionalText(avatarUrl);
    }

    public static string CreateScreenshotFilename(string baseName, DateTime timestamp)
    {
        return CreateScreenshotFilename(baseName, timestamp, "png");
    }

    public static string CreateScreenshotFilename(string baseName, DateTime timestamp, string extension)
    {
        return $"{baseName}_{timestamp:yyyy-MM-dd_HH-mm-ss}.{extension}";
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
            // Step 1: Capture screenshot on main thread (fast ~5ms)
            Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
            
            if (screenshot == null)
            {
                throw new Exception("Failed to capture screenshot - returned null texture");
            }

            // Step 2: Encode on main thread. Unity texture encoding must run here.
            ScreenshotUploadData uploadData = ProcessScreenshotForUpload(screenshot);
            filename = NormalizeScreenshotFilename(filename, uploadData.Extension);

            // Step 4: Upload to Discord on background thread (network operation)
            await Task.Run(async () =>
            {
                await SendFileAsync(uploadData.Data, filename, message, uploadData.ContentType);
            });
        }
        catch (Exception ex)
        {
            throw new Exception($"Error capturing and sending screenshot: {ex.Message}", ex);
        }
    }

    public static Texture2D CaptureScreenshot()
    {
        // Step 1: Capture screenshot on main thread (fast ~5ms)
        Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();

        if (screenshot == null)
        {
            throw new Exception("Failed to capture screenshot - returned null texture");
        }

        return screenshot;
    }

    public byte[] ProcessScreenshot(Texture2D screenshot)
    {
        return ProcessScreenshotForUpload(screenshot).Data;
    }

    public ScreenshotUploadData ProcessScreenshotForUpload(Texture2D screenshot)
    {
        int width = screenshot.width;
        int height = screenshot.height;
        long pixelCount = (long)width * height;
        ScreenshotEncoding screenshotEncoding = pixelCount >= LargeScreenshotPixelThreshold
            ? ScreenshotEncoding.Jpeg
            : ScreenshotEncoding.Png;

        byte[] encodedData;
        string extension;
        string contentType;
        string formatName;

        if (screenshotEncoding == ScreenshotEncoding.Jpeg)
        {
            encodedData = screenshot.EncodeToJPG(LargeScreenshotJpegQuality);
            extension = "jpg";
            contentType = "image/jpeg";
            formatName = $"JPEG quality {LargeScreenshotJpegQuality}";
        }
        else
        {
            encodedData = screenshot.EncodeToPNG();
            extension = "png";
            contentType = "image/png";
            formatName = "PNG";
        }

        // Step 3: Clean up texture immediately (main thread requirement)
        UnityEngine.Object.DestroyImmediate(screenshot);

        if (encodedData == null || encodedData.Length == 0)
        {
            throw new Exception($"Failed to encode screenshot to {formatName}");
        }

        UnityEngine.Debug.Log(
            $"Screenshot captured at {width}x{height} and encoded as {formatName} - " +
            $"{encodedData.Length} bytes, uploading...");

        return new ScreenshotUploadData(encodedData, extension, contentType, formatName);
    }

    /// <summary>
    /// Send a file (like a screenshot) to the Discord webhook.
    /// </summary>
    /// <param name="fileData">The file data as byte array</param>
    /// <param name="filename">The filename for the attachment</param>
    /// <param name="message">Optional message to send with the file</param>
    /// <returns>Task representing the async operation</returns>
    public async Task SendFileAsync(byte[] fileData, string filename, string? message = null, string contentType = "image/png")
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
            await WriteMultipartFormDataAsync(memoryStream, boundary, fileData, filename, message, contentType);
            
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
        using CancellationTokenSource timeoutSource = CreateWebhookTimeoutSource(out int timeoutSeconds);

        try
        {
            using (StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json"))
            using (HttpResponseMessage response = await WebhookHttpClient.PostAsync(
                _webhookUri,
                content,
                timeoutSource.Token
            ))
            {
                string responseText = await response.Content.ReadAsStringAsync();
                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Discord webhook returned status: {(int)response.StatusCode} {response.StatusCode} - {responseText}");
                }

                if (!string.IsNullOrEmpty(responseText))
                {
                    UnityEngine.Debug.Log($"Discord response: {responseText}");
                }
            }
        }
        catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Discord webhook request timed out after {timeoutSeconds} seconds.",
                ex
            );
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
        using CancellationTokenSource timeoutSource = CreateWebhookTimeoutSource(out int timeoutSeconds);

        try
        {
            using (ByteArrayContent content = new ByteArrayContent(formData))
            {
                content.Headers.ContentType = MediaTypeHeaderValue.Parse($"multipart/form-data; boundary={boundary}");

                using (HttpResponseMessage response = await WebhookHttpClient.PostAsync(
                    _webhookUri,
                    content,
                    timeoutSource.Token
                ))
                {
                    string responseText = await response.Content.ReadAsStringAsync();
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new Exception($"Discord webhook returned status: {(int)response.StatusCode} {response.StatusCode} - {responseText}");
                    }

                    if (!string.IsNullOrEmpty(responseText))
                    {
                        UnityEngine.Debug.Log($"Discord file upload response: {responseText}");
                    }
                }
            }
        }
        catch (OperationCanceledException ex) when (timeoutSource.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Discord webhook file upload timed out after {timeoutSeconds} seconds for {formData.Length} bytes.",
                ex
            );
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
    private async Task WriteMultipartFormDataAsync(Stream stream, string boundary, byte[] fileData, string filename, string? message, string contentType)
    {
        string newLine = "\r\n";
        byte[] boundaryBytes = Encoding.UTF8.GetBytes($"--{boundary}{newLine}");
        
        // Write file part
        await stream.WriteAsync(boundaryBytes, 0, boundaryBytes.Length);
        
        string fileHeader = $"Content-Disposition: form-data; name=\"files[0]\"; filename=\"{filename}\"{newLine}" +
                           $"Content-Type: {contentType}{newLine}{newLine}";
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

    private static HttpClient CreateHttpClient()
    {
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
        ServicePointManager.Expect100Continue = false;

        HttpClient client = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("discord-screenshots");
        return client;
    }

    private static CancellationTokenSource CreateWebhookTimeoutSource(out int timeoutSeconds)
    {
        timeoutSeconds = BepinexConfiguration.WebhookTimeoutSeconds.Value;
        return new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
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

    private static string NormalizeScreenshotFilename(string? filename, string extension)
    {
        if (string.IsNullOrEmpty(filename))
        {
            return $"valheim_screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.{extension}";
        }

        string safeFilename = filename!;
        string currentExtension = Path.GetExtension(safeFilename);

        if (string.IsNullOrEmpty(currentExtension))
        {
            return $"{safeFilename}.{extension}";
        }

        if (currentExtension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
            currentExtension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
            currentExtension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase))
        {
            return Path.ChangeExtension(safeFilename, extension) ?? $"{safeFilename}.{extension}";
        }

        return safeFilename;
    }

    private static bool TryNormalizeWebhookUrl(string? webhookUrl, out Uri webhookUri, out string reason)
    {
        webhookUri = null!;

        if (webhookUrl == null)
        {
            reason = "URL cannot be empty";
            return false;
        }

        string trimmedUrl = webhookUrl.Trim();
        if (trimmedUrl.Length == 0)
        {
            reason = "URL cannot be empty";
            return false;
        }

        for (int index = 0; index < trimmedUrl.Length; index++)
        {
            if (char.IsControl(trimmedUrl[index]))
            {
                reason = "URL contains control characters";
                return false;
            }
        }

        Uri? parsedUri;
        if (!Uri.TryCreate(trimmedUrl, UriKind.Absolute, out parsedUri) || parsedUri == null)
        {
            reason = "URL must be an absolute HTTPS URL";
            return false;
        }

        if (!string.Equals(parsedUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            reason = "URL must use HTTPS";
            return false;
        }

        if (string.IsNullOrEmpty(parsedUri.Host))
        {
            reason = "URL must include a host";
            return false;
        }

        webhookUri = parsedUri;
        reason = string.Empty;
        return true;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        if (value == null)
        {
            return null;
        }

        string trimmedValue = value.Trim();
        return trimmedValue.Length == 0 ? null : trimmedValue;
    }

}

public sealed class ScreenshotUploadData
{
    public ScreenshotUploadData(byte[] data, string extension, string contentType, string formatName)
    {
        Data = data;
        Extension = extension;
        ContentType = contentType;
        FormatName = formatName;
    }

    public byte[] Data { get; }
    public string Extension { get; }
    public string ContentType { get; }
    public string FormatName { get; }
}

internal enum ScreenshotEncoding
{
    Png,
    Jpeg
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
