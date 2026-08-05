using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using ERPiData;
using ERPiData.Models.Core;

namespace ERPiApp;

public static class AppConfig
{
    public static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ERPiApp");

    public static string BazeDir => Path.Combine(AppDataDir, "Baze");

    public static string DefaultDbPath => Path.Combine(BazeDir, "erpi.db");

    private static string? _dbPath = null;

    public static string DbPath
    {
        get
        {
            if (_dbPath == null)
            {
                Directory.CreateDirectory(BazeDir);
                var baze = Directory.GetFiles(BazeDir, "*.db");
                _dbPath = baze.Length > 0 ? baze[0] : DefaultDbPath;
            }
            return _dbPath;
        }
        set
        {
            _dbPath = value;
        }
    }

    private static int? _activeFirmaId;
    public static int? ActiveFirmaId
    {
        get => _activeFirmaId;
        set => _activeFirmaId = value;
    }

    private static int? _activeGodina;
    public static int? ActiveGodina
    {
        get => _activeGodina;
        set => _activeGodina = value;
    }

    private static int? _activeMesec;
    public static int? ActiveMesec
    {
        get => _activeMesec;
        set => _activeMesec = value;
    }

    // ── UI PREFERENCES (JSON perzistencija) ──
    private static string SettingsPath => Path.Combine(AppDataDir, "ui_settings.json");
    private static UiSettings? _settings;

    private static UiSettings Settings
    {
        get
        {
            if (_settings == null)
            {
                if (File.Exists(SettingsPath))
                {
                    try
                    {
                        var json = File.ReadAllText(SettingsPath);
                        _settings = JsonSerializer.Deserialize<UiSettings>(json) ?? new UiSettings();
                    }
                    catch
                    {
                        _settings = new UiSettings();
                    }
                }
                else
                {
                    _settings = new UiSettings();
                }
            }
            return _settings;
        }
    }

    private static void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(AppDataDir);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch { /* ne blokira rad aplikacije */ }
    }

    /// <summary>Da li prikazati info traku sa upozorenjem o podrazumevanoj lozinci. Po defaultu: false.</summary>
    public static bool PrikaziInfoTraku
    {
        get => Settings.PrikaziInfoTraku;
        set { Settings.PrikaziInfoTraku = value; SaveSettings(); }
    }
}

internal class UiSettings
{
    public bool PrikaziInfoTraku { get; set; } = false;
}
