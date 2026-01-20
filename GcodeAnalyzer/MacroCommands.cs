using System.Windows.Input;

namespace GcodeAnalyzer;

/// <summary>
/// Static commands for macro insertion.
/// </summary>
public static class MacroCommands
{
    public static readonly RoutedUICommand InsertMacro1 = new(
        "Insert Macro 1",
        "InsertMacro1",
        typeof(MacroCommands),
        [new KeyGesture(Key.D1, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro2 = new(
        "Insert Macro 2",
        "InsertMacro2",
        typeof(MacroCommands),
        [new KeyGesture(Key.D2, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro3 = new(
        "Insert Macro 3",
        "InsertMacro3",
        typeof(MacroCommands),
        [new KeyGesture(Key.D3, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro4 = new(
        "Insert Macro 4",
        "InsertMacro4",
        typeof(MacroCommands),
        [new KeyGesture(Key.D4, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro5 = new(
        "Insert Macro 5",
        "InsertMacro5",
        typeof(MacroCommands),
        [new KeyGesture(Key.D5, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro6 = new(
        "Insert Macro 6",
        "InsertMacro6",
        typeof(MacroCommands),
        [new KeyGesture(Key.D6, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro7 = new(
        "Insert Macro 7",
        "InsertMacro7",
        typeof(MacroCommands),
        [new KeyGesture(Key.D7, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro8 = new(
        "Insert Macro 8",
        "InsertMacro8",
        typeof(MacroCommands),
        [new KeyGesture(Key.D8, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro9 = new(
        "Insert Macro 9",
        "InsertMacro9",
        typeof(MacroCommands),
        [new KeyGesture(Key.D9, ModifierKeys.Control)]);

    public static readonly RoutedUICommand InsertMacro0 = new(
        "Insert Macro 0",
        "InsertMacro0",
        typeof(MacroCommands),
        [new KeyGesture(Key.D0, ModifierKeys.Control)]);
}
