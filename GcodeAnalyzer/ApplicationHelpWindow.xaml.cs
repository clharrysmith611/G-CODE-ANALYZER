using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GcodeAnalyzer;

/// <summary>
/// Application help window with comprehensive feature documentation.
/// </summary>
public partial class ApplicationHelpWindow : Window
{
    private static readonly Brush HeaderBrush = new SolidColorBrush(Color.FromRgb(0, 212, 255));
    private static readonly Brush SubHeaderBrush = new SolidColorBrush(Color.FromRgb(51, 153, 255));
    private static readonly Brush CommandBrush = new SolidColorBrush(Color.FromRgb(255, 215, 0));
    private static readonly Brush ParameterBrush = new SolidColorBrush(Color.FromRgb(152, 251, 152));
    private static readonly Brush DescriptionBrush = new SolidColorBrush(Color.FromRgb(224, 224, 224));
    private static readonly Brush DimBrush = new SolidColorBrush(Color.FromRgb(158, 158, 158));
    private static readonly Brush ExampleBrush = new SolidColorBrush(Color.FromRgb(255, 182, 193));
    private static readonly Brush WarningBrush = new SolidColorBrush(Color.FromRgb(255, 165, 0));
    private static readonly Brush TipBrush = new SolidColorBrush(Color.FromRgb(50, 205, 50));

    public ApplicationHelpWindow()
    {
        InitializeComponent();
        ShowWelcome();
    }

