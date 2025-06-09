using System;
using System.Collections.Generic;
using FlaxEngine;

namespace SimpleSaveSystem;

/// <summary>
/// SimpleSaveSettings class.
/// </summary>
public class SimpleSaveSettings : SettingsBase
{
    /// <summary>
    /// The default file name to use.
    /// </summary>
    public string DefaultFileName = "Default";
    
    /// <summary>
    /// The root save folder name.
    /// </summary>
    public string RootSaveFolderName = "Save";
    
    /// <summary>
    /// Whether to encrypt the save data.
    /// </summary>
    public bool UseEncryption = false;
    
    /// <summary>
    /// The password used for encryption.
    /// </summary>
    public string Password = "password";
    
    /// <summary>
    /// Whether to add and use a hash to check for file changes.
    /// </summary>
    public bool UseHash = true;
    
    /// <summary>
    /// Whether to use verbose logging. Automatically disabled in release builds.
    /// </summary>
    public bool VerboseLogging = true;
}
