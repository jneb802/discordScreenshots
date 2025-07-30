// using System;
// using System.Collections.Generic;
// using System.Text;
// using System.Text.RegularExpressions;
// using UnityEngine;
// using UnityEngine.UI;
// using Jotunn.Managers;
// using Jotunn.GUI;
// using Jotunn.Extensions;

// namespace discordScreenshots
// {
//     public class ItemViewer : MonoBehaviour, Hoverable, Interactable
//     {
//         [Header("Item Viewer Settings")]
//         public string m_name = "Item Viewer";
//         public Transform m_attachPoint;
//         public Transform m_cameraPosition;
        
//         private Transform? m_attachOther;
//         private ItemStand? m_itemStand;
        
//         // Camera and zoom control
//         private bool m_attached = false;
//         private bool m_playerWasVisible = true;
//         private bool m_hudWasHidden = false;
        
//         [Header("Camera Zoom Settings")]
//         // Zoom speed control:
//         // - Higher values = faster zoom (try 3f-5f for quick zoom, 1f for slow)
//         public float m_zoomSpeed = 2f; // How fast to zoom
//         // Zoom distance limits:
//         // - m_minZoomDistance: closest zoom (0.3f = very close, 1f = moderate)
//         // - m_maxZoomDistance: farthest zoom (3f = close view, 8f = wide view)
//         public float m_minZoomDistance = 0.5f; // Closest zoom (0.5 units from piece)
//         public float m_maxZoomDistance = 5f; // Farthest zoom (5 units from piece)
        
//         private float m_currentZoomDistance = 1.5f; // Current zoom level
//         private Vector3 m_originalCameraPosition; // Store original camera position
        
//         // Tooltip display
//         private float m_tooltipUpdateTimer = 0f;
//         private string m_currentTooltipText = "";
        
//         // Native UITooltip system
//         private UITooltip? m_nativeTooltip;
//         private GameObject? m_tooltipAnchor;

//         public void Awake()
//         {
//             // Find the attach_other transform for item detection
//             // Try common paths based on the prefab hierarchy
//             m_attachOther = transform.Find("itemStandHorizontal/attach_other");
//             if (m_attachOther == null)
//             {
//                 m_attachOther = transform.Find("attach_other");
//             }
//             if (m_attachOther == null)
//             {
//                 // Recursive search as fallback
//                 m_attachOther = FindChildRecursive(transform, "attach_other");
//             }
            
//             if (m_attachOther != null)
//             {
//                 Debug.Log("Found attach_other for item detection");
//             }
//             else
//             {
//                 Debug.LogWarning("No attach_other child found in ItemViewer prefab");
//             }

//             // Find the ItemStand component on itemStandHorizontal
//             Transform itemStandHorizontal = FindChildRecursive(transform, "itemStandHorizontal");
//             if (itemStandHorizontal != null)
//             {
//                 m_itemStand = itemStandHorizontal.GetComponent<ItemStand>();
//                 if (m_itemStand != null)
//                 {
//                     Debug.Log("Found ItemStand component on itemStandHorizontal");
//                 }
//                 else
//                 {
//                     Debug.LogWarning("No ItemStand component found on itemStandHorizontal");
//                 }
//             }
//             else
//             {
//                 Debug.LogWarning("No itemStandHorizontal child found in ItemViewer prefab");
//             }
            
//             // Create tooltip anchor for top-left positioning
//             CreateTooltipAnchor();
            
//             // Try early tooltip initialization (before any UI hiding)
//             // This gives us the best chance to find existing UI components
//             InitializeNativeTooltip();
//         }

//         private void CreateTooltipAnchor()
//         {
//             // Create a UI anchor in the top-left corner for the native tooltip
//             m_tooltipAnchor = new GameObject("TooltipAnchor");
            
//             // Find the main UI canvas (usually the GUI root)
//             Canvas mainCanvas = FindObjectOfType<Canvas>();
//             if (mainCanvas != null)
//             {
//                 m_tooltipAnchor.transform.SetParent(mainCanvas.transform);
//             }
//             else
//             {
//                 // Create our own canvas if needed
//                 GameObject canvasObj = new GameObject("TooltipAnchorCanvas");
//                 Canvas canvas = canvasObj.AddComponent<Canvas>();
//                 canvas.renderMode = RenderMode.ScreenSpaceOverlay;
//                 canvas.sortingOrder = 999;
//                 m_tooltipAnchor.transform.SetParent(canvasObj.transform);
//             }
            
