using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace GcodeAnalyzer;

/// <summary>
/// Parses G-code files and extracts commands, coordinates, and detects errors.
/// </summary>
public partial class GcodeParser
{
    [GeneratedRegex(@"[XYZIJKFSE](-?\d+\.?\d*)", RegexOptions.IgnoreCase)]
    private static partial Regex ParameterRegex();

    [GeneratedRegex(@"^[GMT]\d+", RegexOptions.IgnoreCase)]
    private static partial Regex CommandRegex();

    /// <summary>
    /// Parses a G-code file and returns analysis results.
    /// </summary>
    public GcodeAnalysisResult ParseFile(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);

        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException("G-code file not found.", filePath);
        }

        var lines = File.ReadAllLines(filePath);
        return ParseLines(lines);
    }

    /// <summary>
    /// Parses G-code lines and returns analysis results.
    /// </summary>
    public GcodeAnalysisResult ParseLines(string[] lines)
    {
        var result = new GcodeAnalysisResult();
        double currentX = 0, currentY = 0, currentZ = 0;
        double prevX = 0, prevY = 0, prevZ = 0;
        bool hasMovement = false;

        // Track path segments for duplicate detection (segment -> original line number)
        var pathSegments = new Dictionary<PathSegment, int>();

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            int lineNumber = i + 1;

            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(';') || line.StartsWith('('))
            {
                continue;
            }

            // Remove inline comments
            var commentIndex = line.IndexOf(';');
            if (commentIndex > 0)
            {
                line = line[..commentIndex].Trim();
            }

            commentIndex = line.IndexOf('(');
            if (commentIndex > 0)
            {
                line = line[..commentIndex].Trim();
            }

            var command = ParseCommand(line, lineNumber, result.Errors);
            if (command is null)
            {
                continue;
            }

            // Update current position based on command
            if (command.Type is GcodeCommandType.G0 or GcodeCommandType.G1 or GcodeCommandType.G2 or GcodeCommandType.G3)
            {
                // Store previous position before updating
                prevX = currentX;
                prevY = currentY;
                prevZ = currentZ;

                if (command.X.HasValue) currentX = command.X.Value;
                if (command.Y.HasValue) currentY = command.Y.Value;
                if (command.Z.HasValue) currentZ = command.Z.Value;

                command.EndX = currentX;
                command.EndY = currentY;
                command.EndZ = currentZ;

                // Check for duplicate toolpaths
                if (hasMovement)
                {
                    // Check for move to same position (zero-length move)
                    if (ArePositionsEqual(prevX, prevY, prevZ, currentX, currentY, currentZ))
                    {
                        result.Errors.Add(new GcodeError(lineNumber, 
                            $"Redundant move: tool is already at position X{currentX:F3} Y{currentY:F3} Z{currentZ:F3}"));
                    }
                    else
                    {
                        // Check for duplicate path segment (same start and end points)
                        var segment = new PathSegment(prevX, prevY, prevZ, currentX, currentY, currentZ, command.Type, lineNumber);
                        
                        if (pathSegments.TryGetValue(segment, out int originalLineNumber))
                        {
                            result.Errors.Add(new GcodeError(lineNumber, 
                                $"Duplicate toolpath: segment from ({prevX:F3}, {prevY:F3}, {prevZ:F3}) to ({currentX:F3}, {currentY:F3}, {currentZ:F3}) duplicates line {originalLineNumber}",
                                originalLineNumber));
                        }
                        else
                        {
                            pathSegments[segment] = lineNumber;
                        }
                    }
                }

                hasMovement = true;
                result.Commands.Add(command);

                // Update min/max values
                result.UpdateBounds(currentX, currentY, currentZ);
            }
        }

        if (!hasMovement)
        {
            result.Errors.Add(new GcodeError(0, "No movement commands found in file."));
        }

        result.TotalLines = lines.Length;
        return result;
    }

    /// <summary>
    /// Compares two positions for equality within a small tolerance.
    /// </summary>
    private static bool ArePositionsEqual(double x1, double y1, double z1, double x2, double y2, double z2)
    {
        const double tolerance = 0.0001;
        return Math.Abs(x1 - x2) < tolerance &&
               Math.Abs(y1 - y2) < tolerance &&
               Math.Abs(z1 - z2) < tolerance;
    }

    private static GcodeCommand? ParseCommand(string line, int lineNumber, List<GcodeError> errors)
    {
        var upperLine = line.ToUpperInvariant();
        var commandMatch = CommandRegex().Match(upperLine);

        if (!commandMatch.Success)
        {
            // Line doesn't start with a recognized command - could be continuation or error
            if (ParameterRegex().IsMatch(upperLine))
            {
                // Has parameters but no command - might be a modal continuation
                return null;
            }
            return null;
        }

        var commandStr = commandMatch.Value;
        var commandType = ParseCommandType(commandStr);

        if (commandType == GcodeCommandType.Unknown)
        {
            return null;
        }

        var command = new GcodeCommand
        {
            Type = commandType,
            LineNumber = lineNumber,
            RawLine = line
        };

        // Extract parameters
        var parameters = ParameterRegex().Matches(upperLine);
        foreach (Match param in parameters)
        {
            var paramName = param.Value[0];
            if (double.TryParse(param.Value[1..], NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
            {
                switch (paramName)
                {
                    case 'X': command.X = value; break;
                    case 'Y': command.Y = value; break;
                    case 'Z': command.Z = value; break;
                    case 'I': command.I = value; break;
                    case 'J': command.J = value; break;
                    case 'K': command.K = value; break;
                    case 'F': command.FeedRate = value; break;
                    case 'S': command.SpindleSpeed = value; break;
                    case 'E': command.Extrusion = value; break;
                }
            }
            else
            {
                errors.Add(new GcodeError(lineNumber, $"Invalid parameter value: {param.Value}"));
            }
        }

        // Validate arc commands have center offsets
        if (commandType is GcodeCommandType.G2 or GcodeCommandType.G3)
        {
            if (!command.I.HasValue && !command.J.HasValue && !command.K.HasValue)
            {
                errors.Add(new GcodeError(lineNumber, $"Arc command {commandStr} missing I, J, or K offset parameters."));
            }
        }

        // Check for missing feed rate on cutting moves (G1, G2, G3)
        if (commandType is GcodeCommandType.G1 or GcodeCommandType.G2 or GcodeCommandType.G3)
        {
            if (!command.FeedRate.HasValue)
            {
                // This is just a warning - feed rate might be modal from previous command
            }
        }

        return command;
    }

    private static GcodeCommandType ParseCommandType(string command) => command.ToUpperInvariant() switch
    {
        "G0" or "G00" => GcodeCommandType.G0,
        "G1" or "G01" => GcodeCommandType.G1,
        "G2" or "G02" => GcodeCommandType.G2,
        "G3" or "G03" => GcodeCommandType.G3,
        _ => GcodeCommandType.Unknown
    };
}

/// <summary>
/// Represents a parsed G-code command.
/// </summary>
public class GcodeCommand
{
    public GcodeCommandType Type { get; set; }
    public int LineNumber { get; set; }
    public string RawLine { get; set; } = string.Empty;

    public double? X { get; set; }
    public double? Y { get; set; }
    public double? Z { get; set; }
    public double? I { get; set; }
    public double? J { get; set; }
    public double? K { get; set; }
    public double? FeedRate { get; set; }
    public double? SpindleSpeed { get; set; }
    public double? Extrusion { get; set; }

    // Computed end position after this command
    public double EndX { get; set; }
    public double EndY { get; set; }
    public double EndZ { get; set; }

    /// <summary>
    /// Returns true if this is a rapid move (G0).
    /// </summary>
    public bool IsRapid => Type == GcodeCommandType.G0;

    /// <summary>
    /// Returns true if this is a cutting/feed move (G1, G2, G3).
    /// </summary>
    public bool IsCuttingMove => Type is GcodeCommandType.G1 or GcodeCommandType.G2 or GcodeCommandType.G3;
}

/// <summary>
/// Types of G-code commands supported.
/// </summary>
public enum GcodeCommandType
{
    Unknown,
    G0,  // Rapid move
    G1,  // Linear interpolation
    G2,  // Clockwise arc
    G3   // Counter-clockwise arc
}

/// <summary>
/// Represents an error found during G-code parsing.
/// </summary>
public record GcodeError(int LineNumber, string Message, int? RelatedLineNumber = null);

/// <summary>
/// Represents a path segment for duplicate detection.
/// </summary>
public readonly struct PathSegment : IEquatable<PathSegment>
{
    private const double Tolerance = 0.0001;

    public double StartX { get; }
    public double StartY { get; }
    public double StartZ { get; }
    public double EndX { get; }
    public double EndY { get; }
    public double EndZ { get; }
    public GcodeCommandType CommandType { get; }
    public int LineNumber { get; }

    public PathSegment(double startX, double startY, double startZ, 
                       double endX, double endY, double endZ, 
                       GcodeCommandType commandType, int lineNumber)
    {
        StartX = startX;
        StartY = startY;
        StartZ = startZ;
        EndX = endX;
        EndY = endY;
        EndZ = endZ;
        CommandType = commandType;
        LineNumber = lineNumber;
    }

    public bool Equals(PathSegment other)
    {
        // Two segments are equal if they have the same start/end points (within tolerance)
        // regardless of direction for cutting moves
        bool sameDirection = AreValuesEqual(StartX, other.StartX) &&
                             AreValuesEqual(StartY, other.StartY) &&
                             AreValuesEqual(StartZ, other.StartZ) &&
                             AreValuesEqual(EndX, other.EndX) &&
                             AreValuesEqual(EndY, other.EndY) &&
                             AreValuesEqual(EndZ, other.EndZ);

        bool reverseDirection = AreValuesEqual(StartX, other.EndX) &&
                                AreValuesEqual(StartY, other.EndY) &&
                                AreValuesEqual(StartZ, other.EndZ) &&
                                AreValuesEqual(EndX, other.StartX) &&
                                AreValuesEqual(EndY, other.StartY) &&
                                AreValuesEqual(EndZ, other.StartZ);

        return sameDirection || reverseDirection;
    }

    public override bool Equals(object? obj) => obj is PathSegment other && Equals(other);

    public override int GetHashCode()
    {
        // Use a symmetric hash so that reversed segments have the same hash
        int startHash = HashCode.Combine(
            Math.Round(StartX / Tolerance),
            Math.Round(StartY / Tolerance),
            Math.Round(StartZ / Tolerance));
        int endHash = HashCode.Combine(
            Math.Round(EndX / Tolerance),
            Math.Round(EndY / Tolerance),
            Math.Round(EndZ / Tolerance));

        // XOR is symmetric, so (A,B) and (B,A) produce the same hash
        return startHash ^ endHash;
    }

    private static bool AreValuesEqual(double a, double b) => Math.Abs(a - b) < Tolerance;
}
