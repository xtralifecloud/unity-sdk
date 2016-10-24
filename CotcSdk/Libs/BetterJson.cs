
using System.Text;

public static class BetterJson
{
    /// <summary>
    /// Ensure we have a Json-objet string even if input string is a simple-type-value string
    /// </summary>
    /// <param name="inputString">A simple type value becomes an encapsulated value in a Json-objet</param>
    /// <returns>Return a well formated json string</returns>
    public static string EncapsulateBetterFromLiteral(string inputString)
    {
        string stringBetterFromLiteral = BetterFromLiteral(inputString);

        // Handle the case in which we have [value] instead of {"value":[value]}
        if (!string.IsNullOrEmpty(stringBetterFromLiteral) && stringBetterFromLiteral[0] != '{' && stringBetterFromLiteral[0] != '[')
            stringBetterFromLiteral = "{\"value\":" + stringBetterFromLiteral + "}";

        return stringBetterFromLiteral;
    }

    /// <summary>
    // Ensure we have a Json-objet string even if input string is a string containing a Json-objet string
    /// </summary>
    /// <param name="inputString">A string containing double-quotes containing a Json-objet string becomes a string containing a Json-objet string</param>
    /// <returns></returns>
    public static string BetterFromLiteral(string inputString)
    {
        // Handle the case in which we have "{"[key]":[value]}" instead of {"[key]":[value]}
        if (!string.IsNullOrEmpty(inputString) && inputString.Length > 2)
        {
            if (inputString[0] == '"' && (inputString[1] == '{' || inputString[1] == '['))
                inputString = inputString.Substring(1, inputString.Length - 2);

            if (inputString.Length >2 && (inputString[1] == '\\' || inputString[2] == '\\'))
                inputString = FromLiteral(inputString);
        }
        return inputString;
    }

    /// <summary>
    /// Removes escape characters to get a normal string from literal one
    /// </summary>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static string FromLiteral(string inputString)
    {
        StringBuilder stringFromLiteral = new StringBuilder("");

        for (int character = 0; character < inputString.Length; ++character)
        {
            if ((inputString[character] == '\\') && (character + 1 < inputString.Length))
            {
                // Special escape characters
                switch (inputString[character + 1])
                {
                    case '"':
                        stringFromLiteral.Append('"');
                        ++character;
                        continue;

                    case '\\':
                        stringFromLiteral.Append('\\');
                        ++character;
                        continue;

                    case 'a':
                        stringFromLiteral.Append('\a');
                        ++character;
                        continue;

                    case 'b':
                        stringFromLiteral.Append('\b');
                        ++character;
                        continue;

                    case 'f':
                        stringFromLiteral.Append('\f');
                        ++character;
                        continue;

                    case 'n':
                        stringFromLiteral.Append('\n');
                        ++character;
                        continue;

                    case 'r':
                        stringFromLiteral.Append('\r');
                        ++character;
                        continue;

                    case 't':
                        stringFromLiteral.Append('\t');
                        ++character;
                        continue;

                    case 'v':
                        stringFromLiteral.Append('\v');
                        ++character;
                        continue;

                    case '0':
                        stringFromLiteral.Append('\0');
                        ++character;
                        continue;
                }
            }

            // TODO: handle hexadecimal \uXXXX sequence (if we need it)

            stringFromLiteral.Append(inputString[character]);
        }

        return stringFromLiteral.ToString();
    }

    /// <summary>
    /// Adds escape characters to get a string literal
    /// </summary>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static string ToLiteral(string inputString)
    {
        StringBuilder stringLiteral = new StringBuilder("");

        for (int character = 0; character < inputString.Length; ++character)
        {
            // Special escape characters
            switch (inputString[character])
            {
                case '"':
                case '\\':
                    stringLiteral.Append('\\');
                    stringLiteral.Append(inputString[character]);
                    continue;

                case '\a':
                    stringLiteral.Append("\\a");
                    continue;

                case '\b':
                    stringLiteral.Append("\\b");
                    continue;

                case '\f':
                    stringLiteral.Append("\\f");
                    continue;

                case '\n':
                    stringLiteral.Append("\\n");
                    continue;

                case '\r':
                    stringLiteral.Append("\\r");
                    continue;

                case '\t':
                    stringLiteral.Append("\\t");
                    continue;

                case '\v':
                    stringLiteral.Append("\\v");
                    continue;

                case '\0':
                    stringLiteral.Append("\\0");
                    continue;
            }

            // Normal (ASCII code) characters
            if (((int)inputString[character] >= 32) && ((int)inputString[character] <= 126))
            {
                stringLiteral.Append(inputString[character]);
                continue;
            }

            // Default, turn into a hexadecimal \uXXXX sequence
            stringLiteral.Append(("\\u" + IntToHexadecimal((int)inputString[character])));
        }

        return stringLiteral.ToString();
    }

    /// <summary>
    /// Convert an int to a hexadecimal value
    /// </summary>
    /// <param name="number"></param>
    /// <returns></returns>
    private static char[] IntToHexadecimal(int number)
    {
        char[] hexadecimalSequence = new char[4];
        int num;

        for (int pos = 0; pos < 4; ++pos)
        {
            num = number % 16;

            if (num < 10)
                hexadecimalSequence[3 - pos] = (char)('0' + num);
            else
                hexadecimalSequence[3 - pos] = (char)('A' + (num - 10));

            number >>= 4;
        }

        return hexadecimalSequence;
    }
}