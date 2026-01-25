using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using HelixToolkit.Wpf;

namespace GcodeAnalyzer;

/// <summary>
/// 3D visualization control for G-code toolpaths with simulation playback.
/// </summary>
public partial class Toolpath3DViewer : UserControl
{
    private const double TubeRadius = 0.3;
    private const int TubeSegments = 8;
    private const int ArcSegments = 32;
    private const double MinSegmentLength = 0.001;
    private const double ArrowSpacing = 20;
    private const double ArrowSize = 1.5;
    
    // Simulation constants
    private const double ToolPointerHeight = 30;  // Changed from 15 to 30 (2x)
    private const double ToolPointerRadius = 3.0;  // Changed from 1.5 to 3.0 (2x)
    private const double DefaultRapidFeedRate = 5000; // mm/min
    private const double DefaultFeedRate = 1000; // mm/min

    // Simulation state
    private GcodeAnalysisResult? _analysisResult;
    private DispatcherTimer? _simulationTimer;
    private readonly List<SimulationSegment> _simulationSegments = [];
    private double _currentSimulationTime;
    private double _totalSimulationTime;
    private double _playbackSpeed = 1.0;
    private bool _isPlaying;
    private SphereVisual3D? _toolPointer;
    private TubeVisual3D? _toolPointerShaft;
    private ModelVisual3D? _toolPointerContainerRef;

    // Speed multiplier levels for fast forward/rewind
    private static readonly double[] SpeedMultipliers = [0.25, 0.5, 1.0, 2.0, 4.0, 8.0, 16.0, 32.0];
    private int _currentSpeedIndex = 2; // Start at 1x
    
    // Track last reported line to avoid redundant event firing
    private int _lastReportedLine = -1;

    // Track if this is the first render to preserve camera state on subsequent renders
    private bool _isFirstRender = true;
    private Point3D _savedCameraPosition;
    private Vector3D _savedCameraLookDirection;
    private Vector3D _savedCameraUpDirection;

    // Event to notify when current line changes
    public event EventHandler<int>? CurrentLineChanged;

    /// <summary>
    /// Gets the current simulation line number.
    /// </summary>
    public int GetCurrentLine()
    {
        return _lastReportedLine;
    }

    public Toolpath3DViewer()
    {
        InitializeComponent();
        InitializeSimulationTimer();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Find the ToolPointerContainer in the viewport
        _toolPointerContainerRef = FindName("ToolPointerContainer") as ModelVisual3D;
        
        // Set focus to receive keyboard input
        Focus();
    }

    /// <summary>
    /// Handles keyboard shortcuts for camera rotation.
    /// </summary>
    private void UserControl_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        const double rotationAngle = 10.0; // 10 degrees
        
        bool handled = true;
        
        switch (e.Key)
        {
            case Key.Up:
                RotateCameraAroundAxis(new Vector3D(1, 0, 0), rotationAngle);
                break;
            case Key.Down:
                RotateCameraAroundAxis(new Vector3D(1, 0, 0), -rotationAngle);
                break;
            case Key.Left:
                RotateCameraAroundAxis(new Vector3D(0, 1, 0), -rotationAngle);
                break;
            case Key.Right:
                RotateCameraAroundAxis(new Vector3D(0, 1, 0), rotationAngle);
                break;
            case Key.OemComma: // , key
                RotateCameraAroundAxis(new Vector3D(0, 0, 1), -rotationAngle);
                break;
            case Key.OemPeriod: // . key
                RotateCameraAroundAxis(new Vector3D(0, 0, 1), rotationAngle);
                break;
            default:
                handled = false;
                break;
        }
        
