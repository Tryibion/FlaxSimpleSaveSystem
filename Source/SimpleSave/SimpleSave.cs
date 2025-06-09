using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FlaxEditor.Content.Settings;
using FlaxEngine;
using FlaxEngine.Json;
using FlaxEngine.Utilities;

namespace SimpleSaveSystem;

/// <summary>
/// SimpleSave class.
/// </summary>
public static class SimpleSave
{
    private static string _rootSaveFolderName;
    private static string _defaultSaveFileName;
    private static bool _useEncryption;
    private static bool _useHash;
    private static string _encryptPassword;
    private static string _rootSaveDirectoryPath;
    private static string _defaultSaveFilePath;
    private static string _archiveSaveDirectoryPath;
    private static string _activeSlotName;

    private static bool _verboseLogging;

    /// <summary>
    /// The local path that the save data is stored at.
    /// </summary>
    public static string LocalPath
    {
        get
        {
#if FLAX_EDITOR
            var settings = GameSettings.Load();
            var path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), settings.CompanyName, settings.ProductName);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
#else
            if (!Directory.Exists(Globals.ProductLocalFolder))
                Directory.CreateDirectory(Globals.ProductLocalFolder);
            return Globals.ProductLocalFolder;
#endif
        }
    }

    // Save data name, value
    private static Dictionary<string, string> _defaultSaveData;
    // Slot name, file name, save data name, value
    private static Dictionary<string, Dictionary<string, Dictionary<string, string>>> _slotSaveData;

    private static CancellationTokenSource _cancellationToken;
    
    /// <summary>
    /// This fires when anything is saved.
    /// </summary>
    public static event Action Saved;

    /// <summary>
    /// This fires when a save fails.
    /// </summary>
    public static event Action SaveFailed;

    /// <summary>
    /// This fires when anything is loaded.
    /// </summary>
    public static event Action Loaded;
    
    /// <summary>
    /// This fires when a load fails.
    /// </summary>
    public static event Action LoadFailed;
    
    /// <summary>
    /// Fired when the Active Slot Name is Changed.
    /// </summary>
    public static event Action<string> ActiveSlotChanged;

    internal static void Initialize(SimpleSaveSettings settings)
    {
        Initialize(settings.RootSaveFolderName, settings.DefaultFileName, settings.VerboseLogging, settings.UseHash, settings.UseEncryption, settings.Password);
    }

    internal static void Initialize(string rootSaveFolderName, string defaultSaveFileName, bool verboseLogging = false, bool useHash = true, bool encrypt = false, string password = null)
    {
        _rootSaveFolderName = rootSaveFolderName;
        _rootSaveDirectoryPath = Path.Combine(LocalPath, _rootSaveFolderName);
        if (!Directory.Exists(_rootSaveDirectoryPath))
            Directory.CreateDirectory(_rootSaveDirectoryPath);
        _archiveSaveDirectoryPath = Path.Combine(_rootSaveDirectoryPath, "Archive");
        if (!Directory.Exists(_archiveSaveDirectoryPath))
            Directory.CreateDirectory(_archiveSaveDirectoryPath);
        
        _defaultSaveFileName = defaultSaveFileName;
        _defaultSaveFilePath = Path.Combine(_rootSaveDirectoryPath, $"{_defaultSaveFileName}.save");
        _useEncryption = encrypt;
        _useHash = useHash;
        _encryptPassword = password;
#if BUILD_RELEASE
        _verboseLogging = false;
#else
        _verboseLogging = verboseLogging;
#endif
        _cancellationToken = new CancellationTokenSource();
        _defaultSaveData = new Dictionary<string, string>();
        _slotSaveData = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        LogInfo("Initialized Simple Save");
    }

    public static async Task Deinitialize()
    {
        if (_cancellationToken != null)
            await _cancellationToken.CancelAsync();

        // Clear cached data
        _defaultSaveData?.Clear();
        _slotSaveData?.Clear();
        _defaultSaveData = null;
        _slotSaveData = null;
        _cancellationToken?.Dispose();
        LogInfo("Deinitialized Simple Save");
    }

