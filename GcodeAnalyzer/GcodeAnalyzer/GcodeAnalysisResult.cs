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
    /// Returns a summary of the analysis results.
    /// </summary>
    public string GetSummary()
    {
        if (TotalCommands == 0)
        {
            return "No movement commands found.";
        }

        var estimatedTime = GetEstimatedRunTime();

        return $"""
            Total Lines: {TotalLines}
            Movement Commands: {TotalCommands}
            
            X Range: {MinX:F3} to {MaxX:F3} (Width: {Width:F3})
            Y Range: {MinY:F3} to {MaxY:F3} (Height: {Height:F3})
            Z Range: {MinZ:F3} to {MaxZ:F3} (Depth: {Depth:F3})
            
            Estimated Run Time: {FormatTime(estimatedTime)}
            
            Errors Found: {Errors.Count}
            """;
    }
}
