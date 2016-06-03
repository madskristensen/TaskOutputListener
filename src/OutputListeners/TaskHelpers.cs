using System;
using System.Text.RegularExpressions;

namespace TaskOutputListener
{
    static class TaskHelpers
    {
        public static TaskError CreateErrorFromRegex(Match match, string file = null)
        {
            int line;
            int column;

            int.TryParse(match.Groups["line"].Value, out line);
            int.TryParse(match.Groups["column"].Value, out column);

            return new TaskError
            {
                Line = line,
                Column = column,
                ErrorCode = match.Groups["code"]?.Value.Trim(),
                Message = match.Groups["message"]?.Value.Trim(),
                Severity = ParseSeverity(match.Groups["severity"]?.Value.Trim()),
                Filename = file ?? match.Groups["file"]?.Value.Trim()
            };
        }

        private static MessageSeverity ParseSeverity(string severity)
        {
            if (!string.IsNullOrWhiteSpace(severity))
            {
                if (severity.Equals("error", StringComparison.OrdinalIgnoreCase))
                    return MessageSeverity.Error;

                if (severity.Equals("warning", StringComparison.OrdinalIgnoreCase))
                    return MessageSeverity.Warning;

                if (severity.Equals("info", StringComparison.OrdinalIgnoreCase))
                    return MessageSeverity.Info;
            }

            return MessageSeverity.NoProblems;
        }
    }
}