#region Utility

    /// <summary>
    /// Get all the possible slot names. This is pulled from the folder names that exist in the save location.
    /// </summary>
    /// <returns>An array of slot names.</returns>
    public static string[] GetAllSlotNames()
    {
        var names = new List<string>();
        var subDirectories = Directory.GetDirectories(_rootSaveDirectoryPath);
        foreach (var directory in subDirectories)
        {
            var name = Path.GetFileName(directory);
            if (name.Equals("Archive", StringComparison.Ordinal))
                continue;
            names.Add(name);
        }
        return names.ToArray();
    }

    private static string ComputeHash(string data)
    {
        return ComputeHash(Encoding.UTF8.GetBytes(data));
    }
    
    private static string ComputeHash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(data);
        return Encoding.UTF8.GetString(hash);
    }

    private static bool VerifyHash(byte[] data, string expectedHash)
    {
        var actualHash = ComputeHash(data);
        return actualHash == expectedHash;
    }

#endregion

#region Active Slot

    /// <summary>
    /// Get or Set the active slot name.
    /// </summary>
    public static string ActiveSlot
    {
        get => _activeSlotName;
        set
        {
            _activeSlotName = value;
            ActiveSlotChanged?.Invoke(value);
        }
    }

    /// <summary>
    /// Adds data to the active slot.
    /// </summary>
    /// <param name="fileName">The file name to store data in for the active slot.</param>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    public static void AddToActiveSlotCache<T>(string fileName, string key, T value)
    {
        AddToSlotCache(_activeSlotName, fileName, key, value);
    }

    /// <summary>
    /// Try to get the data from the active slot.
    /// </summary>
    /// <param name="fileName">The file name that stores data for the active slot.</param>
    /// <param name="key">The data key.</param>
    /// <param name="value">The data value.</param>
    /// <typeparam name="T">The type of the value.</typeparam>
    /// <returns>True if value is found.</returns>
    public static bool TryGetFromActiveSlotCache<T>(string fileName, string key, out T value)
    {
        return TryGetFromSlotCache(_activeSlotName, fileName, key, out value);
    }

    /// <summary>
    /// Save the active slot.
    /// </summary>
    /// <returns>True if successful.</returns>
    public static bool SaveActiveSlot()
    {
        return SaveSlot(_activeSlotName);
    }

    /// <summary>
    /// Save a specific file in the active slot.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>True if successful.</returns>
    public static bool SaveActiveSlotFile(string fileName)
    {
        return SaveSlotFile(_activeSlotName, fileName);
    }

    /// <summary>
    /// Loads the active slot.
    /// </summary>
    /// <returns>True if successful.</returns>
    public static bool LoadActiveSlot()
    {
        return LoadSlot(_activeSlotName);
    }

    /// <summary>
    /// Loads a specific file in the active slot.
    /// </summary>
    /// <param name="fileName">The file name.</param>
    /// <returns>True if successful.</returns>
    public static bool LoadActiveSlotFile(string fileName)
    {
        return LoadSlotFile(_activeSlotName, fileName);
    }
    
#endregion

