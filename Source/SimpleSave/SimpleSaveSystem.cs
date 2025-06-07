using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FlaxEngine;

namespace SimpleSaveSystem;

/// <summary>
/// SimpleSave GamePlugin.
/// </summary>
public class SimpleSaveSystem : GamePlugin
{
    private SimpleSaveSettings _settings;
    
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        var simpleSaveSettingsAsset = Engine.GetCustomSettings("SimpleSaveSettings");
        if (simpleSaveSettingsAsset == null)
            SimpleSave.LogError("Cannot find assigned SimpleSaveSettings in custom settings.");
        else
        {
            _settings = simpleSaveSettingsAsset.GetInstance<SimpleSaveSettings>();
            SimpleSave.Initialize(_settings);
        }
    }

    /// <inheritdoc/>
    public override async void Deinitialize()
    {
        try
        {
            await SimpleSave.Deinitialize();
        }
        catch (Exception ex)
        {
            SimpleSave.LogWarning($"Exception thrown when deinitializing: {ex.Message}");
        }
        
        base.Deinitialize();
    }
}