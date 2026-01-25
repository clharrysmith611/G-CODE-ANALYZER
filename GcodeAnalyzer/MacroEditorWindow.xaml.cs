using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GcodeAnalyzer;

/// <summary>
/// Window for editing G-code macros.
/// </summary>
public partial class MacroEditorWindow : Window
{
    private List<GcodeMacro> _macros;
    private GcodeMacro? _selectedMacro;
    private bool _unsavedChanges;

    public MacroEditorWindow()
    {
        InitializeComponent();
        LoadMacros();
    }

    private void LoadMacros()
    {
        _macros = MacroSettings.LoadMacros();
        
        // Ensure we have all 10 macros (1-9 and 0)
        for (int i = 1; i <= 9; i++)
        {
            if (!_macros.Any(m => m.KeyNumber == i))
            {
                _macros.Add(new GcodeMacro { KeyNumber = i, Name = $"Macro {i}", GcodeContent = "" });
            }
        }
        if (!_macros.Any(m => m.KeyNumber == 0))
        {
            _macros.Add(new GcodeMacro { KeyNumber = 0, Name = "Macro 0", GcodeContent = "" });
        }
        
        // Sort by key number
        _macros = _macros.OrderBy(m => m.KeyNumber == 0 ? 10 : m.KeyNumber).ToList();
        
        MacroListBox.ItemsSource = _macros;
        MacroListBox.SelectedIndex = 0;
        
        _unsavedChanges = false;
    }

    private void MacroListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (MacroListBox.SelectedItem is not GcodeMacro macro)
        {
            return;
        }

        // Save previous macro if modified
        SaveCurrentMacroToList();

        _selectedMacro = macro;
        TxtKeyNumber.Text = macro.KeyNumber.ToString();
        TxtMacroName.Text = macro.Name;
        TxtGcodeContent.Text = macro.GcodeContent;
    }

    private void SaveCurrentMacroToList()
    {
        if (_selectedMacro is null)
        {
            return;
        }

        // Update the macro in the list
        var name = TxtMacroName.Text.Trim();
        var content = TxtGcodeContent.Text;

        if (_selectedMacro.Name != name || _selectedMacro.GcodeContent != content)
        {
            _selectedMacro.Name = name;
            _selectedMacro.GcodeContent = content;
            _unsavedChanges = true;
            
            // Refresh the list to show updated name
            MacroListBox.Items.Refresh();
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        SaveMacros();
    }

    private void SaveMacros()
    {
        try
        {
            // Save current macro before saving all
            SaveCurrentMacroToList();

            MacroSettings.SaveMacros(_macros);
            _unsavedChanges = false;
            
            MessageBox.Show("Macros saved successfully!", "Success", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving macros: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void BtnResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show(
            "This will reset all macros to their default values. Any custom macros will be lost.\n\nAre you sure you want to continue?",
            "Reset to Defaults",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result == MessageBoxResult.Yes)
        {
            // Delete the settings file
            try
            {
                var settingsPath = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "GcodeAnalyzer", "macros.json");
                
                if (System.IO.File.Exists(settingsPath))
                {
                    System.IO.File.Delete(settingsPath);
                }
                
                LoadMacros();
                _unsavedChanges = false;
                
                MessageBox.Show("Macros have been reset to defaults.", "Reset Complete", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting macros: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }

    protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Check for unsaved changes
        SaveCurrentMacroToList();
        
        if (_unsavedChanges)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SaveMacros();
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }

        base.OnClosing(e);
    }
}
