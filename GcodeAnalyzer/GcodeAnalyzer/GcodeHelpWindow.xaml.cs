using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace GcodeAnalyzer;

/// <summary>
/// G-code and GRBL command reference help window.
/// </summary>
public partial class GcodeHelpWindow : Window
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

    public GcodeHelpWindow()
    {
        InitializeComponent();
        ShowOverview();
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
            ShowOverview();
            return;
        }

        ShowSearchResults(searchText);
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void CloseCommand_Executed(object sender, ExecutedRoutedEventArgs e)
    {
        Close();
    }

    private readonly List<string> _commandOrder =
    [
        "Overview", "Motion", "G0", "G1", "G2", "G3", "G4",
        "Coordinates", "G17", "G20", "G28", "G53", "G54", "G90", "G92",
        "Spindle", "M3", "M5", "M6", "S", "T",
        "Coolant", "M7", "M8", "M9",
        "Program", "M0", "M2",
        "GRBL", "$$", "$#", "$G", "$H", "$X", "$J", "!", "~", "?",
        "Parameters", "F", "XYZ", "IJK", "R", "P", "N"
    ];

    private string _currentCommand = "Overview";

    private void BtnPrevious_Click(object sender, RoutedEventArgs e)
    {
        int currentIndex = _commandOrder.IndexOf(_currentCommand);
        if (currentIndex > 0)
        {
            string previousCommand = _commandOrder[currentIndex - 1];
            NavigateToCommand(previousCommand);
        }
    }

    private void BtnNext_Click(object sender, RoutedEventArgs e)
    {
        int currentIndex = _commandOrder.IndexOf(_currentCommand);
        if (currentIndex < _commandOrder.Count - 1)
        {
            string nextCommand = _commandOrder[currentIndex + 1];
            NavigateToCommand(nextCommand);
        }
    }

    private void NavigateToCommand(string tag)
    {
        _currentCommand = tag;
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
        int currentIndex = _commandOrder.IndexOf(_currentCommand);
        
        if (BtnPrevious != null)
        {
            BtnPrevious.IsEnabled = currentIndex > 0;
        }
        
        if (BtnNext != null)
        {
            BtnNext.IsEnabled = currentIndex < _commandOrder.Count - 1;
        }
    }

    private void ShowContent(string tag)
    {
        if (ContentPanel == null)
            return;

        ContentPanel.Children.Clear();

        _currentCommand = tag;
        UpdateNavigationButtons();

        switch (tag)
        {
            case "Overview": ShowOverview(); break;
            case "Motion": ShowMotionCommands(); break;
            case "G0": ShowG0(); break;
            case "G1": ShowG1(); break;
            case "G2": ShowG2(); break;
            case "G3": ShowG3(); break;
            case "G4": ShowG4(); break;
            case "Coordinates": ShowCoordinateSystem(); break;
            case "G17": ShowG17(); break;
            case "G20": ShowG20(); break;
            case "G28": ShowG28(); break;
            case "G53": ShowG53(); break;
            case "G54": ShowG54(); break;
            case "G90": ShowG90(); break;
            case "G92": ShowG92(); break;
            case "Spindle": ShowSpindleCommands(); break;
            case "M3": ShowM3(); break;
            case "M5": ShowM5(); break;
            case "M6": ShowM6(); break;
            case "S": ShowS(); break;
            case "T": ShowT(); break;
            case "Coolant": ShowCoolant(); break;
            case "M7": ShowM7(); break;
            case "M8": ShowM8(); break;
            case "M9": ShowM9(); break;
            case "Program": ShowProgramControl(); break;
            case "M0": ShowM0(); break;
            case "M2": ShowM2(); break;
            case "GRBL": ShowGRBL(); break;
            case "$$": ShowDollarDollar(); break;
            case "$#": ShowDollarHash(); break;
            case "$G": ShowDollarG(); break;
            case "$H": ShowDollarH(); break;
            case "$X": ShowDollarX(); break;
            case "$J": ShowDollarJ(); break;
            case "!": ShowFeedHold(); break;
            case "~": ShowCycleStart(); break;
            case "?": ShowStatusReport(); break;
            case "Parameters": ShowParameters(); break;
            case "F": ShowF(); break;
            case "XYZ": ShowXYZ(); break;
            case "IJK": ShowIJK(); break;
            case "R": ShowR(); break;
            case "P": ShowP(); break;
            case "N": ShowN(); break;
            default: ShowOverview(); break;
        }
    }

    #region Content Builders

    private void ShowOverview()
    {
        if (ContentPanel == null)
            return;

        ContentPanel.Children.Clear();
        
        AddHeader("Welcome to the G-code Reference Guide");
        AddParagraph("This comprehensive guide covers all standard G-code commands and GRBL-specific features for CNC programming.");
        
        AddSubHeader("What is G-code?");
        AddParagraph("G-code (also known as RS-274) is the most widely used programming language for CNC (Computer Numerical Control) machines. It controls motion, speed, and various machine operations.");
        
        AddSubHeader("Quick Reference Categories");
        AddBulletList(
            "Motion Commands (G0-G3) - Control tool movement",
            "Coordinate System (G17-G92) - Define positioning modes",
            "Spindle & Tool (M3-M6, S, T) - Manage spindle and tools",
            "Coolant (M7-M9) - Control coolant systems",
            "Program Control (M0-M30) - Manage program flow",
            "GRBL Commands ($, !, ~, ?) - Controller-specific features"
        );
        
        AddSubHeader("Tips for Beginners");
        AddTip("Always start your program with G90 (absolute positioning) and G21 (metric) or G20 (imperial) to set up your coordinate system.");
        AddTip("Use G0 for rapid moves when the tool is not cutting, and G1 for controlled cutting moves with a defined feed rate.");
        AddTip("Always include a spindle speed (S) before starting the spindle (M3/M4).");
        
        AddSubHeader("Safety Notes");
        AddWarning("Always verify your G-code in a simulator before running on a real machine.");
        AddWarning("Ensure proper workholding and tool clearance before executing any program.");
        AddWarning("Know the location of the emergency stop button before running CNC operations.");
    }

    private void ShowMotionCommands()
    {
        AddHeader("Motion Commands");
        AddParagraph("Motion commands control how the tool moves through 3D space. These are the most fundamental G-code commands.");
        
        AddCommandSummary("G0", "Rapid Positioning", "Moves at maximum speed, used for non-cutting moves");
        AddCommandSummary("G1", "Linear Interpolation", "Moves in a straight line at specified feed rate");
        AddCommandSummary("G2", "Circular Interpolation CW", "Creates clockwise arc movements");
        AddCommandSummary("G3", "Circular Interpolation CCW", "Creates counter-clockwise arc movements");
        AddCommandSummary("G4", "Dwell", "Pauses program for specified time");
    }

    private void ShowG0()
    {
        AddHeader("G0 - Rapid Positioning");
        AddParagraph("Moves the tool to the specified position at the maximum traverse rate. This command is used for non-cutting moves to position the tool quickly.");
        
        AddSubHeader("Syntax");
        AddCode("G0 X__ Y__ Z__");
        
        AddSubHeader("Parameters");
        AddParameter("X", "Target X coordinate");
        AddParameter("Y", "Target Y coordinate");
        AddParameter("Z", "Target Z coordinate");
        
        AddSubHeader("Examples");
        AddExample("G0 X10 Y20", "Rapid move to X=10, Y=20");
        AddExample("G0 Z5", "Rapid move to Z=5 (retract)");
        AddExample("G0 X0 Y0 Z10", "Rapid move to origin with Z clearance");
        
        AddWarning("G0 moves at maximum speed - ensure clear path to avoid collisions!");
        AddTip("Always use G0 to retract the Z-axis before moving in X/Y to avoid dragging the tool across the workpiece.");
    }

    private void ShowG1()
    {
        AddHeader("G1 - Linear Interpolation");
        AddParagraph("Moves the tool in a straight line at a controlled feed rate. This is the primary command for cutting operations.");
        
        AddSubHeader("Syntax");
        AddCode("G1 X__ Y__ Z__ F__");
        
        AddSubHeader("Parameters");
        AddParameter("X", "Target X coordinate");
        AddParameter("Y", "Target Y coordinate");
        AddParameter("Z", "Target Z coordinate");
        AddParameter("F", "Feed rate (mm/min or in/min)");
        
        AddSubHeader("Examples");
        AddExample("G1 X50 Y30 F500", "Linear move to X=50, Y=30 at 500 mm/min");
        AddExample("G1 Z-5 F100", "Plunge cut to Z=-5 at 100 mm/min");
        AddExample("G1 X100 F1000", "Move to X=100 at 1000 mm/min");
        
        AddTip("The feed rate (F) is modal - once set, it remains active until changed.");
        AddTip("Use slower feed rates for plunge cuts (Z moves) than for lateral cuts (X/Y moves).");
    }

    private void ShowG2()
    {
        AddHeader("G2 - Circular Interpolation (Clockwise)");
        AddParagraph("Creates a clockwise arc from the current position to the target position. The arc center is defined by I/J/K offsets or radius R.");
        
        AddSubHeader("Syntax (Center Format)");
        AddCode("G2 X__ Y__ I__ J__ F__");
        
        AddSubHeader("Syntax (Radius Format)");
        AddCode("G2 X__ Y__ R__ F__");
        
        AddSubHeader("Parameters");
        AddParameter("X, Y, Z", "Target endpoint coordinates");
        AddParameter("I", "X offset from current position to arc center");
        AddParameter("J", "Y offset from current position to arc center");
        AddParameter("K", "Z offset from current position to arc center");
        AddParameter("R", "Arc radius (alternative to I/J/K)");
        AddParameter("F", "Feed rate");
        
        AddSubHeader("Examples");
        AddExample("G2 X20 Y10 I10 J0 F300", "CW arc with center offset I=10, J=0");
        AddExample("G2 X30 Y30 R15 F300", "CW arc with radius 15");
        
        AddTip("I/J/K are relative offsets from the current position to the arc center, not absolute coordinates.");
        AddWarning("When using R format, positive R gives the smaller arc (<180°), negative R gives the larger arc (>180°).");
    }

    private void ShowG3()
    {
        AddHeader("G3 - Circular Interpolation (Counter-Clockwise)");
        AddParagraph("Creates a counter-clockwise arc from the current position to the target position. Works identically to G2 but in the opposite direction.");
        
        AddSubHeader("Syntax");
        AddCode("G3 X__ Y__ I__ J__ F__");
        AddCode("G3 X__ Y__ R__ F__");
        
        AddSubHeader("Parameters");
        AddParagraph("Same parameters as G2 - see G2 documentation for details.");
        
        AddSubHeader("Examples");
        AddExample("G3 X20 Y10 I10 J0 F300", "CCW arc with center offset");
        AddExample("G3 X30 Y30 R15 F300", "CCW arc with radius 15");
        
        AddTip("Visualize clockwise (G2) vs counter-clockwise (G3) by looking down at the XY plane from above.");
    }

    private void ShowG4()
    {
        AddHeader("G4 - Dwell (Pause)");
        AddParagraph("Pauses program execution for a specified amount of time. Useful for allowing spindle to reach speed or chip clearing.");
        
        AddSubHeader("Syntax");
        AddCode("G4 P__");
        
        AddSubHeader("Parameters");
        AddParameter("P", "Dwell time in seconds (GRBL) or milliseconds (some controllers)");
        
        AddSubHeader("Examples");
        AddExample("G4 P1", "Pause for 1 second");
        AddExample("G4 P0.5", "Pause for 0.5 seconds");
        AddExample("M3 S10000", "Start spindle at 10000 RPM");
        AddExample("G4 P2", "Wait 2 seconds for spindle to reach speed");
        
        AddWarning("The P parameter interpretation varies by controller - check your machine's documentation.");
    }

    private void ShowCoordinateSystem()
    {
        AddHeader("Coordinate System Commands");
        AddParagraph("These commands define how coordinates are interpreted and which coordinate system is active.");
        
        AddCommandSummary("G17/G18/G19", "Plane Selection", "Select XY, XZ, or YZ plane for arcs");
        AddCommandSummary("G20/G21", "Units", "Set imperial (inches) or metric (millimeters) units");
        AddCommandSummary("G28", "Return to Home", "Move to machine home position");
        AddCommandSummary("G53", "Machine Coordinates", "Use absolute machine coordinates");
        AddCommandSummary("G54-G59", "Work Coordinates", "Select work coordinate system");
        AddCommandSummary("G90/G91", "Positioning Mode", "Absolute or incremental positioning");
        AddCommandSummary("G92", "Set Position", "Set current position to specified value");
    }

    private void ShowG17()
    {
        AddHeader("G17/G18/G19 - Plane Selection");
        AddParagraph("Selects the working plane for circular interpolation (G2/G3) commands.");
        
        AddSubHeader("Commands");
        AddParameter("G17", "XY plane (default) - arcs in XY, Z is the normal axis");
        AddParameter("G18", "XZ plane - arcs in XZ, Y is the normal axis");
        AddParameter("G19", "YZ plane - arcs in YZ, X is the normal axis");
        
        AddSubHeader("Examples");
        AddExample("G17 G2 X10 Y10 R5", "Arc in XY plane");
        AddExample("G18 G2 X10 Z10 R5", "Arc in XZ plane (for lathe or vertical cuts)");
        
        AddTip("G17 is the default and most commonly used plane for milling operations.");
    }

    private void ShowG20()
    {
        AddHeader("G20/G21 - Unit Selection");
        AddParagraph("Sets the unit system for all coordinate and feed rate values.");
        
        AddSubHeader("Commands");
        AddParameter("G20", "Imperial units (inches)");
        AddParameter("G21", "Metric units (millimeters)");
        
        AddSubHeader("Examples");
        AddExample("G21", "Set metric units");
        AddExample("G21 G1 X50 F500", "Move 50mm at 500mm/min");
        AddExample("G20", "Set imperial units");
        AddExample("G20 G1 X2 F20", "Move 2 inches at 20 in/min");
        
        AddWarning("Always set units at the beginning of your program to avoid confusion!");
        AddTip("GRBL defaults to metric (G21). Check your controller's default setting.");
    }

    private void ShowG28()
    {
        AddHeader("G28 - Return to Home");
        AddParagraph("Moves the machine to its home position (machine zero). Can optionally move through an intermediate point.");
        
        AddSubHeader("Syntax");
        AddCode("G28");
        AddCode("G28 X__ Y__ Z__");
        
        AddSubHeader("Behavior");
        AddParagraph("Without parameters: Moves directly to home position.");
        AddParagraph("With parameters: First moves to the specified intermediate point, then to home.");
        
        AddSubHeader("Examples");
        AddExample("G28", "Return directly to home");
        AddExample("G28 Z0", "First move to Z=0, then home all axes");
        
        AddWarning("Ensure adequate clearance when homing to avoid collisions.");
    }

    private void ShowG53()
    {
        AddHeader("G53 - Machine Coordinate System");
        AddParagraph("Temporarily use absolute machine coordinates for the current line only. Ignores any work offsets.");
        
        AddSubHeader("Syntax");
        AddCode("G53 G0 X__ Y__ Z__");
        
        AddSubHeader("Examples");
        AddExample("G53 G0 Z0", "Move to machine Z=0 (fully retracted)");
        AddExample("G53 G0 X0 Y0", "Move to machine origin");
        
        AddTip("G53 is non-modal - it only applies to the line it's on. The next line returns to the active work coordinate system.");
        AddWarning("G53 only works with G0 and G1 moves.");
    }

    private void ShowG54()
    {
        AddHeader("G54-G59 - Work Coordinate Systems");
        AddParagraph("Selects one of six available work coordinate systems. Each can have different offsets from machine zero.");
        
        AddSubHeader("Commands");
        AddParameter("G54", "Work coordinate system 1 (default)");
        AddParameter("G55", "Work coordinate system 2");
        AddParameter("G56", "Work coordinate system 3");
        AddParameter("G57", "Work coordinate system 4");
        AddParameter("G58", "Work coordinate system 5");
        AddParameter("G59", "Work coordinate system 6");
        
        AddSubHeader("Examples");
        AddExample("G54", "Select work coordinate system 1");
        AddExample("G55", "Select work coordinate system 2");
        
        AddTip("Use multiple work coordinate systems when machining multiple parts or fixtures on the same setup.");
        AddTip("Set work offsets using G10 L2 P# or through your controller's interface.");
    }

    private void ShowG90()
    {
        AddHeader("G90/G91 - Positioning Mode");
        AddParagraph("Sets whether coordinates are interpreted as absolute positions or incremental distances.");
        
        AddSubHeader("Commands");
        AddParameter("G90", "Absolute positioning - coordinates are relative to origin");
        AddParameter("G91", "Incremental positioning - coordinates are relative to current position");
        
        AddSubHeader("Examples");
        AddExample("G90", "Switch to absolute mode");
        AddExample("G90 G1 X50 Y30", "Move to absolute position X=50, Y=30");
        AddExample("G91", "Switch to incremental mode");
        AddExample("G91 G1 X10", "Move 10 units in +X direction from current position");
        
        AddTip("G90 is recommended for most operations as it's easier to verify positions.");
        AddWarning("Be careful when switching modes mid-program - it's easy to lose track of position.");
    }

    private void ShowG92()
    {
        AddHeader("G92 - Set Position");
        AddParagraph("Sets the current position to the specified coordinate values without moving. Creates a temporary offset.");
        
        AddSubHeader("Syntax");
        AddCode("G92 X__ Y__ Z__");
        
        AddSubHeader("Examples");
        AddExample("G92 X0 Y0", "Set current position as X=0, Y=0");
        AddExample("G92 Z0", "Set current Z as zero (useful after tool touch-off)");
        
        AddWarning("G92 offsets are volatile and may be cleared on reset. Use G54-G59 for persistent offsets.");
        AddTip("G92.1 clears the G92 offset. G92.2 suspends it. G92.3 restores it.");
    }

    private void ShowSpindleCommands()
    {
        AddHeader("Spindle & Tool Commands");
        AddParagraph("Commands for controlling the spindle motor and tool management.");
        
        AddCommandSummary("M3", "Spindle On CW", "Start spindle clockwise at set RPM");
        AddCommandSummary("M4", "Spindle On CCW", "Start spindle counter-clockwise at set RPM");
        AddCommandSummary("M5", "Spindle Off", "Stop the spindle");
        AddCommandSummary("M6", "Tool Change", "Perform tool change operation");
        AddCommandSummary("S", "Spindle Speed", "Set spindle speed in RPM");
        AddCommandSummary("T", "Tool Select", "Select tool for next change");
    }

    private void ShowM3()
    {
        AddHeader("M3/M4 - Spindle On");
        AddParagraph("Starts the spindle motor at the programmed speed (S value).");
        
        AddSubHeader("Commands");
        AddParameter("M3", "Start spindle clockwise (standard for right-hand cutters)");
        AddParameter("M4", "Start spindle counter-clockwise (for left-hand cutters or tapping)");
        
        AddSubHeader("Examples");
        AddExample("S10000 M3", "Start spindle CW at 10,000 RPM");
        AddExample("S5000 M4", "Start spindle CCW at 5,000 RPM");
        AddExample("M3 S12000", "Order doesn't matter on same line");
        
        AddTip("Always set spindle speed (S) before or with the M3/M4 command.");
        AddTip("Add a dwell (G4) after M3 to allow spindle to reach full speed before cutting.");
    }

    private void ShowM5()
    {
        AddHeader("M5 - Spindle Off");
        AddParagraph("Stops the spindle motor.");
        
        AddSubHeader("Syntax");
        AddCode("M5");
        
        AddSubHeader("Examples");
        AddExample("M5", "Stop the spindle");
        AddExample("G0 Z10", "Retract");
        AddExample("M5", "Then stop spindle");
        
        AddTip("Always stop the spindle before tool changes or when the program ends.");
    }

    private void ShowM6()
    {
        AddHeader("M6 - Tool Change");
        AddParagraph("Performs a tool change operation. Behavior varies by machine configuration.");
        
        AddSubHeader("Syntax");
        AddCode("T__ M6");
        
        AddSubHeader("Examples");
        AddExample("T1 M6", "Change to tool 1");
        AddExample("T2 M6", "Change to tool 2");
        AddExample("G43 H1", "Apply tool 1 length offset (often follows M6)");
        
        AddTip("On GRBL, M6 typically pauses for manual tool change.");
        AddWarning("Ensure spindle is off and Z is retracted before tool changes.");
    }

    private void ShowS()
    {
        AddHeader("S - Spindle Speed");
        AddParagraph("Sets the spindle speed in RPM (revolutions per minute).");
        
        AddSubHeader("Syntax");
        AddCode("S____");
        
        AddSubHeader("Examples");
        AddExample("S10000", "Set spindle to 10,000 RPM");
        AddExample("S1000 M3", "Set speed and start spindle");
        
        AddTip("S is modal - remains in effect until changed.");
        AddTip("Calculate RPM based on material, tool diameter, and recommended surface speed.");
    }

    private void ShowT()
    {
        AddHeader("T - Tool Select");
        AddParagraph("Selects a tool for the next tool change (M6). Does not perform the change by itself.");
        
        AddSubHeader("Syntax");
        AddCode("T__");
        
        AddSubHeader("Examples");
        AddExample("T1", "Select tool 1");
        AddExample("T2 M6", "Select tool 2 and perform change");
        
        AddTip("Pre-selecting the next tool (T) while cutting can speed up ATC (Automatic Tool Changer) operations.");
    }

    private void ShowCoolant()
    {
        AddHeader("Coolant Commands");
        AddParagraph("Commands for controlling coolant systems.");
        
        AddCommandSummary("M7", "Mist Coolant On", "Activates mist coolant");
        AddCommandSummary("M8", "Flood Coolant On", "Activates flood coolant");
        AddCommandSummary("M9", "Coolant Off", "Turns off all coolant");
    }

    private void ShowM7()
    {
        AddHeader("M7 - Mist Coolant On");
        AddParagraph("Activates the mist coolant system (air/oil mist).");
        AddSubHeader("Syntax");
        AddCode("M7");
    }

    private void ShowM8()
    {
        AddHeader("M8 - Flood Coolant On");
        AddParagraph("Activates the flood coolant system.");
        AddSubHeader("Syntax");
        AddCode("M8");
    }

    private void ShowM9()
    {
        AddHeader("M9 - Coolant Off");
        AddParagraph("Turns off all coolant systems (both mist and flood).");
        AddSubHeader("Syntax");
        AddCode("M9");
    }

    private void ShowProgramControl()
    {
        AddHeader("Program Control Commands");
        AddParagraph("Commands for managing program execution flow.");
        
        AddCommandSummary("M0", "Program Stop", "Unconditional program pause");
        AddCommandSummary("M1", "Optional Stop", "Pause if optional stop is enabled");
        AddCommandSummary("M2", "Program End", "Ends the program");
        AddCommandSummary("M30", "Program End & Reset", "Ends program and resets to start");
    }

    private void ShowM0()
    {
        AddHeader("M0/M1 - Program Pause");
        AddParagraph("Pauses program execution for operator intervention.");
        
        AddSubHeader("Commands");
        AddParameter("M0", "Unconditional stop - always pauses");
        AddParameter("M1", "Optional stop - only pauses if optional stop switch is enabled");
        
        AddSubHeader("Examples");
        AddExample("M0", "Pause for operator");
        AddExample("(Check tool condition)", "Comment explaining pause");
        AddExample("M0", "Pause execution");
        
        AddTip("Use M1 for stops that are only needed during setup or debugging.");
    }

    private void ShowM2()
    {
        AddHeader("M2/M30 - Program End");
        AddParagraph("Ends the program and resets the machine state.");
        
        AddSubHeader("Commands");
        AddParameter("M2", "Program end");
        AddParameter("M30", "Program end and rewind (reset to start)");
        
        AddSubHeader("Examples");
        AddExample("M5", "Stop spindle");
        AddExample("M9", "Coolant off");
        AddExample("G28", "Return home");
        AddExample("M30", "End program");
        
        AddTip("Always include spindle off (M5), coolant off (M9), and safe retract before program end.");
    }

    private void ShowGRBL()
    {
        AddHeader("GRBL-Specific Commands");
        AddParagraph("Special commands specific to GRBL controller firmware. These are real-time commands that don't require line endings.");
        
        AddCommandSummary("$$", "View Settings", "Display all GRBL $ settings");
        AddCommandSummary("$#", "View Offsets", "Display coordinate offsets and probe data");
        AddCommandSummary("$G", "Parser State", "Display active G-code modes");
        AddCommandSummary("$H", "Homing Cycle", "Run the homing sequence");
        AddCommandSummary("$X", "Kill Alarm", "Clear alarm lock state");
        AddCommandSummary("$J", "Jogging", "Jog motion command");
        AddCommandSummary("!", "Feed Hold", "Pause motion immediately");
        AddCommandSummary("~", "Cycle Start", "Resume from feed hold");
        AddCommandSummary("?", "Status Report", "Request current position and state");
    }

    private void ShowDollarDollar()
    {
        AddHeader("$$ - View GRBL Settings");
        AddParagraph("Displays all configurable GRBL settings and their current values.");
        
        AddSubHeader("Common Settings");
        AddParameter("$0", "Step pulse time (microseconds)");
        AddParameter("$1", "Step idle delay (milliseconds)");
        AddParameter("$100/$101/$102", "Steps per mm for X/Y/Z");
        AddParameter("$110/$111/$112", "Max rate (mm/min) for X/Y/Z");
        AddParameter("$120/$121/$122", "Acceleration (mm/sec²) for X/Y/Z");
        AddParameter("$130/$131/$132", "Max travel (mm) for X/Y/Z");
        
        AddSubHeader("Example");
        AddExample("$$", "Display all settings");
        AddExample("$100=250", "Set X steps/mm to 250");
    }

    private void ShowDollarHash()
    {
        AddHeader("$# - View Offsets");
        AddParagraph("Displays all coordinate offset values including work coordinate systems (G54-G59), G92 offset, and probe position.");
        
        AddSubHeader("Example");
        AddExample("$#", "Display all offsets");
    }

    private void ShowDollarG()
    {
        AddHeader("$G - Parser State");
        AddParagraph("Displays the current G-code parser state showing all active modal groups.");
        
        AddSubHeader("Example Output");
        AddCode("[GC:G0 G54 G17 G21 G90 G94 M5 M9 T0 F0 S0]");
        
        AddTip("Useful for debugging to see what modes are currently active.");
    }

    private void ShowDollarH()
    {
        AddHeader("$H - Homing Cycle");
        AddParagraph("Initiates the homing cycle. The machine will move to find the limit switches and establish machine zero.");
        
        AddSubHeader("Example");
        AddExample("$H", "Start homing cycle");
        
        AddWarning("Ensure the machine has clear travel to all limit switches before homing.");
        AddTip("Homing is required after power-up on machines with homing enabled.");
    }

    private void ShowDollarX()
    {
        AddHeader("$X - Kill Alarm");
        AddParagraph("Clears the alarm state and unlocks GRBL. Used after an alarm condition has been resolved.");
        
        AddSubHeader("Example");
        AddExample("$X", "Clear alarm and unlock");
        
        AddWarning("Only use after you've identified and resolved the cause of the alarm!");
    }

    private void ShowDollarJ()
    {
        AddHeader("$J - Jogging");
        AddParagraph("Executes a jog motion. Jog commands can be cancelled with a feed hold and are designed for real-time manual control.");
        
        AddSubHeader("Syntax");
        AddCode("$J=G91 X10 F1000");
        
        AddSubHeader("Examples");
        AddExample("$J=G91 X10 F500", "Jog 10mm in +X at 500mm/min");
        AddExample("$J=G91 Z-5 F200", "Jog 5mm in -Z at 200mm/min");
        AddExample("$J=G90 X0 Y0 F1000", "Jog to absolute position X0 Y0");
        
        AddTip("Jog commands can include G90/G91 and G20/G21 within the command.");
    }

    private void ShowFeedHold()
    {
        AddHeader("! - Feed Hold");
        AddParagraph("Immediately pauses motion while maintaining position. This is a real-time command.");
        
        AddSubHeader("Usage");
        AddParagraph("Send the '!' character at any time to pause motion. No line ending needed.");
        
        AddTip("Use feed hold for quick stops without losing position or aborting the program.");
    }

    private void ShowCycleStart()
    {
        AddHeader("~ - Cycle Start/Resume");
        AddParagraph("Resumes motion after a feed hold or M0 stop. This is a real-time command.");
        
        AddSubHeader("Usage");
        AddParagraph("Send the '~' character to resume. No line ending needed.");
    }

    private void ShowStatusReport()
    {
        AddHeader("? - Status Report");
        AddParagraph("Requests an immediate status report showing current position, state, and buffer status.");
        
        AddSubHeader("Example Output");
        AddCode("<Idle|MPos:0.000,0.000,0.000|FS:0,0|WCO:0.000,0.000,0.000>");
        
        AddSubHeader("Fields");
        AddParameter("State", "Idle, Run, Hold, Jog, Alarm, Door, Check, Home, Sleep");
        AddParameter("MPos", "Machine position");
        AddParameter("WPos", "Work position (if configured)");
        AddParameter("FS", "Feed rate and spindle speed");
        AddParameter("WCO", "Work coordinate offset");
    }

    private void ShowParameters()
    {
        AddHeader("G-code Parameters");
        AddParagraph("Parameters provide values to G-code commands for coordinates, speeds, and other settings.");
        
        AddCommandSummary("F", "Feed Rate", "Movement speed in units per minute");
        AddCommandSummary("X/Y/Z", "Coordinates", "Position values for each axis");
        AddCommandSummary("I/J/K", "Arc Offsets", "Incremental offset to arc center");
        AddCommandSummary("R", "Radius", "Arc radius (alternative to I/J/K)");
        AddCommandSummary("P", "Parameter", "Dwell time or other parameter values");
        AddCommandSummary("N", "Line Number", "Optional line numbering");
    }

    private void ShowF()
    {
        AddHeader("F - Feed Rate");
        AddParagraph("Sets the feed rate for cutting moves (G1, G2, G3). Units are determined by G20/G21 setting.");
        
        AddSubHeader("Syntax");
        AddCode("F____");
        
        AddSubHeader("Examples");
        AddExample("G21 G1 X50 F500", "500 mm/min in metric mode");
        AddExample("G20 G1 X2 F10", "10 inches/min in imperial mode");
        
        AddTip("Feed rate is modal - it stays in effect until changed.");
        AddTip("Appropriate feed rates depend on material, tool, and depth of cut.");
    }

    private void ShowXYZ()
    {
        AddHeader("X/Y/Z - Axis Coordinates");
        AddParagraph("Specify target positions for motion commands. Interpretation depends on G90 (absolute) or G91 (incremental) mode.");
        
        AddSubHeader("Examples");
        AddExample("G90 G0 X10 Y20 Z5", "Absolute move to X=10, Y=20, Z=5");
        AddExample("G91 G1 X5", "Move 5 units in +X direction from current position");
        
        AddTip("Not all axes need to be specified - omitted axes remain at their current position.");
    }

    private void ShowIJK()
    {
        AddHeader("I/J/K - Arc Center Offsets");
        AddParagraph("Define the arc center as an incremental offset from the current position. Used with G2/G3 commands.");
        
        AddSubHeader("Parameters");
        AddParameter("I", "X offset from current position to arc center");
        AddParameter("J", "Y offset from current position to arc center");
        AddParameter("K", "Z offset from current position to arc center (for helical arcs)");
        
        AddSubHeader("Example");
        AddParagraph("Starting at X=0, Y=0, to create an arc with center at X=10, Y=0:");
        AddExample("G2 X20 Y0 I10 J0", "Creates semicircle ending at X=20, Y=0");
        
        AddTip("I/J/K are ALWAYS incremental offsets, regardless of G90/G91 mode.");
    }

    private void ShowR()
    {
        AddHeader("R - Arc Radius");
        AddParagraph("Alternative to I/J/K for defining arcs. Specifies the radius directly.");
        
        AddSubHeader("Syntax");
        AddCode("G2 X__ Y__ R__");
        
        AddSubHeader("Examples");
        AddExample("G2 X20 Y0 R10", "Arc with radius 10 (takes shorter path)");
        AddExample("G2 X20 Y0 R-10", "Arc with radius 10 (takes longer path)");
        
        AddTip("Positive R = arc less than 180°. Negative R = arc greater than 180°.");
        AddWarning("R format cannot create a full circle (360°). Use I/J format for full circles.");
    }

    private void ShowP()
    {
        AddHeader("P - Parameter Value");
        AddParagraph("Multi-purpose parameter used with various commands. Most commonly used for dwell time.");
        
        AddSubHeader("Usage with G4 (Dwell)");
        AddExample("G4 P1.5", "Pause for 1.5 seconds");
        
        AddSubHeader("Usage with G10 (Set Offset)");
        AddExample("G10 L2 P1 X10 Y20", "Set G54 (P1) offset to X=10, Y=20");
        
        AddTip("The meaning of P varies by command - check specific command documentation.");
    }

    private void ShowN()
    {
        AddHeader("N - Line Number");
        AddParagraph("Optional line numbering for G-code programs. Useful for reference and error tracking.");
        
        AddSubHeader("Syntax");
        AddCode("N____ G__ X__ Y__...");
        
        AddSubHeader("Examples");
        AddExample("N10 G0 X0 Y0", "Line 10: Rapid to origin");
        AddExample("N20 G1 X50 F500", "Line 20: Linear move");
        AddExample("N30 G0 Z10", "Line 30: Retract");
        
        AddTip("Line numbers are optional and ignored by most controllers.");
        AddTip("Traditionally numbered in increments of 10 to allow inserting lines later.");
    }

    private void ShowSearchResults(string searchText)
    {
        ContentPanel.Children.Clear();
        AddHeader($"Search Results for \"{searchText}\"");
        
        var allCommands = GetAllCommandsForSearch();
        var matches = allCommands.Where(c => 
            c.Name.ToLowerInvariant().Contains(searchText) || 
            c.Description.ToLowerInvariant().Contains(searchText) ||
            c.Keywords.ToLowerInvariant().Contains(searchText)).ToList();
        
        if (matches.Count == 0)
        {
            AddParagraph("No commands found matching your search. Try different keywords.");
            return;
        }
        
        AddParagraph($"Found {matches.Count} matching command(s):");
        
        foreach (var cmd in matches)
        {
            AddCommandSummary(cmd.Name, cmd.Title, cmd.Description);
        }
    }

    private List<CommandInfo> GetAllCommandsForSearch()
    {
        return
        [
            new("G0", "Rapid Positioning", "Fast non-cutting move", "rapid fast position traverse"),
            new("G1", "Linear Interpolation", "Controlled straight line move", "linear feed cut"),
            new("G2", "Arc Clockwise", "Clockwise circular interpolation", "arc circle cw clockwise"),
            new("G3", "Arc Counter-Clockwise", "Counter-clockwise circular interpolation", "arc circle ccw counterclockwise"),
            new("G4", "Dwell", "Pause for specified time", "dwell pause wait delay"),
            new("G17", "XY Plane", "Select XY plane for arcs", "plane xy"),
            new("G18", "XZ Plane", "Select XZ plane for arcs", "plane xz"),
            new("G19", "YZ Plane", "Select YZ plane for arcs", "plane yz"),
            new("G20", "Imperial Units", "Set units to inches", "inch imperial units"),
            new("G21", "Metric Units", "Set units to millimeters", "metric mm millimeter units"),
            new("G28", "Home", "Return to home position", "home return origin"),
            new("G53", "Machine Coordinates", "Use machine coordinate system", "machine absolute coordinate"),
            new("G54-G59", "Work Coordinates", "Select work coordinate system", "work offset coordinate"),
            new("G90", "Absolute Mode", "Absolute positioning mode", "absolute position mode"),
            new("G91", "Incremental Mode", "Incremental positioning mode", "incremental relative position mode"),
            new("G92", "Set Position", "Set current position value", "set position offset"),
            new("M3", "Spindle CW", "Start spindle clockwise", "spindle start cw clockwise"),
            new("M4", "Spindle CCW", "Start spindle counter-clockwise", "spindle start ccw counterclockwise"),
            new("M5", "Spindle Stop", "Stop spindle", "spindle stop off"),
            new("M6", "Tool Change", "Perform tool change", "tool change atc"),
            new("M7", "Mist Coolant", "Turn on mist coolant", "coolant mist"),
            new("M8", "Flood Coolant", "Turn on flood coolant", "coolant flood"),
            new("M9", "Coolant Off", "Turn off coolant", "coolant off stop"),
            new("M0", "Program Stop", "Pause program execution", "stop pause"),
            new("M2", "Program End", "End program", "end finish stop"),
            new("M30", "Program End Reset", "End program and reset", "end reset rewind"),
            new("S", "Spindle Speed", "Set spindle RPM", "speed rpm spindle"),
            new("F", "Feed Rate", "Set feed rate", "feed rate speed"),
            new("$H", "Homing", "GRBL homing cycle", "home homing grbl"),
            new("$X", "Unlock", "GRBL unlock/kill alarm", "unlock alarm grbl"),
            new("$$", "Settings", "View GRBL settings", "settings config grbl"),
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
            Width = 80
        });
        sp.Children.Add(new TextBlock
        {
            Text = "— " + description,
            FontSize = 13,
            Foreground = DescriptionBrush
        });
        ContentPanel.Children.Add(sp);
    }

    private void AddCode(string code)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(80, 0, 0, 0)),
            BorderBrush = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(4),
            Padding = new Thickness(15, 8, 15, 8),
            Margin = new Thickness(0, 5, 0, 10)
        };
        border.Child = new TextBlock
        {
            Text = code,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 15,
            Foreground = CommandBrush
        };
        ContentPanel.Children.Add(border);
    }

    private void AddExample(string code, string description)
    {
        var sp = new StackPanel { Margin = new Thickness(15, 3, 0, 3) };
        sp.Children.Add(new TextBlock
        {
            Text = code,
            FontFamily = new FontFamily("Consolas"),
            FontSize = 14,
            FontWeight = FontWeights.Bold,
            Foreground = ExampleBrush
        });
        sp.Children.Add(new TextBlock
        {
            Text = "   " + description,
            FontSize = 12,
            Foreground = DimBrush,
            Margin = new Thickness(0, 0, 0, 3)
        });
        ContentPanel.Children.Add(sp);
    }

    private void AddCommandSummary(string code, string title, string description)
    {
        var border = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
            CornerRadius = new CornerRadius(6),
            Padding = new Thickness(15, 10, 15, 10),
            Margin = new Thickness(0, 5, 0, 5)
        };
        
        var sp = new StackPanel();
        
        var headerSp = new StackPanel { Orientation = Orientation.Horizontal };
        headerSp.Children.Add(new TextBlock
        {
            Text = code,
            FontSize = 16,
            FontWeight = FontWeights.Bold,
            Foreground = CommandBrush,
            FontFamily = new FontFamily("Consolas"),
            MinWidth = 80
        });
        headerSp.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            Foreground = SubHeaderBrush,
            Margin = new Thickness(10, 0, 0, 0)
        });
        sp.Children.Add(headerSp);
        
        sp.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 13,
            Foreground = DescriptionBrush,
            Margin = new Thickness(0, 5, 0, 0),
            TextWrapping = TextWrapping.Wrap
        });
        
        border.Child = sp;
        ContentPanel.Children.Add(border);
    }

    private void AddWarning(string text)
    {
        var sp = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 8) };
        sp.Children.Add(new TextBlock { Text = "[!] ", FontSize = 14, Foreground = WarningBrush, FontWeight = FontWeights.Bold });
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
        sp.Children.Add(new TextBlock { Text = "[TIP] ", FontSize = 14, Foreground = TipBrush, FontWeight = FontWeights.Bold });
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

    #endregion
}

internal record CommandInfo(string Name, string Title, string Description, string Keywords);