    private void NavTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        if (e.NewValue is TreeViewItem item && item.Tag is string tag)
        {
            ShowContent(tag);
            ContentScroller?.ScrollToTop();
        }
    }

    private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
    {
        string searchText = TxtSearch.Text.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(searchText))
        {
            ShowWelcome();
            return;
        }
        ShowSearchResults(searchText);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e) => Close();

    private readonly List<string> _articleOrder =
    [
        "Welcome", "GettingStarted", "LoadingFiles", "CreatingFiles", "Interface",
        "Analysis", "Statistics", "Errors", "Filters",
        "Visualization", "View2D", "View3D", "Dimensions", "Colors",
        "Simulation", "Playback", "Speed", "Stepping",
        "Editor", "EditorOverview", "FindReplace", "UnitConverter", "Highlighting", "Preview", "Saving",
        "Macros", "UsingMacros", "EditingMacros", "DefaultMacros",
        "Shortcuts", "Tips", "BestPractices", "Workflow"
    ];

    private string _currentArticle = "Welcome";

    private void BtnPrevious_Click(object sender, RoutedEventArgs e)
    {
        int currentIndex = _articleOrder.IndexOf(_currentArticle);
        if (currentIndex > 0)
        {
            string previousArticle = _articleOrder[currentIndex - 1];
            NavigateToArticle(previousArticle);
        }
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        int currentIndex = _articleOrder.IndexOf(_currentArticle);
        if (currentIndex < _articleOrder.Count - 1)
        {
            string nextArticle = _articleOrder[currentIndex + 1];
            NavigateToArticle(nextArticle);
        }
    }

    private void NavigateToArticle(string tag)
    {
        _currentArticle = tag;
        ShowContent(tag);
        ContentScroller?.ScrollToTop();
        
        // Update tree view selection
        SelectTreeItemByTag(NavTree.Items, tag);
        
        // Update button states
        UpdateNavigationButtons();
    }

    private void SelectTreeItemByTag(ItemCollection items, string tag)
    {
        foreach (var item in items)
        {
            if (item is TreeViewItem treeItem)
            {
                if (treeItem.Tag is string itemTag && itemTag == tag)
                {
                    treeItem.IsSelected = true;
                    treeItem.BringIntoView();
                    return;
                }
                
                if (treeItem.Items.Count > 0)
                {
                    SelectTreeItemByTag(treeItem.Items, tag);
                }
            }
        }
    }

    private void UpdateNavigationButtons()
    {
        int currentIndex = _articleOrder.IndexOf(_currentArticle);
        
        if (BtnPrevious != null)
        {
            BtnPrevious.IsEnabled = currentIndex > 0;
        }
        
        if (BtnNext != null)
        {
            BtnNext.IsEnabled = currentIndex < _articleOrder.Count - 1;
        }
    }

    private void ShowContent(string tag)
    {
        if (ContentPanel == null) return;
        ContentPanel.Children.Clear();

        _currentArticle = tag;
        UpdateNavigationButtons();

        switch (tag)
        {
            case "Welcome": ShowWelcome(); break;
            case "GettingStarted": ShowGettingStarted(); break;
            case "LoadingFiles": ShowLoadingFiles(); break;
            case "CreatingFiles": ShowCreatingFiles(); break;
            case "Interface": ShowInterface(); break;
            case "Analysis": ShowAnalysis(); break;
            case "Statistics": ShowStatistics(); break;
            case "Errors": ShowErrors(); break;
            case "Filters": ShowFilters(); break;
            case "Visualization": ShowVisualization(); break;
            case "View2D": ShowView2D(); break;
            case "View3D": ShowView3D(); break;
            case "Dimensions": ShowDimensions(); break;
            case "Colors": ShowColors(); break;
            case "Simulation": ShowSimulation(); break;
            case "Playback": ShowPlayback(); break;
            case "Speed": ShowSpeed(); break;
            case "Stepping": ShowStepping(); break;
            case "Editor": ShowEditor(); break;
            case "EditorOverview": ShowEditorOverview(); break;
            case "FindReplace": ShowFindReplace(); break;
            case "UnitConverter": ShowUnitConverter(); break;
            case "Highlighting": ShowHighlighting(); break;
            case "Preview": ShowPreview(); break;
            case "Saving": ShowSaving(); break;
            case "Macros": ShowMacros(); break;
            case "UsingMacros": ShowUsingMacros(); break;
            case "EditingMacros": ShowEditingMacros(); break;
            case "DefaultMacros": ShowDefaultMacros(); break;
            case "Shortcuts": ShowShortcuts(); break;
            case "Tips": ShowTips(); break;
            case "BestPractices": ShowBestPractices(); break;
            case "Workflow": ShowWorkflow(); break;
            default: ShowWelcome(); break;
        }
    }

    #region Content Sections

    private void ShowWelcome()
    {
        if (ContentPanel == null) return;
        ContentPanel.Children.Clear();

        AddHeader("Welcome to G-code Analyzer");
        AddParagraph("G-code Analyzer is a comprehensive tool for analyzing, visualizing, and editing G-code files for CNC machines. Whether you're debugging toolpaths, checking for errors, or creating new programs, this application provides the tools you need.");

        AddSubHeader("Key Features");
        AddBulletList(
            "Load and analyze G-code files (.nc, .gcode, .ngc, .tap)",
            "View detailed statistics about your toolpath",
            "Detect common errors and issues in your G-code",
            "Visualize toolpaths in both 2D and 3D views",
            "Simulate toolpath execution with playback controls",
            "Edit G-code with syntax-aware error highlighting",
            "Find and replace text with advanced search options",
            "Convert between inches and millimeters instantly",
            "Use keyboard macros for quick G-code insertion",
            "Measure part dimensions in mm and inches"
        );

        AddSubHeader("Quick Start");
        AddNumberedList(
            "Click 'Select G-code File...' to load a file",
            "Review the analysis results and any detected errors",
            "Switch between 2D and 3D views to visualize the toolpath",
            "Use the simulation controls to play through the program",
            "Click 'Edit / Create G-code File...' to make changes"
        );

        AddTip("Use the navigation panel on the left to explore specific features in detail.");
    }

    private void ShowGettingStarted()
    {
        AddHeader("Getting Started");
        AddParagraph("This section covers the basics of using G-code Analyzer, from loading your first file to understanding the interface.");

        AddSubHeader("System Requirements");
        AddBulletList(
            "Windows 10 or later",
            ".NET 9 Runtime",
            "Graphics card with DirectX 10 support (for 3D view)"
        );

        AddSubHeader("Supported File Formats");
        AddBulletList(
            ".nc - Standard NC file format",
            ".gcode - Common G-code format",
            ".ngc - EMC/LinuxCNC format",
            ".tap - Generic tap file format",
            "Any text file containing G-code"
        );

        AddTip("The application automatically detects the file format based on content, not just the extension.");
    }

    private void ShowLoadingFiles()
    {
        AddHeader("Loading G-code Files");
        AddParagraph("To analyze a G-code file, you first need to load it into the application.");

        AddSubHeader("Steps to Load a File");
        AddNumberedList(
            "Click the 'Select G-code File...' button in the left panel",
            "Navigate to your G-code file in the file dialog",
            "Select the file and click 'Open'",
            "The file will be automatically analyzed and displayed"
        );

        AddSubHeader("After Loading");
        AddParagraph("Once a file is loaded, the application will:");
        AddBulletList(
            "Parse and validate all G-code commands",
            "Display statistics in the Analysis Results panel",
            "Show any detected errors in the Errors list",
            "Render the toolpath in the visualization area"
        );

        AddTip("The file path is shown at the bottom of the left panel after loading.");
    }

    private void ShowCreatingFiles()
    {
        AddHeader("Creating New G-code Files");
        AddParagraph("You can create new G-code files directly within the application.");

        AddSubHeader("Steps to Create a New File");
        AddNumberedList(
            "Click 'Edit / Create G-code File...' without loading a file first",
            "You'll be prompted to create a new file",
            "Choose a location and filename for your new file",
            "The editor will open with an empty file ready for editing"
        );

        AddSubHeader("Editor Features for New Files");
        AddBulletList(
            "Full G-code editing with syntax support",
            "Real-time error highlighting as you type",
            "Macro shortcuts for common G-code snippets",
            "Preview changes before saving"
        );

        AddTip("Use macros (Ctrl+1 through Ctrl+0) to quickly insert common G-code patterns.");
    }

    private void ShowInterface()
    {
        AddHeader("Interface Overview");
        AddParagraph("The G-code Analyzer interface is divided into several key areas:");

        AddSubHeader("Left Panel");
        AddBulletList(
            "File Selection - Load or create G-code files",
            "Analysis Results - Statistics about the loaded file",
            "Error Filters - Toggle visibility of specific error types",
            "Errors List - Detected issues in your G-code"
        );

        AddSubHeader("Right Panel - Visualization");
        AddBulletList(
            "2D View Tab - Top-down view of the toolpath",
            "3D View Tab - Interactive 3D visualization",
            "Legend - Color coding for different move types",
            "View controls for navigation and reset"
        );

        AddSubHeader("Resizable Layout");
        AddParagraph("Drag the splitter between the left and right panels to adjust the layout to your preference.");
    }

    private void ShowAnalysis()
    {
        AddHeader("Analysis Features");
        AddParagraph("G-code Analyzer provides comprehensive analysis of your G-code files to help you understand and debug your toolpaths.");

        AddSubHeader("Automatic Analysis");
        AddParagraph("When you load a file, the application automatically:");
        AddBulletList(
            "Parses every line of G-code",
            "Validates command syntax and parameters",
            "Calculates toolpath statistics",
            "Detects common errors and issues",
            "Renders the toolpath visualization"
        );
    }

    private void ShowStatistics()
    {
        AddHeader("Statistics Panel");
        AddParagraph("The Analysis Results panel displays comprehensive statistics about your G-code file.");

        AddSubHeader("Available Statistics");
        AddParameter("Total Commands", "Number of G-code commands in the file");
        AddParameter("Rapid Moves (G0)", "Count of rapid positioning moves");
        AddParameter("Linear Moves (G1)", "Count of cutting/linear moves");
        AddParameter("Arc Moves (G2/G3)", "Count of circular interpolation moves");
        AddParameter("Total Distance", "Combined distance of all moves");
        AddParameter("Cutting Distance", "Distance of cutting moves only (G1, G2, G3)");
        AddParameter("Rapid Distance", "Distance of rapid moves only (G0)");
        AddParameter("Bounding Box", "X, Y, Z dimensions of the toolpath");

        AddTip("Use these statistics to estimate machining time and verify your toolpath dimensions.");
    }

    private void ShowErrors()
    {
        AddHeader("Error Detection");
        AddParagraph("G-code Analyzer automatically detects common errors and issues in your G-code files.");

        AddSubHeader("Types of Errors Detected");
        AddParameter("Syntax Errors", "Invalid or malformed G-code commands");
        AddParameter("Unknown Commands", "Unrecognized G or M codes");
        AddParameter("Missing Parameters", "Commands missing required values");
        AddParameter("Invalid Values", "Parameters with out-of-range values");
        AddParameter("Redundant Moves", "Moves to the current position (zero-length)");
        AddParameter("Duplicate Toolpaths", "Repeated identical movements");

        AddSubHeader("Error List");
        AddParagraph("Errors are displayed in the Errors panel with:");
        AddBulletList(
            "Line number where the error occurs",
            "Description of the issue",
            "The problematic code (when applicable)"
        );

        AddWarning("Always review and address errors before running G-code on your machine!");
    }

    private void ShowFilters()
    {
        AddHeader("Error Filters");
        AddParagraph("Use the error filter checkboxes to control which types of errors are displayed.");

        AddSubHeader("Available Filters");
        AddParameter("Hide redundant move errors", "Hides zero-length move warnings");
        AddParameter("Hide duplicate toolpath errors", "Hides repeated path warnings");

        AddSubHeader("Why Use Filters?");
        AddParagraph("Some CAM software generates G-code with intentional redundant moves or duplicate paths. These aren't always errors, so you can hide them to focus on more critical issues.");

        AddTip("The editor also respects these filter settings for error highlighting.");
    }

    private void ShowVisualization()
    {
        AddHeader("Visualization Features");
        AddParagraph("G-code Analyzer provides powerful visualization tools to help you understand your toolpaths.");

        AddSubHeader("Available Views");
        AddBulletList(
            "2D View - Top-down (XY plane) view of the toolpath",
            "3D View - Fully interactive 3D visualization"
        );

        AddSubHeader("Common Features");
        AddBulletList(
            "Color-coded move types (rapid, linear, arcs)",
            "Direction arrows showing tool movement",
            "Start point (green) and end point (red) markers",
            "Dimension measurements in mm and inches"
        );
    }

    private void ShowView2D()
    {
        AddHeader("2D View");
        AddParagraph("The 2D view shows a top-down representation of your toolpath, perfect for quick verification.");

        AddSubHeader("Navigation Controls");
        AddParameter("Left Mouse Drag", "Pan the view");
        AddParameter("Right Mouse Drag", "Rotate the view around Z axis");
        AddParameter("Mouse Wheel", "Zoom in/out (centered on cursor)");
        AddParameter("Reset View Button", "Return to default view");

        AddSubHeader("Display Elements");
        AddBulletList(
            "Toolpath lines with direction arrows",
            "Green circle marks the start point",
            "Red circle marks the end point",
            "Dimension brackets showing width and height"
        );

        AddTip("The 2D view is ideal for quickly checking part dimensions and overall toolpath shape.");
    }

    private void ShowView3D()
    {
        AddHeader("3D View");
        AddParagraph("The 3D view provides a fully interactive visualization of your toolpath in three dimensions.");

        AddSubHeader("Navigation Controls");
        AddParameter("Left Mouse Drag", "Pan the view");
        AddParameter("Right Mouse Drag", "Rotate the camera around the toolpath");
        AddParameter("Mouse Wheel", "Zoom in/out");
        AddParameter("Reset View", "Fit the entire toolpath in view");

        AddSubHeader("Preset Views");
        AddParameter("Top", "Look down at XY plane (Z axis view)");
        AddParameter("Front", "Look at XZ plane from front");
        AddParameter("Side", "Look at YZ plane from right side");

        AddSubHeader("Display Features");
        AddBulletList(
            "Grid showing XY plane at Z minimum",
            "Coordinate system indicator (XYZ axes)",
            "View cube for orientation reference",
            "Dimension measurements for X, Y, and Z",
            "Tool pointer during simulation"
        );
    }

    private void ShowDimensions()
    {
        AddHeader("Dimension Measurements");
        AddParagraph("Both 2D and 3D views display dimension measurements to help you verify your toolpath size.");

        AddSubHeader("2D View Dimensions");
        AddBulletList(
            "Width (X) - Horizontal dimension shown below the part",
            "Height (Y) - Vertical dimension shown to the right",
            "Values displayed in both mm and inches"
        );

        AddSubHeader("3D View Dimensions");
        AddBulletList(
            "X dimension - Width along the X axis",
            "Y dimension - Depth along the Y axis",
            "Z dimension - Height along the Z axis",
            "All values in mm and inches"
        );

        AddTip("Dimension brackets include extension lines and tick marks similar to engineering drawings.");
    }

    private void ShowColors()
    {
        AddHeader("Toolpath Colors");
        AddParagraph("Different types of G-code moves are displayed in different colors for easy identification.");

        AddSubHeader("Color Legend");
        AddColorItem("Orange", "Rapid moves (G0) - Non-cutting positioning moves");
        AddColorItem("DodgerBlue", "Linear moves (G1) - Cutting moves in straight lines");
        AddColorItem("LimeGreen", "Arc CW (G2) - Clockwise circular interpolation");
        AddColorItem("MediumPurple", "Arc CCW (G3) - Counter-clockwise arcs");

        AddSubHeader("Markers");
        AddColorItem("Green circle", "Start point of the toolpath");
        AddColorItem("Red circle", "End point of the toolpath");
        AddColorItem("Yellow sphere", "Tool position during simulation (3D view)");

        AddTip("Rapid moves (G0) are shown with dashed lines in the 2D view.");
    }

    private void ShowSimulation()
    {
        AddHeader("Toolpath Simulation");
        AddParagraph("The 3D view includes a powerful simulation feature that plays through your G-code program, showing the tool position over time.");

        AddSubHeader("Simulation Features");
        AddBulletList(
            "Animated tool pointer showing current position",
            "Timeline slider for scrubbing through the program",
            "Real-time display of current line number",
            "Playback speed control from 0.25x to 32x",
            "Step-by-step line navigation"
        );

        AddTip("The simulation respects feed rates from your G-code for accurate timing.");
    }

    private void ShowPlayback()
    {
        AddHeader("Playback Controls");
        AddParagraph("The simulation control bar appears at the bottom of the 3D view when a file is loaded.");

        AddSubHeader("Control Buttons");
        AddParameter("? (Skip to Start)", "Jump to the beginning of the program");
        AddParameter("? (Previous Line)", "Step back to the previous G-code line");
        AddParameter("? (Slower)", "Decrease playback speed");
        AddParameter("?/? (Play/Pause)", "Start or pause the simulation");
        AddParameter("? (Faster)", "Increase playback speed");
        AddParameter("? (Next Line)", "Step forward to the next G-code line");
        AddParameter("? (Skip to End)", "Jump to the end of the program");

        AddSubHeader("Timeline Slider");
        AddParagraph("Drag the timeline slider to jump to any point in the simulation. The current time and total duration are displayed on either side.");
    }

    private void ShowSpeed()
    {
        AddHeader("Speed Control");
        AddParagraph("Control how fast the simulation plays relative to real-time execution.");

        AddSubHeader("Available Speeds");
        AddBulletList(
            "0.25x - Quarter speed (slow motion)",
            "0.5x - Half speed",
            "1x - Real-time (based on feed rates)",
            "2x - Double speed",
            "4x, 8x, 16x, 32x - Fast forward speeds"
        );

        AddSubHeader("Speed Display");
        AddParagraph("The current playback speed is shown in the control bar next to the playback buttons.");

        AddTip("Use slower speeds to carefully examine complex sections of your toolpath.");
    }

    private void ShowStepping()
    {
        AddHeader("Line Stepping");
        AddParagraph("Step through your G-code program one line at a time for detailed analysis.");

        AddSubHeader("How to Step");
        AddBulletList(
            "Click ? (Previous Line) to move to the previous G-code command",
            "Click ? (Next Line) to move to the next G-code command",
            "Stepping automatically pauses playback"
        );

        AddSubHeader("Current Line Display");
        AddParagraph("The current line number is shown in the bottom-right corner of the 3D view. This updates during playback and when stepping.");

        AddTip("Line stepping is perfect for debugging specific sections of your program.");
    }

    private void ShowEditor()
    {
        AddHeader("G-code Editor");
        AddParagraph("The built-in G-code editor provides a powerful environment for creating and modifying G-code files.");

        AddSubHeader("Editor Features");
        AddBulletList(
            "Syntax-aware editing with monospace font",
            "Real-time error detection and highlighting",
            "Line numbers with error indicators",
            "Undo/Redo support",
            "Preview changes before saving",
            "Automatic backup creation",
            "Keyboard macros for quick insertion"
        );

        AddTip("Open the editor by clicking 'Edit / Create G-code File...' in the main window.");
    }

    private void ShowEditorOverview()
    {
        AddHeader("Editor Overview");
        AddParagraph("The G-code Editor window provides a dedicated environment for editing your G-code files.");

        AddSubHeader("Window Layout");
        AddParameter("Menu Bar", "File operations and edit commands");
        AddParameter("Toolbar", "Quick access buttons for common actions");
        AddParameter("Line Numbers", "Shows line numbers with error highlighting");
        AddParameter("Editor Area", "Main text editing area");
        AddParameter("Status Bar", "File path, modification status, and cursor position");

        AddSubHeader("Toolbar Buttons");
        AddParameter("?? Save", "Save the current file");
        AddParameter("?? Save As", "Save to a new file");
        AddParameter("? Undo / ? Redo", "Undo or redo changes");
        AddParameter("?? Find & Replace", "Search and replace text (Ctrl+F)");
        AddParameter("?? Preview Changes", "Update visualization without saving (F6)");
        AddParameter("?? Unit Converter", "Convert inches to millimeters (Ctrl+U)");
        AddParameter("? Macros", "Open the macro editor");
        AddParameter("? G-code Help", "Open G-code command reference (F1)");
    }

    private void ShowFindReplace()
    {
        AddHeader("Find and Replace");
        AddParagraph("Quickly search for and replace text in your G-code files to make bulk edits or corrections.");

        AddSubHeader("Opening Find and Replace");
        AddBulletList(
            "Click the '?? Find & Replace' button in the toolbar",
            "Press Ctrl+F or Ctrl+H from the editor",
            "Select Tools > Find and Replace from the menu"
        );

        AddSubHeader("Search Options");
        AddParameter("Match case", "Distinguish between uppercase and lowercase letters");
        AddParameter("Match whole word only", "Find only complete words, not partial matches");
        AddParameter("Use regular expressions", "Enable advanced pattern matching with regex");
        AddParameter("Wrap around", "Continue searching from the beginning when reaching the end");

        AddSubHeader("Replace Scope");
        AddParameter("Replace next occurrence", "Replace only the next match found");
        AddParameter("Replace in selection", "Replace all matches within the selected text");
        AddParameter("Replace all in document", "Replace all matches throughout the entire file");

        AddSubHeader("Navigation");
        AddParameter("Find Next", "Move to the next occurrence");
        AddParameter("Find Previous", "Move to the previous occurrence");
        AddParameter("F3", "Quick Find Next (when dialog is open)");
        AddParameter("Shift+F3", "Quick Find Previous (when dialog is open)");

        AddSubHeader("Tips for Effective Searching");
        AddBulletList(
            "Use 'Match whole word' when searching for G-code commands (e.g., G0 vs G01)",
            "Enable 'Match case' to distinguish between parameters (X vs x)",
            "Test regex patterns carefully before using Replace All",
            "Use 'Replace in selection' for localized changes",
            "The dialog remains open so you can make multiple searches"
        );

        AddWarning("Always preview your file after using Replace All to ensure correct results!");
    }

    private void ShowUnitConverter()
    {
        AddHeader("Unit Converter");
        AddParagraph("Convert between inches and millimeters to quickly adjust coordinate values in your G-code.");

        AddSubHeader("Opening the Converter");
        AddBulletList(
            "Click the '?? Unit Converter' button in the toolbar",
            "Press Ctrl+U from the editor",
            "Select Tools > Unit Converter from the menu"
        );

        AddSubHeader("How to Use");
        AddNumberedList(
            "Enter a value in inches in the input field",
            "The converted millimeter value appears instantly",
            "Click 'Place in G-code' to insert at cursor position",
            "Or manually copy the converted value"
        );

        AddSubHeader("Conversion Formula");
        AddParameter("Inches to MM", "1 inch = 25.4 millimeters (exact)");
        AddParameter("MM to Inches", "1 millimeter = 0.03937 inches (approximate)");

        AddSubHeader("Practical Examples");
        AddParameter("0.125 inches", "3.175 mm (1/8 inch)");
        AddParameter("0.250 inches", "6.350 mm (1/4 inch)");
        AddParameter("1.000 inches", "25.400 mm");
        AddParameter("10.000 mm", "0.394 inches");

        AddSubHeader("Common Uses");
        AddBulletList(
            "Converting tool diameters from imperial to metric",
            "Adjusting feed rates between unit systems",
            "Converting part dimensions when switching between G20 and G21",
            "Verifying coordinate values in mixed-unit drawings"
        );

        AddTip("The converter uses the standard conversion factor of 25.4mm per inch for precision.");
    }

    private void ShowHighlighting()
    {
        AddHeader("Error Highlighting");
        AddParagraph("The editor highlights lines with errors to help you quickly identify and fix issues.");

        AddSubHeader("How It Works");
        AddBulletList(
            "Lines with errors show a red line number",
            "Error lines have a subtle red background",
            "The error count is shown in the status bar",
            "Highlighting updates automatically as you type"
        );

        AddSubHeader("Error Filters");
        AddParagraph("The editor respects the same error filter settings as the main window. If you've hidden redundant move or duplicate path errors, they won't be highlighted in the editor.");

        AddTip("Error highlighting updates automatically when you press Enter after editing a line.");
    }

    private void ShowPreview()
    {
        AddHeader("Preview Changes");
        AddParagraph("Preview your changes in the 2D and 3D views without saving the file.");

        AddSubHeader("How to Preview");
        AddBulletList(
            "Make your changes in the editor",
            "Click '?? Preview Changes' or press F6",
            "The main window's visualization will update",
            "The file on disk remains unchanged"
        );

        AddSubHeader("Benefits");
        AddBulletList(
            "Test changes before committing them",
            "Quickly iterate on modifications",
            "Compare changes with the original file",
            "Avoid accidental overwrites"
        );

        AddTip("Use preview to verify your changes are correct before saving.");
    }

    private void ShowSaving()
    {
        AddHeader("Saving Files");
        AddParagraph("The editor provides multiple ways to save your work.");

        AddSubHeader("Save Options");
        AddParameter("Save (Ctrl+S)", "Save to the current file");
        AddParameter("Save As (Ctrl+Shift+S)", "Save to a new file");

        AddSubHeader("Automatic Backups");
        AddParagraph("When you save, the editor automatically creates a backup of the existing file with a versioned name (e.g., filename.OLD001.nc). This protects against accidental data loss.");

        AddSubHeader("Unsaved Changes Warning");
        AddParagraph("If you try to close the editor with unsaved changes, you'll be prompted to save or discard your changes.");

        AddWarning("Always verify your G-code after saving before running on your machine!");
    }

    private void ShowMacros()
    {
        AddHeader("G-code Macros");
        AddParagraph("Macros let you quickly insert commonly-used G-code snippets using keyboard shortcuts.");

        AddSubHeader("Macro System Features");
        AddBulletList(
            "10 customizable macros (Ctrl+1 through Ctrl+0)",
            "Each macro can contain multiple lines of G-code",
            "Macros are saved between sessions",
            "Default macros for common operations"
        );

        AddTip("Macros save time and ensure consistency in your G-code programs.");
    }

    private void ShowUsingMacros()
    {
        AddHeader("Using Macros");
        AddParagraph("Insert macros quickly while editing using keyboard shortcuts.");

        AddSubHeader("How to Use");
        AddNumberedList(
            "Position your cursor where you want to insert G-code",
            "Press Ctrl + a number key (1-9 or 0)",
            "The macro content is inserted at the cursor",
            "Automatic newline handling ensures proper formatting"
        );

        AddSubHeader("Keyboard Shortcuts");
        AddParameter("Ctrl+1", "Insert Macro 1");
        AddParameter("Ctrl+2", "Insert Macro 2");
        AddParameter("...", "(and so on)");
        AddParameter("Ctrl+0", "Insert Macro 10");

        AddTip("The macro system intelligently adds newlines before/after the inserted code.");
    }

    private void ShowEditingMacros()
    {
        AddHeader("Editing Macros");
        AddParagraph("Customize macros to match your workflow.");

        AddSubHeader("Opening the Macro Editor");
        AddBulletList(
            "In the G-code Editor, click '? Macros' in the toolbar",
            "The Macro Editor window will open"
        );

        AddSubHeader("Editing a Macro");
        AddNumberedList(
            "Select a macro from the list on the left",
            "Edit the macro name in the Name field",
            "Edit the G-code content in the content area",
            "Click 'Save' to save all changes"
        );

        AddSubHeader("Storage Location");
        AddParagraph("Macros are saved to: %APPDATA%\\GcodeAnalyzer\\macros.json");

        AddTip("You can manually edit the macros.json file if needed.");
    }

    private void ShowDefaultMacros()
    {
        AddHeader("Default Macros");
        AddParagraph("The application comes with sensible default macros for common operations.");

        AddSubHeader("Default Macro List");
        AddParameter("Ctrl+1 - Initialize", "G21 G90, G17 (metric, absolute, XY plane)");
        AddParameter("Ctrl+2 - Home All", "G28 (return to home)");
        AddParameter("Ctrl+3 - Spindle On", "M3 S10000 (clockwise at 10000 RPM)");
        AddParameter("Ctrl+4 - Spindle Off", "M5 (stop spindle)");
        AddParameter("Ctrl+5 - Rapid Retract", "G0 Z10 (rapid to Z=10)");
        AddParameter("Ctrl+6 - Coolant On", "M8 (flood coolant)");
        AddParameter("Ctrl+7 - Coolant Off", "M9 (coolant off)");
        AddParameter("Ctrl+8 - Program End", "M5, M9, G28, M30 (safe shutdown)");
        AddParameter("Ctrl+9 - Dwell 2s", "G4 P2 (pause 2 seconds)");
        AddParameter("Ctrl+0 - Comment", "(Insert comment here)");

        AddSubHeader("Resetting to Defaults");
        AddParagraph("Click '? Reset to Defaults' in the Macro Editor to restore the default macros.");
    }

    private void ShowShortcuts()
    {
        AddHeader("Keyboard Shortcuts");
        AddParagraph("Quick reference for all keyboard shortcuts in the application.");

        AddSubHeader("Main Window");
        AddParagraph("No dedicated shortcuts - use buttons and mouse interaction.");

        AddSubHeader("G-code Editor - File Operations");
        AddParameter("Ctrl+S", "Save file");
        AddParameter("Ctrl+Shift+S", "Save As");
        AddParameter("F6", "Preview changes");
        AddParameter("F1", "Open G-code Help");

        AddSubHeader("G-code Editor - Edit Commands");
        AddParameter("Ctrl+Z", "Undo");
        AddParameter("Ctrl+Y", "Redo");
        AddParameter("Ctrl+X", "Cut");
        AddParameter("Ctrl+C", "Copy");
        AddParameter("Ctrl+V", "Paste");
        AddParameter("Ctrl+A", "Select All");

        AddSubHeader("G-code Editor - Tools");
        AddParameter("Ctrl+F", "Open Find and Replace dialog");
        AddParameter("Ctrl+H", "Open Find and Replace dialog (alternate)");
        AddParameter("Ctrl+U", "Open Unit Converter");

        AddSubHeader("Find and Replace Dialog");
        AddParameter("Enter", "Find Next (from search box)");
        AddParameter("Shift+Enter", "Find Previous (from search box)");
        AddParameter("F3", "Find Next (when dialog is open)");
        AddParameter("Shift+F3", "Find Previous (when dialog is open)");
        AddParameter("Escape", "Close the dialog");

        AddSubHeader("Macro Shortcuts (in Editor)");
        AddParameter("Ctrl+1 to Ctrl+9", "Insert macros 1-9");
        AddParameter("Ctrl+0", "Insert macro 10");

        AddSubHeader("Help Windows");
        AddParameter("Escape", "Close the help window");
    }

    private void ShowTips()
    {
        AddHeader("Tips & Workflows");
        AddParagraph("Learn best practices and efficient workflows for using G-code Analyzer.");

        AddSubHeader("Getting the Most from the Application");
        AddBulletList(
            "Use the 2D view for quick dimension checks",
            "Use the 3D view for understanding complex toolpaths",
            "Simulate your program before running on the machine",
            "Check errors after every edit",
            "Customize macros for your specific machine"
        );
    }

    private void ShowBestPractices()
    {
        AddHeader("Best Practices");
        AddParagraph("Follow these recommendations for the best experience.");

        AddSubHeader("Before Loading Files");
        AddBulletList(
            "Ensure your G-code is from a trusted source",
            "Keep backups of original files",
            "Know your machine's capabilities and limits"
        );

        AddSubHeader("During Analysis");
        AddBulletList(
            "Review all errors, not just the first few",
            "Use filters to focus on critical issues",
            "Verify dimensions match your expectations",
            "Check start and end positions"
        );

        AddSubHeader("When Editing");
        AddBulletList(
            "Use Preview to test changes before saving",
            "Take advantage of macros for consistency",
            "Re-check errors after major edits",
            "Backup important files before editing"
        );

        AddWarning("Always simulate and verify G-code before running on your CNC machine!");
    }

    private void ShowWorkflow()
    {
        AddHeader("Typical Workflow");
        AddParagraph("A step-by-step workflow for analyzing and editing G-code files.");

        AddSubHeader("1. Load and Analyze");
        AddNumberedList(
            "Click 'Select G-code File...'",
            "Choose your G-code file",
            "Review the statistics panel",
            "Check for errors in the error list"
        );

        AddSubHeader("2. Visualize");
        AddNumberedList(
            "Examine the toolpath in 2D view",
            "Switch to 3D view for depth information",
            "Verify dimensions match your design",
            "Check start/end positions"
        );

        AddSubHeader("3. Simulate");
        AddNumberedList(
            "Use the simulation controls to play the program",
            "Watch for unexpected movements",
            "Use line stepping for detailed analysis",
            "Note any issues to fix"
        );

        AddSubHeader("4. Edit (if needed)");
        AddNumberedList(
            "Click 'Edit / Create G-code File...'",
            "Make necessary corrections",
            "Use Preview to verify changes",
            "Save when satisfied"
        );

        AddSubHeader("5. Final Verification");
        AddNumberedList(
            "Re-analyze the modified file",
            "Confirm all errors are resolved",
            "Run a final simulation",
            "Transfer to your CNC machine"
        );
    }

    private void ShowSearchResults(string searchText)
    {
        if (ContentPanel == null) return;
        ContentPanel.Children.Clear();

        AddHeader($"Search Results for \"{searchText}\"");

        var topics = GetSearchableTopics();
        var matches = topics.Where(t =>
            t.Title.ToLowerInvariant().Contains(searchText) ||
            t.Keywords.ToLowerInvariant().Contains(searchText)).ToList();

        if (matches.Count == 0)
        {
            AddParagraph("No topics found matching your search. Try different keywords.");
            return;
        }

        AddParagraph($"Found {matches.Count} matching topic(s):");

        foreach (var topic in matches)
        {
            AddCommandSummary(topic.Tag, topic.Title, topic.Description);
        }
    }

    private static List<(string Tag, string Title, string Description, string Keywords)> GetSearchableTopics()
    {
        return
        [
            ("LoadingFiles", "Loading Files", "How to load G-code files", "load open file select"),
            ("CreatingFiles", "Creating Files", "Create new G-code files", "create new file"),
            ("Statistics", "Statistics", "View toolpath statistics", "stats statistics analysis distance"),
            ("Errors", "Error Detection", "Find and fix errors", "error warning issue problem"),
            ("Filters", "Error Filters", "Filter error types", "filter hide redundant duplicate"),
            ("View2D", "2D View", "Top-down toolpath view", "2d view top xy"),
            ("View3D", "3D View", "Interactive 3D visualization", "3d view rotate zoom"),
            ("Dimensions", "Dimensions", "Measure part size", "dimension measure size mm inch"),
            ("Simulation", "Simulation", "Animate toolpath execution", "simulate play animation"),
            ("Playback", "Playback Controls", "Control simulation playback", "play pause stop rewind"),
            ("EditorOverview", "Editor", "G-code editing features", "edit editor modify"),
            ("FindReplace", "Find and Replace", "Search and replace text", "find replace search regex pattern match"),
            ("UnitConverter", "Unit Converter", "Convert inches to millimeters", "unit convert inch mm millimeter imperial metric"),
            ("Macros", "Macros", "Keyboard shortcuts for G-code", "macro shortcut keyboard"),
            ("Shortcuts", "Keyboard Shortcuts", "All keyboard shortcuts", "keyboard shortcut hotkey"),
            ("Saving", "Saving Files", "Save and backup files", "save backup file"),
            ("Preview", "Preview Changes", "Preview before saving", "preview test changes")
        ];
    }

    #endregion

    #region UI Helpers

    private void AddHeader(string text)
    {
        ContentPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 24,
            FontWeight = FontWeights.Bold,
            Foreground = HeaderBrush,
            Margin = new Thickness(0, 0, 0, 15),
            TextWrapping = TextWrapping.Wrap
        });
    }

    private void AddSubHeader(string text)
    {
        ContentPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            Foreground = SubHeaderBrush,
            Margin = new Thickness(0, 15, 0, 8)
        });
    }

    private void AddParagraph(string text)
    {
        ContentPanel.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 14,
            Foreground = DescriptionBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0, 0, 0, 10)
        });
    }

    private void AddParameter(string name, string description)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(15, 2, 0, 2) };
        sp.Children.Add(new TextBlock
        {
            Text = name,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = ParameterBrush,
            FontFamily = new FontFamily("Consolas"),
            MinWidth = 140
        });
        sp.Children.Add(new TextBlock
        {
            Text = "? " + description,
            FontSize = 13,
            Foreground = DescriptionBrush,
            TextWrapping = TextWrapping.Wrap
        });
        ContentPanel.Children.Add(sp);
    }

    private void AddColorItem(string color, string description)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(15, 4, 0, 4) };
        sp.Children.Add(new TextBlock
        {
            Text = "? " + color,
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = CommandBrush,
            MinWidth = 140
        });
        sp.Children.Add(new TextBlock
        {
            Text = "? " + description,
            FontSize = 13,
            Foreground = DescriptionBrush,
            TextWrapping = TextWrapping.Wrap
        });
        ContentPanel.Children.Add(sp);
    }

    private void AddBulletList(params string[] items)
    {
        foreach (var item in items)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 3, 0, 3) };
            sp.Children.Add(new TextBlock { Text = "• ", FontSize = 14, Foreground = DescriptionBrush });
            sp.Children.Add(new TextBlock
            {
                Text = item,
                FontSize = 13,
                Foreground = DescriptionBrush,
                TextWrapping = TextWrapping.Wrap
            });
            ContentPanel.Children.Add(sp);
        }
    }

    private void AddNumberedList(params string[] items)
    {
        for (int i = 0; i < items.Length; i++)
        {
            var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(10, 3, 0, 3) };
            sp.Children.Add(new TextBlock
            {
                Text = $"{i + 1}. ",
                FontSize = 14,
                Foreground = CommandBrush,
                FontWeight = FontWeights.Bold,
                MinWidth = 25
            });
            sp.Children.Add(new TextBlock
            {
                Text = items[i],
                FontSize = 13,
                Foreground = DescriptionBrush,
                TextWrapping = TextWrapping.Wrap
            });
            ContentPanel.Children.Add(sp);
        }
    }

    private void AddCommandSummary(string tag, string title, string description)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15, 10, 15, 10),
            Margin = new Thickness(0, 5, 0, 5),
            Cursor = Cursors.Hand
        };

        var sp = new StackPanel();
        sp.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = SubHeaderBrush
        });
        sp.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 13,
            Foreground = DescriptionBrush,
            Margin = new Thickness(0, 5, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });

        border.Child = sp;
        border.Tag = tag;
        border.MouseLeftButtonUp += (s, e) =>
        {
            if (s is Border b && b.Tag is string t)
            {
                ShowContent(t);
            }
        };

        ContentPanel.Children.Add(border);
    }

    private void AddWarning(string text)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        sp.Children.Add(new TextBlock { Text = "? ", FontSize = 14, Foreground = WarningBrush, FontWeight = FontWeights.Bold });
        sp.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = WarningBrush,
            TextWrapping = TextWrapping.Wrap,
            FontWeight = FontWeights.SemiBold
        });
        ContentPanel.Children.Add(sp);
    }

    private void AddTip(string text)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 5) };
        sp.Children.Add(new TextBlock { Text = "?? ", FontSize = 14, Foreground = TipBrush, FontWeight = FontWeights.Bold });
        sp.Children.Add(new TextBlock
        {
            Text = text,
            FontSize = 13,
            Foreground = TipBrush,
            TextWrapping = TextWrapping.Wrap,
            FontStyle = FontStyles.Italic
        });
        ContentPanel.Children.Add(sp);
    }

    #endregion
}