#region Cache Methods

    /// <summary>
    /// Adds a value to the default cache.
    /// </summary>
    /// <param name="key">The key identifier.</param>
    /// <param name="value">The value to add.</param>
    public static void AddToDefaultCache<T>(string key, T value)
    {
        var json = JsonSerializer.Serialize(value, typeof(T));
        _defaultSaveData[key] = json;
        LogInfo($"Key: {key}, Value: {value} added to default cache.");
    }

    /// <summary>
    /// Tries to get a value from the default cache.
    /// </summary>
    /// <param name="key">The key identifier.</param>
    /// <param name="value">The value if found, otherwise default.</param>
    /// <returns>True if successful.</returns>
    public static bool TryGetFromDefaultCache<T>(string key, out T value)
    {
        if (!_defaultSaveData.TryGetValue(key, out var jsonValue))
        {
            LogWarning($"Key: {key} does not exist in default cache.");
            value = default(T);
            return false;
        }

        value = JsonSerializer.Deserialize<T>(jsonValue);
        LogInfo($"Key: {key}, Value: {value} accessed from default cache.");
        return true;
    }

    /// <summary>
    /// Add a value to the slot cache. Creates and new slot and file if needed.
    /// </summary>
    /// <param name="slotName">The name of the slot.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="key">The key identifier.</param>
    /// <param name="value">The value to set.</param>
    public static void AddToSlotCache<T>(string slotName, string fileName, string key, T value)
    {
        var json = JsonSerializer.Serialize(value, typeof(T));
        if (!_slotSaveData.TryGetValue(slotName, out var slotData))
        {
            _slotSaveData[slotName] = new Dictionary<string, Dictionary<string, string>>();
            slotData = _slotSaveData[slotName];
        }
        if (!slotData.TryGetValue(fileName, out var fileData))
        {
            slotData[fileName] = new Dictionary<string, string>();
            fileData = slotData[fileName];
        }
        
        fileData[key] = json;
        LogInfo($"Key: {key}, Value: {value} added to Slot: {slotName}, File: {fileName} cache.");
    }
    
    /// <summary>
    /// Tries to get a value from a slot cache.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="key">The key identifier</param>
    /// <param name="value">The value if found. Default otherwise.</param>
    /// <returns>True if successfully found.</returns>
    public static bool TryGetFromSlotCache<T>(string slotName, string fileName, string key, out T value)
    {
        if (!_slotSaveData.TryGetValue(slotName, out var slotData))
        {
            value = default(T);
            return false;
        }
        if (!slotData.TryGetValue(fileName, out var fileData))
        {
            value = default(T);
            return false;
        }
        if (!fileData.TryGetValue(key, out var keyData))
        {
            LogWarning($"Key: {key} does not exist in Slot: {slotName}, File: {fileName}.");
            value = default(T);
            return false;
        }
        
        value = JsonSerializer.Deserialize<T>(keyData);
        LogInfo($"Key: {key}, Value: {value} accessed from Slot: {slotName}, File: {fileName} cache.");
        return true;
    }

#endregion

