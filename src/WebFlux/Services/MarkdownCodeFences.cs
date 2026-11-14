namespace WebFlux.Services;

/// <summary>
/// Finds the lines of a Markdown text that belong to a fenced code block. A line inside a code block is code:
/// a <c># comment</c> there is not a heading and a short line there is not a seam between sections.
/// </summary>
/// <remarks>
/// CommonMark rules: a fence is three or more backticks or tildes indented by fewer than four spaces; a backtick
/// fence's info string holds no backtick; it closes at a fence line of the same character, at least as long, with
/// nothing after it but white space; a fence that never closes runs to the end of the text.
/// </remarks>
internal static class MarkdownCodeFences
{
    /// <summary>
    /// For each line, whether it belongs to a fenced block - the opening line, the lines in it and the closing line.
    /// </summary>
    public static bool[] FencedLines(IReadOnlyList<string> lines)
    {
        var fenced = new bool[lines.Count];
        var fenceChar = '\0';
        var fenceLength = 0;

        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var content = line.TrimStart(' ');
            var indent = line.Length - content.Length;

            if (fenceChar == '\0')
            {
                if (indent < 4 && TryReadOpening(content, out fenceChar, out fenceLength))
                {
                    fenced[i] = true;
                }
            }
            else
            {
                fenced[i] = true;
                if (indent < 4 && IsClosing(content, fenceChar, fenceLength))
                {
                    fenceChar = '\0';
                    fenceLength = 0;
                }
            }
        }

        return fenced;
    }

    private static bool TryReadOpening(string content, out char fenceChar, out int fenceLength)
    {
        fenceChar = '\0';
        fenceLength = 0;

        if (content.Length < 3 || (content[0] != '`' && content[0] != '~'))
        {
            return false;
        }

        var run = RunLength(content, content[0]);
        if (run < 3)
        {
            return false;
        }

        // A backtick fence's info string may not hold a backtick: ```code``` is inline code, not a fence.
        if (content[0] == '`' && content.IndexOf('`', run) >= 0)
        {
            return false;
        }

        fenceChar = content[0];
        fenceLength = run;
        return true;
    }

    private static bool IsClosing(string content, char fenceChar, int fenceLength)
    {
        if (content.Length == 0 || content[0] != fenceChar)
        {
            return false;
        }

        var run = RunLength(content, fenceChar);
        return run >= fenceLength && string.IsNullOrWhiteSpace(content[run..]);
    }

    private static int RunLength(string text, char c)
    {
        var n = 0;
        while (n < text.Length && text[n] == c)
        {
            n++;
        }

        return n;
    }
}
