![DiscordScreenshots](https://i.imgur.com/HiPcG3M.gif)

# Discord Screenshots
A mod to send screenshots to a Discord server. 

## Features
- Hotkey screenshot capture (F12 by default)
- Automatic death screenshots sent to Discord when you die
- Random death message selection from configured phrases
- Optional separate webhook for player capture screenshots

## Death screenshots
Death screenshots are captured when the player dies and sent to Discord after they respawn.

## Configuration
All settings are configurable through the BepInEx config file:
- Discord webhook URL
- Screenshot hotkey (default: F12)
- Webhook username and avatar
- Death messages separated by semicolons
- Optional player capture webhook URL
- Discord request timeout (default: 60 seconds)

Screenshots with at least four million pixels use JPEG to reduce upload size. Smaller screenshots use PNG. The mod checks the captured image dimensions for each screenshot, so resolution changes made after startup are supported.

## Discord Setup
1. Create a Discord webhook in your desired channel
2. Copy the webhook URL to the mod configuration
3. Screenshots will be automatically sent to that channel
