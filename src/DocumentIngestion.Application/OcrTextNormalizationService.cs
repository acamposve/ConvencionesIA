using System.Text;
using DocumentIngestion.Domain;

namespace DocumentIngestion.Application;

public sealed class OcrTextNormalizationService : ITextNormalizationService
{
    public TextNormalizationResult Normalize(string content, Document document)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentNullException.ThrowIfNull(document);

        if (string.IsNullOrEmpty(content))
        {
            throw new InvalidOperationException("Cannot normalize empty content.");
        }

        var normalized = NormalizeContent(content);
        return new TextNormalizationResult(normalized, "OcrCleanup");
    }

    private static string NormalizeContent(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(content.Length);
        var previousWasWhitespace = false;
        var pendingLineBreaks = 0;

        for (var index = 0; index < content.Length; index++)
        {
            var current = content[index];

            if (IsLineBreak(current))
            {
                pendingLineBreaks++;
                continue;
            }

            if (pendingLineBreaks > 0)
            {
                if (pendingLineBreaks > 1)
                {
                    AppendLineBreak(builder);
                }
                else if (builder.Length > 0 && !char.IsWhiteSpace(builder[^1]))
                {
                    if (char.IsPunctuation(builder[^1]) || builder[^1] == '"' || builder[^1] == '\'')
                    {
                        AppendLineBreak(builder);
                    }
                    else
                    {
                        builder.Append(' ');
                        previousWasWhitespace = true;
                    }
                }

                pendingLineBreaks = 0;
            }

            if (IsWhitespace(current))
            {
                if (!previousWasWhitespace)
                {
                    builder.Append(' ');
                    previousWasWhitespace = true;
                }

                continue;
            }

            if (IsNonPrintable(current))
            {
                continue;
            }

            builder.Append(NormalizeCharacter(current));
            previousWasWhitespace = false;
        }

        if (pendingLineBreaks > 1)
        {
            AppendLineBreak(builder);
        }

        return builder.ToString().Trim();
    }

    private static void AppendLineBreak(StringBuilder builder)
    {
        if (builder.Length == 0 || builder[^1] == '\n')
        {
            return;
        }

        builder.Append('\n');
    }

    private static bool IsLineBreak(char value) => value is '\r' or '\n';

    private static bool IsWhitespace(char value) => char.IsWhiteSpace(value) && value is not '\r' and not '\n';

    private static bool IsNonPrintable(char value) => char.IsControl(value) || value == '\u0000';

    private static char NormalizeCharacter(char value)
    {
        return value switch
        {
            '“' or '”' => '"',
            '‘' or '’' => '\'',
            '−' or '–' or '—' => '-',
            '　' => ' ',
            _ => value
        };
    }
}
