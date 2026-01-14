using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace GcodeAnalyzer;

/// <summary>
/// Find and Replace dialog for text editor with support for case-sensitive search,
/// whole word matching, regular expressions, and various replace scopes.
/// </summary>
public partial class FindReplaceDialog : Window
{
    private readonly TextBox _targetTextBox;
    private int _lastFoundIndex = -1;
    private int _lastSearchLength = 0;
    private string _lastSearchText = "";
    
    public ICommand? CloseCommand { get; }
    public ICommand? FindNextCommand { get; }
    public ICommand? FindPreviousCommand { get; }

    public FindReplaceDialog(TextBox targetTextBox)
    {
        InitializeComponent();
        
        _targetTextBox = targetTextBox ?? throw new ArgumentNullException(nameof(targetTextBox));
        
        // Set up commands
        CloseCommand = new RelayCommand(_ => Close());
        FindNextCommand = new RelayCommand(_ => FindNext());
        FindPreviousCommand = new RelayCommand(_ => FindPrevious());
        
        DataContext = this;
        
        // Pre-fill find text with current selection if any
        if (!string.IsNullOrEmpty(_targetTextBox.SelectedText))
        {
            TxtFind.Text = _targetTextBox.SelectedText;
        }
        
        // Focus on find textbox
        Loaded += (s, e) => TxtFind.Focus();
    }

