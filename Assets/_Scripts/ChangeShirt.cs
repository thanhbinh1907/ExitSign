using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Photon.Pun;

public class ChangeShirt : MonoBehaviourPunCallbacks
{
    [Header("Player Detection Settings")]
    [Tooltip("Distance to detect players looking at each other")]
    public float detectionDistance = 10f;
    
    [Tooltip("Angle tolerance for detecting if players are looking at each other")]
    public float lookAngleTolerance = 30f;
    
    [Tooltip("Tag used to identify players")]
    public string playerTag = "Player";
    
    [Header("Material Change Settings")]
    [Tooltip("Time delay before changing shirt material (in seconds)")]
    public float delayBeforeChange = 5f;
    
    [Tooltip("New shirt material to apply")]
    public Material newShirtMaterial;
    
    [Tooltip("Name of the shirt renderer object (child of player)")]
    public string shirtRendererName = "Shirt";
    
    [Tooltip("Material index on the shirt renderer")]
    public int materialIndex = 0;

    [Header("Debug Settings")]
    public bool enableDebugLines = true;
    
    // Private variables for state tracking
    private Dictionary<string, Material> originalMaterials = new Dictionary<string, Material>();
    private Dictionary<string, bool> playersLookingAtEachOther = new Dictionary<string, bool>();
    private Dictionary<string, Coroutine> activeCoroutines = new Dictionary<string, Coroutine>();
    
    void Start()
    {
        // Only run detection on the master client to avoid duplicate logic
        if (!PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnected)
        {
            enabled = false;
            return;
        }
        
        // Start the detection coroutine
        StartCoroutine(DetectPlayersLookingAtEachOther());
    }
    
    void Update()
    {
        // Draw debug lines if enabled
        if (enableDebugLines)
        {
            DrawDebugLines();
        }
    }
    
    /// <summary>
    /// Main coroutine that continuously checks if players are looking at each other
    /// </summary>
    private IEnumerator DetectPlayersLookingAtEachOther()
    {
        while (true)
        {
            // Find all players in the scene
            GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
            
            // Check all pairs of players
            for (int i = 0; i < players.Length; i++)
            {
                for (int j = i + 1; j < players.Length; j++)
                {
                    CheckPlayerPair(players[i], players[j]);
                }
            }
            
            // Wait before next check
            yield return new WaitForSeconds(0.1f); // Check 10 times per second
        }
    }
    
    /// <summary>
    /// Check if two players are looking at each other
    /// </summary>
    private void CheckPlayerPair(GameObject player1, GameObject player2)
    {
        if (player1 == null || player2 == null) return;
        
        // Get player transforms and cameras
        Transform player1Transform = player1.transform;
        Transform player2Transform = player2.transform;
        
        // Find camera transforms (assuming camera is a child of player)
        Camera player1Camera = player1.GetComponentInChildren<Camera>();
        Camera player2Camera = player2.GetComponentInChildren<Camera>();
        
        if (player1Camera == null || player2Camera == null) return;
        
        Transform camera1 = player1Camera.transform;
        Transform camera2 = player2Camera.transform;
        
        // Calculate distance between players
        float distance = Vector3.Distance(player1Transform.position, player2Transform.position);
        if (distance > detectionDistance) return;
        
        // Check if player1 is looking at player2
        Vector3 directionToPlayer2 = (player2Transform.position - camera1.position).normalized;
        float angle1 = Vector3.Angle(camera1.forward, directionToPlayer2);
        
        // Check if player2 is looking at player1
        Vector3 directionToPlayer1 = (player1Transform.position - camera2.position).normalized;
        float angle2 = Vector3.Angle(camera2.forward, directionToPlayer1);
        
        // Check if both players are looking at each other
        bool arePlayersLookingAtEachOther = (angle1 <= lookAngleTolerance && angle2 <= lookAngleTolerance);
        
        // Create a unique pair key
        string pairKey = GetPlayerPairKey(player1, player2);
        
        // If players are looking at each other and we haven't started the timer yet
        if (arePlayersLookingAtEachOther && !playersLookingAtEachOther.ContainsKey(pairKey))
        {
            playersLookingAtEachOther[pairKey] = true;
            
            // Start the delay coroutine
            if (!activeCoroutines.ContainsKey(pairKey))
            {
                activeCoroutines[pairKey] = StartCoroutine(DelayedShirtChange(player1, player2, pairKey));
            }
        }
        // If players stopped looking at each other
        else if (!arePlayersLookingAtEachOther && playersLookingAtEachOther.ContainsKey(pairKey))
        {
            // Cancel the timer
            if (activeCoroutines.ContainsKey(pairKey))
            {
                StopCoroutine(activeCoroutines[pairKey]);
                activeCoroutines.Remove(pairKey);
            }
            playersLookingAtEachOther.Remove(pairKey);
        }
    }
    
    /// <summary>
    /// Generate a unique key for a player pair
    /// </summary>
    private string GetPlayerPairKey(GameObject player1, GameObject player2)
    {
        // Create a consistent key regardless of order
        int id1 = player1.GetInstanceID();
        int id2 = player2.GetInstanceID();
        
        if (id1 < id2)
            return $"{id1}_{id2}";
        else
            return $"{id2}_{id1}";
    }
    
