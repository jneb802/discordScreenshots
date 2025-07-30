// using Jotunn.Configs;
// using Jotunn.Entities;
// using Jotunn.Managers;
// using UnityEngine;
// using System.Linq;
// using Jotunn.Utils;
// using BepInEx;

// namespace discordScreenshots
// {
//     public static class PrefabUtils
//     {

//         public static void Initialize()
//         {
//             // Subscribe to the event that fires when vanilla prefabs are available
//             PrefabManager.OnVanillaPrefabsAvailable += BuildPiece;
//         }

//         public static void BuildPiece()
//         {
//             if (discordScreenshotsPlugin.assetBundle == null)
//             {
//                 Debug.LogError("Asset bundle is null! Make sure to load it first.");
//                 return;
//             }

//             CustomPrefab customPrefab = new CustomPrefab(discordScreenshotsPlugin.assetBundle, "screenshotPiece", true);
//             PrefabManager.Instance.AddPrefab(customPrefab);

//             // Configure the piece
//             PieceConfig pieceConfig = new PieceConfig()
//             {
//                 Name = "Item Viewer",
//                 Description = "A mystical device for examining items in detail",
//                 PieceTable = "Hammer", // Buildable with hammer
//                 Category = PieceCategories.Misc, // Category in the build menu
//                 CraftingStation = "", // No crafting station required (can build anywhere)
//                 Requirements = new RequirementConfig[]
//                 {
//                     new RequirementConfig("Wood", 10, 0, true),
//                     new RequirementConfig("Stone", 5, 0, true)
//                 },
//                 Enabled = true,
//                 AllowedInDungeons = false
//             };

//             // Create the custom piece from asset bundle
//             CustomPiece customPiece = new CustomPiece(PrefabManager.Instance.GetPrefab("screenshotPiece"), false, pieceConfig);
            
//             // Add the ItemViewer component to the prefab
//             if (customPiece.PiecePrefab != null)
//             {
//                 ItemViewer itemViewer = customPiece.PiecePrefab.AddComponent<ItemViewer>();
                
//                 // Find the existing camerapoint transform from the prefab
//                 Transform cameraPoint = customPiece.PiecePrefab.transform.Find("camerapoint");
                
//                 if (cameraPoint != null)
//                 {
//                     // Assign the existing camerapoint to ItemViewer
//                     itemViewer.m_cameraPosition = cameraPoint;
//                     Debug.Log("Found and assigned camerapoint to ItemViewer");
//                 }
//                 else
//                 {
//                     Debug.LogWarning("Could not find 'camerapoint' child in screenshotPiece prefab");
//                 }
                
//                 // Create or find attach point for player positioning
//                 Transform attachPoint = customPiece.PiecePrefab.transform.Find("attachpoint");
//                 if (attachPoint == null)
//                 {
//                     // Create attach point if it doesn't exist - position where player should stand
//                     GameObject attachPointObj = new GameObject("attachpoint");
//                     attachPointObj.transform.SetParent(customPiece.PiecePrefab.transform);

//                     // Player position control (relative to the piece):
//                     // X-axis: left/right (-2f = 2 units left, +2f = 2 units right)
//                     // Y-axis: up/down (0f = ground level, +1f = 1 unit up, -1f = 1 unit down)  
//                     // Z-axis: forward/back (-2f = 2 units behind piece, +2f = 2 units in front)
//                     attachPointObj.transform.localPosition = new Vector3(0f, 0f, 2f); // Player position
//                     attachPointObj.transform.localRotation = Quaternion.LookRotation(Vector3.forward); // Face the piece
//                     attachPoint = attachPointObj.transform;
//                     Debug.Log("Created attachpoint for ItemViewer");
//                 }
                
//                 itemViewer.m_attachPoint = attachPoint;
                
//                 // If no camera position was found, create one relative to the attach point
//                 if (cameraPoint == null)
//                 {
//                     GameObject cameraPointObj = new GameObject("camerapoint");
//                     cameraPointObj.transform.SetParent(customPiece.PiecePrefab.transform);
//                     cameraPointObj.transform.localPosition = new Vector3(0f, 1.8f, -1.5f); // Camera above and closer to piece
//                     cameraPointObj.transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up); // Look at the piece
//                     itemViewer.m_cameraPosition = cameraPointObj.transform;
//                     Debug.Log("Created camerapoint for ItemViewer");
//                 }
//             }

//             // Add the piece to the manager
//             PieceManager.Instance.AddPiece(customPiece);

//             // Unsubscribe from the event since we only need to run this once
//             PrefabManager.OnVanillaPrefabsAvailable -= BuildPiece;
//         }
//     }
// }