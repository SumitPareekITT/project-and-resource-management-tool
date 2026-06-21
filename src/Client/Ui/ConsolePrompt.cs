namespace ProjectResourceManagement.Client.Ui;

using System.Text;

/// <summary>
/// Validated console input. Validation errors are shown inline; the next ShowHeader clears the screen.
/// </summary>
internal static class ConsolePrompt
{
    public static string ReadRequiredText(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = Console.ReadLine()?.Trim() ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            ConsoleScreen.ShowValidationError($"{label} is required.");
        }
    }

    public static string ReadPassword(string label)
    {
        Console.Write($"{label}: ");
        return ReadPasswordCharacters();
    }

    public static string ReadRequiredPassword(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            var value = ReadPasswordCharacters();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            ConsoleScreen.ShowValidationError($"{label} is required.");
        }
    }

    private static string ReadPasswordCharacters()
    {
        var password = new StringBuilder();

        while (true)
        {
            var keyInfo = Console.ReadKey(intercept: true);

            if (keyInfo.Key == ConsoleKey.Enter)
            {
                Console.WriteLine();
                break;
            }

            if (keyInfo.Key == ConsoleKey.Backspace)
            {
                if (password.Length > 0)
                {
                    password.Length--;
                    Console.Write("\b \b");
                }

                continue;
            }

            if (char.IsControl(keyInfo.KeyChar))
            {
                continue;
            }

            password.Append(keyInfo.KeyChar);
            Console.Write('*');
        }

        return password.ToString();
    }

    public static string ReadOptionalText(string label)
    {
        Console.Write($"{label} (blank to skip): ");
        return Console.ReadLine()?.Trim() ?? string.Empty;
    }

    public static int ReadRequiredInt(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            if (int.TryParse(Console.ReadLine(), out var value))
            {
                return value;
            }

            ConsoleScreen.ShowValidationError("Please enter a valid number.");
        }
    }

    public static int? ReadOptionalInt(string label)
    {
        Console.Write($"{label} (blank to skip): ");
        var raw = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return null;
        }

        return int.TryParse(raw, out var value) ? value : null;
    }

    public static decimal ReadRequiredDecimal(string label)
    {
        while (true)
        {
            Console.Write($"{label}: ");
            if (decimal.TryParse(Console.ReadLine(), out var value))
            {
                return value;
            }

            ConsoleScreen.ShowValidationError("Please enter a valid decimal number.");
        }
    }

    public static DateOnly ReadDate(string label, DateOnly? defaultValue = null)
    {
        while (true)
        {
            var suffix = defaultValue is null ? string.Empty : $" (default {defaultValue:yyyy-MM-dd})";
            Console.Write($"{label}{suffix}: ");
            var raw = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(raw) && defaultValue is not null)
            {
                return defaultValue.Value;
            }

            if (DateOnly.TryParse(raw, out var parsed))
            {
                return parsed;
            }

            ConsoleScreen.ShowValidationError("Please enter a valid date (yyyy-MM-dd).");
        }
    }

    public static bool ReadYesNo(string question)
    {
        while (true)
        {
            Console.Write($"{question} [Y/N]: ");
            var answer = Console.ReadLine()?.Trim().ToUpperInvariant();
            if (answer is "Y" or "YES")
            {
                return true;
            }

            if (answer is "N" or "NO")
            {
                return false;
            }

            ConsoleScreen.ShowValidationError("Please answer Y or N.");
        }
    }

    public static bool WantsToGoBack(string? input)
    {
        return string.Equals(input, "B", StringComparison.OrdinalIgnoreCase)
            || string.Equals(input, "0", StringComparison.OrdinalIgnoreCase);
    }
}
