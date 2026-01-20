using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace GcodeAnalyzer;

/// <summary>
/// Represents a line number with optional error highlighting.
/// </summary>
public class LineNumberItem : INotifyPropertyChanged
{
    private Brush _color = Brushes.Gray;
    private Brush _background = Brushes.Transparent;
    
    public int Number { get; set; }
    
    public Brush Color
    {
        get => _color;
        set
        {
            if (_color != value)
            {
                _color = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Color)));
            }
        }
    }
    
    public Brush Background
    {
        get => _background;
        set
        {
            if (_background != value)
            {
                _background = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Background)));
            }
        }
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
}

/// <summary>
/// G-code editor window with syntax error highlighting and line numbers.
/// </summary>
public partial class GcodeEditorWindow : Window
{
    private string? _filePath;
    private bool _isModified;
    private bool _isUpdatingText;
    private readonly GcodeParser _parser = new();
    private HashSet<int> _errorLines = [];
    private HashSet<int> _duplicateReferenceLines = []; // Lines that are referenced by duplicate errors (shown in blue)
    private int _currentSimulationLine = -1; // Track current line from 3D simulator
    private ObservableCollection<LineNumberItem> _lineNumberItems = new(); // Use observable collection for better performance
    private List<GcodeMacro> _macros = []; // Loaded macros

    private static readonly Brush NormalLineColor = new SolidColorBrush(Color.FromRgb(158, 158, 158));
    private static readonly Brush ErrorLineColor = new SolidColorBrush(Color.FromRgb(255, 68, 68));
    private static readonly Brush ErrorLineBackground = new SolidColorBrush(Color.FromArgb(60, 255, 68, 68));
    private static readonly Brush DuplicateReferenceLineColor = new SolidColorBrush(Color.FromRgb(68, 138, 255)); // Blue
    private static readonly Brush DuplicateReferenceLineBackground = new SolidColorBrush(Color.FromArgb(60, 68, 138, 255)); // Semi-transparent blue
    private static readonly Brush SimulationLineColor = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Gold/Yellow
    private static readonly Brush SimulationLineBackground = new SolidColorBrush(Color.FromArgb(60, 255, 215, 0)); // Semi-transparent gold

    // Error message patterns for filtering (same as MainWindow)
    private const string RedundantMovePattern = "Redundant move:";
    private const string DuplicateToolpathPattern = "Duplicate toolpath:";

    // Event to notify main window about preview request
    public event EventHandler? PreviewRequested;

    /// <summary>
    /// Gets or sets whether to hide redundant move errors.
    /// </summary>
    public bool HideRedundantMoves { get; set; }

    /// <summary>
    /// Gets or sets whether to hide duplicate toolpath errors.
    /// </summary>
    public bool HideDuplicatePaths { get; set; }

    /// <summary>
    /// Gets whether the content was modified and saved.
    /// </summary>
    public bool ContentWasModified { get; private set; }

    /// <summary>
    /// Gets the current editor content without saving.
    /// </summary>
    public string GetCurrentContent()
    {
        return EditorTextBox.Text;
    }

