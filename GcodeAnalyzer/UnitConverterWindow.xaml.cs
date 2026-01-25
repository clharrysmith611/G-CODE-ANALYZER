using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace GcodeAnalyzer;

/// <summary>
/// Unit converter dialog for converting inches to millimeters.
/// </summary>
public partial class UnitConverterWindow : Window
{
    private const double InchesToMillimeters = 25.4;
    
    /// <summary>
    /// Gets the millimeter value entered/converted.
    /// </summary>
    public double MillimeterValue { get; private set; }
    
    /// <summary>
    /// Gets whether the user clicked "Place" (true) or "Cancel" (false).
    /// </summary>
    public bool WasPlaced { get; private set; }

    public UnitConverterWindow()
    {
        InitializeComponent();
        TxtInches.Focus();
    }

    private void TxtInches_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtInches.Text))
        {
            TxtMillimeters.Text = "";
            MillimeterValue = 0;
            return;
        }

        if (double.TryParse(TxtInches.Text, out double inches))
        {
            MillimeterValue = inches * InchesToMillimeters;
            TxtMillimeters.Text = MillimeterValue.ToString("F3");
        }
        else
        {
            TxtMillimeters.Text = "Invalid input";
            MillimeterValue = 0;
        }
    }

    private void TxtInches_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        // Allow only numbers, decimal point, and minus sign
        var regex = new Regex(@"^[0-9.\-]+$");
        e.Handled = !regex.IsMatch(e.Text);
    }

    private void BtnPlace_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(TxtInches.Text) || MillimeterValue == 0)
        {
            MessageBox.Show("Please enter a valid inch value.", "Invalid Input", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        WasPlaced = true;
        DialogResult = true;
        Close();
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        WasPlaced = false;
        DialogResult = false;
        Close();
    }

    private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        WasPlaced = false;
        DialogResult = false;
        Close();
    }

    private void PlaceCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        BtnPlace_Click(sender, e);
    }
}
