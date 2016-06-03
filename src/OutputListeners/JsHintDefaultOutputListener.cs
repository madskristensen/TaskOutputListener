using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Web.Extensions.Common.Services;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace TaskOutputListener
{
    [Export(typeof(ITaskRunnerOutputListener))]
    [Name("Microsoft.VisualStudio.TaskRunnerExplorer.OutputListeners.JsHintDefaultOutputListener")]
    [Order(After = "Microsoft.VisualStudio.TaskRunnerExplorer.TaskRunnerConsoles")]
    internal class JsHintDefaultOutputListener : ErrorListOutputListener
    {

        [ImportingConstructor]
        internal JsHintDefaultOutputListener(IErrorListProvider errorProvider, IProjectEventServices projectEventServices)
            : base (errorProvider, projectEventServices, new JsHintDefaultOutputParser())
        {

        }
    }
}
