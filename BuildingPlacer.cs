using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

// A simple component to identify a placed building
public class Building : MonoBehaviour { }

public class BuildingPlacer : MonoBehaviour
{
    // --- Panel Toggle Configuration ---
    [System.Serializable]
    public class PanelToggleConfig
    {
        [Tooltip("The UI Button that will toggle the associated panel.")]
        public Button toggleButton;
        [Tooltip("The GameObject panel that will be activated/deactivated.")]
        public GameObject panelToToggle;
    }

    [Tooltip("Configure your UI buttons and the panels they toggle.")]
    public List<PanelToggleConfig> panelToggleConfigs = new List<PanelToggleConfig>();

    // --- Building Type Configuration ---
    [System.Serializable]
    public class BuildingType
    {
        public string name;
        [Tooltip("The BuildingInfo ScriptableObject for this building type.")]
        public BuildingInfo buildingInfo;
        [Tooltip("The UI Button that, when clicked, starts placing this building.")]
        public Button uiButton;
        [Tooltip("Set this to the layer where placing this specific building is forbidden (e.g., water, other specific buildings).")]
        public LayerMask invalidPlacementLayer;
    }

    [Tooltip("Configure your building types here. Each entry needs a BuildingInfo SO and a UI button.")]
    public List<BuildingType> buildingTypes = new List<BuildingType>();

    // --- Internal State Variables ---
    private BuildingInfo currentBuildingInfo;
    private GameObject ghostBuildingInstance;
    private bool isPlacingBuilding = false;
    private LayerMask currentInvalidPlacementLayer;

    // --- Visuals and Layers ---
    [Tooltip("Assign a transparent material for valid placement.")]
    public Material validPlacementMaterial;
    [Tooltip("Assign a red, transparent material for invalid placement.")]
    public Material invalidPlacementMaterial;

    [Tooltip("Set this to the layer your ground or placable surfaces are on.")]
    public LayerMask groundLayer;
    
    [Tooltip("Offset the ghost building slightly above the ground to prevent Z-fighting.")]
    public float placementYOffset = 0.05f;

    // --- Placement Options ---
    public bool snapToGrid = false;
    public float gridSize = 1f;

    // --- Unity Lifecycle Methods ---

    void Start()
    {
        // Hook up panel toggle buttons
        foreach (PanelToggleConfig config in panelToggleConfigs)
        {
            if (config.toggleButton != null && config.panelToToggle != null)
            {
                GameObject panel = config.panelToToggle;
                config.toggleButton.onClick.AddListener(() => TogglePanel(panel));
                config.panelToToggle.SetActive(false);
            }
            else
            {
                Debug.LogWarning("A PanelToggleConfig entry has unassigned button or panel. Please check the Inspector.");
            }
        }

        // Hook up building selection buttons
        foreach (BuildingType buildingType in buildingTypes)
        {
            if (buildingType.uiButton != null && buildingType.buildingInfo != null)
            {
                BuildingInfo infoToPlace = buildingType.buildingInfo;
                LayerMask invalidLayerForThisBuilding = buildingType.invalidPlacementLayer;

                buildingType.uiButton.onClick.AddListener(() => StartPlacingBuilding(infoToPlace, invalidLayerForThisBuilding));
            }
            else
            {
                Debug.LogWarning($"UI Button or BuildingInfo not assigned for building type: {buildingType.name}. Please check the Inspector.");
            }
        }

        if (ghostBuildingInstance != null)
        {
            Destroy(ghostBuildingInstance);
            ghostBuildingInstance = null;
        }
    }

