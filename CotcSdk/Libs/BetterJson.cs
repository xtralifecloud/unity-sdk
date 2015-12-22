
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
        string stringFromLiteral = "";

        for (int character = 0; character < inputString.Length; ++character)
        {
            if ((inputString[character] == '\\') && (character + 1 < inputString.Length))
            {
                // Special escape characters
                switch (inputString[character + 1])
                {
                    case '"':
                        stringFromLiteral += '"';
                        ++character;
                        continue;

                    case '\\':
                        stringFromLiteral += '\\';
                        ++character;
                        continue;

                    case 'a':
                        stringFromLiteral += '\a';
                        ++character;
                        continue;

                    case 'b':
                        stringFromLiteral += '\b';
                        ++character;
                        continue;

                    case 'f':
                        stringFromLiteral += '\f';
                        ++character;
                        continue;

                    case 'n':
                        stringFromLiteral += '\n';
                        ++character;
                        continue;

                    case 'r':
                        stringFromLiteral += '\r';
                        ++character;
                        continue;

                    case 't':
                        stringFromLiteral += '\t';
                        ++character;
                        continue;

                    case 'v':
                        stringFromLiteral += '\v';
                        ++character;
                        continue;

                    case '0':
                        stringFromLiteral += '\0';
                        ++character;
                        continue;
                }
            }

            // TODO: handle hexadecimal \uXXXX sequence (if we need it)

            stringFromLiteral += inputString[character];
        }

        return stringFromLiteral;
    }

    /// <summary>
    /// Adds escape characters to get a string literal
    /// </summary>
    /// <param name="inputString"></param>
    /// <returns></returns>
    public static string ToLiteral(string inputString)
    {
        string stringLiteral = "";

        for (int character = 0; character < inputString.Length; ++character)
        {
            // Special escape characters
            switch (inputString[character])
            {
                case '"':
                case '\\':
                    stringLiteral += '\\';
                    stringLiteral += inputString[character];
                    continue;

                case '\a':
                    stringLiteral += "\\a";
                    continue;

                case '\b':
                    stringLiteral += "\\b";
                    continue;

                case '\f':
                    stringLiteral += "\\f";
                    continue;

                case '\n':
                    stringLiteral += "\\n";
                    continue;

                case '\r':
                    stringLiteral += "\\r";
                    continue;

                case '\t':
                    stringLiteral += "\\t";
                    continue;

                case '\v':
                    stringLiteral += "\\v";
                    continue;

                case '\0':
                    stringLiteral += "\\0";
                    continue;
            }

            // Normal (ASCII code) characters
            if (((int)inputString[character] >= 32) && ((int)inputString[character] <= 126))
            {
                stringLiteral += inputString[character];
                continue;
            }

            // Default, turn into a hexadecimal \uXXXX sequence
            stringLiteral += ("\\u" + IntToHexadecimal((int)inputString[character]));
        }

        return stringLiteral;
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