#region Save Methods

    private static void CallSaveEvents(bool saveStatus, bool suppressEvents)
    {
        if (suppressEvents) 
            return;

        if (saveStatus)
            Saved?.Invoke();
        else
            SaveFailed?.Invoke();
    }

    /// <summary>
    /// Save the default cache to disk.
    /// </summary>
    /// <param name="suppressEvents">Whether to suppress the save events.</param>
    /// <returns>True if successful.</returns>
    public static bool SaveDefault(bool suppressEvents = false)
    {
        if (!Directory.Exists(_rootSaveDirectoryPath))
            Directory.CreateDirectory(_rootSaveDirectoryPath);
        
        bool success = PerformSave(_defaultSaveData, _defaultSaveFilePath);
        CallSaveEvents(success, suppressEvents);
        return success;
    }

    /// <summary>
    /// Saves the default cache and all slot caches to disk.
    /// </summary>
    /// <returns>True if successful.</returns>
    public static bool SaveAll()
    {
        bool success = true;
        if (!SaveDefault(true))
            success = false;
        if (!SaveAllSlots(true))
            success = false;
        CallSaveEvents(success, false);
        return success;
    }

    /// <summary>
    /// Save all of the slot caches to disk.
    /// </summary>
    /// <param name="suppressEvents">Whether to suppress the save events</param>
    /// <returns>True if successful.</returns>
    public static bool SaveAllSlots(bool suppressEvents = false)
    {
        bool success = true;
        foreach (var slot in _slotSaveData.Keys)
        {
            if (!SaveSlot(slot, true))
                success = false;
        }
        CallSaveEvents(success, suppressEvents);
        return success;
    }

    /// <summary>
    /// Save a specific slot cache to disk.
    /// </summary>
    /// <param name="slotName">The name of the slot to save.</param>
    /// <param name="suppressEvents">Whether to suppress the save events.</param>
    /// <returns>True if successful.</returns>
    public static bool SaveSlot(string slotName, bool suppressEvents = false)
    {
        bool success = true;
        foreach (var fileData in _slotSaveData[slotName])
        {
            if (!SaveSlotFile(slotName, fileData.Key, true))
                success = false;
        }
        CallSaveEvents(success, suppressEvents);
        return success;
    }

    /// <summary>
    /// Saves a specific file in a slot to disk.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <param name="fileName">The file name.</param>
    /// <param name="suppressEvents">Whether to suppress the save events.</param>
    /// <returns>True if successful.</returns>
    public static bool SaveSlotFile(string slotName, string fileName, bool suppressEvents = false)
    {
        var slotDirectoryPath = Path.Combine(_rootSaveDirectoryPath, slotName);
        if (!Directory.Exists(slotDirectoryPath))
            Directory.CreateDirectory(slotDirectoryPath);
        
        bool success = PerformSave(_slotSaveData[slotName][fileName], Path.Combine(slotDirectoryPath, $"{fileName}.save"));
        CallSaveEvents(success, suppressEvents);
        return success;
    }
    
    private static bool PerformSave(Dictionary<string, string> data, string filePath)
    {
        // Move existing file into archive
        if (File.Exists(filePath))
        {
            var relativePath = Path.GetRelativePath(_rootSaveDirectoryPath, filePath);
            relativePath = Path.ChangeExtension(relativePath, ".bkp");
            var archivePath = Path.Combine(_archiveSaveDirectoryPath, relativePath);
            if (File.Exists(archivePath))
                File.Delete(archivePath);
            File.Move(filePath, archivePath);
        }

        try
        {
            // TODO: async file stream?
            using (var inputStream = new MemoryStream())
            using (var writer = new StreamWriter(inputStream, Encoding.UTF8))
            using (var outputStream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read))
            {
                // Write data to stream
                string saveData = JsonSerializer.Serialize(data);

                if (_useHash)
                {
                    var hash = ComputeHash(saveData);
                    saveData = saveData.Insert(0, $"{hash}\n");
                }
                writer.Write(saveData);
                writer.Flush();
                outputStream.Position = 0;
                // Encrypt or copy data into file
                if (_useEncryption)
                    EncryptStream(inputStream, outputStream, _encryptPassword);
                else
                {
                    inputStream.Position = 0;
                    CopyStream(inputStream, outputStream);
                }
            }
            
            LogInfo($"Data saved to {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Data failed to save to {filePath}, {ex.Message}");

            // Move archive back on save failure.
            var relativePath = Path.GetRelativePath(_rootSaveDirectoryPath, filePath);
            Path.ChangeExtension(relativePath, ".bkp");
            var archiveFile = Path.Combine(_archiveSaveDirectoryPath, relativePath);
            if (File.Exists(archiveFile))
            {
                File.Move(archiveFile, filePath);
                LogInfo($"Restoring archived save file {archiveFile} to {filePath}");
            }

            return false;
        }
    }

#endregion

