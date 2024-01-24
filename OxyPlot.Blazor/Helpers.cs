// --------------------------------------------------------------------------------------------------------------------
// <copyright file="BlazorSvgRenderContext.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides a render context for scalable vector graphics output.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Blazor
{
    using System.Text.RegularExpressions;

    internal partial class Helpers
    {
#if NET7_0_OR_GREATER
        [GeneratedRegex("(\r\n|\n|\r)", RegexOptions.CultureInvariant)]
        private static partial Regex LineSplitterRegex();
#endif

        /// <summary>
        /// Splits the text at \r\n or \n or \r into multiple lines
        /// </summary>
        /// <param name="input"></param>
        /// <returns>the array of lines</returns>
        public static string[] SplitToLines(string input)
        {
#if NET7_0_OR_GREATER
            return LineSplitterRegex().Split(input);
#else
            return Regex.Split(input, "(\r\n|\n|\r)", RegexOptions.Compiled | RegexOptions.CultureInvariant);
#endif
        }
    }
}
