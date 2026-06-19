![DiscordScreenshots](https://i.imgur.com/HiPcG3M.gif)

# Discord Screenshots
A mod to send screenshots to a Discord server. 

## Features
- Hotkey screenshot capture (F12 by default)
- Automatic death screenshots sent to Discord when you die

## Death screenshots
Death screenshots are captured when the player dies and sent to Discord after they respawn.

When Smoothbrain's Resurrection mod is installed, Discord Screenshots temporarily moves Resurrection's active death popup offscreen for the single frame used to capture the death screenshot, then immediately restores it. This keeps the screenshot clean without changing Resurrection's respawn or resurrection behavior.

## Configuration
All settings are configurable through the BepInEx config file:
- Discord webhook URL
- Screenshot hotkey (default: F12)
- Webhook username and avatar

## Discord Setup
1. Create a Discord webhook in your desired channel
2. Copy the webhook URL to the mod configuration
3. Screenshots will be automatically sent to that channel
