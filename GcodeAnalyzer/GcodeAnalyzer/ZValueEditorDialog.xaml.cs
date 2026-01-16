using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GcodeAnalyzer;

/// <summary>
/// Dialog for finding and modifying parameters in G-code lines with specific Z values.
/// </summary>
public partial class ZValueEditorDialog : Window
{
    private readonly TextBox _targetTextBox;
    private readonly bool _useSelection;
    private List<int> _matchingLineIndices = new();
    private string _originalText;
    private TextBox? _lastFocusedTextBox;
    
    public ICommand? CloseCommand { get; }
    public ICommand? FindCommand { get; }
    
    /// <summary>
    /// Gets whether changes were applied.
    /// </summary>
    public bool ChangesApplied { get; private set; }

    public ZValueEditorDialog(TextBox targetTextBox, bool useSelection)
    {
        InitializeComponent();
        
        _targetTextBox = targetTextBox ?? throw new ArgumentNullException(nameof(targetTextBox));
        _useSelection = useSelection;
        _originalText = useSelection ? _targetTextBox.SelectedText : _targetTextBox.Text;
        
        // Set up commands
        CloseCommand = new RelayCommand(_ => Close());
        FindCommand = new RelayCommand(_ => BtnFind_Click(this, new RoutedEventArgs()));
        
        DataContext = this;
        
        // Update scope display
        TxtScope.Text = useSelection ? "Scope: Selected text only" : "Scope: Entire document";
        
        // Track focus on textboxes for unit converter
        TxtZValue.GotFocus += (s, e) => _lastFocusedTextBox = TxtZValue;
        TxtNewValue.GotFocus += (s, e) => _lastFocusedTextBox = TxtNewValue;
        
        // Focus on Z value textbox
        Loaded += (s, e) => TxtZValue.Focus();
    }

    private void TxtZValue_TextChanged(object sender, TextChangedEventArgs e)
    {
        // Clear previous results when Z value changes
        _matchingLineIndices.Clear();
        
        // Update the message based on whether a value is entered
        if (string.IsNullOrWhiteSpace(TxtZValue.Text))
        {
            TxtMatchCount.Text = "Click Find to search all lines";
            TxtPreview.Text = "Click Find to locate all lines in scope";
        }
        else
        {
            TxtMatchCount.Text = "Click Find to search";
            TxtPreview.Text = "Click Find to locate matching lines";
        }
    }

    private void ModificationOption_Changed(object sender, RoutedEventArgs e)
    {
        if (LblNewValue == null) return;
        
        if (RbModifyZ?.IsChecked == true)
        {
            LblNewValue.Content = "New Z Value:";
            TxtNewValue.ToolTip = "Enter the new Z value or offset (e.g., -6.0 or +0.5)";
        }
        else if (RbModifyX?.IsChecked == true)
        {
            LblNewValue.Content = "New X Value:";
            TxtNewValue.ToolTip = "Enter the new X value or offset (e.g., 10.0 or -2.5)";
        }
        else if (RbModifyY?.IsChecked == true)
        {
            LblNewValue.Content = "New Y Value:";
            TxtNewValue.ToolTip = "Enter the new Y value or offset (e.g., 15.0 or +3.0)";
        }
        else if (RbModifyF?.IsChecked == true)
        {
            LblNewValue.Content = "New F Value:";
            TxtNewValue.ToolTip = "Enter the new feed rate or offset (e.g., 500 or +100)";
        }
        else if (RbModifyS?.IsChecked == true)
        {
            LblNewValue.Content = "New S Value:";
            TxtNewValue.ToolTip = "Enter the new spindle speed or offset (e.g., 10000 or -1000)";
        }
        else if (RbModifyP?.IsChecked == true)
        {
            LblNewValue.Content = "New P Value:";
            TxtNewValue.ToolTip = "Enter the new dwell time or offset in seconds (e.g., 2.0 or +0.5)";
        }
        else if (RbModifyT?.IsChecked == true)
        {
            LblNewValue.Content = "New T Value:";
            TxtNewValue.ToolTip = "Enter the new tool number (e.g., 1, 2, 3)";
        }
        else if (RbModifyM?.IsChecked == true)
        {
            LblNewValue.Content = "New M Value:";
            TxtNewValue.ToolTip = "Enter the new M-code (e.g., 3, 5, 8, 9, 30)";
        }
        else if (RbModifyE?.IsChecked == true)
        {
            LblNewValue.Content = "New E Value:";
            TxtNewValue.ToolTip = "Enter the new extrusion rate or offset (e.g., 50.5 or +5.0)";
        }
        else if (RbModifyA?.IsChecked == true)
        {
            LblNewValue.Content = "New A Value:";
            TxtNewValue.ToolTip = "Enter the new A-axis angle or offset in degrees (e.g., 45.0 or +10.0)";
        }
        else if (RbModifyB?.IsChecked == true)
        {
            LblNewValue.Content = "New B Value:";
            TxtNewValue.ToolTip = "Enter the new B-axis angle or offset in degrees (e.g., 90.0 or -15.0)";
        }
        else if (RbModifyQ?.IsChecked == true)
        {
            LblNewValue.Content = "New Q Value:";
            TxtNewValue.ToolTip = "Enter the new peck depth or offset (e.g., 2.0 or -0.5)";
        }
        
        // Re-enable text box for all options (no special cases anymore)
        if (TxtNewValue != null)
        {
            TxtNewValue.IsEnabled = true;
        }
    }

