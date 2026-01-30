# G-code Analyzer

A comprehensive G-code visualization, analysis, and editing tool for CNC machining. Analyze toolpaths, detect errors, simulate execution, and edit your G-code files with confidence.

![G-code Analyzer](https://via.placeholder.com/800x450.png?text=G-code+Analyzer+Screenshot)

## Features

### 📊 Comprehensive Analysis
- **Detailed Statistics**: View total commands, move counts, distances, and bounding box dimensions
- **Error Detection**: Automatically identify syntax errors, invalid commands, redundant moves, and duplicate toolpaths
- **Smart Filtering**: Toggle visibility of specific error types to focus on critical issues
- **Real-time Analysis**: Instant feedback as you load or edit files

### 🎨 Advanced Visualization
- **2D View**: Top-down (XY plane) visualization with pan, zoom, and rotation controls
- **3D View**: Fully interactive 3D rendering with camera controls and preset views (Top, Front, Side)
- **Color-Coded Toolpaths**: 
  - Orange: Rapid moves (G0)
  - Blue: Linear cutting moves (G1)
  - Green: Clockwise arcs (G2)
  - Purple: Counter-clockwise arcs (G3)
- **Dimension Measurements**: Automatic display of part dimensions in both millimeters and inches
- **Direction Arrows**: Visual indicators showing tool movement direction

### 🎬 Toolpath Simulation
- **Animated Playback**: Watch your toolpath execute in real-time
- **Speed Control**: Adjust playback speed from 0.25x to 32x
- **Timeline Scrubbing**: Jump to any point in the program instantly
- **Line Stepping**: Step through your G-code one line at a time for detailed analysis
- **Current Line Tracking**: See exactly which line is executing during simulation

### ✏️ Powerful G-code Editor
- **Syntax-Aware Editing**: Monospace font with line numbers and error highlighting
- **Real-time Error Detection**: Errors highlighted as you type
- **Undo/Redo Support**: Full editing history with keyboard shortcuts
- **Preview Mode**: Test changes in 2D/3D views without saving (F6)
- **Automatic Backups**: Versioned backups created when saving files

### 🔍 Advanced Selection Tools
- **Simulator-Based Selection**: Select code based on current simulation position
- **Range Selection**: Select from simulator to start/end of file
- **Two-Step Selection**: Set start and end points independently for precise control
- **Select All / None / Invert**: Standard selection operations

### 👁️ Visibility Filtering System
- **Hide/Show Selected Lines**: Focus on specific sections of complex toolpaths
- **Line Range Filtering**: Show or hide specific line number ranges
- **Smart Filters**: 
  - Hide all rapid moves (G0)
  - Show only cutting moves (G1, G2, G3)
  - Invert visibility
- **Visual Indicators**: Hidden lines shown as faint gray outlines for context
- **Perfect for Debugging**: Isolate overlapping or problematic toolpath sections

### 🔧 Productivity Tools
- **Find and Replace**: Advanced search with regex support, case matching, and whole word options
- **Unit Converter**: Quick conversion between inches and millimeters with G-code insertion
- **Keyboard Macros**: 10 customizable macros (Ctrl+1 through Ctrl+0) for common G-code snippets
- **Macro Editor**: Customize and manage your macro library
- **Default Macros Included**: Initialize, home, spindle control, coolant, and more

### 📁 File Support
- Supports multiple G-code formats: `.nc`, `.gcode`, `.ngc`, `.tap`
- Create new G-code files from scratch
- Load and analyze existing files
- Save and Save As with versioned backups

## System Requirements

- **OS**: Windows 10 or later
- **Runtime**: .NET 9.0
- **Graphics**: DirectX 10 compatible graphics card (for 3D visualization)
- **Memory**: 2 GB RAM minimum, 4 GB recommended

## Installation

1. Download the latest release from the [Releases](https://github.com/clharrysmith611/GcodeAnalyzer/releases) page
2. Extract the archive to your desired location
3. Ensure [.NET 9.0 Runtime](https://dotnet.microsoft.com/download/dotnet/9.0) is installed
4. Run `GcodeAnalyzer.exe`, or 'setup.exe' to install.

## Quick Start

1. **Load a File**: Click "Select G-code File..." and choose your G-code file
2. **Review Analysis**: Check the statistics panel and error list
3. **Visualize**: Switch between 2D and 3D views to examine the toolpath
4. **Simulate**: Use playback controls to watch the program execute
5. **Edit** (optional): Click "Edit / Create G-code File..." to make changes

## Usage Guide

### Main Window

#### Loading Files
- Click **Select G-code File...** to load an existing file
- Click **Edit / Create G-code File...** to create a new file or edit the loaded file
- View statistics, errors, and visualizations automatically upon loading

#### Visualization Controls

**2D View:**
- **Left Mouse Drag**: Pan the view
- **Right Mouse Drag**: Rotate around Z-axis
- **Mouse Wheel**: Zoom in/out
- **Reset View Button**: Return to default view

**3D View:**
- **Left Mouse Drag**: Pan the view
- **Right Mouse Drag**: Rotate camera
- **Mouse Wheel**: Zoom in/out
- **Arrow Keys**: Rotate camera
- **Comma/Period**: Rotate around Z-axis
- **Preset Views**: Top, Front, Side buttons

#### Simulation Controls
- **⏮ Skip to Start**: Jump to beginning
- **⏪ Previous Line**: Step back one line
- **⏬ Slower**: Decrease playback speed
- **▶/⏸ Play/Pause**: Start or pause simulation
- **⏩ Faster**: Increase playback speed
- **⏩ Next Line**: Step forward one line
- **⏭ Skip to End**: Jump to end

### G-code Editor

#### Opening the Editor
- From the main window, click **Edit / Create G-code File...**
- The editor opens with the current file or creates a new one if prompted

#### Editor Features

**File Operations:**
- `Ctrl+S`: Save
- `Ctrl+Shift+S`: Save As
- `Alt+F4`: Close editor

**Edit Commands:**
- `Ctrl+Z`: Undo
- `Ctrl+Y`: Redo
- `Ctrl+X`: Cut
- `Ctrl+C`: Copy
- `Ctrl+V`: Paste
- `Ctrl+A`: Select All

**Tools:**
- `Ctrl+F` or `Ctrl+H`: Find and Replace
- `Ctrl+E`: Z-Value Parameter Editor
- `F6`: Preview Changes (updates main window without saving)
- `Ctrl+U`: Unit Converter
- `F1`: G-code Help Reference

**Macros:**
- `Ctrl+1` through `Ctrl+9`: Insert macros 1-9
- `Ctrl+0`: Insert macro 10
- Edit macros via **Tools → Macros Editor**

**Selection (via Select menu):**
- `Ctrl+A`: Select All
- `Ctrl+D`: Select None (deselect)
- `Ctrl+I`: Invert Selection

**Note:** Other Select and View menu commands (simulator-based selection, visibility filtering) are accessed through menus only.

#### View Menu (Visibility Filtering)
Control which lines are visible in the 3D view:
- **Show All Lines (Reset)**: Display all lines
- **Hide Selected Lines**: Hide currently selected code
- **Show Only Selected Lines**: Show only selected code
- **Add to Hidden**: Incrementally hide selections
- **Unhide Selected Lines**: Restore hidden selections
- **Hide Line Range...**: Hide specific line numbers
- **Show Only Line Range...**: Show specific line numbers
- **Hide Rapid Moves (G0)**: Filter out all rapids
- **Show Only Cutting Moves**: Show only G1/G2/G3
- **Invert Visibility**: Swap hidden and visible lines

### Visibility Filtering Workflows

**Example 1: Focus on a Specific Section**
1. Run the simulator to the start of the section
2. In the editor, **Select → Set Selection Start at Simulator Position**
3. Continue simulator to the end of the section
4. **Select → Set Selection End at Simulator Position**
5. **View → Show Only Selected Lines**
6. The 3D view now shows only that section

**Example 2: Hide Completed Work**
1. Run simulator to current progress point
2. **Select → Select From Simulator Position to Start**
3. **View → Hide Selected Lines**
4. Focus on remaining toolpath

**Example 3: Examine Only Cutting Paths**
1. **View → Hide Rapid Moves (G0)**
2. See only the cutting operations without positioning moves

**Note:** Simulator-based selection and visibility filtering commands in the Select and View menus do not have keyboard shortcuts.

## Keyboard Shortcuts Reference

### Main Window
| Shortcut | Action |
|----------|--------|
| Arrow Keys | Rotate 3D camera |
| `,` (comma) | Rotate camera clockwise around Z |
| `.` (period) | Rotate camera counter-clockwise around Z |

### Editor - File Operations
| Shortcut | Action |
|----------|--------|
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Alt+F4` | Close |

### Editor - Editing
| Shortcut | Action |
|----------|--------|
| `Ctrl+Z` | Undo |
| `Ctrl+Y` | Redo |
| `Ctrl+X` | Cut |
| `Ctrl+C` | Copy |
| `Ctrl+V` | Paste |
| `Ctrl+A` | Select All |

### Editor - Tools
| Shortcut | Action |
|----------|--------|
| `Ctrl+F`, `Ctrl+H` | Find and Replace |
| `Ctrl+E` | Z-Value Parameter Editor |
| `F6` | Preview Changes |
| `Ctrl+U` | Unit Converter |
| `F1` | G-code Help |

### Editor - Selection
| Shortcut | Action |
|----------|--------|
| `Ctrl+A` | Select All |
| `Ctrl+D` | Select None |
| `Ctrl+I` | Invert Selection |

### Editor - Macros
| Shortcut | Action |
|----------|--------|
| `Ctrl+1` - `Ctrl+9` | Insert macros 1-9 |
| `Ctrl+0` | Insert macro 10 |

### Find and Replace Dialog
| Shortcut | Action |
|----------|--------|
| `Enter` | Find Next |
| `Shift+Enter` | Find Previous |
| `F3` | Find Next (dialog open) |
| `Shift+F3` | Find Previous (dialog open) |
| `Escape` | Close dialog |

## Default Macros

| Macro | Shortcut | G-code | Description |
|-------|----------|--------|-------------|
| Initialize | `Ctrl+1` | `G21 G90 G17` | Metric, absolute, XY plane |
| Home All | `Ctrl+2` | `G28` | Return to home position |
| Spindle On | `Ctrl+3` | `M3 S10000` | Start spindle at 10000 RPM |
| Spindle Off | `Ctrl+4` | `M5` | Stop spindle |
| Rapid Retract | `Ctrl+5` | `G0 Z10` | Rapid to Z=10 |
| Coolant On | `Ctrl+6` | `M8` | Flood coolant on |
| Coolant Off | `Ctrl+7` | `M9` | Coolant off |
| Program End | `Ctrl+8` | `M5 M9 G28 M30` | Safe shutdown sequence |
| Dwell 2s | `Ctrl+9` | `G4 P2` | Pause for 2 seconds |
| Comment | `Ctrl+0` | `(Insert comment here)` | Add a comment line |

## Technologies Used

- **Framework**: .NET 9.0
- **UI**: WPF (Windows Presentation Foundation)
- **3D Graphics**: WPF 3D with DirectX backend
- **Language**: C# 13.0
- **Architecture**: MVVM-inspired with code-behind

## Project Structure

```
GcodeAnalyzer/
├── GcodeAnalyzer/
│   ├── MainWindow.xaml/.cs           # Main application window
│   ├── GcodeEditorWindow.xaml/.cs   # G-code editor window
│   ├── Toolpath3DViewer.xaml/.cs    # 3D visualization control
│   ├── ApplicationHelpWindow.xaml/.cs # Help documentation window
│   ├── GcodeHelpWindow.xaml/.cs     # G-code command reference
│   ├── FindReplaceWindow.xaml/.cs   # Find/replace dialog
│   ├── UnitConverterWindow.xaml/.cs # Unit conversion tool
│   ├── MacroEditorWindow.xaml/.cs   # Macro management
│   ├── GcodeParser.cs               # G-code parsing engine
│   ├── GcodeAnalysisResult.cs       # Analysis data model
│   └── ... (additional files)
└── README.md
```

## Error Detection

The application automatically detects and reports:

- **Syntax Errors**: Malformed G-code commands
- **Unknown Commands**: Unrecognized G or M codes
- **Missing Parameters**: Required values not provided
- **Invalid Values**: Out-of-range parameter values
- **Redundant Moves**: Zero-length movements (can be filtered)
- **Duplicate Toolpaths**: Repeated identical paths (can be filtered)

## Best Practices

1. **Always Review Errors**: Check the error list before running G-code on your machine
2. **Use Simulation**: Run the simulator to verify toolpath behavior
3. **Preview Before Saving**: Use F6 to preview changes without committing
4. **Backup Important Files**: The app creates backups, but maintain your own as well
5. **Verify Dimensions**: Check that bounding box dimensions match your design
6. **Use Visibility Filtering**: For complex files, hide sections to focus on specific areas
7. **Test After Editing**: Always re-analyze after making changes

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. For major changes, please open an issue first to discuss what you would like to change.

### Development Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/GcodeAnalyzer.git
   ```
2. Open `GcodeAnalyzer.sln` in Visual Studio 2022 or later
3. Ensure .NET 9.0 SDK is installed
4. Build and run the solution

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with WPF and .NET 9.0
- Inspired by the need for better G-code visualization and analysis tools
- Thanks to the CNC community for feedback and feature requests

## Support

If you encounter any issues or have questions:
- Open an [Issue](https://github.com/yourusername/GcodeAnalyzer/issues)
- Check the built-in help system (yellow ? button in main window)
- Press F1 in the editor for G-code command reference

## Screenshots

### Main Window with 2D View
![2D View](https://via.placeholder.com/800x500.png?text=2D+Toolpath+View)

### 3D Visualization with Simulation
![3D View](https://via.placeholder.com/800x500.png?text=3D+View+with+Simulation)

### G-code Editor
![Editor](https://via.placeholder.com/800x500.png?text=G-code+Editor)

### Error Detection
![Errors](https://via.placeholder.com/800x500.png?text=Error+Detection)

---

**Made with ❤️ for the CNC community**
