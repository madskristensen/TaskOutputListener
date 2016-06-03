using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Web.Extensions.Common.Services;

namespace TaskOutputListener
{
    [Export(typeof(ITaskRunnerOutputListener))]
    [Name("EslintOutputListener")]
    [Order(After = "Microsoft.VisualStudio.TaskRunnerExplorer.TaskRunnerConsoles")]
    internal class EslintOutputListener : ErrorListOutputListener
    {
        [ImportingConstructor]
        internal EslintOutputListener(IErrorListProvider errorProvider, IProjectEventServices projectEventServices)
            : base(errorProvider, projectEventServices, new EslintOutputParser())
        { }
    }

    internal class EslintOutputParser : ITaskRunnerOutputParser
    {
        // Example: 2:5   error  Unexpected constant condition        no-constant-condition
        private static Regex _regex = new Regex(@"(?<line>[0-9]+):(?<column>[0-9]+)\s+(?<severity>error|warning)\s+(?<message>.+)\s(?<code>[\w-]+)", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static Regex _colorSwitchRegex = new Regex(@"\u001b\[\d+m");
        private string _lastFile;

        public OutputParserResult ParseOutput(IEnumerable<string> inputLines)
        {
            OutputParserResult parserResult = new OutputParserResult();

            if (inputLines == null)
                return parserResult;

            List<string> outputLines = new List<string>();

            foreach (var line in inputLines)
            {
                var cleanLine = _colorSwitchRegex.Replace(line, "");

                if (cleanLine.Contains(":\\"))
                {
                    var trimmed = cleanLine.Trim();
                    if (File.Exists(trimmed))
                    {
                        _lastFile = trimmed;
                    }
                }

                // If there is no file name, proceed to next line
                if (string.IsNullOrEmpty(_lastFile))
                {
                    outputLines.Add(line);
                    continue;
                }

                Match match = _regex.Match(cleanLine);

                if (match.Success)
                {
                    TaskError error = TaskHelpers.CreateErrorFromRegex(match, _lastFile);
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
    }
}