#region Load Methods

    private static void CallLoadEvents(bool loadStatus, bool suppressEvents)
    {
        if (suppressEvents) 
            return;

        if (loadStatus)
            Loaded?.Invoke();
        else
            LoadFailed?.Invoke();
    }

    /// <summary>
    /// Loads the default and all slots into cache from disk.
    /// </summary>
    /// <returns>True if successful.</returns>
    public static bool LoadAll()
    {
        bool success = true;
        if (!LoadDefault(true))
            success = false;
        if (!LoadAllSlots(true))
            success = false;
        CallLoadEvents(success, false);
        return success;
    }
    
    /// <summary>
    /// Loads the default cache from disk.
    /// </summary>
    /// <param name="suppressEvents">Whether to suppress the load events.</param>
    /// <returns>True if successful.</returns>
    public static bool LoadDefault(bool suppressEvents = false)
    {
        bool success = PerformLoad(_defaultSaveData, _defaultSaveFilePath);
        CallLoadEvents(success, suppressEvents);
        return success;
    }

    /// <summary>
    /// Loads all the slots into cache from disk.
    /// </summary>
    /// <param name="suppressEvents">Whether to suppress the load events.</param>
    /// <returns>True if successful.</returns>
    public static bool LoadAllSlots(bool suppressEvents = false)
    {
        bool success = true;
        var subDirectories = Directory.GetDirectories(_rootSaveDirectoryPath);
        foreach (var directory in subDirectories)
        {
            var name = Path.GetDirectoryName(directory);
            _slotSaveData[name] = new Dictionary<string, Dictionary<string, string>>();
            if (!LoadSlot(name, true))
                success = false;
        }
        CallLoadEvents(success, suppressEvents);
        return success;
    }

    /// <summary>
    /// Loads a specific slot into cache from disk.
    /// </summary>
    /// <param name="slotName">The name of the slot.</param>
    /// <param name="suppressEvents">Whether to suppress the load events.</param>
    /// <returns>True if successful.</returns>
    public static bool LoadSlot(string slotName, bool suppressEvents = false)
    {
        bool success = true;
        var slotDirectoryPath = Path.Combine(_rootSaveDirectoryPath, slotName);
        if (!Directory.Exists(slotDirectoryPath))
        {
            CallLoadEvents(false, suppressEvents);
            return false;
        }

        if (!_slotSaveData.TryGetValue(slotName, out var slotData))
        {
            _slotSaveData[slotName] = new Dictionary<string, Dictionary<string, string>>();
            slotData = _slotSaveData[slotName];
        }

        slotData.Clear();
        var files = Directory.GetFiles(slotDirectoryPath, "*.save", SearchOption.AllDirectories);
        if (files.Length == 0)
            success = false;
  
        foreach (var file in files)
        {
            var fileName = Path.GetFileName(file);
            slotData[fileName] = new Dictionary<string, string>();
            if (!LoadSlotFile(slotName, fileName, true))
                success = false;
        }

        CallLoadEvents(success, suppressEvents);
        return success;
    }

    /// <summary>
    /// Loads a specific file from a slot into cache from disk.
    /// </summary>
    /// <param name="slotName">The name of the slot.</param>
    /// <param name="fileName">The name of the file.</param>
    /// <param name="suppressEvents">Whether to suppress the load events.</param>
    /// <returns>True if successful.</returns>
    public static bool LoadSlotFile(string slotName, string fileName, bool suppressEvents = false)
    {
        var slotFilePath = Path.Combine(_rootSaveDirectoryPath, slotName, $"{fileName}.save");

        // Create new cache values if they don't exist.
        if (!_slotSaveData.TryGetValue(slotName, out var slotData))
        {
            _slotSaveData[slotName] = new Dictionary<string, Dictionary<string, string>>();
            slotData = _slotSaveData[slotName];
        }
        if (!slotData.TryGetValue(fileName, out var fileData))
        {
            _slotSaveData[slotName][fileName] = new Dictionary<string, string>();
            fileData = _slotSaveData[slotName][fileName];
        }

        bool success = PerformLoad(fileData, slotFilePath);
        CallLoadEvents(success, suppressEvents);
        return success;
    }

    private static bool PerformLoad(Dictionary<string, string> data, string filePath)
    {
        if (!File.Exists(filePath))
        {
            LogWarning($"Missing save file at path {filePath}. Checking archive for file.");
            var relativePath = Path.GetRelativePath(_rootSaveDirectoryPath, filePath);
            relativePath = Path.ChangeExtension(relativePath, ".bkp");
            var archiveFile = Path.Combine(_archiveSaveDirectoryPath, relativePath);
            if (File.Exists(archiveFile))
            {
                File.Move(archiveFile, filePath);
                LogInfo($"Restoring archived save file {archiveFile} to {filePath}");
            }
            else
            {
                LogError($"No save file found at {filePath} or in archive.");
                return false;
            }
        }

        try
        {
            // TODO: Async file read?
            using (var inputStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var outputStream = new MemoryStream())
            using (var reader = new StreamReader(outputStream, Encoding.UTF8))
            {
                // Decrypt or copy data into file
                if (_useEncryption)
                    DecryptStream(inputStream, outputStream, _encryptPassword);
                else
                {
                    CopyStream(inputStream, outputStream);
                    outputStream.Position = 0;
                }

                var saveData = reader.ReadToEnd();
                if (_useHash)
                {
                    var endOfHashIndex = saveData.IndexOf('\n');
                    var hash = saveData.Substring(0, endOfHashIndex);
                    saveData = saveData.Substring(endOfHashIndex + 1);
                    var match = VerifyHash(Encoding.UTF8.GetBytes(saveData), hash);
                    if (!match)
                    {
                        throw new Exception("Hash verification failed on loaded save.");
                    }
                }

                var desData = JsonSerializer.Deserialize<Dictionary<string, string>>(saveData);
                data.Clear();
                data.AddRange(desData);
            }
            
            LogInfo($"Data loaded from {filePath}");
            return true;
        }
        catch (Exception ex)
        {
            LogError($"Failed to load save file {filePath}, {ex.Message}");
            return false;
        }
    }

