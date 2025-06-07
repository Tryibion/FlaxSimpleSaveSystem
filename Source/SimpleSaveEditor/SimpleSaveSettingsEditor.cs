using System;
using System.Collections.Generic;
using System.IO;
using FlaxEditor.CustomEditors;
using FlaxEditor.CustomEditors.Editors;
using FlaxEditor.CustomEditors.Elements;
using FlaxEngine;

namespace SimpleSaveSystem.Editor;

/// <summary>
/// SimpleSaveSettingsEditor class.
/// </summary>
[CustomEditor(typeof(SimpleSaveSettings)), DefaultEditor]
public class SimpleSaveSettingsEditor : GenericEditor
{
    private ButtonElement _removeAllButton;
    private ButtonElement _removeDefaultButton;
    private ButtonElement _removeAllSlotsButton;
    private TextBoxElement _slotNameTextBox;
    private ButtonElement _removeSlotButton;
    private ButtonElement _openSavePathButton;
    
    public override void Initialize(LayoutElementsContainer layout)
    {
        var settingsGroup = layout.Group("Settings");
        base.Initialize(settingsGroup);
        layout.Space(2);

        var actionGroup = layout.Group("Actions");
        actionGroup.Space(2);
        _removeAllButton = actionGroup.Button("Remove All Save Data");
        _removeAllButton.Button.Clicked += OnRemoveAllButtonClicked;
        actionGroup.Space(2);
        _removeDefaultButton = actionGroup.Button("Remove Default Save Data");
        _removeDefaultButton.Button.Clicked += OnRemoveDefaultButtonClicked;
        actionGroup.Space(2);
        _removeAllSlotsButton = actionGroup.Button("Remove All Slots Save Data");
        _removeAllSlotsButton.Button.Clicked += OnRemoveAllSlotsButtonClicked;
        actionGroup.Space(2);
        actionGroup.Label("Remove a specific slot:");
        _slotNameTextBox = actionGroup.TextBox();
        _slotNameTextBox.TextBox.WatermarkText = "Enter slot name here...";
        _removeSlotButton = actionGroup.Button("Remove Slot");
        _removeSlotButton.Button.Clicked += OnRemoveSlotButtonClicked;
        
        actionGroup.Space(5);
        _openSavePathButton = actionGroup.Button("Open Save Path");
        _openSavePathButton.Button.Clicked += OnOpenSavePath;

    }

    private void OnOpenSavePath()
    {
        string saveFolder = string.Empty;
        if (Values[0] is SimpleSaveSettings settings)
        {
            saveFolder = settings.RootSaveFolderName;
        }
        var path = Path.Combine(SimpleSave.LocalPath, saveFolder);
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        FileSystem.ShowFileExplorer(path);
    }

    private void OnRemoveSlotButtonClicked()
    {
        SimpleSave.RemoveSlot(_slotNameTextBox.Text);
    }

    private void OnRemoveAllSlotsButtonClicked()
    {
        SimpleSave.RemoveAllSlots();
    }

    private void OnRemoveAllButtonClicked()
    {
        SimpleSave.RemoveAll();
    }
    
    private void OnRemoveDefaultButtonClicked()
    {
        SimpleSave.RemoveDefault();
    }

    protected override void Deinitialize()
    {
        _removeAllButton.Button.Clicked -= OnRemoveAllButtonClicked;
        _removeDefaultButton.Button.Clicked -= OnRemoveDefaultButtonClicked;
        _removeAllSlotsButton.Button.Clicked -= OnRemoveAllSlotsButtonClicked;
        _removeSlotButton.Button.Clicked -= OnRemoveSlotButtonClicked;
        _openSavePathButton.Button.Clicked -= OnOpenSavePath;
        base.Deinitialize();
    }
}
