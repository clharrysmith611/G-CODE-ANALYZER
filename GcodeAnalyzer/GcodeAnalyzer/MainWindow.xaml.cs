using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace GcodeAnalyzer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string? _selectedFilePath;
        private GcodeAnalysisResult? _analysisResult;
        private readonly GcodeParser _parser = new();
        private GcodeEditorWindow? _editorWindow; // Track editor instance

        // Arrow spacing - draw an arrow every N pixels along the path
        private const double ArrowSpacing = 80;
        private const double ArrowSize = 8;

        // Error message patterns for filtering
        private const string RedundantMovePattern = "Redundant move:";
        private const string DuplicateToolpathPattern = "Duplicate toolpath:";

        // Track which views need updating
        private bool _is3DViewDirty = true;
        private bool _is2DViewDirty = true;

        // 2D view mouse interaction state
        private bool _is2DPanning;
        private bool _is2DRotating;
        private Point _last2DMousePosition;

        public MainWindow()
        {
            InitializeComponent();
            
            // Subscribe to 3D viewer's current line changed event
            Viewer3D.CurrentLineChanged += Viewer3D_CurrentLineChanged;
        }

        private void Viewer3D_CurrentLineChanged(object? sender, int lineNumber)
        {
            // Update editor window if it's open
            if (_editorWindow != null)
            {
                if (lineNumber > 0)
                {
                    _editorWindow.SetCurrentSimulationLine(lineNumber);
                }
                else
                {
                    _editorWindow.ClearSimulationLine();
                }
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Title = "Select G-code File",
                Filter = "G-code Files (*.nc;*.gcode;*.ngc;*.tap)|*.nc;*.gcode;*.ngc;*.tap|All Files (*.*)|*.*",
                FilterIndex = 1
            };

            if (openFileDialog.ShowDialog() == true)
            {
                _selectedFilePath = openFileDialog.FileName;
                TxtFileName.Text = System.IO.Path.GetFileName(_selectedFilePath);
                TxtFileName.Foreground = Brushes.Black;

                // Automatically analyze the file after selection
                AnalyzeFile();
            }
        }

        /// <summary>
        /// Opens the application help window.
        /// </summary>
        private void BtnAppHelp_Click(object sender, RoutedEventArgs e)
        {
            var helpWindow = new ApplicationHelpWindow
            {
                Owner = this
            };
            helpWindow.Show();
        }

        /// <summary>
        /// Opens the G-code editor window.
        /// </summary>
        private void BtnEditFile_Click(object sender, RoutedEventArgs e)
        {
            // If no file is selected, prompt to create a new one
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                var result = MessageBox.Show(
                    "No G-code file is currently loaded. Would you like to create a new G-code file?",
                    "Create New File",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // Prompt for new file name
                var saveDialog = new SaveFileDialog
                {
                    Title = "Create New G-code File",
                    Filter = "G-code Files (*.nc;*.gcode;*.ngc;*.tap)|*.nc;*.gcode;*.ngc;*.tap|All Files (*.*)|*.*",
                    DefaultExt = ".gcode",
                    FileName = "untitled.gcode"
                };

                if (saveDialog.ShowDialog() != true)
                {
                    return;
                }

                try
                {
                    // Create an empty file
                    File.WriteAllText(saveDialog.FileName, "");
                    
                    // Set this as the selected file
                    _selectedFilePath = saveDialog.FileName;
                    TxtFileName.Text = System.IO.Path.GetFileName(_selectedFilePath);
                    TxtFileName.Foreground = Brushes.Black;
                    
                    // Open the editor with the new empty file
                    OpenEditor();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error creating file: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }
            else
            {
                // File is already loaded, open the editor
                OpenEditor();
            }
        }

        /// <summary>
        /// Opens the editor window with the current file.
        /// </summary>
        private void OpenEditor()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                return;
            }

            // If editor is already open, just bring it to front
            if (_editorWindow != null)
            {
                _editorWindow.Activate();
                return;
            }

            _editorWindow = new GcodeEditorWindow
            {
                Owner = this,
                HideRedundantMoves = ChkHideRedundantMoves.IsChecked == true,
                HideDuplicatePaths = ChkHideDuplicatePaths.IsChecked == true
            };
            
            // Subscribe to preview event
            _editorWindow.PreviewRequested += EditorWindow_PreviewRequested;
            
            // Subscribe to closed event - combine all cleanup in one handler
            _editorWindow.Closed += (s, args) =>
            {
                // Check if content was modified and saved
                bool wasModified = _editorWindow?.ContentWasModified == true;
                
                // Clear the reference
                _editorWindow = null;
                
                // Refresh views if content was modified
                if (wasModified)
                {
                    RefreshAllViews();
                }
            };
            
            _editorWindow.LoadFile(_selectedFilePath);
            _editorWindow.Show();
            
            // Sync the current simulation line with the editor after it's fully loaded
            // Use Dispatcher to ensure the window is fully rendered before scrolling
            _editorWindow.Dispatcher.BeginInvoke(new Action(() =>
            {
                SyncEditorWithCurrentLine();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// Syncs the editor with the current simulation line from the 3D viewer.
        /// </summary>
        private void SyncEditorWithCurrentLine()
        {
            if (_editorWindow == null) return;
            
            // Get the current line from the 3D viewer
            int currentLine = Viewer3D.GetCurrentLine();
            
            if (currentLine > 0)
            {
                _editorWindow.SetCurrentSimulationLine(currentLine);
            }
        }

        private void EditorWindow_PreviewRequested(object? sender, EventArgs e)
        {
            try
            {
                if (_editorWindow == null)
                {
                    System.Diagnostics.Debug.WriteLine("Preview requested but editor window is null");
                    return;
                }
                
                // Get the current editor content without saving
                var currentContent = _editorWindow.GetCurrentContent();
                
                if (string.IsNullOrEmpty(currentContent))
                {
                    System.Diagnostics.Debug.WriteLine("Preview requested but content is empty");
                    return;
                }
                
                // Parse the content
                var lines = currentContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                _analysisResult = _parser.ParseLines(lines);

                // Display statistics
                TxtStatistics.Text = _analysisResult.GetSummary();

                // Display filtered errors
                UpdateErrorList();

                // Force update both views
                DrawToolpath();
                Draw3DToolpath();
                
                System.Diagnostics.Debug.WriteLine("Preview completed successfully");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Preview error: {ex.Message}");
                MessageBox.Show($"Error previewing changes: {ex.Message}", "Preview Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Refreshes all views after the G-code file has been modified.
        /// </summary>
        private void RefreshAllViews()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                return;
            }

            try
            {
                // Re-parse the file
                _analysisResult = _parser.ParseFile(_selectedFilePath);

                // Display statistics
                TxtStatistics.Text = _analysisResult.GetSummary();

                // Display filtered errors
                UpdateErrorList();

                // Force update both views regardless of which tab is selected
                DrawToolpath();
                Draw3DToolpath();

                // Reset dirty flags
                _is2DViewDirty = false;
                _is3DViewDirty = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error analyzing file: {ex.Message}", "Analysis Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ClearResults();
            }
        }

        /// <summary>
        /// Analyzes the currently selected G-code file.
        /// </summary>
        private void AnalyzeFile()
        {
            if (string.IsNullOrEmpty(_selectedFilePath))
            {
                return;
            }

            try
            {
                _analysisResult = _parser.ParseFile(_selectedFilePath);

                // Display statistics
                TxtStatistics.Text = _analysisResult.GetSummary();

                // Display filtered errors
                UpdateErrorList();

                // Mark both views as needing update
                _is2DViewDirty = true;
                _is3DViewDirty = true;

                // Update the current view
                UpdateCurrentView();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error analyzing file: {ex.Message}", "Analysis Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                ClearResults();
            }
        }

        /// <summary>
        /// Updates the currently visible view.
        /// </summary>
        private void UpdateCurrentView()
        {
            if (ViewTabs.SelectedIndex == 0 && _is2DViewDirty)
            {
                DrawToolpath();
                _is2DViewDirty = false;
            }
            else if (ViewTabs.SelectedIndex == 1 && _is3DViewDirty)
            {
                Draw3DToolpath();
                _is3DViewDirty = false;
            }
        }

        /// <summary>
        /// Handles tab selection changes to update the view.
        /// </summary>
        private void ViewTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only handle when this is from our TabControl
            if (e.Source != ViewTabs)
            {
                return;
            }

            UpdateCurrentView();
        }

        private void ClearResults()
        {
            _analysisResult = null;
            TxtStatistics.Text = "Load a file to see results.";
            LstErrors.ItemsSource = null;
            ToolpathCanvas.Children.Clear();
            Viewer3D.Clear();
            _is2DViewDirty = true;
            _is3DViewDirty = true;
        }

        /// <summary>
        /// Updates the error list based on current filter settings.
        /// </summary>
        private void UpdateErrorList()
        {
            if (_analysisResult is null)
            {
                LstErrors.ItemsSource = null;
                return;
            }

            var filteredErrors = _analysisResult.Errors.AsEnumerable();

            if (ChkHideRedundantMoves.IsChecked == true)
            {
                filteredErrors = filteredErrors.Where(e => !e.Message.StartsWith(RedundantMovePattern));
            }

            if (ChkHideDuplicatePaths.IsChecked == true)
            {
                filteredErrors = filteredErrors.Where(e => !e.Message.StartsWith(DuplicateToolpathPattern));
            }

            LstErrors.ItemsSource = filteredErrors.ToList();
        }

        private void ChkHideRedundantMoves_Changed(object sender, RoutedEventArgs e)
        {
            UpdateErrorList();
        }

        private void ChkHideDuplicatePaths_Changed(object sender, RoutedEventArgs e)
        {
            UpdateErrorList();
        }

        /// <summary>
        /// Draws the 3D toolpath visualization.
        /// </summary>
        private void Draw3DToolpath()
        {
            if (_analysisResult is null || _analysisResult.TotalCommands == 0)
            {
                Viewer3D.Clear();
                return;
            }

            Viewer3D.RenderToolpath(_analysisResult);
        }

        #region 2D View Mouse Interaction

        private void Canvas2D_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            _is2DPanning = true;
            _last2DMousePosition = e.GetPosition(Canvas2DContainer);
            Canvas2DContainer.CaptureMouse();
            e.Handled = true;
        }

        private void Canvas2D_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_is2DPanning)
            {
                _is2DPanning = false;
                Canvas2DContainer.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void Canvas2D_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            _is2DRotating = true;
            _last2DMousePosition = e.GetPosition(Canvas2DContainer);
            Canvas2DContainer.CaptureMouse();
            e.Handled = true;
        }

        private void Canvas2D_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_is2DRotating)
            {
                _is2DRotating = false;
                Canvas2DContainer.ReleaseMouseCapture();
                e.Handled = true;
            }
        }

        private void Canvas2D_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_is2DPanning && !_is2DRotating)
            {
                return;
            }

            var currentPosition = e.GetPosition(Canvas2DContainer);
            var delta = currentPosition - _last2DMousePosition;
            _last2DMousePosition = currentPosition;

            if (_is2DPanning)
            {
                // Pan the canvas
                Canvas2DTranslate.X += delta.X;
                Canvas2DTranslate.Y += delta.Y;
                e.Handled = true;
            }
            else if (_is2DRotating)
            {
                // Rotate around Z axis (use horizontal mouse movement)
                double rotateSpeed = 0.3;
                Canvas2DRotate.Angle += delta.X * rotateSpeed;
                e.Handled = true;
            }
        }

        private void Canvas2D_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            // Zoom in/out
            double zoomFactor = e.Delta > 0 ? 1.1 : 0.9;
            
            // Get the mouse position relative to the container
            var mousePos = e.GetPosition(Canvas2DContainer);
            
            // Apply zoom
            double newScaleX = Canvas2DScale.ScaleX * zoomFactor;
            double newScaleY = Canvas2DScale.ScaleY * zoomFactor;
            
            // Limit zoom range
            newScaleX = Math.Clamp(newScaleX, 0.1, 10.0);
            newScaleY = Math.Clamp(newScaleY, 0.1, 10.0);
            
            // Calculate the offset adjustment to zoom toward mouse position
            double scaleChange = newScaleX / Canvas2DScale.ScaleX;
            
            // Adjust translation to keep the point under the mouse stationary
            Canvas2DTranslate.X = mousePos.X - (mousePos.X - Canvas2DTranslate.X) * scaleChange;
            Canvas2DTranslate.Y = mousePos.Y - (mousePos.Y - Canvas2DTranslate.Y) * scaleChange;
            
            Canvas2DScale.ScaleX = newScaleX;
            Canvas2DScale.ScaleY = newScaleY;
            
            e.Handled = true;
        }

        private void BtnReset2DView_Click(object sender, RoutedEventArgs e)
        {
            // Reset all transforms
            Canvas2DScale.ScaleX = 1;
            Canvas2DScale.ScaleY = 1;
            Canvas2DRotate.Angle = 0;
            Canvas2DTranslate.X = 0;
            Canvas2DTranslate.Y = 0;
        }

        #endregion

        private void DrawToolpath()
        {
            ToolpathCanvas.Children.Clear();

            if (_analysisResult is null || _analysisResult.TotalCommands == 0)
            {
                return;
            }

            // Calculate canvas size and scaling
            const double margin = 80; // Increased from 20 to 80 to make room for dimensions
            const double minCanvasSize = 400;

            double dataWidth = _analysisResult.Width;
            double dataHeight = _analysisResult.Height;

            // Handle edge case where all points are on a line
            if (dataWidth < 0.001) dataWidth = 1;
            if (dataHeight < 0.001) dataHeight = 1;

            // Calculate scale to fit in canvas while maintaining aspect ratio
            double canvasWidth = Math.Max(minCanvasSize, dataWidth + margin * 2);
            double canvasHeight = Math.Max(minCanvasSize, dataHeight + margin * 2);

            double scaleX = (canvasWidth - margin * 2) / dataWidth;
            double scaleY = (canvasHeight - margin * 2) / dataHeight;
            double scale = Math.Min(scaleX, scaleY);

            // Ensure minimum scale for visibility
            scale = Math.Max(scale, 1);

            // Recalculate canvas size based on scale
            canvasWidth = dataWidth * scale + margin * 2;
            canvasHeight = dataHeight * scale + margin * 2;

            ToolpathCanvas.Width = canvasWidth;
            ToolpathCanvas.Height = canvasHeight;

            double offsetX = margin - _analysisResult.MinX * scale;
            double offsetY = margin - _analysisResult.MinY * scale;

            // Track previous position for drawing lines
            double prevX = 0, prevY = 0;
            bool isFirst = true;

            foreach (var command in _analysisResult.Commands)
            {
                double startX = prevX;
                double startY = prevY;
                double endX = command.EndX * scale + offsetX;
                // Flip Y axis for screen coordinates (Y increases downward in WPF)
                double endY = canvasHeight - (command.EndY * scale + offsetY);

                if (!isFirst)
                {
                    startX = prevX;
                    startY = prevY;

                    if (command.Type is GcodeCommandType.G2 or GcodeCommandType.G3)
                    {
                        // Draw arc
                        DrawArc(command, startX, startY, endX, endY, scale, canvasHeight, offsetX, offsetY);
                    }
                    else
                    {
                        // Draw line
                        var line = new Line
                        {
                            X1 = startX,
                            Y1 = startY,
                            X2 = endX,
                            Y2 = endY,
                            Stroke = GetCommandColor(command.Type),
                            StrokeThickness = command.IsRapid ? 1 : 2,
                            StrokeDashArray = command.IsRapid ? [4, 2] : null
                        };
                        ToolpathCanvas.Children.Add(line);

                        // Draw direction arrows along the line
                        DrawDirectionArrows(startX, startY, endX, endY, GetCommandColor(command.Type));
                    }
                }

                prevX = endX;
                prevY = endY;
                isFirst = false;
            }

            // Draw start point marker
            if (_analysisResult.Commands.Count > 0)
            {
                var firstCmd = _analysisResult.Commands[0];
                double startMarkerX = firstCmd.EndX * scale + offsetX;
                double startMarkerY = canvasHeight - (firstCmd.EndY * scale + offsetY);

                var startMarker = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.LimeGreen,
                    Stroke = Brushes.DarkGreen,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(startMarker, startMarkerX - 5);
                Canvas.SetTop(startMarker, startMarkerY - 5);
                ToolpathCanvas.Children.Add(startMarker);

                // Draw end point marker (red)
                var lastCmd = _analysisResult.Commands[^1];
                double endMarkerX = lastCmd.EndX * scale + offsetX;
                double endMarkerY = canvasHeight - (lastCmd.EndY * scale + offsetY);

                var endMarker = new Ellipse
                {
                    Width = 10,
                    Height = 10,
                    Fill = Brushes.Red,
                    Stroke = Brushes.DarkRed,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(endMarker, endMarkerX - 5);
                Canvas.SetTop(endMarker, endMarkerY - 5);
                ToolpathCanvas.Children.Add(endMarker);
            }

            // Draw dimensional measurements
            DrawDimensions(dataWidth, dataHeight, scale, canvasWidth, canvasHeight, offsetX, offsetY);
        }

        /// <summary>
        /// Draws dimensional measurement lines showing width and height in mm and inches.
        /// </summary>
        private void DrawDimensions(double dataWidth, double dataHeight, double scale, 
            double canvasWidth, double canvasHeight, double offsetX, double offsetY)
        {
            const double dimLineOffset = 30; // Distance from the edge of the part
            const double tickSize = 8;
            const double mmToInches = 0.0393701;

            var dimBrush = new SolidColorBrush(Color.FromRgb(100, 200, 255)); // Light blue
            var dimLineBrush = new SolidColorBrush(Color.FromRgb(100, 200, 255));
            const double dimLineThickness = 1.5;

            // Calculate bounding box in screen coordinates
            double minScreenX = _analysisResult.MinX * scale + offsetX;
            double maxScreenX = _analysisResult.MaxX * scale + offsetX;
            double minScreenY = canvasHeight - (_analysisResult.MaxY * scale + offsetY);
            double maxScreenY = canvasHeight - (_analysisResult.MinY * scale + offsetY);

            // Draw horizontal dimension (width) - below the part
            double horizDimY = maxScreenY + dimLineOffset;
            
            // Horizontal dimension line
            var horizLine = new Line
            {
                X1 = minScreenX,
                Y1 = horizDimY,
                X2 = maxScreenX,
                Y2 = horizDimY,
                Stroke = dimLineBrush,
                StrokeThickness = dimLineThickness
            };
            ToolpathCanvas.Children.Add(horizLine);

            // Left tick
            var leftTick = new Line
            {
                X1 = minScreenX,
                Y1 = horizDimY - tickSize / 2,
                X2 = minScreenX,
                Y2 = horizDimY + tickSize / 2,
                Stroke = dimLineBrush,
                StrokeThickness = dimLineThickness
            };
            ToolpathCanvas.Children.Add(leftTick);

            // Right tick
            var rightTick = new Line
            {
                X1 = maxScreenX,
                Y1 = horizDimY - tickSize / 2,
                X2 = maxScreenX,
                Y2 = horizDimY + tickSize / 2,
                Stroke = dimLineBrush,
                StrokeThickness = dimLineThickness
            };
            ToolpathCanvas.Children.Add(rightTick);

            // Extension lines for horizontal dimension
            var leftExtLine = new Line
            {
                X1 = minScreenX,
                Y1 = maxScreenY,
                X2 = minScreenX,
                Y2 = horizDimY + tickSize / 2,
                Stroke = dimLineBrush,
                StrokeThickness = 1,
                StrokeDashArray = [3, 2]
            };
            ToolpathCanvas.Children.Add(leftExtLine);

            var rightExtLine = new Line
            {
                X1 = maxScreenX,
                Y1 = maxScreenY,
                X2 = maxScreenX,
                Y2 = horizDimY + tickSize / 2,
                Stroke = dimLineBrush,
                StrokeThickness = 1,
                StrokeDashArray = [3, 2]
            };
            ToolpathCanvas.Children.Add(rightExtLine);

            // Width text
            double widthInches = dataWidth * mmToInches;
            var widthText = new TextBlock
            {
                Text = $"{dataWidth:F2} mm\n({widthInches:F3}\")",
                Foreground = dimBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(200, 37, 37, 38)) // Semi-transparent dark background
            };
            Canvas.SetLeft(widthText, (minScreenX + maxScreenX) / 2 - 30);
            Canvas.SetTop(widthText, horizDimY + 10);
            ToolpathCanvas.Children.Add(widthText);

            // Draw vertical dimension (height) - to the right of the part
            double vertDimX = maxScreenX + dimLineOffset;

            // Vertical dimension line
            var vertLine = new Line
            {
                X1 = vertDimX,
                Y1 = minScreenY,
                X2 = vertDimX,
                Y2 = maxScreenY,
                Stroke = dimLineBrush,
                StrokeThickness = dimLineThickness
            };
            ToolpathCanvas.Children.Add(vertLine);

            // Top tick
            var topTick = new Line
            {
                X1 = vertDimX - tickSize / 2,
                Y1 = minScreenY,
                X2 = vertDimX + tickSize / 2,
                Y2 = minScreenY,
                Stroke = dimLineBrush,
                StrokeThickness = dimLineThickness
            };
            ToolpathCanvas.Children.Add(topTick);

            // Bottom tick
            var bottomTick = new Line
            {
                X1 = vertDimX - tickSize / 2,
                Y1 = maxScreenY,
                X2 = vertDimX + tickSize / 2,
                Y2 = maxScreenY,
                Stroke = dimLineBrush,
                StrokeThickness = dimLineThickness
            };
            ToolpathCanvas.Children.Add(bottomTick);

            // Extension lines for vertical dimension
            var topExtLine = new Line
            {
                X1 = maxScreenX,
                Y1 = minScreenY,
                X2 = vertDimX + tickSize / 2,
                Y2 = minScreenY,
                Stroke = dimLineBrush,
                StrokeThickness = 1,
                StrokeDashArray = [3, 2]
            };
            ToolpathCanvas.Children.Add(topExtLine);

            var bottomExtLine = new Line
            {
                X1 = maxScreenX,
                Y1 = maxScreenY,
                X2 = vertDimX + tickSize / 2,
                Y2 = maxScreenY,
                Stroke = dimLineBrush,
                StrokeThickness = 1,
                StrokeDashArray = [3, 2]
            };
            ToolpathCanvas.Children.Add(bottomExtLine);

            // Height text (rotated)
            double heightInches = dataHeight * mmToInches;
            var heightText = new TextBlock
            {
                Text = $"{dataHeight:F2} mm\n({heightInches:F3}\")",
                Foreground = dimBrush,
                FontSize = 11,
                FontWeight = FontWeights.Bold,
                TextAlignment = TextAlignment.Center,
                Background = new SolidColorBrush(Color.FromArgb(200, 37, 37, 38)),
                RenderTransform = new RotateTransform(-90)
            };
            Canvas.SetLeft(heightText, vertDimX + 15);
            Canvas.SetTop(heightText, (minScreenY + maxScreenY) / 2 + 30);
            ToolpathCanvas.Children.Add(heightText);
        }

        /// <summary>
        /// Draws direction arrows along a line segment.
        /// </summary>
        private void DrawDirectionArrows(double startX, double startY, double endX, double endY, SolidColorBrush color)
        {
            double dx = endX - startX;
            double dy = endY - startY;
            double length = Math.Sqrt(dx * dx + dy * dy);

            if (length < ArrowSpacing / 2)
            {
                return; // Line too short for arrows
            }

            // Normalize direction
            double dirX = dx / length;
            double dirY = dy / length;

            // Calculate perpendicular for arrow wings
            double perpX = -dirY;
            double perpY = dirX;

            // Draw arrows at regular intervals
            int arrowCount = Math.Max(1, (int)(length / ArrowSpacing));
            double step = length / (arrowCount + 1);

            for (int i = 1; i <= arrowCount; i++)
            {
                double t = step * i;
                double arrowX = startX + dirX * t;
                double arrowY = startY + dirY * t;

                // Arrow tip is at (arrowX, arrowY), pointing in direction of travel
                var arrow = new Polygon
                {
                    Fill = color,
                    Points = new PointCollection
                    {
                        new Point(arrowX + dirX * ArrowSize, arrowY + dirY * ArrowSize),
                        new Point(arrowX - dirX * ArrowSize / 2 + perpX * ArrowSize / 2, arrowY - dirY * ArrowSize / 2 + perpY * ArrowSize / 2),
                        new Point(arrowX - dirX * ArrowSize / 2 - perpX * ArrowSize / 2, arrowY - dirY * ArrowSize / 2 - perpY * ArrowSize / 2)
                    }
                };
                ToolpathCanvas.Children.Add(arrow);
            }
        }

        private void DrawArc(GcodeCommand command, double startX, double startY, 
            double endX, double endY, double scale, double canvasHeight, double offsetX, double offsetY)
        {
            // For simplicity, approximate arcs with line segments
            // A full arc implementation would use PathGeometry with ArcSegment

            if (!command.I.HasValue && !command.J.HasValue)
            {
                // If no center offset, just draw a line
                var line = new Line
                {
                    X1 = startX,
                    Y1 = startY,
                    X2 = endX,
                    Y2 = endY,
                    Stroke = GetCommandColor(command.Type),
                    StrokeThickness = 2
                };
                ToolpathCanvas.Children.Add(line);
                DrawDirectionArrows(startX, startY, endX, endY, GetCommandColor(command.Type));
                return;
            }

            // Calculate center point
            double prevEndX = (startX - offsetX) / scale;
            double prevEndY = (canvasHeight - startY - offsetY) / scale;
            double centerX = prevEndX + (command.I ?? 0);
            double centerY = prevEndY + (command.J ?? 0);

            // Calculate radius
            double radius = Math.Sqrt(Math.Pow(command.I ?? 0, 2) + Math.Pow(command.J ?? 0, 2));

            // Calculate start and end angles
            double startAngle = Math.Atan2(prevEndY - centerY, prevEndX - centerX);
            double endAngle = Math.Atan2(command.EndY - centerY, command.EndX - centerX);

            // Determine sweep direction
            bool clockwise = command.Type == GcodeCommandType.G2;

            // Generate arc points
            int segments = 32;
            double angleDiff = endAngle - startAngle;

            // Adjust angle difference for arc direction
            if (clockwise && angleDiff > 0) angleDiff -= 2 * Math.PI;
            if (!clockwise && angleDiff < 0) angleDiff += 2 * Math.PI;

            double angleStep = angleDiff / segments;

            var polyline = new Polyline
            {
                Stroke = GetCommandColor(command.Type),
                StrokeThickness = 2
            };

            var arcPoints = new List<Point>();
            for (int i = 0; i <= segments; i++)
            {
                double angle = startAngle + angleStep * i;
                double x = centerX + radius * Math.Cos(angle);
                double y = centerY + radius * Math.Sin(angle);

                double screenX = x * scale + offsetX;
                double screenY = canvasHeight - (y * scale + offsetY);

                var point = new Point(screenX, screenY);
                polyline.Points.Add(point);
                arcPoints.Add(point);
            }

            ToolpathCanvas.Children.Add(polyline);

            // Draw arrows along the arc
            DrawArcDirectionArrows(arcPoints, GetCommandColor(command.Type));
        }

        /// <summary>
        /// Draws direction arrows along an arc defined by a list of points.
        /// </summary>
        private void DrawArcDirectionArrows(List<Point> points, SolidColorBrush color)
        {
            if (points.Count < 2)
            {
                return;
            }

            // Calculate total arc length
            double totalLength = 0;
            for (int i = 1; i < points.Count; i++)
            {
                double dx = points[i].X - points[i - 1].X;
                double dy = points[i].Y - points[i - 1].Y;
                totalLength += Math.Sqrt(dx * dx + dy * dy);
            }

            if (totalLength < ArrowSpacing / 2)
            {
                return;
            }

            int arrowCount = Math.Max(1, (int)(totalLength / ArrowSpacing));
            double targetSpacing = totalLength / (arrowCount + 1);

            double accumulatedLength = 0;
            int nextArrowIndex = 1;
            double nextArrowDistance = targetSpacing;

            for (int i = 1; i < points.Count && nextArrowIndex <= arrowCount; i++)
            {
                double dx = points[i].X - points[i - 1].X;
                double dy = points[i].Y - points[i - 1].Y;
                double segmentLength = Math.Sqrt(dx * dx + dy * dy);

                while (accumulatedLength + segmentLength >= nextArrowDistance && nextArrowIndex <= arrowCount)
                {
                    double t = (nextArrowDistance - accumulatedLength) / segmentLength;
                    double arrowX = points[i - 1].X + dx * t;
                    double arrowY = points[i - 1].Y + dy * t;

                    // Direction along the arc at this point
                    double dirX = dx / segmentLength;
                    double dirY = dy / segmentLength;
                    double perpX = -dirY;
                    double perpY = dirX;

                    var arrow = new Polygon
                    {
                        Fill = color,
                        Points = new PointCollection
                        {
                            new Point(arrowX + dirX * ArrowSize, arrowY + dirY * ArrowSize),
                            new Point(arrowX - dirX * ArrowSize / 2 + perpX * ArrowSize / 2, arrowY - dirY * ArrowSize / 2 + perpY * ArrowSize / 2),
                            new Point(arrowX - dirX * ArrowSize / 2 - perpX * ArrowSize / 2, arrowY - dirY * ArrowSize / 2 - perpY * ArrowSize / 2)
                        }
                    };
                    ToolpathCanvas.Children.Add(arrow);

                    nextArrowIndex++;
                    nextArrowDistance = targetSpacing * nextArrowIndex;
                }

                accumulatedLength += segmentLength;
            }
        }

        private static SolidColorBrush GetCommandColor(GcodeCommandType type) => type switch
        {
            GcodeCommandType.G0 => Brushes.Orange,
            GcodeCommandType.G1 => Brushes.Blue,
            GcodeCommandType.G2 => Brushes.Green,
            GcodeCommandType.G3 => Brushes.Purple,
            _ => Brushes.Gray
        };
    }
}