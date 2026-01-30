using System.IO;
using System.Text.Json;

namespace GcodeAnalyzer;

/// <summary>
/// Represents a single G-code macro.
/// </summary>
public class GcodeMacro
{
    public int KeyNumber { get; set; } // 1-9 and 0
    public string Name { get; set; } = string.Empty;
    public string GcodeContent { get; set; } = string.Empty;
}

/// <summary>
/// Manages persistent storage of G-code macros.
/// </summary>
public static class MacroSettings
{
    private static readonly string SettingsDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "GcodeAnalyzer");
    
    private static readonly string SettingsFilePath = Path.Combine(SettingsDirectory, "macros.json");

    /// <summary>
    /// Loads macros from persistent storage.
    /// </summary>
    public static List<GcodeMacro> LoadMacros()
    {
        try
        {
            if (!File.Exists(SettingsFilePath))
            {
                return GetDefaultMacros();
            }

            var json = File.ReadAllText(SettingsFilePath);
            var macros = JsonSerializer.Deserialize<List<GcodeMacro>>(json);
            
            return macros ?? GetDefaultMacros();
        }
        catch
        {
            return GetDefaultMacros();
        }
    }

    /// <summary>
    /// Saves macros to persistent storage.
    /// </summary>
    public static void SaveMacros(List<GcodeMacro> macros)
    {
        try
        {
            if (!Directory.Exists(SettingsDirectory))
            {
                Directory.CreateDirectory(SettingsDirectory);
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            var json = JsonSerializer.Serialize(macros, options);
            File.WriteAllText(SettingsFilePath, json);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to save macros: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets default macro definitions.
    /// </summary>
    private static List<GcodeMacro> GetDefaultMacros()
    {
        return
        [
            new GcodeMacro { KeyNumber = 1, Name = "Initialize", GcodeContent = "G21 G90\nG17" },
            new GcodeMacro { KeyNumber = 2, Name = "Home All", GcodeContent = "G28" },
            new GcodeMacro { KeyNumber = 3, Name = "Spindle On", GcodeContent = "M3 S10000" },
            new GcodeMacro { KeyNumber = 4, Name = "Spindle Off", GcodeContent = "M5" },
            new GcodeMacro { KeyNumber = 5, Name = "Rapid Retract", GcodeContent = "G0 Z10" },
            new GcodeMacro { KeyNumber = 6, Name = "Coolant On", GcodeContent = "M8" },
            new GcodeMacro { KeyNumber = 7, Name = "Coolant Off", GcodeContent = "M9" },
            new GcodeMacro { KeyNumber = 8, Name = "Program End", GcodeContent = "M5\nM9\nG28\nM30" },
            new GcodeMacro { KeyNumber = 9, Name = "Dwell 2s", GcodeContent = "G4 P2" },
            new GcodeMacro { KeyNumber = 0, Name = "Comment", GcodeContent = "(Insert comment here)" }
        ];
    }
}
