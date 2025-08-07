using UnityEngine;
using TMPro;
using System.Collections.Generic;

[System.Serializable]
public class ResourceDisplay
{
    public ResourceType resourceType;
    public TMP_Text textComponent;
}

public class ResourceUI : MonoBehaviour
{
    [Tooltip("List of resources to display and their corresponding TextMeshPro UI components.")]
    public List<ResourceDisplay> resourceDisplays = new List<ResourceDisplay>();

    private ResourceManager resourceManager;

    private void Start()
    {
        resourceManager = ResourceManager.Instance;
        if (resourceManager == null)
        {
            Debug.LogError("ResourceManager not found! Make sure it exists in the scene.");
        }
    }

    private void Update()
    {
        if (resourceManager != null)
        {
            // Iterate through the list to update each resource's text component
            foreach (var display in resourceDisplays)
            {
                if (display.textComponent != null)
                {
                    int amount = resourceManager.GetResourceAmount(display.resourceType);
                    display.textComponent.text = display.resourceType.ToString() + ": " + amount;
                }
            }
        }
    }
}
