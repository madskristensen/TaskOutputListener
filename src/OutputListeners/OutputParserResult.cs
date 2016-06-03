using System.Collections.Generic;

namespace TaskOutputListener
{
    internal class OutputParserResult
    {
        public OutputParserResult()
        {
            OutputLines = new List<string>();
            ErrorList = new List<IErrorListItem>();
        }
        public IEnumerable<string> OutputLines { get; set; }
        public List<IErrorListItem> ErrorList { get; set; }
    }
}