    /// <summary>
    /// Triggers a preview of the current changes without saving the file.
    /// </summary>
    private void PreviewChanges()
    {
        try
        {
            // Refresh error highlighting first
            RefreshErrorHighlighting();
            
            // Notify the main window to update its views
            // Use Dispatcher to ensure we're on the UI thread and handle any timing issues
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PreviewRequested?.Invoke(this, EventArgs.Empty);
            }), System.Windows.Threading.DispatcherPriority.Normal);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during preview: {ex.Message}", "Preview Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    /// <summary>
    /// Updates the current simulation line marker (called from 3D viewer).
    /// </summary>
    public void SetCurrentSimulationLine(int lineNumber)
    {
        if (_currentSimulationLine == lineNumber) return; // Already set
        
        int previousLine = _currentSimulationLine;
        _currentSimulationLine = lineNumber;
        
        // Only update the affected line items instead of rebuilding the entire list
        UpdateSimulationLineHighlight(previousLine, lineNumber);
        
        // Scroll to keep the current line centered
        ScrollToLineCenter(lineNumber);
    }

    /// <summary>
    /// Scrolls the editor to keep the specified line as close to vertical center as possible.
    /// </summary>
    private void ScrollToLineCenter(int lineNumber)
    {
        if (lineNumber <= 0) return;
        
        // Use Dispatcher to ensure the UI has updated before scrolling
        Dispatcher.BeginInvoke(new Action(() =>
        {
            try
            {
                // Get line index (0-based)
                int lineIndex = lineNumber - 1;
                if (lineIndex < 0 || lineIndex >= EditorTextBox.LineCount)
                {
                    return;
                }
                
                // Get the scroll viewer for the editor
                var scrollViewer = FindVisualChild<ScrollViewer>(EditorTextBox);
                if (scrollViewer == null) return;
                
                // Get the character index for the start of the line
                int charIndex = EditorTextBox.GetCharacterIndexFromLineIndex(lineIndex);
                if (charIndex < 0) return;
                
                // Get the rect for the character position to get actual vertical position
                var rect = EditorTextBox.GetRectFromCharacterIndex(charIndex);
                
                // Calculate viewport height
                double viewportHeight = scrollViewer.ViewportHeight;
                
                // Calculate offset to center the line
                // The rect.Top is relative to the TextBox content, not the viewport
                double targetOffset = scrollViewer.VerticalOffset + rect.Top - (viewportHeight / 2);
                
                // Clamp to valid range
                double maxOffset = scrollViewer.ExtentHeight - viewportHeight;
                targetOffset = Math.Max(0, Math.Min(targetOffset, maxOffset));
                
                // Scroll to the calculated position
                scrollViewer.ScrollToVerticalOffset(targetOffset);
            }
            catch
            {
                // Silently handle any errors
            }
        }), System.Windows.Threading.DispatcherPriority.Background);
    }
    
    /// <summary>
    /// Finds a visual child of a specific type in the visual tree.
    /// </summary>
    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        if (parent == null) return null;
        
        int childCount = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < childCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T typedChild)
            {
                return typedChild;
            }
            
            var result = FindVisualChild<T>(child);
            if (result != null)
            {
                return result;
            }
        }
        
        return null;
    }

    /// <summary>
    /// Clears the current simulation line marker.
    /// </summary>
    public void ClearSimulationLine()
    {
        if (_currentSimulationLine == -1) return; // Already cleared
        
        int previousLine = _currentSimulationLine;
        _currentSimulationLine = -1;
        
        // Only update the previously highlighted line
        UpdateSimulationLineHighlight(previousLine, -1);
    }

    /// <summary>
    /// Updates only the simulation line highlighting without rebuilding the entire list.
    /// </summary>
    private void UpdateSimulationLineHighlight(int previousLine, int newLine)
    {
        // Update the previous line (if valid)
        if (previousLine > 0 && previousLine <= _lineNumberItems.Count)
        {
            var prevItem = _lineNumberItems[previousLine - 1];
            bool isError = _errorLines.Contains(previousLine);
            bool isDuplicateReference = _duplicateReferenceLines.Contains(previousLine);
            
            if (isError)
            {
                prevItem.Color = ErrorLineColor;
                prevItem.Background = ErrorLineBackground;
            }
            else if (isDuplicateReference)
            {
                prevItem.Color = DuplicateReferenceLineColor;
                prevItem.Background = DuplicateReferenceLineBackground;
            }
            else
            {
                prevItem.Color = NormalLineColor;
                prevItem.Background = Brushes.Transparent;
            }
        }
        
        // Update the new line (if valid)
        if (newLine > 0 && newLine <= _lineNumberItems.Count)
        {
            var newItem = _lineNumberItems[newLine - 1];
            newItem.Color = SimulationLineColor;
            newItem.Background = SimulationLineBackground;
        }
    }

    public GcodeEditorWindow()
    {
        InitializeComponent();
        
        // Set the ItemsSource once during initialization
        LineNumbersPanel.ItemsSource = _lineNumberItems;
        
        // Load macros
        LoadMacros();
    }

    /// <summary>
    /// Loads macros from persistent storage.
    /// </summary>
    private void LoadMacros()
    {
        _macros = MacroSettings.LoadMacros();
    }

    /// <summary>
    /// Loads a G-code file into the editor.
    /// </summary>
    public void LoadFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            MessageBox.Show($"File not found: {filePath}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        try
        {
            _filePath = filePath;
            var content = File.ReadAllText(filePath);
            
            // Set content without triggering modified flag
            _isUpdatingText = true;
            EditorTextBox.Text = content;
            _isUpdatingText = false;

            _isModified = false;
            
            // Analyze for errors first, then update line numbers with highlighting
            RefreshErrorHighlighting();
            UpdateStatusBar();

            Title = $"G-code Editor - {Path.GetFileName(filePath)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error loading file: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private string[] GetEditorLines()
    {
        return EditorTextBox.Text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
    }

    private void UpdateLineNumbers()
    {
        var lines = GetEditorLines();
        int lineCount = lines.Length;
        if (lineCount == 0) lineCount = 1;
        
        // Clear and rebuild the collection
        _lineNumberItems.Clear();
        
        for (int i = 1; i <= lineCount; i++)
        {
            bool isError = _errorLines.Contains(i);
            bool isDuplicateReference = _duplicateReferenceLines.Contains(i);
            bool isCurrentSimulation = i == _currentSimulationLine;
            
            // Simulation line takes priority over all other highlighting
            if (isCurrentSimulation)
            {
                _lineNumberItems.Add(new LineNumberItem
                {
                    Number = i,
                    Color = SimulationLineColor,
                    Background = SimulationLineBackground
                });
            }
            else if (isError)
            {
                _lineNumberItems.Add(new LineNumberItem
                {
                    Number = i,
                    Color = ErrorLineColor,
                    Background = ErrorLineBackground
                });
            }
            else if (isDuplicateReference)
            {
                _lineNumberItems.Add(new LineNumberItem
                {
                    Number = i,
                    Color = DuplicateReferenceLineColor,
                    Background = DuplicateReferenceLineBackground
                });
            }
            else
            {
                _lineNumberItems.Add(new LineNumberItem
                {
                    Number = i,
                    Color = NormalLineColor,
                    Background = Brushes.Transparent
                });
            }
        }
    }

    private void UpdateStatusBar()
    {
        TxtFilePath.Text = _filePath ?? "New File";
        TxtModified.Text = _isModified ? "? Modified" : "";
        TxtErrorCount.Text = _errorLines.Count > 0 ? $"? {_errorLines.Count} error(s)" : "";
        
        // Update line/column info
        int caretIndex = EditorTextBox.CaretIndex;
        int lineNumber = EditorTextBox.GetLineIndexFromCharacterIndex(caretIndex) + 1;
        int colNumber = caretIndex - EditorTextBox.GetCharacterIndexFromLineIndex(lineNumber - 1) + 1;
        TxtLineInfo.Text = $"Line {lineNumber}, Col {colNumber}";
    }

    private void RefreshErrorHighlighting()
    {
        _errorLines.Clear();
        _duplicateReferenceLines.Clear();
        
        var lines = GetEditorLines();
        
        // Parse to find errors
        try
        {
            var result = _parser.ParseLines(lines);
            
            // Apply filters
            var filteredErrors = result.Errors.AsEnumerable();

            if (HideRedundantMoves)
            {
                filteredErrors = filteredErrors.Where(e => !e.Message.StartsWith(RedundantMovePattern));
            }

            if (HideDuplicatePaths)
            {
                filteredErrors = filteredErrors.Where(e => !e.Message.StartsWith(DuplicateToolpathPattern));
            }

            foreach (var error in filteredErrors)
            {
                _errorLines.Add(error.LineNumber);
                
                // If this error has a related line number (e.g., duplicate reference), add it to the blue highlight set
                if (error.RelatedLineNumber.HasValue && error.RelatedLineNumber.Value > 0)
                {
                    _duplicateReferenceLines.Add(error.RelatedLineNumber.Value);
                }
            }
        }
        catch
        {
            // If parsing fails, don't highlight anything
        }
        
        // Update line numbers with error highlighting
        UpdateLineNumbers();
        UpdateStatusBar();
    }

    #region Event Handlers

    private void EditorTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdatingText) return;
        
        _isModified = true;
        ContentWasModified = true;
        
        // Refresh error highlighting (which also updates line numbers)
        RefreshErrorHighlighting();
    }

    private void EditorTextBox_SelectionChanged(object sender, RoutedEventArgs e)
    {
        UpdateStatusBar();
    }

    private void EditorTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        // When user presses Enter, update all views
        if (e.Key == Key.Enter)
        {
            // Use BeginInvoke to ensure the Enter key has been processed
            // and the text has been updated before we preview
            Dispatcher.BeginInvoke(new Action(() =>
            {
                PreviewChanges();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }
    }

    private void EditorTextBox_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        // Synchronize line numbers scroll with editor scroll
        LineNumberScroller.ScrollToVerticalOffset(e.VerticalOffset);
    }

    private void MenuSave_Click(object sender, RoutedEventArgs e)
    {
        Save();
    }

    private void MenuSaveAs_Click(object sender, RoutedEventArgs e)
    {
        SaveAs();
    }

    private void MenuClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SaveCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Save();
    }

    private void SaveAsCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        SaveAs();
    }

    private void PreviewChangesCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        PreviewChanges();
    }

    private void BtnPreviewChanges_Click(object sender, RoutedEventArgs e)
    {
        PreviewChanges();
    }

    private void BtnHelp_Click(object sender, RoutedEventArgs e)
    {
        ShowHelp();
    }

    private void HelpCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ShowHelp();
    }

    private void UnitConverterCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ShowUnitConverter();
    }

    private void EditCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        // Delegate to the TextBox's built-in command handling
        if (EditorTextBox != null && e.Command != null)
        {
            if (e.Command == ApplicationCommands.Undo)
            {
                e.CanExecute = EditorTextBox.CanUndo;
            }
            else if (e.Command == ApplicationCommands.Redo)
            {
                e.CanExecute = EditorTextBox.CanRedo;
            }
            else if (e.Command == ApplicationCommands.Cut || e.Command == ApplicationCommands.Copy)
            {
                e.CanExecute = EditorTextBox.SelectionLength > 0;
            }
            else if (e.Command == ApplicationCommands.Paste)
            {
                e.CanExecute = Clipboard.ContainsText();
            }
            else if (e.Command == ApplicationCommands.SelectAll)
            {
                e.CanExecute = EditorTextBox.Text.Length > 0;
            }
            else
            {
                e.CanExecute = true;
            }
        }
        e.Handled = true;
    }

    private void EditCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        // Delegate to the TextBox's built-in command handling
        if (EditorTextBox != null && e.Command != null)
        {
            if (e.Command == ApplicationCommands.Undo)
            {
                EditorTextBox.Undo();
            }
            else if (e.Command == ApplicationCommands.Redo)
            {
                EditorTextBox.Redo();
            }
            else if (e.Command == ApplicationCommands.Cut)
            {
                EditorTextBox.Cut();
            }
            else if (e.Command == ApplicationCommands.Copy)
            {
                EditorTextBox.Copy();
            }
            else if (e.Command == ApplicationCommands.Paste)
            {
                EditorTextBox.Paste();
            }
            else if (e.Command == ApplicationCommands.SelectAll)
            {
                EditorTextBox.SelectAll();
            }
        }
        e.Handled = true;
    }

    private void ShowHelp()
    {
        var helpWindow = new GcodeHelpWindow
        {
            Owner = this
        };
        helpWindow.Show();
    }

    private void BtnMacros_Click(object sender, RoutedEventArgs e)
    {
        ShowMacroEditor();
    }

    private void ShowMacroEditor()
    {
        var macroWindow = new MacroEditorWindow
        {
            Owner = this
        };
        macroWindow.ShowDialog();
        
        // Reload macros after editor closes
        LoadMacros();
    }

    private void BtnUnitConverter_Click(object sender, RoutedEventArgs e)
    {
        ShowUnitConverter();
    }

    private void ShowUnitConverter()
    {
        var converterWindow = new UnitConverterWindow
        {
            Owner = this
        };
        
        if (converterWindow.ShowDialog() == true && converterWindow.WasPlaced)
        {
            // Insert the millimeter value at the current cursor position
            int caretIndex = EditorTextBox.CaretIndex;
            string currentText = EditorTextBox.Text;
            string valueToInsert = converterWindow.MillimeterValue.ToString("F3");
            
            EditorTextBox.Text = currentText.Insert(caretIndex, valueToInsert);
            EditorTextBox.CaretIndex = caretIndex + valueToInsert.Length;
            EditorTextBox.Focus();
        }
    }

    private void BtnFindReplace_Click(object sender, RoutedEventArgs e)
    {
        ShowFindReplace();
    }

    private void FindReplaceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ShowFindReplace();
    }

    private void ShowFindReplace()
    {
        var findReplaceDialog = new FindReplaceDialog(EditorTextBox)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        findReplaceDialog.Show();
    }

    private void BtnZValueEditor_Click(object sender, RoutedEventArgs e)
    {
        ShowZValueEditor();
    }

    private void ZValueEditorCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        ShowZValueEditor();
    }

    private void ShowZValueEditor()
    {
        bool useSelection = EditorTextBox.SelectionLength > 0;
        
        var zValueEditorDialog = new ZValueEditorDialog(EditorTextBox, useSelection)
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        zValueEditorDialog.ShowDialog();
        
        if (zValueEditorDialog.ChangesApplied)
        {
            _isModified = true;
            ContentWasModified = true;
            RefreshErrorHighlighting();
        }
    }

    private void BtnRemoveG0Lines_Click(object sender, RoutedEventArgs e)
    {
        RemoveG0Lines();
    }

    private void RemoveG0Lines()
    {
        bool useSelection = EditorTextBox.SelectionLength > 0;
        string textToProcess;
        int selectionStart = 0;
        int selectionLength = 0;

        if (useSelection)
        {
            textToProcess = EditorTextBox.SelectedText;
            selectionStart = EditorTextBox.SelectionStart;
            selectionLength = EditorTextBox.SelectionLength;
        }
        else
        {
            textToProcess = EditorTextBox.Text;
        }

        // Split into lines
        string[] lines = textToProcess.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        
        // Count how many G0 lines we'll remove
        int g0Count = lines.Count(line => line.TrimStart().StartsWith("G0", StringComparison.OrdinalIgnoreCase));
        
        if (g0Count == 0)
        {
            MessageBox.Show("No lines starting with G0 found in the " + (useSelection ? "selection" : "document") + ".",
                "No G0 Lines",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        // Confirm with user
        var result = MessageBox.Show(
            $"This will remove {g0Count} line(s) starting with G0 from the {(useSelection ? "selection" : "document")}.\n\nContinue?",
            "Remove G0 Lines",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        // Filter out G0 lines
        var filteredLines = lines.Where(line => !line.TrimStart().StartsWith("G0", StringComparison.OrdinalIgnoreCase));
        string newText = string.Join(Environment.NewLine, filteredLines);

        // Apply changes using SelectedText to preserve undo
        if (useSelection)
        {
            EditorTextBox.Focus();
            EditorTextBox.SelectionStart = selectionStart;
            EditorTextBox.SelectionLength = selectionLength;
            EditorTextBox.SelectedText = newText;
            
            // Restore selection to show what was changed
            EditorTextBox.SelectionStart = selectionStart;
            EditorTextBox.SelectionLength = newText.Length;
        }
        else
        {
            // For entire document, select all and replace
            int originalCaretPosition = EditorTextBox.CaretIndex;
            
            EditorTextBox.Focus();
            EditorTextBox.SelectAll();
            EditorTextBox.SelectedText = newText;
            
            // Restore caret position
            EditorTextBox.CaretIndex = Math.Min(originalCaretPosition, newText.Length);
        }

        // Mark as modified and refresh
        _isModified = true;
        ContentWasModified = true;
        RefreshErrorHighlighting();
        
        // Trigger preview to update visualization
        PreviewChanges();
    }

    private void InvertSelection_Click(object sender, RoutedEventArgs e)
    {
        InvertSelection();
    }

    private void InvertSelectionCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        InvertSelection();
    }

    private void InvertSelection()
    {
        if (EditorTextBox.Text.Length == 0)
        {
            return; // Nothing to invert
        }

        int textLength = EditorTextBox.Text.Length;
        int selectionStart = EditorTextBox.SelectionStart;
        int selectionLength = EditorTextBox.SelectionLength;

        if (selectionLength == 0)
        {
            // If nothing is selected, select all
            EditorTextBox.SelectAll();
            return;
        }

        if (selectionLength == textLength)
        {
            // If everything is selected, deselect all
            EditorTextBox.Select(0, 0);
            return;
        }

        // Invert the selection
        // If there's text before the selection, select that
        if (selectionStart > 0)
        {
            EditorTextBox.Select(0, selectionStart);
        }
        // If there's text after the selection, select from end of selection to end of document
        else if (selectionStart + selectionLength < textLength)
        {
            EditorTextBox.Select(selectionStart + selectionLength, textLength - (selectionStart + selectionLength));
        }
        else
        {
            // Selection is at the end, select from beginning to start of selection
            EditorTextBox.Select(0, selectionStart);
        }
    }

    #region Macro Insertion

    private void InsertMacro1_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(1);
    private void InsertMacro2_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(2);
    private void InsertMacro3_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(3);
    private void InsertMacro4_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(4);
    private void InsertMacro5_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(5);
    private void InsertMacro6_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(6);
    private void InsertMacro7_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(7);
    private void InsertMacro8_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(8);
    private void InsertMacro9_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(9);
    private void InsertMacro0_Executed(object sender, ExecutedRoutedEventArgs e) => InsertMacro(0);

    /// <summary>
    /// Inserts a macro's G-code at the current cursor position.
    /// </summary>
    private void InsertMacro(int keyNumber)
    {
        var macro = _macros.FirstOrDefault(m => m.KeyNumber == keyNumber);
        
        if (macro is null || string.IsNullOrEmpty(macro.GcodeContent))
        {
            return;
        }

        int caretIndex = EditorTextBox.CaretIndex;
        string currentText = EditorTextBox.Text;
        
        // Insert the macro content at cursor position
        // Add a newline before if we're not at the start of a line
        bool needsNewlineBefore = caretIndex > 0 && currentText[caretIndex - 1] != '\n';
        
        // Add a newline after if we're not at the end and the next char isn't a newline
        bool needsNewlineAfter = caretIndex < currentText.Length && currentText[caretIndex] != '\n' && currentText[caretIndex] != '\r';
        
        string textToInsert = macro.GcodeContent;
        if (needsNewlineBefore)
        {
            textToInsert = "\n" + textToInsert;
        }
        if (needsNewlineAfter)
        {
            textToInsert = textToInsert + "\n";
        }
        
        EditorTextBox.Text = currentText.Insert(caretIndex, textToInsert);
        EditorTextBox.CaretIndex = caretIndex + textToInsert.Length;
        EditorTextBox.Focus();
    }

    #endregion

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        if (_isModified)
        {
            var result = MessageBox.Show(
                "You have unsaved changes. Do you want to save before closing?",
                "Unsaved Changes",
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    if (!Save())
                    {
                        e.Cancel = true;
                    }
                    break;
                case MessageBoxResult.Cancel:
                    e.Cancel = true;
                    break;
            }
        }
    }

    #endregion

    #region Save Operations

    /// <summary>
    /// Creates a backup of the file with versioned naming (filename.OLD###.extension).
    /// </summary>
    private void CreateBackupFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            string directory = Path.GetDirectoryName(filePath) ?? "";
            string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            // Find the next available backup number
            int backupNumber = 1;
            string backupPath;

            do
            {
                string backupFileName = $"{fileNameWithoutExt}.OLD{backupNumber:D3}{extension}";
                backupPath = Path.Combine(directory, backupFileName);
                backupNumber++;
            }
            while (File.Exists(backupPath));

            // Copy the existing file to the backup location
            File.Copy(filePath, backupPath, false);
        }
        catch (Exception ex)
        {
            // Log the error but don't prevent the save operation
            MessageBox.Show($"Warning: Could not create backup file: {ex.Message}", 
                "Backup Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private bool Save()
    {
        if (string.IsNullOrEmpty(_filePath))
        {
            return SaveAs();
        }

        try
        {
            // Create backup before saving
            CreateBackupFile(_filePath);

            File.WriteAllText(_filePath, EditorTextBox.Text);
            _isModified = false;
            ContentWasModified = true;
            UpdateStatusBar();
            
            // Refresh error highlighting after save
            RefreshErrorHighlighting();
            
            return true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }
    }

    private bool SaveAs()
    {
        var saveDialog = new SaveFileDialog
        {
            Title = "Save G-code File As",
            Filter = "G-code Files (*.nc;*.gcode;*.ngc;*.tap)|*.nc;*.gcode;*.ngc;*.tap|All Files (*.*)|*.*",
            DefaultExt = ".nc",
            FileName = _filePath != null ? Path.GetFileName(_filePath) : "untitled.nc"
        };

        if (saveDialog.ShowDialog() == true)
        {
            try
            {
                // Create backup if the target file already exists
                if (File.Exists(saveDialog.FileName))
                {
                    CreateBackupFile(saveDialog.FileName);
                }

                File.WriteAllText(saveDialog.FileName, EditorTextBox.Text);
                _filePath = saveDialog.FileName;
                _isModified = false;
                ContentWasModified = true;
                Title = $"G-code Editor - {Path.GetFileName(_filePath)}";
                UpdateStatusBar();
                
                // Refresh error highlighting after save
                RefreshErrorHighlighting();
                
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving file: {ex.Message}", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        return false;
    }

    #endregion
}
