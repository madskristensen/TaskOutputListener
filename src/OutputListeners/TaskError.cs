using System;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace TaskOutputListener
{
    internal class TaskError : IErrorListItem
    {
        public int Column { get; set; }
        public ITaskRunnerCommand Command { get; set; }
        public object ErrorCode { get; set; }
        public object ErrorSource { get; set; }
        public string Filename { get; set; }
        public int Line { get; set; }
        public string Message { get; set; }
        public string ProjectName { get; set; }
        public MessageSeverity Severity { get; set; }
        public object TaskCategory { get; set; }

        public override string ToString()
        {
            string result = "";

            result += string.Format("Filename:{0}, ", Filename);
            result += string.Format("Column:{0}, ", Column);
            result += string.Format("Line:{0}, ", Line);
            result += string.Format("ProjectName:{0}, ", ProjectName);
            result += string.Format("Severity:{0}, ", Severity);
            result += string.Format("ErrorCode:{0}", ErrorCode);

            return result;
        }

    }
}