    private void BtnFind_Click(object sender, RoutedEventArgs e)
    {
        string input = TxtZValue.Text.Trim();
        
        // If no value entered, find all lines in scope
        if (string.IsNullOrWhiteSpace(input))
        {
            FindAllLines();
            return;
        }
        
        // Try to parse as a range first
        if (TryParseZRange(input, out double minZ, out double maxZ))
        {
            FindMatchingLinesInRange(minZ, maxZ);
        }
        // Try to parse as a single value
        else if (double.TryParse(input, out double zValue))
        {
            FindMatchingLines(zValue);
        }
        else
        {
            TxtMatchCount.Text = "Invalid Z value or range";
            TxtPreview.Text = "Please enter a valid numeric Z value (e.g., -5.0) or range (e.g., -3.2 to -5.1)";
        }
    }

    private void FindAllLines()
    {
        _matchingLineIndices.Clear();
        
        string[] lines = _originalText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var previewText = new StringBuilder();
        
        // Add all non-empty lines to the matching list
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            // Skip empty lines and comment-only lines
            if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("(") && !line.StartsWith(";"))
            {
                _matchingLineIndices.Add(i);
                
                // Only show first 50 lines in preview to avoid overwhelming the display
                if (previewText.Length < 5000)
                {
                    previewText.AppendLine($"Line {i + 1}: {lines[i]}");
                }
            }
        }
        
        if (_matchingLineIndices.Count > 0)
        {
            TxtMatchCount.Text = $"Found {_matchingLineIndices.Count} line(s) (all non-empty lines in scope)";
            
            if (previewText.Length >= 5000)
            {
                previewText.AppendLine("... (preview truncated, showing first ~50 lines)");
            }
            
            TxtPreview.Text = previewText.ToString();
        }
        else
        {
            TxtMatchCount.Text = "No lines found";
            TxtPreview.Text = "No non-empty lines found in the scope";
        }
    }

    private bool TryParseZRange(string input, out double minZ, out double maxZ)
    {
        minZ = 0;
        maxZ = 0;
        
        // Match patterns like: "-3.2 to -5.1", "-3.2 - -5.1", "-10--20", etc.
        // Support "to", "-", "–" (en-dash), "—" (em-dash) as separators
        var rangePattern = @"^\s*(-?\d+\.?\d*)\s*(?:to|-|–|—)\s*(-?\d+\.?\d*)\s*$";
        var match = Regex.Match(input, rangePattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            if (double.TryParse(match.Groups[1].Value, out double value1) &&
                double.TryParse(match.Groups[2].Value, out double value2))
            {
                // Ensure minZ is always the smaller value
                minZ = Math.Min(value1, value2);
                maxZ = Math.Max(value1, value2);
                return true;
            }
        }
        
        return false;
    }

    private void FindMatchingLines(double zValue)
    {
        _matchingLineIndices.Clear();
        
        string[] lines = _originalText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var previewText = new StringBuilder();
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (LineHasZValue(lines[i], zValue))
            {
                _matchingLineIndices.Add(i);
                previewText.AppendLine($"Line {i + 1}: {lines[i]}");
            }
        }
        
        if (_matchingLineIndices.Count > 0)
        {
            TxtMatchCount.Text = $"Found {_matchingLineIndices.Count} matching line(s) with Z={zValue:F3}";
            TxtPreview.Text = previewText.ToString();
        }
        else
        {
            TxtMatchCount.Text = "No matches found";
            TxtPreview.Text = $"No lines found with Z value of {zValue:F3}";
        }
    }

    private void FindMatchingLinesInRange(double minZ, double maxZ)
    {
        _matchingLineIndices.Clear();
        
        string[] lines = _originalText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var previewText = new StringBuilder();
        
        for (int i = 0; i < lines.Length; i++)
        {
            if (LineHasZValueInRange(lines[i], minZ, maxZ))
            {
                _matchingLineIndices.Add(i);
                previewText.AppendLine($"Line {i + 1}: {lines[i]}");
            }
        }
        
        if (_matchingLineIndices.Count > 0)
        {
            TxtMatchCount.Text = $"Found {_matchingLineIndices.Count} matching line(s) in range Z={minZ:F3} to {maxZ:F3}";
            TxtPreview.Text = previewText.ToString();
        }
        else
        {
            TxtMatchCount.Text = "No matches found";
            TxtPreview.Text = $"No lines found with Z values between {minZ:F3} and {maxZ:F3}";
        }
    }

    private bool LineHasZValue(string line, double targetZValue)
    {
        // Match Z parameter followed by a number (with optional sign and decimal)
        var match = Regex.Match(line, @"Z\s*(-?\d+\.?\d*)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            if (double.TryParse(match.Groups[1].Value, out double lineZValue))
            {
                // Compare with small tolerance for floating point
                return Math.Abs(lineZValue - targetZValue) < 0.0001;
            }
        }
        
        return false;
    }

    private bool LineHasZValueInRange(string line, double minZ, double maxZ)
    {
        // Match Z parameter followed by a number (with optional sign and decimal)
        var match = Regex.Match(line, @"Z\s*(-?\d+\.?\d*)", RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            if (double.TryParse(match.Groups[1].Value, out double lineZValue))
            {
                // Check if the value is within the range (inclusive)
                return lineZValue >= minZ && lineZValue <= maxZ;
            }
        }
        
        return false;
    }

    private void BtnPreview_Click(object sender, RoutedEventArgs e)
    {
        if (_matchingLineIndices.Count == 0)
        {
            MessageBox.Show("No matching lines found. Click Find first.", "No Matches",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(TxtNewValue.Text))
        {
            MessageBox.Show("Please enter a new value to preview.", "Missing Value",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        var preview = GeneratePreview();
        TxtPreview.Text = preview;
    }

    private void BtnUnitConverter_Click(object sender, RoutedEventArgs e)
    {
        // Use the last focused textbox, defaulting to TxtNewValue if none tracked
        TextBox targetTextBox = _lastFocusedTextBox ?? TxtNewValue;
        
        var unitConverterWindow = new UnitConverterWindow
        {
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        
        if (unitConverterWindow.ShowDialog() == true && unitConverterWindow.WasPlaced)
        {
            // Insert the converted millimeter value into the target textbox
            string convertedValue = unitConverterWindow.MillimeterValue.ToString("F3").TrimEnd('0').TrimEnd('.');
            
            if (targetTextBox.SelectionLength > 0)
            {
                // Replace selected text
                int selectionStart = targetTextBox.SelectionStart;
                targetTextBox.SelectedText = convertedValue;
                targetTextBox.SelectionStart = selectionStart + convertedValue.Length;
            }
            else
            {
                // Insert at cursor position
                int caretIndex = targetTextBox.CaretIndex;
                targetTextBox.Text = targetTextBox.Text.Insert(caretIndex, convertedValue);
                targetTextBox.CaretIndex = caretIndex + convertedValue.Length;
            }
            
            targetTextBox.Focus();
        }
    }

    private string GeneratePreview()
    {
        string[] lines = _originalText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var previewText = new StringBuilder();
        string newValue = TxtNewValue.Text.Trim();
        
        previewText.AppendLine("PREVIEW OF CHANGES:");
        previewText.AppendLine("===================");
        
        // Show the operation mode
        if (RbAddOffset?.IsChecked == true)
        {
            previewText.AppendLine($"Mode: Add offset (+{newValue})");
        }
        else if (RbSubtractOffset?.IsChecked == true)
        {
            previewText.AppendLine($"Mode: Subtract offset (-{newValue})");
        }
        else
        {
            previewText.AppendLine($"Mode: Set to absolute value ({newValue})");
        }
        
        previewText.AppendLine();
        
        foreach (int lineIndex in _matchingLineIndices)
        {
            string originalLine = lines[lineIndex];
            string modifiedLine = ModifyLine(originalLine, newValue);
            
            previewText.AppendLine($"Line {lineIndex + 1}:");
            previewText.AppendLine($"  Before: {originalLine}");
            previewText.AppendLine($"  After:  {modifiedLine}");
            previewText.AppendLine();
        }
        
        return previewText.ToString();
    }

    private void BtnApply_Click(object sender, RoutedEventArgs e)
    {
        if (_matchingLineIndices.Count == 0)
        {
            MessageBox.Show("No matching lines found. Click Find first.", "No Matches",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        
        if (string.IsNullOrWhiteSpace(TxtNewValue.Text))
        {
            MessageBox.Show("Please enter a new value to apply.", "Missing Value",
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        
        var result = MessageBox.Show(
            $"This will modify {_matchingLineIndices.Count} line(s). Continue?",
            "Confirm Changes",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (result == MessageBoxResult.Yes)
        {
            ApplyChanges();
            ChangesApplied = true;
            Close();
        }
    }

    private void ApplyChanges()
    {
        string[] lines = _originalText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string newValue = TxtNewValue.Text.Trim();
        
        // Modify matching lines
        foreach (int lineIndex in _matchingLineIndices)
        {
            lines[lineIndex] = ModifyLine(lines[lineIndex], newValue);
        }
        
        // Reconstruct the text
        string modifiedText = string.Join(Environment.NewLine, lines);
        
        // Apply to the TextBox using proper methods to support undo
        if (_useSelection)
        {
            // For selection, we can use SelectedText which supports undo
            int selectionStart = _targetTextBox.SelectionStart;
            int selectionLength = _targetTextBox.SelectionLength;
            
            _targetTextBox.Focus();
            _targetTextBox.SelectionStart = selectionStart;
            _targetTextBox.SelectionLength = selectionLength;
            _targetTextBox.SelectedText = modifiedText;
            
            // Restore selection to show what was changed
            _targetTextBox.SelectionStart = selectionStart;
            _targetTextBox.SelectionLength = modifiedText.Length;
        }
        else
        {
            // For entire document, we need to select all and replace to maintain undo
            int originalCaretPosition = _targetTextBox.CaretIndex;
            
            _targetTextBox.Focus();
            _targetTextBox.SelectAll();
            _targetTextBox.SelectedText = modifiedText;
            
            // Restore caret position
            _targetTextBox.CaretIndex = Math.Min(originalCaretPosition, modifiedText.Length);
        }
    }

    private string ModifyLine(string line, string newValue)
    {
        string parameter = GetSelectedParameter();
        
        // Check if the line already has this parameter
        var pattern = $@"{parameter}\s*(-?\d+\.?\d*)";
        var match = Regex.Match(line, pattern, RegexOptions.IgnoreCase);
        
        if (match.Success)
        {
            string currentValueStr = match.Groups[1].Value;
            string finalValue;
            
            // Determine the final value based on the selected mode
            if (RbAddOffset?.IsChecked == true)
            {
                // Add offset mode
                if (double.TryParse(currentValueStr, out double currentValue) &&
                    double.TryParse(newValue, out double offset))
                {
                    double result = currentValue + offset;
                    finalValue = result.ToString("F3").TrimEnd('0').TrimEnd('.');
                }
                else
                {
                    finalValue = newValue; // Fallback to absolute if parsing fails
                }
            }
            else if (RbSubtractOffset?.IsChecked == true)
            {
                // Subtract offset mode
                if (double.TryParse(currentValueStr, out double currentValue) &&
                    double.TryParse(newValue, out double offset))
                {
                    double result = currentValue - offset;
                    finalValue = result.ToString("F3").TrimEnd('0').TrimEnd('.');
                }
                else
                {
                    finalValue = newValue; // Fallback to absolute if parsing fails
                }
            }
            else
            {
                // Absolute mode (default)
                finalValue = newValue;
            }
            
            // Replace existing value
            return Regex.Replace(line, pattern, $"{parameter}{finalValue}", RegexOptions.IgnoreCase);
        }
        else if (ChkAddIfMissing.IsChecked == true)
        {
            // When adding a new parameter, only use absolute value (offsets don't make sense)
            string valueToAdd = newValue;
            
            // Add the parameter at the end of the line (before any comment)
            int commentIndex = line.IndexOf('(');
            if (commentIndex >= 0)
            {
                // Insert before comment
                return line.Insert(commentIndex, $" {parameter}{valueToAdd}");
            }
            else
            {
                // Append to end
                return line.TrimEnd() + $" {parameter}{valueToAdd}";
            }
        }
        
        // No change needed
        return line;
    }

    private string GetSelectedParameter()
    {
        if (RbModifyZ?.IsChecked == true) return "Z";
        if (RbModifyX?.IsChecked == true) return "X";
        if (RbModifyY?.IsChecked == true) return "Y";
        if (RbModifyF?.IsChecked == true) return "F";
        if (RbModifyS?.IsChecked == true) return "S";
        if (RbModifyP?.IsChecked == true) return "P";
        if (RbModifyT?.IsChecked == true) return "T";
        if (RbModifyM?.IsChecked == true) return "M";
        if (RbModifyE?.IsChecked == true) return "E";
        if (RbModifyA?.IsChecked == true) return "A";
        if (RbModifyB?.IsChecked == true) return "B";
        if (RbModifyQ?.IsChecked == true) return "Q";
        return "Z"; // Default
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