        e.Handled = handled;
    }

    /// <summary>
    /// Rotates the camera around a specified axis by the given angle.
    /// </summary>
    private void RotateCameraAroundAxis(Vector3D axis, double angleDegrees)
    {
        if (Viewport3D.Camera is not PerspectiveCamera camera)
        {
            return;
        }

        // Calculate the target point (where we're looking at)
        var target = camera.Position + camera.LookDirection;
        
        // Create rotation transform around the target point
        var rotation = new AxisAngleRotation3D(axis, angleDegrees);
        var transform = new RotateTransform3D(rotation, target);
        
        // Apply rotation to camera position and orientation
        camera.Position = transform.Transform(camera.Position);
        camera.LookDirection = target - camera.Position;
        camera.UpDirection = transform.Transform(camera.UpDirection);
    }

    /// <summary>
    /// Configures the camera controller when the control is loaded.
    /// </summary>
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        var controller = Viewport3D.CameraController;
        if (controller is not null)
        {
            // Configure left mouse button for panning, right for rotation
            controller.LeftRightPanSensitivity = 1.0;
            controller.UpDownPanSensitivity = 1.0;
            
            // Swap the default gestures by handling mouse events
            Viewport3D.PreviewMouseDown += Viewport3D_PreviewMouseDown;
            Viewport3D.PreviewMouseUp += Viewport3D_PreviewMouseUp;
            Viewport3D.PreviewMouseMove += Viewport3D_PreviewMouseMove;
        }
    }

    private bool _isPanning;
    private bool _isRotating;
    private Point _lastMousePosition;

    private void Viewport3D_PreviewMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            _isPanning = true;
            _lastMousePosition = e.GetPosition(Viewport3D);
            Viewport3D.CaptureMouse();
            e.Handled = true;
        }
        else if (e.RightButton == MouseButtonState.Pressed)
        {
            _isRotating = true;
            _lastMousePosition = e.GetPosition(Viewport3D);
            Viewport3D.CaptureMouse();
            e.Handled = true;
        }
    }

    private void Viewport3D_PreviewMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.LeftButton == MouseButtonState.Released && _isPanning)
        {
            _isPanning = false;
            Viewport3D.ReleaseMouseCapture();
            e.Handled = true;
        }
        if (e.RightButton == MouseButtonState.Released && _isRotating)
        {
            _isRotating = false;
            Viewport3D.ReleaseMouseCapture();
            e.Handled = true;
        }
    }

    private void Viewport3D_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isPanning && !_isRotating)
        {
            return;
        }

        var currentPosition = e.GetPosition(Viewport3D);
        var delta = currentPosition - _lastMousePosition;
        _lastMousePosition = currentPosition;

        var camera = Viewport3D.Camera as PerspectiveCamera;
        if (camera is null)
        {
            return;
        }

        if (_isPanning)
        {
            // Pan the camera
            var lookDirection = camera.LookDirection;
            lookDirection.Normalize();
            var upDirection = camera.UpDirection;
            upDirection.Normalize();
            var rightDirection = Vector3D.CrossProduct(lookDirection, upDirection);
            rightDirection.Normalize();

            double panSpeed = 0.5;
            var panVector = rightDirection * (-delta.X * panSpeed) + upDirection * (delta.Y * panSpeed);
            camera.Position += panVector;
            e.Handled = true;
        }
        else if (_isRotating)
        {
            // Rotate the camera around the look target
            double rotateSpeed = 0.3;
            
            // Calculate the target point (where we're looking at)
            var target = camera.Position + camera.LookDirection;
            
            // Horizontal rotation (around up axis)
            var horizontalRotation = new AxisAngleRotation3D(new Vector3D(0, 0, 1), -delta.X * rotateSpeed);
            var horizontalTransform = new RotateTransform3D(horizontalRotation, target);
            camera.Position = horizontalTransform.Transform(camera.Position);
            camera.LookDirection = target - camera.Position;
            
            // Vertical rotation (around right axis) - inverted Y for natural feel
            var lookDir = camera.LookDirection;
            lookDir.Normalize();
            var rightDir = Vector3D.CrossProduct(lookDir, camera.UpDirection);
            rightDir.Normalize();
            
            var verticalRotation = new AxisAngleRotation3D(rightDir, -delta.Y * rotateSpeed);
            var verticalTransform = new RotateTransform3D(verticalRotation, target);
            camera.Position = verticalTransform.Transform(camera.Position);
            camera.LookDirection = target - camera.Position;
            camera.UpDirection = verticalTransform.Transform(camera.UpDirection);
            
            e.Handled = true;
        }
    }

    private void InitializeSimulationTimer()
    {
        _simulationTimer = new DispatcherTimer(DispatcherPriority.Normal)
        {
            Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
        };
        _simulationTimer.Tick += SimulationTimer_Tick;
    }

    private void SimulationTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPlaying || _totalSimulationTime <= 0)
        {
            return;
        }

        // Advance simulation time based on playback speed
        // Timer interval is 16ms, convert to minutes and apply speed
        double deltaMinutes = (16.0 / 60000.0) * _playbackSpeed;
        _currentSimulationTime += deltaMinutes;

        // Clamp to bounds
        if (_currentSimulationTime >= _totalSimulationTime)
        {
            _currentSimulationTime = _totalSimulationTime;
            _isPlaying = false;
            _simulationTimer?.Stop();
            UpdatePlayPauseButton();
        }
        else if (_currentSimulationTime < 0)
        {
            _currentSimulationTime = 0;
            _isPlaying = false;
            _simulationTimer?.Stop();
            UpdatePlayPauseButton();
        }

        // Update directly without BeginInvoke to avoid queuing and stuttering
        UpdateToolPointerPosition();
        UpdateTimelineSlider();
        UpdateTimeDisplay();
    }

    /// <summary>
    /// Renders the toolpath and prepares simulation data.
    /// </summary>
    public void RenderToolpath(GcodeAnalysisResult analysisResult)
    {
        // Save camera state if this is not the first render
        if (!_isFirstRender && Viewport3D.Camera is PerspectiveCamera camera)
        {
            _savedCameraPosition = camera.Position;
            _savedCameraLookDirection = camera.LookDirection;
            _savedCameraUpDirection = camera.UpDirection;
        }

        // Save current simulation state before clearing
        double savedSimulationTime = _currentSimulationTime;
        bool savedIsPlaying = _isPlaying;
        double savedPlaybackSpeed = _playbackSpeed;
        int savedSpeedIndex = _currentSpeedIndex;

        // Clear the toolpath container but DON'T reset the first render flag
        ToolpathContainer.Children.Clear();
        
        // Stop simulation temporarily but don't reset time
        if (_isPlaying)
        {
            _simulationTimer?.Stop();
        }

        _analysisResult = analysisResult;

        var simulationControls = FindName("SimulationControls") as Border;
        var lineNumberDisplay = FindName("LineNumberDisplay") as Border;

        if (analysisResult is null || analysisResult.TotalCommands == 0)
        {
            if (simulationControls is not null)
            {
                simulationControls.Visibility = Visibility.Collapsed;
            }
            if (lineNumberDisplay is not null)
            {
                lineNumberDisplay.Visibility = Visibility.Collapsed;
            }
            return;
        }

        // Update grid size based on toolpath bounds
        UpdateGrid(analysisResult);

        // Build simulation segments and render toolpath
        BuildSimulationSegments(analysisResult);

        // Track previous position
        double prevX = 0, prevY = 0, prevZ = 0;
        bool isFirst = true;

        foreach (var command in analysisResult.Commands)
        {
            if (!isFirst)
            {
                var startPoint = new Point3D(prevX, prevY, prevZ);
                var endPoint = new Point3D(command.EndX, command.EndY, command.EndZ);

                if (command.Type is GcodeCommandType.G2 or GcodeCommandType.G3)
                {
                    RenderArc(command, startPoint, endPoint);
                }
                else
                {
                    RenderLine(startPoint, endPoint, command.Type);
                }
            }

            prevX = command.EndX;
            prevY = command.EndY;
            prevZ = command.EndZ;
            isFirst = false;
        }

        // Add start and end markers
        if (analysisResult.Commands.Count > 0)
        {
            var firstCmd = analysisResult.Commands[0];
            AddMarker(new Point3D(firstCmd.EndX, firstCmd.EndY, firstCmd.EndZ), Colors.LimeGreen, 1.5);

            var lastCmd = analysisResult.Commands[^1];
            AddMarker(new Point3D(lastCmd.EndX, lastCmd.EndY, lastCmd.EndZ), Colors.Red, 1.5);
        }

        // Add 3D dimension measurements
        AddDimensionMeasurements(analysisResult);

        // Create tool pointer
        CreateToolPointer(analysisResult);

        // Show simulation controls and line number display
        if (simulationControls is not null)
        {
            simulationControls.Visibility = Visibility.Visible;
        }
        if (lineNumberDisplay is not null)
        {
            lineNumberDisplay.Visibility = Visibility.Visible;
        }
        
        // Only reset simulation on first render, otherwise restore saved state
        if (_isFirstRender)
        {
            ResetSimulation();
            Viewport3D.ZoomExtents();
            _isFirstRender = false;
        }
        else
        {
            // Restore simulation state
            _currentSimulationTime = Math.Min(savedSimulationTime, _totalSimulationTime);
            _playbackSpeed = savedPlaybackSpeed;
            _currentSpeedIndex = savedSpeedIndex;
            
            // Update UI elements
            UpdateToolPointerPosition();
            UpdateTimelineSlider();
            UpdateTimeDisplay();
            UpdateSpeedDisplay();
            
            // Resume playback if it was playing
            if (savedIsPlaying)
            {
                _isPlaying = true;
                _simulationTimer?.Start();
            }
            
            UpdatePlayPauseButton();
            
            // Restore camera state
            if (Viewport3D.Camera is PerspectiveCamera restoredCamera)
            {
                restoredCamera.Position = _savedCameraPosition;
                restoredCamera.LookDirection = _savedCameraLookDirection;
                restoredCamera.UpDirection = _savedCameraUpDirection;
            }
        }
    }

    private void BuildSimulationSegments(GcodeAnalysisResult analysisResult)
    {
        _simulationSegments.Clear();
        _totalSimulationTime = 0;

        double prevX = 0, prevY = 0, prevZ = 0;
        double currentFeedRate = DefaultFeedRate;
        bool isFirst = true;

        foreach (var command in analysisResult.Commands)
        {
            if (command.FeedRate.HasValue && command.FeedRate.Value > 0)
            {
                currentFeedRate = command.FeedRate.Value;
            }

            if (!isFirst)
            {
                var segment = new SimulationSegment
                {
                    StartX = prevX,
                    StartY = prevY,
                    StartZ = prevZ,
                    EndX = command.EndX,
                    EndY = command.EndY,
                    EndZ = command.EndZ,
                    Command = command,
                    StartTime = _totalSimulationTime
                };

                // Calculate distance and duration
                double distance;
                if (command.Type is GcodeCommandType.G2 or GcodeCommandType.G3)
                {
                    distance = CalculateArcLength(command, prevX, prevY, prevZ);
                    segment.IsArc = true;
                    segment.ArcPoints = GenerateArcPoints(command, prevX, prevY, prevZ);
                }
                else
                {
                    double dx = command.EndX - prevX;
                    double dy = command.EndY - prevY;
                    double dz = command.EndZ - prevZ;
                    distance = Math.Sqrt(dx * dx + dy * dy + dz * dz);
                }

                double effectiveFeedRate = command.Type == GcodeCommandType.G0 
                    ? DefaultRapidFeedRate 
                    : currentFeedRate;

                segment.Duration = effectiveFeedRate > 0 ? distance / effectiveFeedRate : 0;
                segment.EndTime = segment.StartTime + segment.Duration;

                _simulationSegments.Add(segment);
                _totalSimulationTime = segment.EndTime;
            }

            prevX = command.EndX;
            prevY = command.EndY;
            prevZ = command.EndZ;
            isFirst = false;
        }

        // Update total time display
        UpdateTotalTimeDisplay();
    }

    private List<Point3D> GenerateArcPoints(GcodeCommand command, double startX, double startY, double startZ)
    {
        var points = new List<Point3D>();

        if (!command.I.HasValue && !command.J.HasValue)
        {
            points.Add(new Point3D(startX, startY, startZ));
            points.Add(new Point3D(command.EndX, command.EndY, command.EndZ));
            return points;
        }

        double centerX = startX + (command.I ?? 0);
        double centerY = startY + (command.J ?? 0);
        double radius = Math.Sqrt(Math.Pow(command.I ?? 0, 2) + Math.Pow(command.J ?? 0, 2));

        double startAngle = Math.Atan2(startY - centerY, startX - centerX);
        double endAngle = Math.Atan2(command.EndY - centerY, command.EndX - centerX);

        bool clockwise = command.Type == GcodeCommandType.G2;
        double angleDiff = endAngle - startAngle;
        if (clockwise && angleDiff > 0) angleDiff -= 2 * Math.PI;
        if (!clockwise && angleDiff < 0) angleDiff += 2 * Math.PI;

        double zStep = (command.EndZ - startZ) / ArcSegments;

        for (int i = 0; i <= ArcSegments; i++)
        {
            double t = (double)i / ArcSegments;
            double angle = startAngle + angleDiff * t;
            double x = centerX + radius * Math.Cos(angle);
            double y = centerY + radius * Math.Sin(angle);
            double z = startZ + zStep * i;
            points.Add(new Point3D(x, y, z));
        }

        return points;
    }

    private double CalculateArcLength(GcodeCommand command, double startX, double startY, double startZ)
    {
        if (!command.I.HasValue && !command.J.HasValue)
        {
            double deltaX = command.EndX - startX;
            double deltaY = command.EndY - startY;
            double deltaZ = command.EndZ - startZ;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        double centerX = startX + (command.I ?? 0);
        double centerY = startY + (command.J ?? 0);
        double radius = Math.Sqrt(Math.Pow(command.I ?? 0, 2) + Math.Pow(command.J ?? 0, 2));

        if (radius < 0.001) return 0;

        double startAngle = Math.Atan2(startY - centerY, startX - centerX);
        double endAngle = Math.Atan2(command.EndY - centerY, command.EndX - centerX);

        bool clockwise = command.Type == GcodeCommandType.G2;
        double angleDiff = endAngle - startAngle;
        if (clockwise && angleDiff > 0) angleDiff -= 2 * Math.PI;
        if (!clockwise && angleDiff < 0) angleDiff += 2 * Math.PI;

        double arcLength2D = Math.Abs(angleDiff) * radius;
        double zDelta = command.EndZ - startZ;
        return Math.Sqrt(arcLength2D * arcLength2D + zDelta * zDelta);
    }

    private void CreateToolPointer(GcodeAnalysisResult analysisResult)
    {
        if (_toolPointerContainerRef is null)
        {
            _toolPointerContainerRef = FindName("ToolPointerContainer") as ModelVisual3D;
        }

        // Remove existing tool pointer
        if (_toolPointer is not null && _toolPointerContainerRef is not null)
        {
            _toolPointerContainerRef.Children.Remove(_toolPointer);
        }
        if (_toolPointerShaft is not null && _toolPointerContainerRef is not null)
        {
            _toolPointerContainerRef.Children.Remove(_toolPointerShaft);
        }

        if (analysisResult.Commands.Count == 0 || _toolPointerContainerRef is null) return;

        var firstCmd = analysisResult.Commands[0];
        var startPos = new Point3D(firstCmd.EndX, firstCmd.EndY, firstCmd.EndZ);

        // Create tool tip (sphere)
        var tipMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Yellow));
        _toolPointer = new SphereVisual3D
        {
            Center = startPos,
            Radius = ToolPointerRadius,
            Material = tipMaterial
        };

        // Create tool shaft (vertical line)
        var shaftMaterial = new DiffuseMaterial(new SolidColorBrush(Colors.Silver));
        _toolPointerShaft = new TubeVisual3D
        {
            Path = [startPos, new Point3D(startPos.X, startPos.Y, startPos.Z + ToolPointerHeight)],
            Diameter = ToolPointerRadius * 0.5,
            Material = shaftMaterial
        };

        _toolPointerContainerRef.Children.Add(_toolPointer);
        _toolPointerContainerRef.Children.Add(_toolPointerShaft);
    }

    private void UpdateToolPointerPosition()
    {
        if (_toolPointer is null || _toolPointerShaft is null || _simulationSegments.Count == 0)
        {
            return;
        }

        // Find current segment
        Point3D position;
        SimulationSegment? currentSegment = null;
        
        if (_currentSimulationTime <= 0)
        {
            var firstSeg = _simulationSegments[0];
            position = new Point3D(firstSeg.StartX, firstSeg.StartY, firstSeg.StartZ);
            currentSegment = firstSeg;
        }
        else if (_currentSimulationTime >= _totalSimulationTime)
        {
            var lastSeg = _simulationSegments[^1];
            position = new Point3D(lastSeg.EndX, lastSeg.EndY, lastSeg.EndZ);
            currentSegment = lastSeg;
        }
        else
        {
            // Find the segment containing current time
            var segment = _simulationSegments.FirstOrDefault(s => 
                _currentSimulationTime >= s.StartTime && _currentSimulationTime < s.EndTime);

            if (segment is null)
            {
                segment = _simulationSegments.LastOrDefault(s => _currentSimulationTime >= s.StartTime);
            }

            if (segment is not null)
            {
                currentSegment = segment;
                
                double t = segment.Duration > 0 
                    ? (_currentSimulationTime - segment.StartTime) / segment.Duration 
                    : 0;
                t = Math.Clamp(t, 0, 1);

                if (segment.IsArc && segment.ArcPoints?.Count > 1)
                {
                    // Interpolate along arc points
                    double arcT = t * (segment.ArcPoints.Count - 1);
                    int index = (int)arcT;
                    double localT = arcT - index;

                    if (index >= segment.ArcPoints.Count - 1)
                    {
                        position = segment.ArcPoints[^1];
                    }
                    else
                    {
                        var p1 = segment.ArcPoints[index];
                        var p2 = segment.ArcPoints[index + 1];
                        position = new Point3D(
                            p1.X + (p2.X - p1.X) * localT,
                            p1.Y + (p2.Y - p1.Y) * localT,
                            p1.Z + (p2.Z - p1.Z) * localT);
                    }
                }
                else
                {
                    // Linear interpolation
                    position = new Point3D(
                        segment.StartX + (segment.EndX - segment.StartX) * t,
                        segment.StartY + (segment.EndY - segment.StartY) * t,
                        segment.StartZ + (segment.EndZ - segment.StartZ) * t);
                }
            }
            else
            {
                return;
            }
        }

        // Update tool pointer position
        _toolPointer.Center = position;
        _toolPointerShaft.Path = [position, new Point3D(position.X, position.Y, position.Z + ToolPointerHeight)];
        
        // Update line number display
        UpdateLineNumberDisplay(currentSegment);
    }

    private void UpdateLineNumberDisplay(SimulationSegment? segment)
    {
        int currentLine = segment?.Command?.LineNumber ?? -1;
        
        if (FindName("TxtCurrentLine") is TextBlock txt)
        {
            if (currentLine > 0)
            {
                txt.Text = currentLine.ToString();
            }
            else
            {
                txt.Text = "-";
            }
        }
        
        // Only fire event if the line actually changed
        if (currentLine != _lastReportedLine)
        {
            _lastReportedLine = currentLine;
            CurrentLineChanged?.Invoke(this, currentLine);
        }
    }

    #region Simulation Controls

    private void BtnSkipToStart_Click(object sender, RoutedEventArgs e)
    {
        _currentSimulationTime = 0;
        _lastReportedLine = -1; // Force update on next position update
        UpdateToolPointerPosition();
        UpdateTimelineSlider();
        UpdateTimeDisplay();
    }

    private void BtnPreviousLine_Click(object sender, RoutedEventArgs e)
    {
        // Pause playback during stepping
        if (_isPlaying)
        {
            PauseSimulation();
        }

        if (_simulationSegments.Count == 0) return;

        // Find the current segment index
        int currentIndex = -1;
        
        // First, try to find the segment we're currently in or at the start of
        for (int i = 0; i < _simulationSegments.Count; i++)
        {
            // Check if we're within this segment OR at its exact start time
            if (_currentSimulationTime >= _simulationSegments[i].StartTime && 
                _currentSimulationTime <= _simulationSegments[i].EndTime)
            {
                currentIndex = i;
                break;
            }
        }
        
        // If we're not in a segment, find the one we just passed
        if (currentIndex == -1)
        {
            for (int i = _simulationSegments.Count - 1; i >= 0; i--)
            {
                if (_currentSimulationTime > _simulationSegments[i].EndTime)
                {
                    currentIndex = i;
                    break;
                }
            }
        }

        // Move to previous line
        if (currentIndex > 0)
        {
            // Special handling: if we're exactly at the start of the current segment,
            // we should go to the previous segment
            if (Math.Abs(_currentSimulationTime - _simulationSegments[currentIndex].StartTime) < 0.0001)
            {
                _currentSimulationTime = _simulationSegments[currentIndex - 1].StartTime;
            }
            else
            {
                // We're in the middle or end of a segment, go to its start
                _currentSimulationTime = _simulationSegments[currentIndex].StartTime;
            }
        }
        else if (currentIndex == 0)
        {
            // Already at first segment, go to start
            _currentSimulationTime = 0;
        }
        else
        {
            // We're before all segments, go to start
            _currentSimulationTime = 0;
        }

        _lastReportedLine = -1; // Force update on next position update
        UpdateToolPointerPosition();
        UpdateTimelineSlider();
        UpdateTimeDisplay();
    }

    private void BtnNextLine_Click(object sender, RoutedEventArgs e)
    {
        // Pause playback during stepping
        if (_isPlaying)
        {
            PauseSimulation();
        }

        if (_simulationSegments.Count == 0) return;

        // Find the current segment index
        int currentIndex = -1;
        
        // First, try to find the segment we're currently in
        for (int i = 0; i < _simulationSegments.Count; i++)
        {
            if (_currentSimulationTime >= _simulationSegments[i].StartTime && 
                _currentSimulationTime < _simulationSegments[i].EndTime)
            {
                currentIndex = i;
                break;
            }
        }
        
        // If we're not in a segment, find the one we just passed or are about to enter
        if (currentIndex == -1)
        {
            // Check if we're before the first segment
            if (_currentSimulationTime < _simulationSegments[0].StartTime)
            {
                currentIndex = -1; // Before first segment
            }
            else
            {
                // Find the last segment we passed
                for (int i = _simulationSegments.Count - 1; i >= 0; i--)
                {
                    if (_currentSimulationTime >= _simulationSegments[i].EndTime)
                    {
                        currentIndex = i;
                        break;
                    }
                }
            }
        }

        // Move to next line
        if (currentIndex == -1)
        {
            // Before first segment, go to first segment
            _currentSimulationTime = _simulationSegments[0].StartTime;
        }
        else if (currentIndex < _simulationSegments.Count - 1)
        {
            // Go to the start of the next segment
            _currentSimulationTime = _simulationSegments[currentIndex + 1].StartTime;
        }
        else
        {
            // Already at last segment, go to end
            _currentSimulationTime = _totalSimulationTime;
        }

        _lastReportedLine = -1; // Force update on next position update
        UpdateToolPointerPosition();
        UpdateTimelineSlider();
        UpdateTimeDisplay();
    }

    private void BtnRewind_Click(object sender, RoutedEventArgs e)
    {
        // Decrease speed or go backwards
        if (_currentSpeedIndex > 0)
        {
            _currentSpeedIndex--;
        }
        _playbackSpeed = SpeedMultipliers[_currentSpeedIndex];
        UpdateSpeedDisplay();
    }

    private void BtnPlayPause_Click(object sender, RoutedEventArgs e)
    {
        if (_isPlaying)
        {
            PauseSimulation();
        }
        else
        {
            PlaySimulation();
        }
    }

    private void BtnFastForward_Click(object sender, RoutedEventArgs e)
    {
        // Increase speed
        if (_currentSpeedIndex < SpeedMultipliers.Length - 1)
        {
            _currentSpeedIndex++;
        }
        _playbackSpeed = SpeedMultipliers[_currentSpeedIndex];
        UpdateSpeedDisplay();
    }

    private void BtnSkipToEnd_Click(object sender, RoutedEventArgs e)
    {
        _currentSimulationTime = _totalSimulationTime;
        _isPlaying = false;
        _simulationTimer?.Stop();
        _lastReportedLine = -1; // Force update on next position update
        UpdatePlayPauseButton();
        UpdateToolPointerPosition();
        UpdateTimelineSlider();
        UpdateTimeDisplay();
    }

    private void TimelineSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isPlaying || _totalSimulationTime <= 0) return;

        // User is scrubbing the timeline
        _currentSimulationTime = (e.NewValue / 100.0) * _totalSimulationTime;
        _lastReportedLine = -1; // Force update on next position update
        UpdateToolPointerPosition();
        UpdateTimeDisplay();
    }

    private void PlaySimulation()
    {
        if (_currentSimulationTime >= _totalSimulationTime)
        {
            _currentSimulationTime = 0;
        }

        _isPlaying = true;
        _simulationTimer?.Start();
        UpdatePlayPauseButton();
    }

    private void PauseSimulation()
    {
        _isPlaying = false;
        _simulationTimer?.Stop();
        UpdatePlayPauseButton();
    }

    private void StopSimulation()
    {
        _isPlaying = false;
        _simulationTimer?.Stop();
        _currentSimulationTime = 0;
        _simulationSegments.Clear();
        _lastReportedLine = -1; // Reset line tracking
        UpdatePlayPauseButton();
    }

    private void ResetSimulation()
    {
        _currentSimulationTime = 0;
        _currentSpeedIndex = 2; // 1x speed
        _playbackSpeed = SpeedMultipliers[_currentSpeedIndex];
        _isPlaying = false;
        _simulationTimer?.Stop();
        _lastReportedLine = -1; // Reset line tracking

        UpdateToolPointerPosition();
        UpdateTimelineSlider();
        UpdateTimeDisplay();
        UpdateSpeedDisplay();
        UpdatePlayPauseButton();
    }

    private void UpdatePlayPauseButton()
    {
        if (FindName("BtnPlayPause") is Button btn)
        {
            btn.Content = _isPlaying ? "\u23F8" : "\u25B6";
        }
    }

    private void UpdateTimelineSlider()
    {
        if (FindName("TimelineSlider") is Slider slider)
        {
            if (_totalSimulationTime > 0)
            {
                slider.Value = (_currentSimulationTime / _totalSimulationTime) * 100.0;
            }
            else
            {
                slider.Value = 0;
            }
        }
    }

    private void UpdateTimeDisplay()
    {
        var current = TimeSpan.FromMinutes(_currentSimulationTime);
        if (FindName("TxtCurrentTime") is TextBlock txt)
        {
            txt.Text = FormatTimeSpan(current);
        }
    }

    private void UpdateTotalTimeDisplay()
    {
        var total = TimeSpan.FromMinutes(_totalSimulationTime);
        if (FindName("TxtTotalTime") is TextBlock txt)
        {
            txt.Text = FormatTimeSpan(total);
        }
    }

    private void UpdateSpeedDisplay()
    {
        if (FindName("TxtSpeed") is TextBlock txt)
        {
            txt.Text = $"{_playbackSpeed:0.##}x";
        }
    }

    private static string FormatTimeSpan(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}:{time.Minutes:D2}:{time.Seconds:D2}";
        }
        return $"{time.Minutes}:{time.Seconds:D2}";
    }

    #endregion

    /// <summary>
    /// Clears all toolpath geometry from the viewport.
    /// </summary>
    public void Clear()
    {
        ToolpathContainer.Children.Clear();

        // Hide line number display when cleared
        if (FindName("LineNumberDisplay") is Border lineNumberDisplay)
        {
            lineNumberDisplay.Visibility = Visibility.Collapsed;
        }
        
        // Reset first render flag when clearing - next render will be considered "first"
        _isFirstRender = true;
    }

    /// <summary>
    /// Updates the grid size based on toolpath bounds.
    /// </summary>
    private void UpdateGrid(GcodeAnalysisResult result)
    {
        double maxDimension = Math.Max(result.Width, result.Height);
        maxDimension = Math.Max(maxDimension, result.Depth);
        maxDimension = Math.Max(maxDimension, 50); // Minimum grid size

        // Round up to next 10
        double gridSize = Math.Ceiling(maxDimension / 10) * 10 * 1.5;

        GridLines.Width = gridSize;
        GridLines.Length = gridSize;

        // Center the grid
        double centerX = (result.MinX + result.MaxX) / 2;
        double centerY = (result.MinY + result.MaxY) / 2;
        GridLines.Center = new Point3D(centerX, centerY, result.MinZ);
    }

    /// <summary>
    /// Renders a straight line segment between two points.
    /// </summary>
    private void RenderLine(Point3D start, Point3D end, GcodeCommandType commandType)
    {
        // Skip zero-length or nearly zero-length lines
        if ((end - start).Length < MinSegmentLength)
        {
            return;
        }
        
        var color = GetCommandColor(commandType);
        var material = new DiffuseMaterial(new SolidColorBrush(color));

        // Use tubes for cutting moves, thinner lines for rapids
        double radius = commandType == GcodeCommandType.G0 ? TubeRadius * 0.5 : TubeRadius;

        var tube = new TubeVisual3D
        {
            Path = [start, end],
            Diameter = radius * 2,
            ThetaDiv = TubeSegments,
            Material = material
        };

        ToolpathContainer.Children.Add(tube);

        // Add direction arrows along the line
        AddDirectionArrows(start, end, color);
    }

    /// <summary>
    /// Adds direction arrows along a line segment.
    /// </summary>
    private void AddDirectionArrows(Point3D start, Point3D end, Color color)
    {
        var direction = end - start;
        double length = direction.Length;

        if (length < ArrowSpacing / 2)
        {
            return; // Segment too short for arrows
        }

        // Normalize direction
        direction.Normalize();

        // Calculate number of arrows
        int arrowCount = Math.Max(1, (int)(length / ArrowSpacing));
        double step = length / (arrowCount + 1);

        var material = new DiffuseMaterial(new SolidColorBrush(color));

        for (int i = 1; i <= arrowCount; i++)
        {
            double t = step * i;
            var arrowPosition = start + direction * t;

            var arrow = new ArrowVisual3D
            {
                Point1 = arrowPosition - direction * ArrowSize,
                Point2 = arrowPosition + direction * ArrowSize,
                Diameter = ArrowSize * 0.8,
                HeadLength = ArrowSize,
                ThetaDiv = 8,
                Material = material
            };

            ToolpathContainer.Children.Add(arrow);
        }
    }

    /// <summary>
    /// Adds direction arrows along an arc defined by points.
    /// </summary>
    private void AddArcDirectionArrows(Point3DCollection points, Color color)
    {
        if (points.Count < 2)
        {
            return;
        }

        // Calculate total arc length
        double totalLength = 0;
        for (int i = 1; i < points.Count; i++)
        {
            totalLength += (points[i] - points[i - 1]).Length;
        }

        if (totalLength < ArrowSpacing / 2)
        {
            return;
        }

        int arrowCount = Math.Max(1, (int)(totalLength / ArrowSpacing));
        double targetSpacing = totalLength / (arrowCount + 1);

        var material = new DiffuseMaterial(new SolidColorBrush(color));

        double accumulatedLength = 0;
        int nextArrowIndex = 1;
        double nextArrowDistance = targetSpacing;

        for (int i = 1; i < points.Count && nextArrowIndex <= arrowCount; i++)
        {
            var segmentDir = points[i] - points[i - 1];
            double segmentLength = segmentDir.Length;

            while (accumulatedLength + segmentLength >= nextArrowDistance && nextArrowIndex <= arrowCount)
            {
                double t = (nextArrowDistance - accumulatedLength) / segmentLength;
                var arrowPosition = points[i - 1] + segmentDir * t;

                // Direction along the arc at this point
                var direction = segmentDir;
                direction.Normalize();

                var arrow = new ArrowVisual3D
                {
                    Point1 = arrowPosition - direction * ArrowSize,
                    Point2 = arrowPosition + direction * ArrowSize,
                    Diameter = ArrowSize * 0.8,
                    HeadLength = ArrowSize,
                    ThetaDiv = 8,
                    Material = material
                };

                ToolpathContainer.Children.Add(arrow);

                nextArrowIndex++;
                nextArrowDistance = targetSpacing * nextArrowIndex;
            }

            accumulatedLength += segmentLength;
        }
    }

    /// <summary>
    /// Renders an arc segment (G2/G3).
    /// </summary>
    private void RenderArc(GcodeCommand command, Point3D start, Point3D end)
    {
        var color = GetCommandColor(command.Type);
        var material = new DiffuseMaterial(new SolidColorBrush(color));

        // If no center offset, draw as line
        if (!command.I.HasValue && !command.J.HasValue)
        {
            RenderLine(start, end, command.Type);
            return;
        }

        // Calculate center point
        double centerX = start.X + (command.I ?? 0);
        double centerY = start.Y + (command.J ?? 0);
        double centerZ = start.Z + (command.K ?? 0);

        // Calculate radius
        double radius = Math.Sqrt(Math.Pow(command.I ?? 0, 2) + Math.Pow(command.J ?? 0, 2));

        // Skip degenerate arcs with zero or near-zero radius
        if (radius < MinSegmentLength)
        {
            return;
        }

        // Calculate start and end angles
        double startAngle = Math.Atan2(start.Y - centerY, start.X - centerX);
        double endAngle = Math.Atan2(end.Y - centerY, end.X - centerX);

        // Determine sweep direction
        bool clockwise = command.Type == GcodeCommandType.G2;

        // Calculate angle difference
        double angleDiff = endAngle - startAngle;
        if (clockwise && angleDiff > 0) angleDiff -= 2 * Math.PI;
        if (!clockwise && angleDiff < 0) angleDiff += 2 * Math.PI;

        double angleStep = angleDiff / ArcSegments;

        // Z interpolation
        double zStep = (end.Z - start.Z) / ArcSegments;

        // Generate arc points
        var points = new TubeVisual3D
        {
            Path = new Point3DCollection(),
            Diameter = TubeRadius * 2,
            ThetaDiv = TubeSegments,
            Material = material
        };

        for (int i = 0; i <= ArcSegments; i++)
        {
            double angle = startAngle + angleStep * i;
            double x = centerX + radius * Math.Cos(angle);
            double y = centerY + radius * Math.Sin(angle);
            double z = start.Z + zStep * i;

            points.Path.Add(new Point3D(x, y, z));
        }

        ToolpathContainer.Children.Add(points);

        // Add direction arrows along the arc
        AddArcDirectionArrows(points.Path, color);
    }

    /// <summary>
    /// Adds a spherical marker at the specified position.
    /// </summary>
    private void AddMarker(Point3D position, Color color, double radius)
    {
        var material = new DiffuseMaterial(new SolidColorBrush(color));

        var sphere = new SphereVisual3D
        {
            Center = position,
            Radius = radius,
            Material = material
        };

        ToolpathContainer.Children.Add(sphere);
    }

    /// <summary>
    /// Gets the color for a command type.
    /// </summary>
    private static Color GetCommandColor(GcodeCommandType type) => type switch
    {
        GcodeCommandType.G0 => Colors.Orange,
        GcodeCommandType.G1 => Colors.DodgerBlue,
        GcodeCommandType.G2 => Colors.LimeGreen,
        GcodeCommandType.G3 => Colors.MediumPurple,
        _ => Colors.Gray
    };

    /// <summary>
    /// Adds 3D dimension measurement brackets showing X, Y, and Z extents.
    /// </summary>
    private void AddDimensionMeasurements(GcodeAnalysisResult result)
    {
        const double mmToInches = 0.0393701;
        const double dimLineOffset = 15;
        const double tickSize = 2;
        const double lineThickness = 0.3;
        const double minDimension = 0.001; // Minimum dimension to display
        
        var dimColor = Color.FromRgb(100, 200, 255);
        var dimMaterial = new DiffuseMaterial(new SolidColorBrush(dimColor));
        
        double minX = result.MinX, maxX = result.MaxX;
        double minY = result.MinY, maxY = result.MaxY;
        double minZ = result.MinZ, maxZ = result.MaxZ;
        
        // Only add X dimension if width is non-zero
        if (result.Width > minDimension)
        {
            // X dimension (Width) - bottom front
            double xDimY = minY - dimLineOffset;
            AddDimensionLine(new Point3D(minX, xDimY, minZ), new Point3D(maxX, xDimY, minZ), dimMaterial, lineThickness);
            AddDimensionLine(new Point3D(minX, xDimY - tickSize, minZ), new Point3D(minX, xDimY + tickSize, minZ), dimMaterial, lineThickness);
            AddDimensionLine(new Point3D(maxX, xDimY - tickSize, minZ), new Point3D(maxX, xDimY + tickSize, minZ), dimMaterial, lineThickness);
            AddDashedLine(new Point3D(minX, minY, minZ), new Point3D(minX, xDimY + tickSize, minZ), dimMaterial, lineThickness);
            AddDashedLine(new Point3D(maxX, minY, minZ), new Point3D(maxX, xDimY + tickSize, minZ), dimMaterial, lineThickness);
            AddDimensionText(new Point3D((minX + maxX) / 2, xDimY - 5, minZ), 
                $"X: {result.Width:F2} mm ({result.Width * mmToInches:F3}\")", dimColor);
        }
        
        // Only add Y dimension if height is non-zero
        if (result.Height > minDimension)
        {
            // Y dimension (Height) - right front
            double yDimX = maxX + dimLineOffset;
            AddDimensionLine(new Point3D(yDimX, minY, minZ), new Point3D(yDimX, maxY, minZ), dimMaterial, lineThickness);
            AddDimensionLine(new Point3D(yDimX - tickSize, minY, minZ), new Point3D(yDimX + tickSize, minY, minZ), dimMaterial, lineThickness);
            AddDimensionLine(new Point3D(yDimX - tickSize, maxY, minZ), new Point3D(yDimX + tickSize, maxY, minZ), dimMaterial, lineThickness);
            AddDashedLine(new Point3D(maxX, minY, minZ), new Point3D(yDimX + tickSize, minY, minZ), dimMaterial, lineThickness);
            AddDashedLine(new Point3D(maxX, maxY, minZ), new Point3D(yDimX + tickSize, maxY, minZ), dimMaterial, lineThickness);
            AddDimensionText(new Point3D(yDimX + 5, (minY + maxY) / 2, minZ), 
                $"Y: {result.Height:F2} mm ({result.Height * mmToInches:F3}\")", dimColor);
        }
        
        // Only add Z dimension if depth is non-zero
        if (result.Depth > minDimension)
        {
            // Z dimension (Depth) - right back
            double zDimX = maxX + dimLineOffset;
            AddDimensionLine(new Point3D(zDimX, maxY, minZ), new Point3D(zDimX, maxY, maxZ), dimMaterial, lineThickness);
            AddDimensionLine(new Point3D(zDimX - tickSize, maxY, minZ), new Point3D(zDimX + tickSize, maxY, minZ), dimMaterial, lineThickness);
            AddDimensionLine(new Point3D(zDimX - tickSize, maxY, maxZ), new Point3D(zDimX + tickSize, maxY, maxZ), dimMaterial, lineThickness);
            AddDashedLine(new Point3D(maxX, maxY, minZ), new Point3D(zDimX + tickSize, maxY, minZ), dimMaterial, lineThickness);
            AddDashedLine(new Point3D(maxX, maxY, maxZ), new Point3D(zDimX + tickSize, maxY, maxZ), dimMaterial, lineThickness);
            AddDimensionText(new Point3D(zDimX + 5, maxY, (minZ + maxZ) / 2), 
                $"Z: {result.Depth:F2} mm ({result.Depth * mmToInches:F3}\")", dimColor);
        }
    }

    private void AddDimensionLine(Point3D start, Point3D end, Material material, double thickness)
    {
        // Check if the line has a valid length to avoid HelixToolkit errors
        if ((end - start).Length < MinSegmentLength)
        {
            return;
        }
        
        var tube = new TubeVisual3D
        {
            Path = [start, end],
            Diameter = thickness,
            ThetaDiv = 8,
            Material = material
        };
        ToolpathContainer.Children.Add(tube);
    }

    private void AddDashedLine(Point3D start, Point3D end, Material material, double thickness)
    {
        var direction = end - start;
        double length = direction.Length;
        
        // Check if the line has a valid length
        if (length < MinSegmentLength)
        {
            return;
        }
        
        direction.Normalize();
        
        const double dashLength = 2.0;
        const double gapLength = 1.5;
        int segmentCount = (int)(length / (dashLength + gapLength));
        
        for (int i = 0; i < segmentCount; i++)
        {
            double startDist = i * (dashLength + gapLength);
            double endDist = Math.Min(startDist + dashLength, length);
            AddDimensionLine(start + direction * startDist, start + direction * endDist, material, thickness * 0.7);
        }
    }

    private void AddDimensionText(Point3D position, string text, Color color)
    {
        var billboard = new BillboardTextVisual3D
        {
            Text = text,
            Position = position,
            Foreground = new SolidColorBrush(color),
            Background = new SolidColorBrush(Color.FromArgb(200, 37, 37, 38)),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(4, 2, 4, 2)
        };
        ToolpathContainer.Children.Add(billboard);
    }

    /// <summary>
    /// Handles changes to the light angle slider.
    /// </summary>
    private void LightAngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        // Find the main directional light in the viewport
        var mainLight = FindName("MainDirectionalLight") as DirectionalLight;
        if (mainLight is null)
        {
            return;
        }

        // Convert angle to radians
        double angleRadians = e.NewValue * Math.PI / 180.0;
        
        // Calculate light direction based on angle
        // The light rotates around the Z axis (horizontal rotation)
        // Default direction points downward and slightly forward
        double x = Math.Cos(angleRadians);
        double y = Math.Sin(angleRadians);
        double z = -1.0; // Always pointing downward
        
        // Normalize the direction vector
        var direction = new Vector3D(x, y, z);
        direction.Normalize();
        
        mainLight.Direction = direction;
    }

    private void BtnResetView_Click(object sender, RoutedEventArgs e)
    {
        Viewport3D.ZoomExtents();
    }

    private void BtnTopView_Click(object sender, RoutedEventArgs e)
    {
        Viewport3D.Camera.Position = new Point3D(0, 0, 500);
        Viewport3D.Camera.LookDirection = new Vector3D(0, 0, -1);
        Viewport3D.Camera.UpDirection = new Vector3D(0, 1, 0);
        Viewport3D.ZoomExtents();
    }

    private void BtnFrontView_Click(object sender, RoutedEventArgs e)
    {
        Viewport3D.Camera.Position = new Point3D(0, -500, 0);
        Viewport3D.Camera.LookDirection = new Vector3D(0, 1, 0);
        Viewport3D.Camera.UpDirection = new Vector3D(0, 0, 1);
        Viewport3D.ZoomExtents();
    }

    private void BtnSideView_Click(object sender, RoutedEventArgs e)
    {
        Viewport3D.Camera.Position = new Point3D(500, 0, 0);
        Viewport3D.Camera.LookDirection = new Vector3D(-1, 0, 0);
        Viewport3D.Camera.UpDirection = new Vector3D(0, 0, 1);
        Viewport3D.ZoomExtents();
    }
}

/// <summary>
/// Represents a segment of the simulation timeline.
/// </summary>
internal class SimulationSegment
{
    public double StartX { get; set; }
    public double StartY { get; set; }
    public double StartZ { get; set; }
    public double EndX { get; set; }
    public double EndY { get; set; }
    public double EndZ { get; set; }
    public double StartTime { get; set; }
    public double EndTime { get; set; }
    public double Duration { get; set; }
    public bool IsArc { get; set; }
    public List<Point3D>? ArcPoints { get; set; }
    public GcodeCommand Command { get; set; } = null!;
}