    /// <summary>
    /// Coroutine that waits for the specified delay then changes shirt materials
    /// </summary>
    private IEnumerator DelayedShirtChange(GameObject player1, GameObject player2, string pairKey)
    {
        Debug.Log($"Players are looking at each other! Material will change in {delayBeforeChange} seconds...");
        
        // Wait for the specified delay
        yield return new WaitForSeconds(delayBeforeChange);
        
        // Double-check that players are still looking at each other
        if (playersLookingAtEachOther.ContainsKey(pairKey))
        {
            // Change shirt materials for both players
            ChangePlayerShirtMaterial(player1);
            ChangePlayerShirtMaterial(player2);
            
            Debug.Log("Shirt materials changed for both players!");
            
            // Send RPC to all clients to sync the change
            if (PhotonNetwork.IsConnected)
            {
                photonView.RPC("SyncShirtMaterialChange", RpcTarget.Others, 
                    GetPlayerPhotonViewID(player1), GetPlayerPhotonViewID(player2));
            }
        }
        
        // Clean up
        playersLookingAtEachOther.Remove(pairKey);
        activeCoroutines.Remove(pairKey);
    }
    
    /// <summary>
    /// Change the shirt material for a specific player
    /// </summary>
    private void ChangePlayerShirtMaterial(GameObject player)
    {
        if (player == null || newShirtMaterial == null) return;
        
        // Find the shirt renderer
        Transform shirtTransform = FindChildRecursive(player.transform, shirtRendererName);
        if (shirtTransform == null)
        {
            Debug.LogWarning($"Could not find shirt renderer '{shirtRendererName}' on player {player.name}");
            return;
        }
        
        Renderer shirtRenderer = shirtTransform.GetComponent<Renderer>();
        if (shirtRenderer == null)
        {
            Debug.LogWarning($"No Renderer component found on {shirtRendererName}");
            return;
        }
        
        // Store original material if not already stored
        string playerKey = player.GetInstanceID().ToString();
        if (!originalMaterials.ContainsKey(playerKey))
        {
            Material[] materials = shirtRenderer.materials;
            if (materialIndex < materials.Length)
            {
                originalMaterials[playerKey] = materials[materialIndex];
            }
        }
        
        // Change the shirt material
        Material[] currentMaterials = shirtRenderer.materials;
        if (materialIndex < currentMaterials.Length)
        {
            currentMaterials[materialIndex] = newShirtMaterial;
            shirtRenderer.materials = currentMaterials;
        }
    }
    
    /// <summary>
    /// RPC to synchronize shirt material changes across all clients
    /// </summary>
    [PunRPC]
    void SyncShirtMaterialChange(int player1ViewID, int player2ViewID)
    {
        // Find players by PhotonView ID
        PhotonView pv1 = PhotonView.Find(player1ViewID);
        PhotonView pv2 = PhotonView.Find(player2ViewID);
        
        if (pv1 != null)
            ChangePlayerShirtMaterial(pv1.gameObject);
            
        if (pv2 != null)
            ChangePlayerShirtMaterial(pv2.gameObject);
    }
    
    /// <summary>
    /// Get PhotonView ID for a player
    /// </summary>
    private int GetPlayerPhotonViewID(GameObject player)
    {
        PhotonView pv = player.GetComponent<PhotonView>();
        return pv != null ? pv.ViewID : -1;
    }
    
    /// <summary>
    /// Recursively find a child transform by name
    /// </summary>
    private Transform FindChildRecursive(Transform parent, string childName)
    {
        // Check direct children first
        Transform child = parent.Find(childName);
        if (child != null)
            return child;
        
        // Recursively search in all children
        foreach (Transform t in parent)
        {
            Transform result = FindChildRecursive(t, childName);
            if (result != null)
                return result;
        }
        
        return null;
    }
    
    /// <summary>
    /// Reset a player's shirt material to original
    /// </summary>
    public void ResetPlayerShirtMaterial(GameObject player)
    {
        if (player == null) return;
        
        string playerKey = player.GetInstanceID().ToString();
        if (!originalMaterials.ContainsKey(playerKey)) return;
        
        Transform shirtTransform = FindChildRecursive(player.transform, shirtRendererName);
        if (shirtTransform == null) return;
        
        Renderer shirtRenderer = shirtTransform.GetComponent<Renderer>();
        if (shirtRenderer == null) return;
        
        Material[] currentMaterials = shirtRenderer.materials;
        if (materialIndex < currentMaterials.Length)
        {
            currentMaterials[materialIndex] = originalMaterials[playerKey];
            shirtRenderer.materials = currentMaterials;
        }
    }
    
    /// <summary>
    /// Reset all players' shirt materials to original
    /// </summary>
    public void ResetAllShirtMaterials()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        foreach (GameObject player in players)
        {
            ResetPlayerShirtMaterial(player);
        }
        
        // Clear stored materials
        originalMaterials.Clear();
    }
    
    /// <summary>
    /// Draw debug lines to visualize player detection
    /// </summary>
    private void DrawDebugLines()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag(playerTag);
        
        foreach (GameObject player in players)
        {
            Camera playerCamera = player.GetComponentInChildren<Camera>();
            if (playerCamera != null)
            {
                // Draw forward direction
                Debug.DrawRay(playerCamera.transform.position, 
                    playerCamera.transform.forward * detectionDistance, Color.blue);
                
                // Draw cone of vision
                Vector3 left = Quaternion.AngleAxis(-lookAngleTolerance, playerCamera.transform.up) * playerCamera.transform.forward;
                Vector3 right = Quaternion.AngleAxis(lookAngleTolerance, playerCamera.transform.up) * playerCamera.transform.forward;
                
                Debug.DrawRay(playerCamera.transform.position, left * detectionDistance, Color.yellow);
                Debug.DrawRay(playerCamera.transform.position, right * detectionDistance, Color.yellow);
            }
        }
    }
    
    void OnDisable()
    {
        // Stop all active coroutines when disabled
        foreach (var coroutine in activeCoroutines.Values)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }
        activeCoroutines.Clear();
        playersLookingAtEachOther.Clear();
    }
}
