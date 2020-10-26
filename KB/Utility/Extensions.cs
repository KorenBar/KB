using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KB.Utility
{
    public static class Extensions
    {
        static public bool IsMiddleEasternLetter(this char c) => c >= 'א' && c <= 'ת' || c >= 'ء' && c <= 'ي';

        static public bool IsStartingWithMiddleEasternLetter(this string str) =>
            IsMiddleEasternLetter(str.DefaultIfEmpty('A').FirstOrDefault(c => !(c == ' ' || char.IsDigit(c) || char.IsSymbol(c) || char.IsPunctuation(c))));

        /// <summary>
        /// RightToLeft will be set by the first letter.
        /// </summary>
        /// <param name="str">Input text</param>
        /// <returns>The text with middle eastern words reversed.</returns>
        static public string ReverseMiddleEastern(this string str) => ReverseMiddleEastern(str, IsStartingWithMiddleEasternLetter(str));

        static public string ReverseMiddleEastern(this string str, bool rightToLeft)
        {
            if (string.IsNullOrWhiteSpace(str)) return str;

            // A Part is:
            // 1. Each sequence of words in the same direction (+ the spaces between them)
            // 2. Sequence of spaces between differnt parts direction (include start and end spaces)
            // 3. Each symbol between differnt parts direction (exclude math and currency symbols adjacent to digits)
            List<string> parts = new List<string>();
            StringBuilder part = new StringBuilder(); // Hold the current part 
            int spaces = 0; // Sequence of spaces that we don't know yet to which part they belong.
            bool onMidEast = false; // Current direction

            foreach (char c in str)
            {
                if (c == ' ') spaces++;
                else
                { // Is not a space
                    bool isMidEast = IsMiddleEasternLetter(c);
                    bool isDigitOrSymbol = char.IsDigit(c) || char.IsSymbol(c) || char.IsPunctuation(c);
                    string p = part.ToString(); // Convert the part to string once

                    // If the direction was changed and is not because it's a symbol, or that it's a symbol but the current part contains only spaces or symbols.
                    if ((isMidEast != onMidEast && !isDigitOrSymbol) || isDigitOrSymbol && p.All(pc => pc == ' ' || char.IsSymbol(pc) || char.IsPunctuation(pc)))
                    { // Start a new part
                        if (p.Length > 0)
                        { // The part is not empty, move the part to the list.
                            // If it was a Middle Eastern language part, reverse it excluding digits and symbols.
                            if (onMidEast) p = Reverse(p, true, true);
                            parts.Add(p);
                            part.Clear();
                        }
                        // Append the separating spaces between the parts (as an independent part)
                        if (spaces > 0) parts.Add(new string(' ', spaces));
                        // Remember the current direction (note that if the isDigitOrSymbol is true, probably that onMidEast is already false)
                        if (!isDigitOrSymbol) onMidEast = isMidEast;
                    }
                    else if (spaces > 0) // The direction was not changed and we have to append the spaces to the current part.
                        if (parts.Count > 0 || part.Length > 0)
                            part.Append(' ', spaces);
                        else // These are spaces at the start of the text, add them as an independent part.
                            parts.Add(new string(' ', spaces));

                    spaces = 0; // We have took care of the accumulated spaces, reset the counter.

                    part.Append(c); // Add the char to the current part.
                }
            }

            // Add the last part
            string lp = part.ToString();
            if (!string.IsNullOrEmpty(lp))
                parts.Add(onMidEast ? Reverse(lp, true, true) : lp);

            // Add the remaining spaces
            if (spaces > 0) parts.Add(new string(' ', spaces));

            // Is RightToLeft Required
            if (rightToLeft) parts.Reverse();

            return string.Join(string.Empty, parts);
        }

        static public string Reverse(this string str) => Reverse(str, false, false);

        static public string Reverse(this string str, bool excludeDigits, bool excludeSymbols)
        {
            char[] strArray = str.ToCharArray();
            Array.Reverse(strArray);
            if (!excludeDigits && !excludeSymbols)
                return new string(strArray);

            StringBuilder result = new StringBuilder();
            StringBuilder tempDigits = new StringBuilder();
            foreach (var c in strArray)
                if (excludeDigits && char.IsDigit(c) || excludeSymbols && (char.IsSymbol(c) || char.IsPunctuation(c)))
                    tempDigits.Append(c);
                else
                {
                    if (tempDigits.Length > 0) result.Append(tempDigits.ToString().Reverse().ToArray());
                    tempDigits.Clear();
                    result.Append(c);
                }
            if (tempDigits.Length > 0) result.Append(tempDigits.ToString().Reverse().ToArray());
            return result.ToString();
        }

        /// <summary>
        /// Justifying Text (flush left and right)
        /// </summary>
        /// <param name="str">The text to be fully justified</param>
        /// <param name="lineSize">Max charters on a single line</param>
        /// <param name="minSpaces">Min spaces between words</param>
        /// <param name="maxSpaces">Max spaces between words</param>
        /// <param name="wordCut">string to place when cutting a word between lines</param>
        /// <returns>fully justified string</returns>
        static public string FullyJustifyString(this string str, int lineSize, int minSpaces, int maxSpaces, string wordCut)
        {
            if (wordCut == null) wordCut = string.Empty;
            // Each line can contains at least 1 char + wordCut
            // We got at least a single word (not null)
            // minSpaces >= 0 and minSpaces > maxSpaces
            if (lineSize - wordCut.Length <= 0
                || minSpaces < 0 || minSpaces > maxSpaces
                || str == null) throw new ArgumentException("Invalid parameters");

            var sourceLines = str.Replace("\r", "").Split('\n');
            List<string> resLines = new List<string>();

            foreach (var sl in sourceLines)
            {
                List<string> resLineWords = new List<string>();
                int resLineFreeSpace = lineSize;
                int spacesBetweenResLineWords = minSpaces;
                string sep;
                foreach (var slword in sl.Split(' ').Where(w => !string.IsNullOrEmpty(w)))
                {
                    string slw = slword;
                    workOnThatWord:
                    resLineFreeSpace = lineSize - (resLineWords.Sum(w => w.Length) + resLineWords.Count * minSpaces);
                    spacesBetweenResLineWords = resLineWords.Count >= 2
                        ? minSpaces + (resLineFreeSpace + minSpaces) / (resLineWords.Count - 1)
                        : minSpaces;
                    int resCuttedLineFreeSpace = resLineFreeSpace - wordCut.Length;
                    // If we can insert the current word.
                    if (resLineFreeSpace >= slw.Length)
                        // Insert the word as is
                        resLineWords.Add(slw);
                    else
                    { // Can not insert the whole word.
                        // If the spaces will be greater than the maxSpaces
                        // And after cut we will have at least 2 chars from the current word.
                        if (spacesBetweenResLineWords > maxSpaces && resCuttedLineFreeSpace > 1)
                        { // Cut the word
                            resLineWords.Add(slw.Substring(0, resCuttedLineFreeSpace) + wordCut);
                            slw = slw.Substring(resCuttedLineFreeSpace, slw.Length - resCuttedLineFreeSpace);
                        }
                        else
                        { // Not enough space, create a new line.
                            sep = string.Empty;
                            for (int i = 0; i < spacesBetweenResLineWords; i++)
                                sep += " ";
                            resLines.Add(string.Join(sep, resLineWords));
                            resLineWords = new List<string>();
                        }
                        // We have not done with that word, stay on this loop.
                        goto workOnThatWord;
                    }
                }

                // Line end, limit spaces to the maxSpaces.
                resLineFreeSpace = lineSize - (resLineWords.Sum(w => w.Length) + resLineWords.Count * minSpaces);
                spacesBetweenResLineWords = resLineWords.Count >= 2
                    ? minSpaces + (resLineFreeSpace + minSpaces) / (resLineWords.Count - 1)
                    : minSpaces;
                sep = string.Empty;
                for (int i = 0; i < Math.Min(spacesBetweenResLineWords, maxSpaces); i++)
                    sep += " ";
                resLines.Add(string.Join(sep, resLineWords));
            }

            return string.Join("\r\n", resLines);
        }

        /// <summary>
        /// Cutting or left padding to fit the input string to the target length.
        /// </summary>
        /// <param name="str">Input string</param>
        /// <param name="targetLength">Target length</param>
        /// <returns></returns>
        public static string FixLength(this string str, int targetLength) => FixLength(str, targetLength, false, false);

        /// <summary>
        /// Cutting or adding spaces to fit the input string to the target length.
        /// </summary>
        /// <param name="str">Input string</param>
        /// <param name="targetLength">Target length</param>
        /// <param name="leftPad">True for left pad or False for right pad</param>
        /// <param name="reversePadForMidEast">True to reverse the pad direction if the input string starting with Middle Eastern letter</param>
        /// <returns></returns>
        public static string FixLength(this string str, int targetLength, bool leftPad, bool reversePadForMidEast) =>
            (leftPad ^ (reversePadForMidEast && IsStartingWithMiddleEasternLetter(str)) 
            ? str.PadLeft(targetLength) : str.PadRight(targetLength))
                .Substring(0, targetLength);

        /// <summary>
        /// Merge all of the inner exceptions messages
        /// </summary>
        /// <returns>All messages one after the other with the separator between them</returns>
        public static string JoinMessages(this Exception exception, string separator = "    ") =>
            exception != null ? exception.Message + separator + JoinMessages(exception.InnerException, separator) : string.Empty;
    }
}