//             // Set up RectTransform for top-left positioning
//             RectTransform anchorRect = m_tooltipAnchor.AddComponent<RectTransform>();
//             anchorRect.anchorMin = new Vector2(0, 1); // Top-left anchor
//             anchorRect.anchorMax = new Vector2(0, 1); // Top-left anchor
//             anchorRect.pivot = new Vector2(0, 1); // Top-left pivot
//             anchorRect.anchoredPosition = new Vector2(20, -20); // 20 pixels from top-left corner
//             anchorRect.sizeDelta = Vector2.zero; // No size needed, just an anchor point
            
//             Debug.Log("Created tooltip anchor in top-left corner");
//         }

//         private Transform FindChildRecursive(Transform parent, string name)
//         {
//             foreach (Transform child in parent)
//             {
//                 if (child.name == name)
//                     return child;
                
//                 Transform result = FindChildRecursive(child, name);
//                 if (result != null)
//                     return result;
//             }
//             return null;
//         }

//         private void Update()
//         {
//             // Update tooltip text when there's an item on the stand
//             m_tooltipUpdateTimer += Time.deltaTime;
//             if (m_tooltipUpdateTimer >= 0.5f && m_itemStand != null)
//             {
//                 m_tooltipUpdateTimer = 0f;
//                 UpdateTooltipText();
//             }
            
//             // Handle camera zoom when attached
//             if (m_attached && m_cameraPosition != null)
//             {
//                 // Get scroll wheel input
//                 float scrollInput = Input.GetAxis("Mouse ScrollWheel");
                
//                 if (scrollInput != 0f)
//                 {
//                     // Zoom control:
//                     // - Mouse wheel up = zoom in (decrease distance)
//                     // - Mouse wheel down = zoom out (increase distance)
//                     // - m_zoomSpeed controls how fast zooming happens
//                     // - Clamped between m_minZoomDistance and m_maxZoomDistance
//                     m_currentZoomDistance -= scrollInput * m_zoomSpeed;
//                     m_currentZoomDistance = Mathf.Clamp(m_currentZoomDistance, m_minZoomDistance, m_maxZoomDistance);
                    
//                     // Calculate new camera position based on zoom
//                     // Move camera directly toward/away from the item stand
//                     if (m_attachOther != null)
//                     {
//                         // Get direction from item stand to camera (for proper zoom direction)
//                         Vector3 standToCamera = (m_originalCameraPosition - m_attachOther.localPosition).normalized;
//                         Vector3 zoomOffset = standToCamera * (m_currentZoomDistance - 1.5f);
//                         Vector3 newPosition = m_originalCameraPosition + zoomOffset;
//                         m_cameraPosition.localPosition = newPosition;
//                     }
//                 }
//             }
            
//             // Exit camera mode with Escape or Tab
//             if (m_attached && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Tab)))
//             {
//                 ExitCameraMode();
//             }
//         }

//         private void UpdateTooltipText()
//         {
//             if (m_itemStand == null)
//                 return;
                
//             // Check if there's an item on the stand
//             if (m_itemStand.HaveAttachment())
//             {
//                 try
//                 {
//                     // Get the item data from the ItemStand
//                     ItemDrop.ItemData itemData = GetItemDataFromStand();
                    
//                     if (itemData != null)
//                     {
//                         // Generate full tooltip using Valheim's tooltip system
//                         string fullTooltip = itemData.GetTooltip();
                        
//                         // Store the tooltip text for display
//                         m_currentTooltipText = fullTooltip;
                        
//                         // Update native tooltip if in camera mode
//                         if (m_attached && m_nativeTooltip != null && m_tooltipAnchor != null)
//                         {
//                             string itemName = itemData.m_shared.m_name;
//                             m_nativeTooltip.Set(itemName, fullTooltip, m_tooltipAnchor.GetComponent<RectTransform>());
//                         }
//                     }
//                     else
//                     {
//                         // Fallback if we can't get item data
//                         string itemName = !string.IsNullOrEmpty(m_itemStand.m_currentItemName) ? 
//                             m_itemStand.m_currentItemName : "Unknown Item";
//                         m_currentTooltipText = itemName + "\n\nItem data could not be loaded.\nTry removing and replacing the item.";
                        
//                         if (m_attached && m_nativeTooltip != null && m_tooltipAnchor != null)
//                         {
//                             m_nativeTooltip.Set(itemName, m_currentTooltipText, m_tooltipAnchor.GetComponent<RectTransform>());
//                         }
//                     }
//                 }
//                 catch (System.Exception ex)
//                 {
//                     Debug.LogError($"Error getting item tooltip: {ex.Message}");
//                     // Fallback to simple display
//                     string itemName = !string.IsNullOrEmpty(m_itemStand.m_currentItemName) ? 
//                         m_itemStand.m_currentItemName : "Unknown Item";
                    
//                     m_currentTooltipText = itemName + "\n\nError loading item details.\nCheck console for more information.";
                    
