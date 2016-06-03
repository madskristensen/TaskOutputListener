using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Web.Extensions.Common.Services;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace TaskOutputListener
{
    [Export(typeof(ITaskRunnerOutputListener))]
    [Name("Microsoft.VisualStudio.TaskRunnerExplorer.OutputListeners.JsHintStylishOutputListener")]
    [Order(After = "Microsoft.VisualStudio.TaskRunnerExplorer.OutputListeners.JsHintDefaultOutputListener")]
    internal class JsHintStylishOutputListener : ErrorListOutputListener
    {
        [ImportingConstructor]
        internal JsHintStylishOutputListener(IErrorListProvider errorProvider, IProjectEventServices projectEventServices)
            : base(errorProvider, projectEventServices, new JsHintStylishOutputParser())
        {

        }
    }
}
