using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TaskOutputListener
{
    internal class JsHintStylishOutputParser : ITaskRunnerOutputParser
    {
        private List<string> _linesBuffer;
        private string _previousFileName;
        private List<ErrorInfo> _errorsBuffer;
        private bool _isBuffering;

        // Matches ANSI terminal escape codes
        static private Regex colorSwitchRegex = new Regex(@"\u001b\[\d+m");

        // Matches a LF character, CR character and a tab character respectively
        static private Regex lineBreakRegex = new Regex(@"[\r\n\t]");

        // Matches an error summary line with the number and type of error (warnings, errors, No problems)
        static private Regex summaryRegex = new Regex
            (
                  @"^\s*"                                          // Beginning of line and any ammount of spaces
                + @"(‼[\s]+[\d]+[\s]+warnings?)|"                  // Format for a "warnings" errors summary line
                + @"(×[\s]+[\d]+[\s]+errors?)|"                    // Format for a "errors" errors summary line
                + @"(√[\s]+No problems)"                           // Format for a "No problems" errors summary line
                + @"\s*$",                                         // Any ammount of spaces and End-of-line
                RegexOptions.IgnoreCase
            );

        // Matches and extracts Line, Column, Message, Message color code and error code from an output line
        // Note: that the ANSI terminal escape codes are necessary to determine the serverity of the error
        //   based on the color it is reported.
        // Example: "  \u001b[90mline 6\u001b[39m  \u001b[90mcol 7\u001b[39m   \u001b[94mMissing semicolon.\u001b[39m\r\n"
        // Example: "  \u001b[90mline 6\u001b[39m  \u001b[90mcol 7\u001b[39m  \u001b[94mMissing semicolon.\u001b[39m  \u001b[90m(W033)\u001b[39m\r\n"
        static private Regex lineColumnRegex = new Regex
            (
                  @"^\s*"                                         // Beginning of line and any ammount of white spaces
                + @"\u001b\[\d+m"                                 // Matches ANSI terminal escape codes.
                + @"line[\s]+"                                    // Matches "line" followed by spaces
                + "(?<LINE>[0-9]*)"                               // Matches any number
                + @"\u001b\[\d+m"                                 // Matches ANSI terminal escape codes.
                + @"[\s]+"                                        // Matches one or more white spaces
                + @"\u001b\[\d+m"                                 // Matches ANSI terminal escape codes.
                + @"col[\s]+"                                     // Matches "col" followed by white spaces
                + "(?<COLUMN>[0-9]*)"                             // Matches any number.
                + @"\u001b\[\d+m"                                 // Matches ANSI terminal escape codes.
                + @"[\s]+"                                        // Matches one or more spaces
                + @"\u001b\[(?<COLOR>[0-9]*)m"                    // Matches ANSI terminal escape codes.
                + @"(?<TEXT>[^\u001b]+)"                          // Matches anything that is not a "\u001b]"
                + @"\u001b\[\d+m"                                 // Matches ANSI terminal escape codes.
                + @"(\s+\u001b\[\d+m)?"                           // Optional: matches white spaces followed by an ANSI terminal escape codes
                + @"(?<ERRORCODE>\([^\)]+\))?"                    // Optional: matches '(' followed by any character that is not a ')', followed by ')'
                + @"(\u001b\[\d+m)?"                              // Optional: matches ANSI terminal escape codes.
                + @"\s*$",                                        // Matches any ammount of spaces and End-of-line
                RegexOptions.IgnoreCase
            );

        // Matches and extracts file path from an output line
        // Example: "wwwroot\js\site.min.js"
        static private Regex filePathRegex = new Regex
            (
                  @"^\s*"                                         // Beginning of line and any ammount of spaces
                + @"(?<FILEPATH>[\w\s\\\\]+(\.\w+)+)"             // Match a file path in the format above.
                + @"\s*$",                                        // Any ammount of spaces and End-of-line
                RegexOptions.IgnoreCase
            );

        public bool IsBuffering
        {
            get
            {
                return _isBuffering;
            }

            set
            {
                _isBuffering = value;
            }
        }

        public JsHintStylishOutputParser()
        {
            _linesBuffer = new List<string>();
            _previousFileName = "";
            _errorsBuffer = new List<ErrorInfo>();
            _isBuffering = false;
        }

        /// <summary>
        /// Parses a collection of strings and returns a OutputParserResult
        /// with the set of parsed errors and set of "unmatched" lines to be passed along to the
        /// next listeners
        /// </summary>
        /// <param name="inputLines"></param>
        /// <returns></returns>
        public OutputParserResult ParseOutput(IEnumerable<string> inputLines)
        {
            OutputParserResult parserResult = new OutputParserResult();
            if (inputLines == null)
            {
                return parserResult;
            }

            List<string> outputLines = new List<string>();
            List<string> processingLines = new List<string>();

            processingLines.AddRange(_linesBuffer);
            processingLines.AddRange(inputLines);

            for (int index = _linesBuffer.Count; index < processingLines.Count; index++)
            {
                LineType lineType = ParseLine(processingLines[index]);
                switch (lineType)
                {
                    case LineType.NoMatch:
                        if (_isBuffering)
                        {
                            outputLines.AddRange(_linesBuffer);
                            outputLines.Add(processingLines[index]);

                            _isBuffering = false;
                            _linesBuffer.Clear();
                            _errorsBuffer.Clear();
                            _previousFileName = "";
                        }
                        else
                        {
                            outputLines.Add(processingLines[index]);
                        }

                        break;

                    case LineType.FileInfo:
                        _isBuffering = true;
                        _linesBuffer.Add(processingLines[index]);

                        break;

                    case LineType.LineColumnInfo:
                        if (_isBuffering)
                        {
                            _linesBuffer.Add(processingLines[index]);
                        }
                        else
                        {
                            outputLines.Add(processingLines[index]);
                        }
                        break;

                    case LineType.SummaryInfo:
                        if (_isBuffering)
                        {
                            parserResult.ErrorList.AddRange(GenerateErrorListItems());

                            _isBuffering = false;
                            _linesBuffer.Clear();
                            _errorsBuffer.Clear();
                            _previousFileName = "";
                        }
                        else
                        {
                            outputLines.Add(processingLines[index]);
                        }
                        break;
                }

            }

            parserResult.OutputLines = outputLines;
            return parserResult;

        }


        /// <summary>
        /// Parses a single line and returns its type:
        /// NoMatch, FileInfo, LineColumnInfo or SummaryInfo
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private LineType ParseLine(string line)
        {
            LineType lineType = LineType.NoMatch;
            string noColorSwitchLine = colorSwitchRegex.Replace(line, "");
            string cleanLine = lineBreakRegex.Replace(noColorSwitchLine, "");

            // File path
            string filePath = IsFilePathMatch(cleanLine);
            if (filePath != string.Empty)
            {
                _previousFileName = filePath;
                lineType = LineType.FileInfo;
            }

            // Error line and column
            ErrorInfo errorInfo = IsLineColumnMatch(line);
            if (errorInfo != null)
            {
                if (_isBuffering)
                {
                    _errorsBuffer.Add(errorInfo);
                    lineType = LineType.LineColumnInfo;
                }
            }

            // Summary line
            bool isSummary = IsSummaryLine(cleanLine);
            if (isSummary)
            {
                lineType = LineType.SummaryInfo;
            }

            return lineType;
        }

        /// <summary>
        /// Generates a list of IErrorListItem from the
        /// buffered errors info
        /// </summary>
        /// <returns></returns>
        private List<IErrorListItem> GenerateErrorListItems()
        {
            List<IErrorListItem> errors = new List<IErrorListItem>();

            foreach (ErrorInfo errorInfo in _errorsBuffer)
            {
                TaskError error = new TaskError
                {
                    Line = errorInfo.LineNumber,
                    Column = errorInfo.ColumnNumber,
                    Message = errorInfo.Message,
                    Filename = _previousFileName,
                    Severity = errorInfo.Severity,
                    ErrorCode = errorInfo.ErrorCode
                };

                errors.Add(error);
            }

            return errors;
        }

        /// <summary>
        /// Checks if a line matches any of expected formmat
        /// for a summary line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private bool IsSummaryLine(string line)
        {
            Match summaryMatch = summaryRegex.Match(line);

            if (summaryMatch.Success)
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if a line matches the format for file path header
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private string IsFilePathMatch(string line)
        {
            Match filePathMatch = filePathRegex.Match(line);

            if (filePathMatch.Success)
            {
                return filePathMatch.Groups["FILEPATH"].Value;
            }

            return string.Empty;
        }

        /// <summary>
        /// Checks if a line matches any the formmat
        /// for a line and column info line
        /// </summary>
        /// <param name="line"></param>
        /// <returns></returns>
        private ErrorInfo IsLineColumnMatch(string line)
        {
            ErrorInfo error = null;

            string lineNumberStr = "";
            string colNumberStr = "";
            string message = "";
            string severity = "";
            string errorCode = "";

            Match errorMatch = lineColumnRegex.Match(line);

            if (errorMatch.Success)
            {
                lineNumberStr = errorMatch.Groups["LINE"].Value;
                colNumberStr = errorMatch.Groups["COLUMN"].Value;
                severity = errorMatch.Groups["COLOR"].Value;
                message = errorMatch.Groups["TEXT"].Value.TrimEnd(' ');
                errorCode = errorMatch.Groups["ERRORCODE"].Value;

                int lineNumber;
                int colNumber;
                int.TryParse(lineNumberStr, out lineNumber);
                int.TryParse(colNumberStr, out colNumber);

                error = new ErrorInfo
                {
                    LineNumber = lineNumber,
                    ColumnNumber = colNumber,
                    Message = message,
                    ErrorCode = errorCode,
                    Severity = severity == "31" ? MessageSeverity.Error : MessageSeverity.Warning
                };

            }

            return error;
        }

        /// <summary>
        /// Private class to hold buffered error info before
        /// the errors are flushed
        /// </summary>
        private class ErrorInfo
        {
            public int LineNumber { get; set; }
            public int ColumnNumber { get; set; }
            public string Message { get; set; }
            public string ErrorCode { get; set; }
            public MessageSeverity Severity { get; set; }

        }

        /// <summary>
        /// Indicates the type of line that is parsed
        /// </summary>
        private enum LineType
        {
            FileInfo = 0,
            LineColumnInfo = 1,
            SummaryInfo = 2,
            NoMatch = 3
        }
    }
}