//                     if (m_attached && m_nativeTooltip != null && m_tooltipAnchor != null)
//                     {
//                         m_nativeTooltip.Set(itemName, m_currentTooltipText, m_tooltipAnchor.GetComponent<RectTransform>());
//                     }
//                 }
//             }
//             else
//             {
//                 // No item on stand
//                 m_currentTooltipText = "No Item\n\nPlace an item on the stand to see its details.\n\nUse hotbar keys 1-8 to place items.";
                
//                 if (m_attached && m_nativeTooltip != null && m_tooltipAnchor != null)
//                 {
//                     m_nativeTooltip.Set("No Item", m_currentTooltipText, m_tooltipAnchor.GetComponent<RectTransform>());
//                 }
//             }
//         }

//         private ItemDrop.ItemData GetItemDataFromStand()
//         {
//             if (m_itemStand == null)
//                 return null;

//             try
//             {
//                 // Get the item prefab name from the ItemStand
//                 string itemName = m_itemStand.GetAttachedItem();
//                 if (string.IsNullOrEmpty(itemName))
//                     return null;

//                 // Get the item prefab from ObjectDB
//                 GameObject itemPrefab = PrefabManager.Instance.GetPrefab(itemName);
//                 if (itemPrefab == null)
//                     return null;

//                 // Get the ItemDrop component and its data
//                 ItemDrop itemDrop = itemPrefab.GetComponent<ItemDrop>();
//                 if (itemDrop == null)
//                     return null;

//                 // Clone the item data so we can modify it safely
//                 ItemDrop.ItemData itemData = itemDrop.m_itemData.Clone();

//                 // Load the specific instance data from the ItemStand's ZDO
//                 ZNetView nview = m_itemStand.GetComponent<ZNetView>();
//                 if (nview != null && nview.IsValid())
//                 {
//                     ZDO zdo = nview.GetZDO();
//                     // Load item data manually
//                     itemData.m_durability = zdo.GetFloat(ZDOVars.s_durability, itemData.m_durability);
//                     itemData.m_stack = zdo.GetInt(ZDOVars.s_stack, itemData.m_stack);
//                     itemData.m_quality = zdo.GetInt(ZDOVars.s_quality, itemData.m_quality);
//                     itemData.m_variant = zdo.GetInt(ZDOVars.s_variant, itemData.m_variant);
//                     itemData.m_crafterID = zdo.GetLong(ZDOVars.s_crafterID, itemData.m_crafterID);
//                     itemData.m_crafterName = zdo.GetString(ZDOVars.s_crafterName, itemData.m_crafterName);
//                     itemData.m_worldLevel = zdo.GetInt(ZDOVars.s_worldLevel, itemData.m_worldLevel);
//                 }

//                 return itemData;
//             }
//             catch (System.Exception ex)
//             {
//                 Debug.LogError($"Error loading item data from stand: {ex.Message}");
//                 return null;
//             }
//         }

//         // Hoverable interface
//         public string GetHoverText()
//         {
//             if (m_attached)
//             {
//                 // When in camera mode, just show controls (tooltip is displayed via native system)
//                 return $"{m_name}\n[<color=yellow><b>E</b></color>] Exit camera mode\n[<color=yellow><b>Mouse Wheel</b></color>] Zoom\n[<color=yellow><b>Esc/Tab</b></color>] Exit";
//             }
//             else
//             {
//                 return $"{m_name}\n[<color=yellow><b>E</b></color>] Enter camera mode";
//             }
//         }

//         public string GetHoverName()
//         {
//             return m_name;
//         }

//         // Interactable interface
//         public bool Interact(Humanoid character, bool hold, bool alt)
//         {
//             // Toggle camera mode
//             if (!m_attached)
//             {
//                 // Enter camera mode
//                 EnterCameraMode();
//             }
//             else
//             {
//                 // Exit camera mode
//                 ExitCameraMode();
//             }
//             return true;
//         }
        
//         private void EnterCameraMode()
//         {
//             if (m_attachPoint != null && m_cameraPosition != null && Player.m_localPlayer != null && !m_attached)
//             {
//                 // Store original camera position for zoom calculations
//                 m_originalCameraPosition = m_cameraPosition.localPosition;
//                 m_currentZoomDistance = 1.5f; // Reset zoom level
                
//                 // Initialize native tooltip BEFORE hiding HUD
//                 InitializeNativeTooltip();
                
//                 // Store current player visibility state and hide player
//                 m_playerWasVisible = true; // Assume player was visible
//                 SetPlayerVisibility(false);
                
//                 // Store current HUD state and hide HUD
//                 if (Hud.instance != null)
//                 {
//                     m_hudWasHidden = Hud.instance.m_userHidden;
//                     Hud.instance.m_userHidden = true;
//                 }
                
