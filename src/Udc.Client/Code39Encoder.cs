using System.Linq;

namespace Udc.Client;

/// <summary>
/// Minimal Code 39 ("Code 3 of 9") barcode encoder. Produces a module string (<c>'1'</c> = ink/bar,
/// <c>'0'</c> = background/space, one character per fixed-width module) a renderer can draw
/// directly as a row of equal-width rectangles - no run-length/wide-vs-narrow logic to get wrong on
/// the rendering side, since every module is already the same width here.
///
/// The per-character patterns below are copied from the widely-used, independently-maintained
/// python-barcode library's <c>barcode.charsets.code39</c> table (not reconstructed from memory),
/// since this app has no printer or barcode scanner to physically verify a tag against - the only
/// thing that can be checked here is that this table matches a trusted external reference exactly.
/// No checksum character is appended, matching how barcode scanners are set up by default.
/// </summary>
public static class Code39Encoder
{
    private const string Middle = "0";
    private const string Edge = "100010111011101";

    private static readonly Dictionary<char, string> Patterns = new()
    {
        ['0'] = "101000111011101", ['1'] = "111010001010111", ['2'] = "101110001010111",
        ['3'] = "111011100010101", ['4'] = "101000111010111", ['5'] = "111010001110101",
        ['6'] = "101110001110101", ['7'] = "101000101110111", ['8'] = "111010001011101",
        ['9'] = "101110001011101", ['A'] = "111010100010111", ['B'] = "101110100010111",
        ['C'] = "111011101000101", ['D'] = "101011100010111", ['E'] = "111010111000101",
        ['F'] = "101110111000101", ['G'] = "101010001110111", ['H'] = "111010100011101",
        ['I'] = "101110100011101", ['J'] = "101011100011101", ['K'] = "111010101000111",
        ['L'] = "101110101000111", ['M'] = "111011101010001", ['N'] = "101011101000111",
        ['O'] = "111010111010001", ['P'] = "101110111010001", ['Q'] = "101010111000111",
        ['R'] = "111010101110001", ['S'] = "101110101110001", ['T'] = "101011101110001",
        ['U'] = "111000101010111", ['V'] = "100011101010111", ['W'] = "111000111010101",
        ['X'] = "100010111010111", ['Y'] = "111000101110101", ['Z'] = "100011101110101",
        ['-'] = "100010101110111", ['.'] = "111000101011101", [' '] = "100011101011101",
        ['$'] = "100010001000101", ['/'] = "100010001010001", ['+'] = "100010100010001",
        ['%'] = "101000100010001",
    };

    /// <summary>True if every character of <paramref name="value"/> (case-insensitive) is in Code
    /// 39's supported charset - digits, A-Z, space, and <c>-.$/+%</c>.</summary>
    public static bool CanEncode(string value) =>
        value.Length > 0 && value.ToUpperInvariant().All(c => Patterns.ContainsKey(c));

    /// <summary>Returns the full module string: the start/stop pattern, a narrow inter-character
    /// gap, one pattern per character of <paramref name="value"/>, another gap, then the stop
    /// pattern again. Throws <see cref="ArgumentException"/> for a character Code 39 can't encode -
    /// check <see cref="CanEncode"/> first.</summary>
    public static string Encode(string value)
    {
        if (!CanEncode(value)) throw new ArgumentException($"'{value}' contains a character Code 39 cannot encode.", nameof(value));
        var parts = new List<string> { Edge };
        parts.AddRange(value.ToUpperInvariant().Select(c => Patterns[c]));
        parts.Add(Edge);
        return string.Join(Middle, parts);
    }
}