#endregion

#region Remove Methods

    /// <summary>
    /// Clears the data in the default and slot caches.
    /// </summary>
    public static void ClearAllCache()
    {
        ClearDefaultCache();
        ClearAllSlotCache();
    }

    /// <summary>
    /// Clears the data from the default cache.
    /// </summary>
    public static void ClearDefaultCache()
    {
        _defaultSaveData.Clear();
    }

    /// <summary>
    /// Clears the data from all the slot caches.
    /// </summary>
    public static void ClearAllSlotCache()
    {
        _slotSaveData.Clear();
    }

    /// <summary>
    /// Clears the data from a specific slot.
    /// </summary>
    /// <param name="slotName">The name of the slot.</param>
    public static void ClearSlotCache(string slotName)
    {
        _slotSaveData[slotName].Clear();
    }

    /// <summary>
    /// Removes the default and all the slots from disk.
    /// </summary>
    public static void RemoveAll()
    {
        RemoveDefault();
        RemoveAllSlots();
    }

    /// <summary>
    /// Removes the default and default archive from disk.
    /// </summary>
    public static void RemoveDefault()
    {
        if (File.Exists(_defaultSaveFilePath))
            File.Delete(_defaultSaveFilePath);
        var archiveFile = Path.Combine(_archiveSaveDirectoryPath, $"{_defaultSaveFileName}.bkp");
        if (File.Exists(Path.Combine(archiveFile)))
            File.Delete(Path.Combine(archiveFile));
    }

    /// <summary>
    /// Removes all the slots from disk.
    /// </summary>
    public static void RemoveAllSlots()
    {
        var directories = Directory.GetDirectories(_rootSaveDirectoryPath);
        foreach (var directory in directories)
        {
            Directory.Delete(directory, true);
        }
    }

    /// <summary>
    /// Removes a specific slot from disk.
    /// </summary>
    /// <param name="slotName">The specific slot to remove.</param>
    public static void RemoveSlot(string slotName)
    {
        if (string.IsNullOrEmpty(slotName))
            return;
        
        var slotPath = Path.Combine(_rootSaveDirectoryPath, slotName);
        if (Directory.Exists(slotPath))
            Directory.Delete(slotPath, true);
        var archivePath = Path.Combine(_archiveSaveDirectoryPath, slotName);
        if (Directory.Exists(archivePath))
            Directory.Delete(archivePath, true);
    }

    /// <summary>
    /// Remove a specific file from a slot from disk.
    /// </summary>
    /// <param name="slotName">The slot name.</param>
    /// <param name="fileName">The file name.</param>
    public static void RemoveSlotFile(string slotName, string fileName)
    {
        if (string.IsNullOrEmpty(slotName) || string.IsNullOrEmpty(fileName))
            return;
        
        var slotFilePath = Path.Combine(_rootSaveDirectoryPath, slotName, $"{fileName}.save");
        if (File.Exists(slotFilePath))
            File.Delete(slotFilePath);
        var archiveFile = Path.Combine(_archiveSaveDirectoryPath, slotName, $"{fileName}.bkp");
        if (File.Exists(archiveFile))
            File.Delete(archiveFile);
    }