//                 // Attach player like the Barber does - lock player movement and set camera view
//                 Player.m_localPlayer.AttachStart(m_attachPoint, null, true, false, false, "", Vector3.zero, m_cameraPosition);
//                 m_attached = true;
                
//                 // Block input for the player
//                 GUIManager.BlockInput(true);
                
//                 // Update tooltip text immediately when entering camera mode
//                 UpdateTooltipText();
//             }
//         }
        
//         private void ExitCameraMode()
//         {
//             if (m_attached && Player.m_localPlayer != null)
//             {
//                 // Hide native tooltip
//                 HideNativeTooltip();
                
//                 // Detach player to restore normal camera
//                 Player.m_localPlayer.AttachStop();
//                 m_attached = false;
                
//                 // Restore player visibility
//                 SetPlayerVisibility(m_playerWasVisible);
                
//                 // Restore HUD visibility
//                 if (Hud.instance != null)
//                 {
//                     Hud.instance.m_userHidden = m_hudWasHidden;
//                 }
                
//                 // Restore original camera position
//                 if (m_cameraPosition != null)
//                 {
//                     m_cameraPosition.localPosition = m_originalCameraPosition;
//                 }
                
//                 // Restore input
//                 GUIManager.BlockInput(false);
//             }
//         }

//         private void InitializeNativeTooltip()
//         {
//             // Don't reinitialize if we already have one
//             if (m_nativeTooltip != null)
//                 return;
                
//             GameObject tooltipPrefab = null;
        
            
//             // Method 2: Try to get tooltip prefab from InventoryGui
//             if (tooltipPrefab == null && InventoryGui.instance != null)
//             {
//                 // InventoryGui should have a UITooltip component
//                 UITooltip inventoryTooltip = InventoryGui.instance.GetComponent<UITooltip>();
//                 if (inventoryTooltip != null && inventoryTooltip.m_tooltipPrefab != null)
//                 {
//                     tooltipPrefab = inventoryTooltip.m_tooltipPrefab;
//                     Debug.Log("Found tooltip prefab from InventoryGui");
//                 }
//             }
            
//             // Create our own UITooltip component if we found a prefab
//             if (tooltipPrefab != null)
//             {
//                 m_nativeTooltip = gameObject.AddComponent<UITooltip>();
//                 m_nativeTooltip.m_tooltipPrefab = tooltipPrefab;
//                 Debug.Log("Successfully initialized native UITooltip with prefab");
//             }
//             else
//             {
//                 Debug.LogWarning("Could not find any tooltip prefab - tooltip display will not work");
//                 Debug.LogWarning("This might be because the HUD/UI is not yet initialized or is hidden");
//             }
//         }

//         private void HideNativeTooltip()
//         {
//             if (m_nativeTooltip != null)
//             {
//                 UITooltip.HideTooltip();
//             }
//         }
        
//         private void SetPlayerVisibility(bool visible)
//         {
//             if (Player.m_localPlayer == null) return;

//             // Find and toggle all SkinnedMeshRenderer components on the player
//             SkinnedMeshRenderer[] meshRenderers = Player.m_localPlayer.GetComponentsInChildren<SkinnedMeshRenderer>();
//             foreach (SkinnedMeshRenderer renderer in meshRenderers)
//             {
//                 renderer.enabled = visible;
//             }

//             // Also hide regular MeshRenderer components (for equipment, etc.)
//             MeshRenderer[] regularRenderers = Player.m_localPlayer.GetComponentsInChildren<MeshRenderer>();
//             foreach (MeshRenderer renderer in regularRenderers)
//             {
//                 renderer.enabled = visible;
//             }

//             Debug.Log($"Player visibility set to: {visible}");
//         }

//         public bool UseItem(Humanoid user, ItemDrop.ItemData item)
//         {
//             return false;
//         }
        
//         private void OnDestroy()
//         {
//             // Hide native tooltip
//             HideNativeTooltip();
            
//             // Destroy tooltip anchor
//             if (m_tooltipAnchor != null)
//             {
//                 Destroy(m_tooltipAnchor);
//             }
            
//             // Ensure player is detached if still attached
//             if (m_attached && Player.m_localPlayer != null)
//             {
//                 Player.m_localPlayer.AttachStop();
//                 m_attached = false;
                
//                 // Restore player visibility if it was hidden
//                 SetPlayerVisibility(m_playerWasVisible);
                
//                 // Restore HUD visibility
//                 if (Hud.instance != null)
//                 {
//                     Hud.instance.m_userHidden = m_hudWasHidden;
//                 }
                
//                 // Restore input
//                 GUIManager.BlockInput(false);
//             }
//         }
//     }
// }
