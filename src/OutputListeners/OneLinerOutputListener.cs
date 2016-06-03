using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TaskRunnerExplorer;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Web.Extensions.Common.Services;

namespace TaskOutputListener
{
    [Export(typeof(ITaskRunnerOutputListener))]
    [Name("CssLintCompactOutputListener")]
    [Order(Before = "Microsoft.VisualStudio.TaskRunnerExplorer.TaskRunnerConsoles")]
    internal class OneLinerOuputListener : ErrorListOutputListener
    {
        [ImportingConstructor]
        internal OneLinerOuputListener(IErrorListProvider errorProvider, IProjectEventServices projectEventServices)
            : base(errorProvider, projectEventServices, new OneLinerOutputParser())
        { }
    }

    internal class OneLinerOutputParser : ITaskRunnerOutputParser
    {
        private static Regex _colorSwitchRegex = new Regex(@"\u001b\[\d+m");
        private static IEnumerable<Regex> _regexes = new List<Regex>
        {
            new Regex(@"^(?<file>([a-z]:)[^:]+): line (?<line>[0-9]+), col (?<column>[0-9]+), (?<severity>[\w]+) - (?<message>.+)", RegexOptions.IgnoreCase | RegexOptions.Compiled)
        };

        private IEnumerable<string> ParseOutputInternal(IEnumerable<string> inputLines, OutputParserResult parserResult)
        {
            foreach (var line in inputLines)
            {
                bool matchFound = false;

                if (line.Length < 200)
                {
                    var cleanLine = _colorSwitchRegex.Replace(line, string.Empty);

                    foreach (var regex in _regexes)
                    {
                        Match match = regex.Match(cleanLine);

                        if (match.Success)
                        {
                            TaskError error = TaskHelpers.CreateErrorFromRegex(match);
                            parserResult.ErrorList.Add(error);
                            matchFound = true;
                            break;
                        }
                    }
                }

                if (!matchFound)
                {
                    yield return line;
                }
            }
        }

        public OutputParserResult ParseOutput(IEnumerable<string> inputLines)
        {
            OutputParserResult parserResult = new OutputParserResult();

            if (inputLines == null)
                return parserResult;


            parserResult.OutputLines = ParseOutputInternal(inputLines, parserResult);

            return parserResult;
        }
    }
}

