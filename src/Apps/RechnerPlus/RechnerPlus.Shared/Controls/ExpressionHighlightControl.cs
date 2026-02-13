using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace RechnerPlus.Controls;

/// <summary>
/// TextBlock mit Syntax-Highlighting für Rechner-Ausdrücke.
/// Zahlen: TextPrimaryBrush, Operatoren: PrimaryBrush, Klammern: TextMutedBrush (halbtransparent).
/// </summary>
public class ExpressionHighlightControl : TextBlock
{
    public static readonly StyledProperty<string> ExpressionProperty =
        AvaloniaProperty.Register<ExpressionHighlightControl, string>(nameof(Expression), "");

    public string Expression
    {
        get => GetValue(ExpressionProperty);
        set => SetValue(ExpressionProperty, value);
    }

    static ExpressionHighlightControl()
    {
        ExpressionProperty.Changed.AddClassHandler<ExpressionHighlightControl>(
            (ctrl, _) => ctrl.RebuildInlines());
    }

    private void RebuildInlines()
    {
        Inlines?.Clear();
        var expr = Expression;
        if (string.IsNullOrEmpty(expr)) return;

        // Ressourcen aus dem Theme laden
        var primaryBrush = GetBrush("PrimaryBrush") ?? Brushes.CornflowerBlue;
        var textBrush = GetBrush("TextPrimaryBrush") ?? Brushes.White;
        var mutedBrush = GetBrush("TextMutedBrush") ?? Brushes.Gray;

        // Nur bei kurzen Ausdrücken highlighten (Performance)
        if (expr.Length > 50)
        {
            Inlines ??= new InlineCollection();
            Inlines.Add(new Run { Text = expr, Foreground = mutedBrush });
            return;
        }

        Inlines ??= new InlineCollection();
        var current = new System.Text.StringBuilder();
        TokenType currentType = TokenType.Number;

        foreach (char c in expr)
        {
            var type = ClassifyChar(c);
            if (type != currentType && current.Length > 0)
            {
                AddRun(current.ToString(), currentType, primaryBrush, textBrush, mutedBrush);
                current.Clear();
            }
            current.Append(c);
            currentType = type;
        }

        if (current.Length > 0)
            AddRun(current.ToString(), currentType, primaryBrush, textBrush, mutedBrush);
    }

    private void AddRun(string text, TokenType type, IBrush primary, IBrush textColor, IBrush muted)
    {
        var brush = type switch
        {
            TokenType.Operator => primary,
            TokenType.Parenthesis => muted,
            _ => muted // Zahlen in Expression-Zeile auch in Muted
        };

        var run = new Run { Text = text, Foreground = brush };
        if (type == TokenType.Parenthesis)
            run.FontWeight = FontWeight.Normal;
        else if (type == TokenType.Operator)
            run.FontWeight = FontWeight.Bold;

        Inlines!.Add(run);
    }

    private static TokenType ClassifyChar(char c) => c switch
    {
        '+' or '-' or '\u2212' or '*' or '\u00D7' or '/' or '\u00F7' or '^' => TokenType.Operator,
        '(' or ')' => TokenType.Parenthesis,
        _ => TokenType.Number
    };

    private IBrush? GetBrush(string key)
    {
        if (Application.Current?.TryGetResource(key, ActualThemeVariant, out var resource) == true)
            return resource as IBrush;
        return null;
    }

    private enum TokenType { Number, Operator, Parenthesis }
}