    private void TxtFind_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                FindPrevious();
            }
            else
            {
                FindNext();
            }
            e.Handled = true;
        }
    }

    private void TxtReplace_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnReplace_Click(sender, e);
            e.Handled = true;
        }
    }

    private void BtnFindNext_Click(object sender, RoutedEventArgs e)
    {
        FindNext();
    }

    private void BtnFindPrevious_Click(object sender, RoutedEventArgs e)
    {
        FindPrevious();
    }

    private void BtnReplace_Click(object sender, RoutedEventArgs e)
    {
        if (RbReplaceOnce.IsChecked == true)
        {
            ReplaceNext();
        }
        else if (RbReplaceSelection.IsChecked == true)
        {
            ReplaceInSelection();
        }
        else if (RbReplaceAll.IsChecked == true)
        {
            ReplaceAll();
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    /// <summary>
    /// Finds the next occurrence of the search text.
    /// </summary>
    private void FindNext()
    {
        string findText = TxtFind.Text;
        
        if (string.IsNullOrEmpty(findText))
        {
            UpdateStatus("Please enter text to find");
            return;
        }
        
        string text = _targetTextBox.Text;
        int startIndex = _targetTextBox.SelectionStart + _targetTextBox.SelectionLength;
        
        // If the search text changed, reset the search
        if (findText != _lastSearchText)
        {
            _lastFoundIndex = -1;
            _lastSearchText = findText;
        }
        
        int foundIndex = FindInText(text, findText, startIndex, forward: true);
        
        if (foundIndex >= 0)
        {
            SelectFoundText(foundIndex);
            UpdateStatus($"Found at position {foundIndex + 1}");
        }
        else if (ChkWrapAround.IsChecked == true && startIndex > 0)
        {
            // Try wrapping around from the beginning
            foundIndex = FindInText(text, findText, 0, forward: true);
            if (foundIndex >= 0 && foundIndex < startIndex)
            {
                SelectFoundText(foundIndex);
                UpdateStatus($"Found at position {foundIndex + 1} (wrapped)");
            }
            else
            {
                UpdateStatus("No matches found");
            }
        }
        else
        {
            UpdateStatus("No matches found");
        }
    }

    /// <summary>
    /// Finds the previous occurrence of the search text.
    /// </summary>
    private void FindPrevious()
    {
        string findText = TxtFind.Text;
        
        if (string.IsNullOrEmpty(findText))
        {
            UpdateStatus("Please enter text to find");
            return;
        }
        
        string text = _targetTextBox.Text;
        int startIndex = _targetTextBox.SelectionStart - 1;
        
        if (startIndex < 0)
            startIndex = text.Length - 1;
        
        // If the search text changed, reset the search
        if (findText != _lastSearchText)
        {
            _lastFoundIndex = -1;
            _lastSearchText = findText;
        }
        
        int foundIndex = FindInText(text, findText, startIndex, forward: false);
        
        if (foundIndex >= 0)
        {
            SelectFoundText(foundIndex);
            UpdateStatus($"Found at position {foundIndex + 1}");
        }
        else if (ChkWrapAround.IsChecked == true)
        {
            // Try wrapping around from the end
            foundIndex = FindInText(text, findText, text.Length - 1, forward: false);
            if (foundIndex >= 0 && foundIndex > _targetTextBox.SelectionStart)
            {
                SelectFoundText(foundIndex);
                UpdateStatus($"Found at position {foundIndex + 1} (wrapped)");
            }
            else
            {
                UpdateStatus("No matches found");
            }
        }
        else
        {
            UpdateStatus("No matches found");
        }
    }

    /// <summary>
    /// Replaces the next occurrence of the search text.
    /// </summary>
    private void ReplaceNext()
    {
        string findText = TxtFind.Text;
        string replaceText = TxtReplace.Text ?? "";
        
        if (string.IsNullOrEmpty(findText))
        {
            UpdateStatus("Please enter text to find");
            return;
        }
        
        // Check if current selection matches the find text
        if (IsCurrentSelectionMatch(findText))
        {
            // Replace the selected text
            int selectionStart = _targetTextBox.SelectionStart;
            _targetTextBox.SelectedText = replaceText;
            _targetTextBox.SelectionStart = selectionStart;
            _targetTextBox.SelectionLength = replaceText.Length;
            
            UpdateStatus("Replaced 1 occurrence");
            
            // Find next occurrence
            FindNext();
        }
        else
        {
            // Find the next occurrence first
            FindNext();
        }
    }

    /// <summary>
    /// Replaces all occurrences within the current selection.
    /// </summary>
    private void ReplaceInSelection()
    {
        string findText = TxtFind.Text;
        string replaceText = TxtReplace.Text ?? "";
        
        if (string.IsNullOrEmpty(findText))
        {
            UpdateStatus("Please enter text to find");
            return;
        }
        
        if (_targetTextBox.SelectionLength == 0)
        {
            UpdateStatus("Please select text first");
            return;
        }
        
        int selectionStart = _targetTextBox.SelectionStart;
        int selectionEnd = selectionStart + _targetTextBox.SelectionLength;
        string selectedText = _targetTextBox.SelectedText;
        
        string result;
        int count;
        
        if (ChkUseRegex.IsChecked == true)
        {
            try
            {
                var options = GetRegexOptions();
                var regex = new Regex(findText, options);
                count = regex.Matches(selectedText).Count;
                result = regex.Replace(selectedText, replaceText);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Regex error: {ex.Message}");
                return;
            }
        }
        else
        {
            count = CountMatches(selectedText, findText, 0, selectedText.Length);
            result = ReplaceInRange(selectedText, findText, replaceText);
        }
        
        if (count > 0)
        {
            _targetTextBox.SelectedText = result;
            _targetTextBox.SelectionStart = selectionStart;
            _targetTextBox.SelectionLength = result.Length;
            UpdateStatus($"Replaced {count} occurrence(s) in selection");
        }
        else
        {
            UpdateStatus("No matches found in selection");
        }
    }

    /// <summary>
    /// Replaces all occurrences in the entire document.
    /// </summary>
    private void ReplaceAll()
    {
        string findText = TxtFind.Text;
        string replaceText = TxtReplace.Text ?? "";
        
        if (string.IsNullOrEmpty(findText))
        {
            UpdateStatus("Please enter text to find");
            return;
        }
        
        string text = _targetTextBox.Text;
        string result;
        int count;
        
        if (ChkUseRegex.IsChecked == true)
        {
            try
            {
                var options = GetRegexOptions();
                var regex = new Regex(findText, options);
                count = regex.Matches(text).Count;
                result = regex.Replace(text, replaceText);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Regex error: {ex.Message}");
                return;
            }
        }
        else
        {
            count = CountMatches(text, findText, 0, text.Length);
            result = ReplaceInRange(text, findText, replaceText);
        }
        
        if (count > 0)
        {
            int caretPosition = _targetTextBox.CaretIndex;
            _targetTextBox.Text = result;
            _targetTextBox.CaretIndex = Math.Min(caretPosition, result.Length);
            UpdateStatus($"Replaced {count} occurrence(s) in document");
        }
        else
        {
            UpdateStatus("No matches found in document");
        }
    }

    /// <summary>
    /// Finds text within a range, either forward or backward.
    /// </summary>
    private int FindInText(string text, string findText, int startIndex, bool forward)
    {
        if (startIndex < 0 || startIndex > text.Length)
            return -1;
        
        if (ChkUseRegex.IsChecked == true)
        {
            try
            {
                var options = GetRegexOptions();
                var regex = new Regex(findText, options);
                
                if (forward)
                {
                    var match = regex.Match(text, startIndex);
                    if (match.Success)
                    {
                        _lastSearchLength = match.Length;
                        return match.Index;
                    }
                }
                else
                {
                    // For backward search with regex, find all matches up to startIndex
                    var matches = regex.Matches(text.Substring(0, startIndex + 1));
                    if (matches.Count > 0)
                    {
                        var lastMatch = matches[matches.Count - 1];
                        _lastSearchLength = lastMatch.Length;
                        return lastMatch.Index;
                    }
                }
            }
            catch
            {
                return -1;
            }
        }
        else
        {
            var comparison = ChkMatchCase.IsChecked == true 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;
            
            if (forward)
            {
                int index = startIndex;
                while (index <= text.Length - findText.Length)
                {
                    int foundIndex = text.IndexOf(findText, index, comparison);
                    if (foundIndex == -1)
                        break;
                    
                    if (!ChkWholeWord.IsChecked == true || IsWholeWord(text, foundIndex, findText.Length))
                    {
                        _lastSearchLength = findText.Length;
                        return foundIndex;
                    }
                    
                    index = foundIndex + 1;
                }
            }
            else
            {
                int index = startIndex;
                while (index >= 0)
                {
                    int foundIndex = text.LastIndexOf(findText, index, index + 1, comparison);
                    if (foundIndex == -1)
                        break;
                    
                    if (!ChkWholeWord.IsChecked == true || IsWholeWord(text, foundIndex, findText.Length))
                    {
                        _lastSearchLength = findText.Length;
                        return foundIndex;
                    }
                    
                    index = foundIndex - 1;
                }
            }
        }
        
        return -1;
    }

    /// <summary>
    /// Checks if the match at the given position is a whole word.
    /// </summary>
    private static bool IsWholeWord(string text, int index, int length)
    {
        if (index > 0 && char.IsLetterOrDigit(text[index - 1]))
            return false;
        
        if (index + length < text.Length && char.IsLetterOrDigit(text[index + length]))
            return false;
        
        return true;
    }

    /// <summary>
    /// Checks if the current selection matches the find text.
    /// </summary>
    private bool IsCurrentSelectionMatch(string findText)
    {
        if (_targetTextBox.SelectionLength == 0)
            return false;
        
        string selectedText = _targetTextBox.SelectedText;
        
        if (ChkUseRegex.IsChecked == true)
        {
            try
            {
                var options = GetRegexOptions();
                var regex = new Regex(findText, options);
                return regex.IsMatch(selectedText) && regex.Match(selectedText).Value == selectedText;
            }
            catch
            {
                return false;
            }
        }
        else
        {
            var comparison = ChkMatchCase.IsChecked == true 
                ? StringComparison.Ordinal 
                : StringComparison.OrdinalIgnoreCase;
            
            return string.Equals(selectedText, findText, comparison);
        }
    }

    /// <summary>
    /// Counts the number of matches in a text range.
    /// </summary>
    private int CountMatches(string text, string findText, int startIndex, int length)
    {
        int count = 0;
        int searchIndex = startIndex;
        int endIndex = startIndex + length;
        
        var comparison = ChkMatchCase.IsChecked == true 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;
        
        while (searchIndex < endIndex)
        {
            int foundIndex = text.IndexOf(findText, searchIndex, endIndex - searchIndex, comparison);
            if (foundIndex == -1)
                break;
            
            if (!ChkWholeWord.IsChecked == true || IsWholeWord(text, foundIndex, findText.Length))
            {
                count++;
            }
            
            searchIndex = foundIndex + 1;
        }
        
        return count;
    }

    /// <summary>
    /// Replaces all occurrences in a text range.
    /// </summary>
    private string ReplaceInRange(string text, string findText, string replaceText)
    {
        var comparison = ChkMatchCase.IsChecked == true 
            ? StringComparison.Ordinal 
            : StringComparison.OrdinalIgnoreCase;
        
        if (!ChkWholeWord.IsChecked == true && !ChkMatchCase.IsChecked == true)
        {
            // Simple case-insensitive replacement
            return Regex.Replace(text, Regex.Escape(findText), replaceText, RegexOptions.IgnoreCase);
        }
        else if (!ChkWholeWord.IsChecked == true)
        {
            // Simple case-sensitive replacement
            return text.Replace(findText, replaceText);
        }
        else
        {
            // Whole word replacement
            var result = text;
            int offset = 0;
            int searchIndex = 0;
            
            while (searchIndex < result.Length)
            {
                int foundIndex = result.IndexOf(findText, searchIndex, comparison);
                if (foundIndex == -1)
                    break;
                
                if (IsWholeWord(result, foundIndex, findText.Length))
                {
                    result = result.Remove(foundIndex, findText.Length);
                    result = result.Insert(foundIndex, replaceText);
                    searchIndex = foundIndex + replaceText.Length;
                }
                else
                {
                    searchIndex = foundIndex + 1;
                }
            }
            
            return result;
        }
    }

    /// <summary>
    /// Selects the found text in the target textbox.
    /// </summary>
    private void SelectFoundText(int foundIndex)
    {
        _lastFoundIndex = foundIndex;
        _targetTextBox.Focus();
        _targetTextBox.SelectionStart = foundIndex;
        _targetTextBox.SelectionLength = _lastSearchLength;
        _targetTextBox.ScrollToLine(_targetTextBox.GetLineIndexFromCharacterIndex(foundIndex));
    }

    /// <summary>
    /// Gets the regex options based on the current settings.
    /// </summary>
    private RegexOptions GetRegexOptions()
    {
        var options = RegexOptions.None;
        
        if (!ChkMatchCase.IsChecked == true)
            options |= RegexOptions.IgnoreCase;
        
        return options;
    }

    /// <summary>
    /// Updates the status message.
    /// </summary>
    private void UpdateStatus(string message)
    {
        TxtStatus.Text = message;
    }
}

/// <summary>
/// Simple relay command implementation for ICommand.
/// </summary>
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}
