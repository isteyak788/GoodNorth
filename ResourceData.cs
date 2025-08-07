using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class ResourceGenerationBySeason
{
    public Season season;
    [Tooltip("The multiplier for the base generation amount during this season. (e.g., 2.0 for double, 0.5 for half)")]
    public float generationMultiplier = 1.0f;
}

[CreateAssetMenu(fileName = "New Resource Data", menuName = "Resources/Resource Data")]
public class ResourceData : ScriptableObject
{
    public string resourceName;
    public string resourceDescription;
    public Sprite resourceIcon;

    public ResourceType resourceType;
    
    [Tooltip("The number of resources generated once when the building is first created.")]
    public int initialGenerationAmount = 0; // The new field

    [Tooltip("The number of in-game days between each resource generation.")]
    public int generationIntervalDays = 1;

    [Tooltip("The base number of resources generated per interval, before seasonal multipliers.")]
    public int generationPerInterval = 10;

    [Tooltip("Define resource generation multipliers for each season. The number of entries should match the number of seasons.")]
    public List<ResourceGenerationBySeason> generationBySeason = new List<ResourceGenerationBySeason>();
}
