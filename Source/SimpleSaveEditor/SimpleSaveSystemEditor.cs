using System;
using System.IO;
using FlaxEditor;
using FlaxEditor.Content.Settings;
using FlaxEditor.GUI;
using FlaxEditor.GUI.ContextMenu;
using FlaxEngine;

namespace SimpleSaveSystem.Editor;

public class SimpleSaveSystemEditor : EditorPlugin
{
    /// <inheritdoc />
    public override Type GamePluginType => typeof(SimpleSaveSystem);
    
    private string _settingsPath;
    private MainMenuButton _pluginButton;
    private ContextMenuButton _openButton;
    private JsonAsset _jsonAsset;

    public override void InitializeEditor()
    {
        _settingsPath = Path.Combine(Globals.ProjectContentFolder, "Settings", "SimpleSaveSettings.json");
        if (!File.Exists(_settingsPath))
        {
            FlaxEditor.Editor.SaveJsonAsset(_settingsPath, new SimpleSaveSettings());
        }
        _jsonAsset = Engine.GetCustomSettings("SimpleSaveSettings");
        if (_jsonAsset == null)
        {
            _jsonAsset = Content.LoadAsync<JsonAsset>(_settingsPath);
            GameSettings.SetCustomSettings("SimpleSaveSettings", _jsonAsset);
        }

        _pluginButton = Editor.UI.MainMenu.GetButton("Plugins") ?? Editor.UI.MainMenu.AddButton("Plugins");
        _openButton = _pluginButton.ContextMenu.AddButton("Open Simple Save Settings", OpenSettings);
    }

    private void OpenSettings()
    {
        Editor.ContentEditing.Open(_jsonAsset);
    }

    public override void DeinitializeEditor()
    {
        _openButton.Dispose();
        _openButton = null;
        _pluginButton = null;
        Content.UnloadAsset(_jsonAsset);
    }
}