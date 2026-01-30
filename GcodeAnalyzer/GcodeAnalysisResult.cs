namespace GcodeAnalyzer;

/// <summary>
/// Contains the results of G-code analysis including bounds, errors, and parsed commands.
/// </summary>
public class GcodeAnalysisResult
{
    /// <summary>
    /// Default rapid feed rate in mm/min when not specified (typical CNC machine rapids).
    /// </summary>
    private const double DefaultRapidFeedRate = 5000;

    /// <summary>
    /// Default feed rate in mm/min when not specified.
    /// </summary>
    private const double DefaultFeedRate = 1000;

    public double MinX { get; private set; } = double.MaxValue;
    public double MaxX { get; private set; } = double.MinValue;
    public double MinY { get; private set; } = double.MaxValue;
    public double MaxY { get; private set; } = double.MinValue;
    public double MinZ { get; private set; } = double.MaxValue;
    public double MaxZ { get; private set; } = double.MinValue;

    public int TotalLines { get; set; }
    public int TotalCommands => Commands.Count;

    public List<GcodeCommand> Commands { get; } = [];
    public List<GcodeError> Errors { get; } = [];

    public bool HasErrors => Errors.Count > 0;

    /// <summary>
    /// Updates the bounding box with new coordinates.
    /// </summary>
    public void UpdateBounds(double x, double y, double z)
    {
        MinX = Math.Min(MinX, x);
        MaxX = Math.Max(MaxX, x);
        MinY = Math.Min(MinY, y);
        MaxY = Math.Max(MaxY, y);
        MinZ = Math.Min(MinZ, z);
        MaxZ = Math.Max(MaxZ, z);
    }

    /// <summary>
    /// Gets the width of the toolpath in the X direction.
    /// </summary>
    public double Width => MaxX - MinX;

    /// <summary>
    /// Gets the height of the toolpath in the Y direction.
    /// </summary>
    public double Height => MaxY - MinY;

    /// <summary>
    /// Gets the depth of the toolpath in the Z direction.
    /// </summary>
    public double Depth => MaxZ - MinZ;

    /// <summary>
    /// Calculates the estimated run time for all commands.
    /// </summary>
    /// <returns>Estimated run time as a TimeSpan.</returns>
    public TimeSpan GetEstimatedRunTime()
    {
        if (Commands.Count == 0)
        {
            return TimeSpan.Zero;
        }

        double totalMinutes = 0;
        double prevX = 0, prevY = 0, prevZ = 0;
        double currentFeedRate = DefaultFeedRate;
        bool isFirst = true;

        foreach (var command in Commands)
        {
            // Update feed rate if specified
            if (command.FeedRate.HasValue && command.FeedRate.Value > 0)
            {
                currentFeedRate = command.FeedRate.Value;
            }

            if (!isFirst)
            {
                double distance = CalculateDistance(command, prevX, prevY, prevZ);

                // Use rapid feed rate for G0, otherwise use current feed rate
                double effectiveFeedRate = command.Type == GcodeCommandType.G0
                    ? DefaultRapidFeedRate
                    : currentFeedRate;

                if (effectiveFeedRate > 0 && distance > 0)
                {
                    totalMinutes += distance / effectiveFeedRate;
                }
            }

            prevX = command.EndX;
            prevY = command.EndY;
            prevZ = command.EndZ;
            isFirst = false;
        }

        return TimeSpan.FromMinutes(totalMinutes);
    }

    /// <summary>
    /// Calculates the distance traveled for a command.
    /// </summary>
    private static double CalculateDistance(GcodeCommand command, double startX, double startY, double startZ)
    {
        double endX = command.EndX;
        double endY = command.EndY;
        double endZ = command.EndZ;

        if (command.Type is GcodeCommandType.G2 or GcodeCommandType.G3)
        {
            // Arc move - calculate arc length
            return CalculateArcLength(command, startX, startY, startZ, endX, endY, endZ);
        }

        // Linear move - calculate straight line distance
        double dx = endX - startX;
        double dy = endY - startY;
        double dz = endZ - startZ;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Calculates the arc length for G2/G3 commands.
    /// </summary>
    private static double CalculateArcLength(GcodeCommand command, double startX, double startY, double startZ,
        double endX, double endY, double endZ)
    {
        // If no center offset, treat as linear
        if (!command.I.HasValue && !command.J.HasValue)
        {
            double deltaX = endX - startX;
            double deltaY = endY - startY;
            double deltaZ = endZ - startZ;
            return Math.Sqrt(deltaX * deltaX + deltaY * deltaY + deltaZ * deltaZ);
        }

        // Calculate center point
        double centerX = startX + (command.I ?? 0);
        double centerY = startY + (command.J ?? 0);

        // Calculate radius
        double radius = Math.Sqrt(Math.Pow(command.I ?? 0, 2) + Math.Pow(command.J ?? 0, 2));

        if (radius < 0.001)
        {
            return 0;
        }

        // Calculate start and end angles
        double startAngle = Math.Atan2(startY - centerY, startX - centerX);
        double endAngle = Math.Atan2(endY - centerY, endX - centerX);

        // Determine sweep direction
        bool clockwise = command.Type == GcodeCommandType.G2;

        // Calculate angle difference
        double angleDiff = endAngle - startAngle;
        if (clockwise && angleDiff > 0) angleDiff -= 2 * Math.PI;
        if (!clockwise && angleDiff < 0) angleDiff += 2 * Math.PI;

        // Arc length in XY plane
        double arcLength2D = Math.Abs(angleDiff) * radius;

        // Account for Z movement (helical interpolation)
        double dz = endZ - startZ;
        return Math.Sqrt(arcLength2D * arcLength2D + dz * dz);
    }

    /// <summary>
    /// Formats a TimeSpan as a human-readable string.
    /// </summary>
    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
        {
            return $"{(int)time.TotalHours}h {time.Minutes}m {time.Seconds}s";
        }
        if (time.TotalMinutes >= 1)
        {
            return $"{time.Minutes}m {time.Seconds}s";
        }
        return $"{time.TotalSeconds:F1}s";
    }

