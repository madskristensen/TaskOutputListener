using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TaskOutputListener
{
    internal class JsHintDefaultOutputParser : ITaskRunnerOutputParser
    {

        // Example: "  \u001b[90mline 6\u001b[39m  \u001b[90mcol 7\u001b[39m   \u001b[94mMissing semicolon.\u001b[39m\r\n"        
        // Example: "  \u001b[90mline 6\u001b[39m  \u001b[90mcol 7\u001b[39m   \u001b[94mMissing semicolon.\u001b[39m\r\n"
        static private Regex lineColumnRegex = new Regex
            (
                  @"^\s*"                                          // Beginning of line and any ammount of spaces
                + @"(?<FILEPATH>([^\\]+\\)+[^\\\.]+(\.\w+)+)"      // Matches any character except '\', followed by '\': 1 or more times
                                                                   //     followed by any character except ('\', '.'): 1 or more times
                                                                   //     followed by '.' followed by any  word character: : 1 or more times 
                + @"\:\s+"                                         // Matches ":" followed by one or more white spaces
                + @"line\s+"                                       // Matches "line" followed by one or more white spaces
                + @"(?<LINE>[0-9]*)"                               // Matches any number
                + @",\s+"                                          // Matches ',' followed by one or more white spaces
                + @"col\s+"                                        // Matches "col" followed by one or more white spaces
                + @"(?<COLUMN>[0-9]*)"                             // Matches any number
                + @",\s*"                                          // Matches "," followed by any number of white spaces
                + @"(?<TEXT>[^\(]+)"                               // Matches any character that is not a '(': one or more times
                + @"(?<ERRORCODE>\([^\)]+\))?"                     // Optional: matches '(' followed by any character that is not a ')'
                                                                   //      followed by ")"
                + @"\s*$",                                         // Any ammount of spaces and End-of-line
                RegexOptions.IgnoreCase
            );

        static private Regex colorSwitchRegex = new Regex(@"\u001b\[\d+m");
        static private Regex lineBreakRegex = new Regex(@"[\r\n\t]");

        public JsHintDefaultOutputParser()
        {
        }

        public OutputParserResult ParseOutput(IEnumerable<string> inputLines)
        {
            OutputParserResult parserResult = new OutputParserResult();
            if (inputLines == null)
            {
                return parserResult;
            }

            List<string> outputLines = new List<string>();
            List<string> processingLines = new List<string>();

            processingLines.AddRange(inputLines);

            foreach (string line in processingLines)
            {

                TaskError error = IsErrorMatch(line);

                if (error != null)
                {
                    parserResult.ErrorList.Add(error);
                }
                else
                {
                    outputLines.Add(line);
                }

            }

            parserResult.OutputLines = outputLines;
            return parserResult;

        }

        private TaskError IsErrorMatch(string line)
        {
            TaskError error = null;

            string fileName = "";
            string lineNumberStr = "";
            string colNumberStr = "";
            string message = "";
            string errorCode = "";

            string noColorSwitchLine = colorSwitchRegex.Replace(line, "");
            string cleanLine = lineBreakRegex.Replace(noColorSwitchLine, "");

            Match errorMatch = lineColumnRegex.Match(cleanLine);

            if (errorMatch.Success)
            {
                fileName = errorMatch.Groups["FILEPATH"].Value;
                lineNumberStr = errorMatch.Groups["LINE"].Value;
                colNumberStr = errorMatch.Groups["COLUMN"].Value;
                message = errorMatch.Groups["TEXT"].Value.TrimEnd(' ');
                errorCode = errorMatch.Groups["ERRORCODE"].Value;


                // Convert / to \
                fileName = fileName.Replace('/', '\\');
                int lineNumber;
                int colNumber;
                int.TryParse(lineNumberStr, out lineNumber);
                int.TryParse(colNumberStr, out colNumber);

                error = new TaskError
                {
                    Line = lineNumber,
                    Column = colNumber,
                    Message = message,
                    Filename = fileName,
                    Severity = MessageSeverity.Error,
                    ErrorCode = errorCode
                };
            }

            return error;
        }

    }

}

