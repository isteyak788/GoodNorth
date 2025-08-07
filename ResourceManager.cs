using System.Collections.Generic;
using UnityEngine;

public enum ResourceType
{
    Food,
    Wood,
    Stone,
    Gold,
    Human,
    // Add more as needed
}

public class ResourceManager : MonoBehaviour
{
    public static ResourceManager Instance { get; private set; }

    private Dictionary<ResourceType, int> resources = new Dictionary<ResourceType, int>();
    public IReadOnlyDictionary<ResourceType, int> Resources => resources;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        foreach (ResourceType type in System.Enum.GetValues(typeof(ResourceType)))
        {
            resources[type] = 0;
        }
    }

    public void AddResource(ResourceType type, int amount)
    {
        if (resources.ContainsKey(type))
        {
            resources[type] += amount;
            Debug.Log($"Added {amount} of {type}. Total: {resources[type]}");
        }
    }

    public int GetResourceAmount(ResourceType type)
    {
        return resources.GetValueOrDefault(type, 0);
    }
}
