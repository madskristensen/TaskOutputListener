using Microsoft.VisualStudio.Web.Extensions.Common.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TaskRunnerExplorer;

namespace TaskOutputListener
{
    /// <summary>
    /// Base class for errors output listeners
    /// </summary>
    internal abstract class ErrorListOutputListener : ITaskRunnerOutputListener
    {
        private readonly OutputErrorsFactory _factory;
        private Stopwatch _timer;
        private IErrorListProvider _errorProvider { get; set; }
        private ITaskRunnerOutputParser _outputParser;
        internal protected ErrorListOutputListener (IErrorListProvider errorProvider,
                                                    IProjectEventServices ProjectEventServices,
                                                    ITaskRunnerOutputParser outputParser)
        {

            ProjectEventServices.ProjectLoaded += ProjectEventServices_ProjectLoaded;
            ProjectEventServices.ProjectClosing += ProjectEventServices_ProjectClosing;
            ProjectEventServices.SolutionClosing += ProjectEventServices_SolutionClosing;

            _errorProvider = errorProvider;
            _factory = new OutputErrorsFactory(_errorProvider);
            _outputParser = outputParser;
            _timer = new Stopwatch();

            Initialize();
        }

        private void ProjectEventServices_SolutionClosing(object sender, EventArgs e)
        {
            RemoveErrors();
        }

        private void ProjectEventServices_ProjectLoaded(object sender, ProjectEventArgs e)
        {
            Initialize();
        }

        private void ProjectEventServices_ProjectClosing(object sender, ProjectEventArgs e)
        {
            RemoveErrors();
        }

        public void Initialize()
        {
            // Ataches the Factory to the error provider
            _errorProvider.AddErrorListFactory(_factory);
            _timer.Start();
        }

        /// <summary>
        /// Updates errors list
        /// </summary>
        /// <param name="snapshot">New error list snapshot</param>
        internal void UpdateErrorsList()
        {
            // Tell the provider to mark all the sinks dirty (so, as a side-effect, they will start an update pass that will get the new snapshot
            // from the factory).
            _errorProvider.UpdateAllSinks(_factory);
        }


        /// <summary>
        /// Handles the command output (inputLines) and generates errors as appropriate.
        /// </summary>
        /// <param name="task">Task being executed</param>
        /// <param name="projectName">Project name of the task</param>
        /// <param name="inputLines">Set of command output messages to be parsed</param>
        /// <returns></returns>
        public IEnumerable<string> HandleLines(ITaskRunnerNode task, string projectName, IEnumerable<string> lines)
        {
            bool errorListNeedsUpdate = false;

            // Remove existing Factory errors associated with the running task after idle time
            if (_timer.ElapsedMilliseconds > 1000)
            {
                _factory.ClearErrors(task);
                errorListNeedsUpdate = true;
            }


            OutputParserResult parserOutput = _outputParser.ParseOutput(lines);
            if (parserOutput != null && parserOutput.ErrorList.Count() > 0)
            {
                OutputErrorSnapshot currentSnapshot = _factory.CurrentSnapshot as OutputErrorSnapshot;
                List<IErrorListItem> newErrors = RemoveErrorDuplicates(task, projectName, parserOutput, currentSnapshot);

                if (newErrors.Count > 0)
                {
                    _factory.AddErrorItems(newErrors);
                    errorListNeedsUpdate = true;
                }
            }

            // Only update the errors list if Factory errors has changed.
            if (errorListNeedsUpdate)
            {
                UpdateErrorsList();
            }

            // Reset and start the timer to measure new elapsed time between new line(s) inputs
            _timer.Reset();
            _timer.Start();

            return parserOutput.OutputLines;
        }

        private static List<IErrorListItem> RemoveErrorDuplicates(ITaskRunnerNode task,
                                                                  string projectName,
                                                                  OutputParserResult parserOutput,
                                                                  OutputErrorSnapshot currentSnapshot)
        {
            List<IErrorListItem> newErrors = new List<IErrorListItem>();

            foreach (IErrorListItem error in parserOutput.ErrorList)
            {
                error.ProjectName = projectName;
                error.ErrorSource = task.Name;
                error.Filename = Path.Combine(task.Command.WorkingDirectory, error.Filename);
                error.Command = task.Command;

                if (!currentSnapshot.Errors.Contains(error))
                {
                    newErrors.Add(error);
                }
            }

            return newErrors;
        }

        /// <summary>
        /// Deletes the errors from the factory
        /// </summary>
        public void RemoveErrors()
        {
            _factory.ClearErrors();
        }

        public void Dispose()
        {
            if (_errorProvider != null)
            {
                // Detach the Factory from the error provider
                _errorProvider.RemoveErrorListFactory(_factory);
            }

            _factory.Dispose();

        }
    }
}
