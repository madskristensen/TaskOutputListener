using System.Collections.Generic;

namespace TaskOutputListener
{
    /// <summary>
    /// Parses inputlines into error items on output listeners
    /// </summary>
    internal interface ITaskRunnerOutputParser
    {
        OutputParserResult ParseOutput(IEnumerable<string> inputLines);
    }
}