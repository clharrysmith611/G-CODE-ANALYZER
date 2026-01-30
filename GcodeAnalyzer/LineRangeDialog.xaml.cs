using System.Windows;

namespace GcodeAnalyzer;

/// <summary>
/// Dialog for entering a line range for visibility filtering.
/// </summary>
public partial class LineRangeDialog : Window
{
    /// <summary>
    /// Gets the start line number entered by the user.
    /// </summary>
    public int StartLine { get; private set; }

    /// <summary>
    /// Gets the end line number entered by the user.
    /// </summary>
    public int EndLine { get; private set; }

    public LineRangeDialog(string title, string prompt)
    {
        InitializeComponent();
        Title = title;
        TxtPrompt.Text = prompt;
        Owner = Application.Current.MainWindow;
    }

    private void BtnOK_Click(object sender, RoutedEventArgs e)
    {
        if (!int.TryParse(TxtStartLine.Text, out int startLine) || startLine < 1)
        {
            MessageBox.Show("Please enter a valid start line number (1 or greater).",
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtStartLine.Focus();
            return;
        }

        if (!int.TryParse(TxtEndLine.Text, out int endLine) || endLine < 1)
        {
            MessageBox.Show("Please enter a valid end line number (1 or greater).",
                "Invalid Input", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtEndLine.Focus();
            return;
        }

        if (endLine < startLine)
        {
            MessageBox.Show("End line must be greater than or equal to start line.",
                "Invalid Range", MessageBoxButton.OK, MessageBoxImage.Warning);
            TxtEndLine.Focus();
            return;
        }

        StartLine = startLine;
        EndLine = endLine;
        DialogResult = true;
    }
}