    void Update()
    {
        if (isPlacingBuilding && currentBuildingInfo != null)
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // Use a combined layer mask from the global ground and the specific building's invalid layer
            LayerMask combinedLayerMask = groundLayer | currentInvalidPlacementLayer;

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, combinedLayerMask))
            {
                Vector3 placementPosition = hit.point + Vector3.up * placementYOffset;

                if (snapToGrid)
                {
                    placementPosition.x = Mathf.Round(placementPosition.x / gridSize) * gridSize;
                    placementPosition.y = Mathf.Round(placementPosition.y / gridSize) * gridSize;
                    placementPosition.z = Mathf.Round(placementPosition.z / gridSize) * gridSize;
                }

                if (ghostBuildingInstance == null)
                {
                    GameObject prefabToInstantiate = currentBuildingInfo.buildingGhostPrefab != null ?
                                                     currentBuildingInfo.buildingGhostPrefab :
                                                     currentBuildingInfo.buildingPrefab;

                    if (prefabToInstantiate != null)
                    {
                        ghostBuildingInstance = Instantiate(prefabToInstantiate);
                        DisableGhostComponents(ghostBuildingInstance);
                    }
                    else
                    {
                        Debug.LogError($"Ghost or main prefab is missing for {currentBuildingInfo.buildingName}. Cannot create ghost.");
                        CancelBuildingPlacement();
                        return;
                    }
                }
                ghostBuildingInstance.transform.position = placementPosition;
                ghostBuildingInstance.SetActive(true);

                // Check the hit layer to determine placement validity and visuals
                int hitLayer = hit.collider.gameObject.layer;
                bool isGround = ((1 << hitLayer) & groundLayer) != 0;
                bool isInvalid = ((1 << hitLayer) & currentInvalidPlacementLayer) != 0;

                if (isGround)
                {
                    bool isValidPlacement = IsPlacementValid(placementPosition);
                    SetGhostMaterial(ghostBuildingInstance, isValidPlacement ? validPlacementMaterial : invalidPlacementMaterial);

                    if (Input.GetMouseButtonDown(0) && isValidPlacement)
                    {
                        PlaceBuilding(placementPosition);
                    }
                }
                else if (isInvalid)
                {
                    // If the ray hits an invalid layer, show the ghost with the invalid material but don't allow placement.
                    SetGhostMaterial(ghostBuildingInstance, invalidPlacementMaterial);
                }
            }
            else
            {
                if (ghostBuildingInstance != null)
                {
                    ghostBuildingInstance.SetActive(false);
                }
            }

            if (Input.GetMouseButtonDown(1))
            {
                CancelBuildingPlacement();
            }
        }
    }

    // --- Public Methods (Called by UI or other scripts) ---

    public void StartPlacingBuilding(BuildingInfo info, LayerMask invalidLayer)
    {
        currentBuildingInfo = info;
        currentInvalidPlacementLayer = invalidLayer;
        isPlacingBuilding = true;

        if (ghostBuildingInstance != null)
        {
            Destroy(ghostBuildingInstance);
            ghostBuildingInstance = null;
        }

        Debug.Log("Started placing: " + info.buildingName);

        foreach (PanelToggleConfig config in panelToggleConfigs)
        {
            if (config.panelToToggle != null && config.panelToToggle.activeSelf)
            {
                config.panelToToggle.SetActive(false);
            }
        }
    }

    // --- Private Helper Methods ---

    private void PlaceBuilding(Vector3 position)
    {
        if (currentBuildingInfo != null && currentBuildingInfo.buildingPrefab != null)
        {
            GameObject newBuilding = Instantiate(currentBuildingInfo.buildingPrefab, position, Quaternion.identity);
            
            // Add the Building component to identify this as a placed building
            newBuilding.AddComponent<Building>();

            Debug.Log("Building placed: " + currentBuildingInfo.buildingName);
        }
    }

    private void CancelBuildingPlacement()
    {
        isPlacingBuilding = false;
        currentBuildingInfo = null;

        if (ghostBuildingInstance != null)
        {
            Destroy(ghostBuildingInstance);
            ghostBuildingInstance = null;
        }
        Debug.Log("Building placement cancelled.");
    }

    public void TogglePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);

            if (!panel.activeSelf && isPlacingBuilding)
            {
                CancelBuildingPlacement();
            }
        }
    }

    private void SetGhostMaterial(GameObject ghost, Material material)
    {
        Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            if (r.sharedMaterial != material)
            {
                r.material = material;
            }
        }
    }

    private void DisableGhostComponents(GameObject ghost)
    {
        Collider[] colliders = ghost.GetComponentsInChildren<Collider>(true);
        foreach (Collider c in colliders)
        {
            c.enabled = false;
        }

        MonoBehaviour[] scripts = ghost.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (MonoBehaviour s in scripts)
        {
            if (s != this)
            {
                s.enabled = false;
            }
        }

        Rigidbody[] rigidbodies = ghost.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rigidbodies)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }
    }

    // --- Placement Validation Logic ---
    private bool IsPlacementValid(Vector3 position)
    {
        if (currentBuildingInfo == null || currentBuildingInfo.buildingPrefab == null)
        {
            Debug.LogError("Current Building Info or its Prefab is null during validation.");
            return false;
        }

        Vector3 halfExtents = currentBuildingInfo.buildingBoundsExtents;

        // Fallback: If extents are zero, try to get from ghost's collider
        if (halfExtents == Vector3.zero)
        {
            Debug.LogWarning($"BuildingInfo for {currentBuildingInfo.buildingName} has zero bounds extents. Attempting to use ghost collider bounds. Please set 'Building Bounds Extents' in the BuildingInfo ScriptableObject for accurate validation.");
            if (ghostBuildingInstance != null)
            {
                Collider ghostCollider = ghostBuildingInstance.GetComponent<Collider>();
                if (ghostCollider != null)
                {
                    halfExtents = ghostCollider.bounds.extents;
                }
            }
            if (halfExtents == Vector3.zero) return true;
        }

        // Check for overlaps with other colliders
        Collider[] hitColliders = Physics.OverlapBox(
            position,
            halfExtents,
            Quaternion.identity
        );

        // Iterate through the colliders found and check if they belong to a building.
        foreach (Collider col in hitColliders)
        {
            // Check if the collider is attached to a game object with the 'Building' component.
            if (col.GetComponent<Building>() != null)
            {
                // Ensure we're not colliding with the ghost building itself
                if (col.transform.root != ghostBuildingInstance.transform.root)
                {
                    Debug.Log($"Overlap detected with existing building: {col.gameObject.name}");
                    return false;
                }
            }
        }

        return true;
    }
}