    /// <summary>
    /// Gets the count of rapid moves (G0).
    /// </summary>
    public int RapidMoveCount => Commands.Count(c => c.Type == GcodeCommandType.G0);

    /// <summary>
    /// Gets the count of linear cutting moves (G1).
    /// </summary>
    public int LinearMoveCount => Commands.Count(c => c.Type == GcodeCommandType.G1);

    /// <summary>
    /// Gets the count of clockwise arc moves (G2).
    /// </summary>
    public int ArcCWMoveCount => Commands.Count(c => c.Type == GcodeCommandType.G2);

    /// <summary>
    /// Gets the count of counter-clockwise arc moves (G3).
    /// </summary>
    public int ArcCCWMoveCount => Commands.Count(c => c.Type == GcodeCommandType.G3);

    /// <summary>
    /// Gets the total arc move count (G2 + G3).
    /// </summary>
    public int ArcMoveCount => ArcCWMoveCount + ArcCCWMoveCount;

    /// <summary>
    /// Calculates the total distance of all moves.
    /// </summary>
    public double GetTotalDistance()
    {
        if (Commands.Count == 0) return 0;

        double total = 0;
        double prevX = 0, prevY = 0, prevZ = 0;
        bool isFirst = true;

        foreach (var command in Commands)
        {
            if (!isFirst)
            {
                total += CalculateDistance(command, prevX, prevY, prevZ);
            }
            prevX = command.EndX;
            prevY = command.EndY;
            prevZ = command.EndZ;
            isFirst = false;
        }
        return total;
    }

    /// <summary>
    /// Calculates the total distance of rapid moves (G0).
    /// </summary>
    public double GetRapidDistance()
    {
        if (Commands.Count == 0) return 0;

        double total = 0;
        double prevX = 0, prevY = 0, prevZ = 0;
        bool isFirst = true;

        foreach (var command in Commands)
        {
            if (!isFirst && command.Type == GcodeCommandType.G0)
            {
                total += CalculateDistance(command, prevX, prevY, prevZ);
            }
            prevX = command.EndX;
            prevY = command.EndY;
            prevZ = command.EndZ;
            isFirst = false;
        }
        return total;
    }

    /// <summary>
    /// Calculates the total distance of cutting moves (G1, G2, G3).
    /// </summary>
    public double GetCuttingDistance()
    {
        if (Commands.Count == 0) return 0;

        double total = 0;
        double prevX = 0, prevY = 0, prevZ = 0;
        bool isFirst = true;

        foreach (var command in Commands)
        {
            if (!isFirst && command.Type is GcodeCommandType.G1 or GcodeCommandType.G2 or GcodeCommandType.G3)
            {
                total += CalculateDistance(command, prevX, prevY, prevZ);
            }
            prevX = command.EndX;
            prevY = command.EndY;
            prevZ = command.EndZ;
            isFirst = false;
        }
        return total;
    }

    /// <summary>
    /// Returns a summary of the analysis results.
    /// </summary>
    public string GetSummary()
    {
        if (TotalCommands == 0)
        {
            return "No movement commands found.";
        }

        var estimatedTime = GetEstimatedRunTime();
        var totalDistance = GetTotalDistance();
        var cuttingDistance = GetCuttingDistance();
        var rapidDistance = GetRapidDistance();

        return $"""
            ??? FILE STATISTICS ???
            Total Lines: {TotalLines}
            Movement Commands: {TotalCommands}
            
            ??? MOVE COUNTS ???
            Rapid Moves (G0): {RapidMoveCount}
            Linear Moves (G1): {LinearMoveCount}
            Arc CW Moves (G2): {ArcCWMoveCount}
            Arc CCW Moves (G3): {ArcCCWMoveCount}
            
            ??? DISTANCES ???
            Total Distance: {totalDistance:F2} mm
            Cutting Distance: {cuttingDistance:F2} mm
            Rapid Distance: {rapidDistance:F2} mm
            
            ??? BOUNDING BOX ???
            X: {MinX:F3} to {MaxX:F3} (Width: {Width:F3} mm)
            Y: {MinY:F3} to {MaxY:F3} (Height: {Height:F3} mm)
            Z: {MinZ:F3} to {MaxZ:F3} (Depth: {Depth:F3} mm)
            
            ??? TIME ESTIMATE ???
            Estimated Run Time: {FormatTime(estimatedTime)}
            
            ??? ERRORS ???
            Errors Found: {Errors.Count}
            """;
    }
}
