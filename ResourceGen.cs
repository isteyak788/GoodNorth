using UnityEngine;
using System.Collections.Generic;

public class ResourceGen : MonoBehaviour
{
    public ResourceData data;

    private ResourceManager resourceManager;
    private int _daysSinceLastGeneration = 0;
    private bool _hasGeneratedInitially = false;

    private void Start()
    {
        resourceManager = ResourceManager.Instance;

        if (data == null || resourceManager == null || TimeManager.Instance == null)
        {
            Debug.LogError("ResourceGen is missing a critical reference. Cannot generate resources.");
            enabled = false;
            return;
        }

        // Perform initial generation if it hasn't been done yet and the amount is not zero
        if (!_hasGeneratedInitially && data.initialGenerationAmount > 0)
        {
            GenerateInitialResources();
        }

        TimeManager.Instance.OnNewDay += OnNewDayHandler;
    }

    private void OnDisable()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.OnNewDay -= OnNewDayHandler;
        }
    }

    private void OnNewDayHandler(int currentDay, int currentMonth, int currentYear)
    {
        _daysSinceLastGeneration++;

        if (_daysSinceLastGeneration >= data.generationIntervalDays)
        {
            _daysSinceLastGeneration = 0;
            GenerateResources();
        }
    }

    // New method for one-time initial generation
    private void GenerateInitialResources()
    {
        resourceManager.AddResource(data.resourceType, data.initialGenerationAmount);
        Debug.Log($"Initial generation of {data.initialGenerationAmount} {data.resourceType} from {gameObject.name}");
        _hasGeneratedInitially = true;
    }

    private void GenerateResources()
    {
        Season currentSeason = TimeManager.Instance.GetCurrentSeason();

        // Start with the base generation per interval
        int finalGenerationAmount = data.generationPerInterval;
        
        // Find the correct generation multiplier for the current season
        float generationMultiplierForSeason = 1.0f; // Default to 1.0 if not found
        foreach (var seasonGenData in data.generationBySeason)
        {
            if (seasonGenData.season == currentSeason)
            {
                generationMultiplierForSeason = seasonGenData.generationMultiplier;
                break;
            }
        }
        
        // Apply the multiplier to the base amount
        finalGenerationAmount = Mathf.RoundToInt(data.generationPerInterval * generationMultiplierForSeason);
        
        // Add the calculated resource amount to the manager
        resourceManager.AddResource(data.resourceType, finalGenerationAmount);
    }
}