#endregion
    
#region Logging
    internal static void LogInfo(string message)
    {
        if (!_verboseLogging)
            return;

        Debug.Write(LogType.Info, $"[Simple Save]: {message}");
    }

    internal static void LogWarning(string message)
    {
        Debug.Write(LogType.Warning, $"[Simple Save]: {message}");
    }

    internal static void LogError(string message)
    {
        Debug.Write(LogType.Error, $"[Simple Save]: {message}");
    }
#endregion

#region Encryption
    private const int KeySize = 16;
    private const int SaltSize = 16;
    private const int IvSize = 16;
    private const int Iterations = 100;
    private const int BufferSize = 2048;
    
    private static string EncryptString(string text, string password)
    {
        byte[] encrypted = EncryptBytes(Encoding.UTF8.GetBytes(text), password);
        return Encoding.UTF8.GetString(encrypted);
    }
    
    private static string DecryptString(string text, string password)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        return Encoding.UTF8.GetString(DecryptBytes(bytes, password));
    }
    
    private static byte[] EncryptBytes(byte[] bytes, string password)
    {
        using (var input = new MemoryStream(bytes))
        using (var output = new MemoryStream())
        {
            EncryptStream(input, output, password);
            return output.ToArray();
        }
    }
    
    private static byte[] DecryptBytes(byte[] bytes, string password)
    {
        using (var input = new MemoryStream(bytes))
        using (var output = new MemoryStream())
        {
            DecryptStream(input, output, password);
            return output.ToArray();
        }
    }

    private static void EncryptStream(Stream input, Stream output, string password)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            byte[] salt = GenerateRandomBytes(SaltSize);
            DeriveKeyAndIV(password, salt, out byte[] key, out byte[] iv);

            aes.Key = key;
            aes.IV = iv;

            input.Position = 0;
            output.Write(salt, 0, salt.Length); // prepend salt
            using (CryptoStream cs = new(output, aes.CreateEncryptor(), CryptoStreamMode.Write))
                CopyStream(input, cs);
        }
    }

    private static void DecryptStream(Stream input, Stream output, string password)
    {
        byte[] salt = new byte[SaltSize];
        input.ReadExactly(salt, 0, salt.Length);
        DeriveKeyAndIV(password, salt, out byte[] key, out byte[] iv);

        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;

            using (CryptoStream cs = new(input, aes.CreateDecryptor(), CryptoStreamMode.Read))
                CopyStream(cs, output);
        }
        output.Position = 0;
    }

    private static void CopyStream(Stream input, Stream output)
    {
        byte[] buffer = new byte[BufferSize];
        int read;
        while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            output.Write(buffer, 0, read);
    }

    private static void DeriveKeyAndIV(string password, byte[] salt, out byte[] key, out byte[] iv)
    {
        if (string.IsNullOrEmpty(password))
        {
            // Use default key/IV if no password is provided
            key = Encoding.UTF8.GetBytes("HJSNbHOltg9iJMCv");
            iv = Encoding.UTF8.GetBytes("6D5A7556737FE0Js");
        }
        else
        {
            using var kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            key = kdf.GetBytes(KeySize);
            iv = kdf.GetBytes(IvSize);
        }
    }

    private static byte[] GenerateRandomBytes(int length)
    {
        byte[] bytes = new byte[length];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return bytes;
    }
#endregion
}
