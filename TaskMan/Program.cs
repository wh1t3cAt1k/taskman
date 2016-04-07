using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

using Mono.Options;

using TaskMan.Control;
using TaskMan.Objects;
using System.Configuration;

namespace TaskMan
{
	public class Program
	{
		public static int Main(string[] args)
		{
			TaskMan program = new TaskMan();

			try
			{
				program.Run(args);
			}
			catch (Exception exception)
			{
				if (program.IsVerbose)
				{
					program.ErrorStream.WriteLine(
						Messages.ErrorPerformingOperation,
						program.CurrentOperation,
						exception.Message.DecapitaliseFirstLetter());

					program.ErrorStream.WriteLine(Messages.ExceptionStackTrace);
					program.ErrorStream.Write(exception.StackTrace);
				}
				else
				{
					program.ErrorStream.WriteLine(
						Messages.Error,
						exception.Message.DecapitaliseFirstLetter());
				}

				return -1;
			}

			return 0;
		}
	}

	/// <summary>
	/// The main entity responsible for interacting with the user. 
	/// Also encapsulates the options for a particular run.
	/// </summary>
	public class TaskMan
	{
		#region Constants

		static readonly RegexOptions StandardRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

		static readonly Regex ConfirmActionRegex = new Regex(@"^\s*y(es)?\s*$", StandardRegexOptions);
		static readonly Regex TaskSetDescriptionRegex = new Regex(@"^(description)$", StandardRegexOptions);
		static readonly Regex TaskSetFinishedRegex = new Regex(@"^(finished|completed|accomplished)$", StandardRegexOptions);
		static readonly Regex TaskSetPriorityRegex = new Regex(@"^(priority|importance)$", StandardRegexOptions);

		#endregion

		#region Command Line Flags

		IEnumerable<Flag> _flags;

		Flag<bool> _displayHelpFlag = new Flag<bool>(
			"displays TaskMan's help text", 
			"?|help");

		Flag<bool> _displayLicenseFlag = new Flag<bool>(
			"displays TaskMan's licensing terms", 
			"license");

		Flag<bool> _displayVersionFlag = new Flag<bool>(
			"displays TaskMan's version", 
			"version");

		Flag<bool> _configurationGlobalFlag = new Flag<bool>(
			"specifies that the provided configuration parameter should be set globally and not just for the current user",
			"G|global");

		Flag<bool> _interactiveFlag = new Flag<bool>(
			"displays a confirmation prompt before executing an operation", 
			"I|interactive");

		Flag<bool> _verboseFlag = new Flag<bool>(
			"increase error message verbosity",
			"v|verbose");

		Flag<bool> _silentFlag = new Flag<bool>(
			"do not display any messages except errors",
			"S|silent");

		Flag<bool> _includeAllFlag = new Flag<bool>(
			"forces an operation to be executed upon all tasks", 
			"A|all");

		Flag<string> _descriptionFlag = new Flag<string>(
			"specifies the description for a task (not functional yet)",
			"d|description");

		Flag<string> _priorityFlag = new TaskFilterFlag<string>(
			"filters tasks by priority or specifies a task's priority", 
			"p=|priority=",
			filterPriority: 1,
			filterPredicate: (flagValue, task) => 
				task.Priority == ParseHelper.ParsePriority(flagValue));

		Flag<string> _identityFilterFlag = new TaskFilterFlag<string>(
            "filters tasks by their ID or ID range",
            "i=|id=",
			filterPriority: 1,
			filterPredicate: (flagValue, task) => 
			{
				IEnumerable<int> allowedIds = ParseHelper.ParseTaskId(flagValue);
				return allowedIds.Contains(task.ID);
			});

		Flag<bool> _pendingFilterFlag = new TaskFilterFlag<bool>(
			"filters out any finished tasks", 
			"P|pending|unfinished",
			filterPriority: 1,
			filterPredicate: (_, task) => task.IsFinished == false);

		Flag<bool> _finishedFilterFlag = new TaskFilterFlag<bool>(
			"filters out any unfinished tasks", 
			"F|finished|completed",
			filterPriority: 1,
			filterPredicate: (_, task) => task.IsFinished == true);

		Flag<string> _descriptionFilterFlag = new TaskFilterFlag<string>(
			"filters tasks by their description matching a regex", 
			"r=|like=",
			filterPriority: 1,
			filterPredicate: (pattern, task) => Regex.IsMatch(
				task.Description, 
				pattern, 
				RegexOptions.IgnoreCase));

		Flag<int> _numberSkipFlag = new TaskFilterFlag<int>(
            "skips a given number of tasks when displaying the result",
            "s=|skip=",
            filterPriority: 2,
            filterPredicate: (flagValue, task, taskIndex) => taskIndex + 1 > flagValue);

		Flag<int> _numberLimitFlag = new TaskFilterFlag<int>(
			"limits the total number of tasks displayed",
			"n=|limit=",
			filterPriority: 3,
			filterPredicate: (flagValue, task, taskIndex) => taskIndex < flagValue);

		Flag<string> _orderByFlag = new Flag<string>(
			"orders the tasks by the specified criteria",
			"o=|order=|sort=");

		#endregion

		#region Command Verbs

		IEnumerable<Command> _commands;

		Command _addTaskCommand;
		Command _deleteTasksCommand; 
		Command _completeTasksCommand;
		Command _displayTasksCommand;
		Command _updateTasksCommand;
		Command _configureCommand;

		#endregion

		#region Command Aliases

		IEnumerable<Alias> _aliases;

		Alias _configureTaskListAlias;
		Alias _clearTasksAlias;

		#endregion

		/// <summary>
		/// Gets or sets the current operation performed by the program.
		/// </summary>
		/// <value>The current operation performed by TaskMan.</value>
		public string CurrentOperation { get; private set; }

		/// <summary>
		/// Gets a value indicating whether this instance is verbose.
		/// </summary>
		public bool IsVerbose 
		{ 
			get 
			{
				return 
					this._verboseFlag.IsSet &&
					this._verboseFlag.Value;
			}
		}

		/// <summary>
		/// Gets the error stream used by this instance.
		/// </summary>
		public TextWriter ErrorStream
		{
			get
			{
				return _error;
			}
		}

		/// <summary>
		/// Mono OptionSet object for command line flag parsing.
		/// </summary>
		private OptionSet _optionSet;

		TextWriter _output = Console.Out;
		TextWriter _error = Console.Error;
			
		public TaskMan(
			Func<List<Task>> taskReadFunction = null,
			Action<List<Task>> taskSaveFunction = null,
			TextWriter outputStream = null,
			TextWriter errorStream = null)
		{
			this._optionSet = new OptionSet();

			// Collect non-public instance fields
			// - 
			IEnumerable<FieldInfo> privateFields = 
				typeof(TaskMan).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

			// Collect flags
			// -
			_flags = privateFields
				.Where(fieldInfo => typeof(Flag).IsAssignableFrom(fieldInfo.FieldType))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Flag>();
				
			_flags.ForEach(flag => flag.AddToOptionSet(this._optionSet));

			// Setup program commands
			// -
			_configureCommand = new Command(
				@"^(config|configure)$",
				isReadUpdateDelete: false,
				supportedFlags: new [] { _configurationGlobalFlag, _interactiveFlag });

			_addTaskCommand = new Command(
				@"^(add|new|create)$", 
				isReadUpdateDelete: false,
				supportedFlags: 
					new Flag[] { _interactiveFlag, _descriptionFlag, _priorityFlag, _silentFlag, _verboseFlag });

			_deleteTasksCommand = new Command(
				@"^(delete|remove)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(new [] { _numberLimitFlag, _numberSkipFlag })
					.Concat(new [] { _interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag }));

			_completeTasksCommand = new Command(
				@"^(complete|finish|accomplish)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(new [] { _numberLimitFlag, _numberSkipFlag })
					.Concat(new [] { _interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag }));

			_displayTasksCommand = new Command(
				@"^(show|display|view)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Concat(new Flag[] { _includeAllFlag, _verboseFlag, _orderByFlag }));
			
			_updateTasksCommand = new Command(
				@"^(update|change|modify|set)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(new [] { _numberLimitFlag, _numberSkipFlag })
					.Concat(new [] { _interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag }));

			_commands = privateFields
				.Where(fieldInfo => fieldInfo.FieldType == typeof(Command))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Command>();

			// Setup program aliases
			// -
			_configureTaskListAlias = new Alias(
				"list",
				$"{_configureCommand.ExampleUsage} {_configuration.CurrentTaskList.Name}");

			_clearTasksAlias = new Alias(
				"clear",
				$"{_deleteTasksCommand.ExampleUsage} {_includeAllFlag.ExampleUsage} {_interactiveFlag.ExampleUsage}");

			_aliases = privateFields
				.Where(fieldInfo => fieldInfo.FieldType == typeof(Alias))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Alias>();

			// Setup IO
			// -
			_readTasks = taskReadFunction ?? this.ReadTasksFromFile;
			_saveTasks = taskSaveFunction ?? this.SaveTasksIntoFile;

			_output = outputStream ?? this._output;
			_error = errorStream ?? this._error;
		}

		/// <summary>
		/// Sets the function that would be called to read the task list.
		/// Can be used to override the default function that reads the tasks from file, 
		/// e.g. for the purpose of unit testing.
		/// </summary>
		Func<List<Task>> _readTasks;

		/// <summary>
		/// Sets the function that saves the task list.
		/// Can be used to override the default function that saves the tasks into file,
		/// e.g. for the purpose of unit testing.
		/// </summary>
		Action<List<Task>> _saveTasks;

		TaskmanConfiguration _configuration = new TaskmanConfiguration();

		/// <summary>
		/// Gets the full filename of the file that stores
		/// the current task list. Does not guarantee that 
		/// the file exists.
		/// </summary>
		private string CurrentTaskListFile 
		{
			get
			{
				return Path.Combine(
					_configuration.UserConfigurationDirectory,
					_configuration.GetParameter(_configuration.CurrentTaskList.Name) + ".tmf");
			}
		}

		/// <summary>
		/// Retrieves the task list from the tasks binary file.
		/// </summary>
		/// <returns>The tasks list read from the file.</returns>
		List<Task> ReadTasksFromFile()
		{
			if (!File.Exists(this.CurrentTaskListFile))
			{
				return new List<Task>();
			}

			using (FileStream inputFileStream = 
				new FileStream(this.CurrentTaskListFile, FileMode.Open, FileAccess.Read))
			{
				if (inputFileStream.Position < inputFileStream.Length)
				{
					BinaryFormatter binaryFormatter = new BinaryFormatter();
					return (List<Task>)binaryFormatter.Deserialize(inputFileStream);
				}
				else
				{
					return new List<Task>();
				}
			}
		}

		/// <summary>
		/// Sorts and saves a given collection of tasks to the tasks binary file.
		/// </summary>
		/// <param name="tasks">A collection of tasks.</param>
		void SaveTasksIntoFile(List<Task> tasks)
		{
			FileStream outputFileStream = 
				new FileStream(this.CurrentTaskListFile, FileMode.Create, FileAccess.Write);

			BinaryFormatter binaryFormatter = new BinaryFormatter();
			binaryFormatter.Serialize(outputFileStream, tasks);

			outputFileStream.Close();
		}

		/// <summary>
		/// Writes the specified string to the output 
		/// unless the silent flag is set.
		/// </summary>
		void OutputWrite(string text, params object[] args)
		{
			if (!_silentFlag.IsSet || !_silentFlag.Value)
			{
				_output.Write(string.Format(text, args));
			}
		}

		/// <summary>
		/// Writes the specified string to the output 
		/// unless the silent flag is set.
		/// </summary>
		void OutputWriteLine(string text, params object[] args)
		{
			OutputWrite(text + "\n", args);
		}

		/// <summary>
		/// Confirms with the user that the described action will
		/// be executed.
		/// </summary>
		/// 
		/// <returns>
		/// <c>true</c>, if operation was confirmed, 
		/// <c>false</c> otherwise.
		/// </returns>
		bool ConfirmOperation(
			string actionDescription, 
			params object[] formatArguments)
		{
			bool confirmationResult = true;

			if (_interactiveFlag.IsSet && _interactiveFlag)
			{
				OutputWriteLine(string.Format(actionDescription, formatArguments));
				OutputWrite(Messages.YesNoConfirmationPrompt);

				confirmationResult = ConfirmActionRegex.IsMatch(Console.ReadLine());
			}

			if (!confirmationResult)
			{
				OutputWriteLine(Messages.Cancelled);
			}

			return confirmationResult;
		}

		/// <summary>
		/// Confirms with the user that an action will be performed
		/// upon the specified tasks (e.g. updated, added, etc).
		/// </summary>
		/// <returns>
		/// <c>true</c>, if operation was confirmed, 
		/// <c>false</c> otherwise.
		/// </returns>
		bool ConfirmTaskOperation(IEnumerable<Task> relevantTasks, string willBe)
		{
			StringWriter actionDescription = new StringWriter();

			actionDescription.WriteLine(
				Messages.TheFollowingObjectWillBeAction, 
				"tasks", 
				willBe);

			relevantTasks.Take(3).ForEach(
				task => DisplayTask(task, actionDescription));

			if (relevantTasks.Skip(3).Any())
			{
				actionDescription.WriteLine(
					Messages.AndNumberMore, 
					relevantTasks.Skip(3).Count());
			}

			return ConfirmOperation(actionDescription.ToString().TrimEnd('\n'));
		}

		public void Run(IEnumerable<string> originalArguments)
		{
			if (originalArguments.Any())
			{
				this.CurrentOperation = "expand aliases";

				IEnumerable<Alias> matchingAliases = 
					_aliases.Where(alias => alias.Name == originalArguments.First());

				if (matchingAliases.Any())
				{
					// Replace the alias with its expansion.
					// -
					originalArguments = Enumerable.Concat(
						matchingAliases.Single().ExpansionArray,
						originalArguments.Skip(1));
				}
			}

			this.CurrentOperation = "parse command line arguments";

			LinkedList<string> commandLineArguments =
				new LinkedList<string>(_optionSet.Parse(originalArguments));

			string commandName = commandLineArguments.First?.Value;

			if (commandName == null)
			{
				if (this.HandleGlobalFlags())
				{
					return;
				}
				else
				{
					// TaskMan operates as "show" by default.
					// -
					commandName = "show";
					commandLineArguments = new LinkedList<string>(new [] { commandName }); 
				}
			}

			commandLineArguments.RemoveFirst();

			this.CurrentOperation = "recognize the command";

			IEnumerable<Command> matchingCommands = _commands.Matching(commandName);

			if (!matchingCommands.Any())
			{
				throw new TaskManException(Messages.UnknownCommand, commandName);
			}
			else if (!matchingCommands.IsSingleton())
			{
				throw new TaskManException(Messages.MoreThanOneCommandMatchesInput);
			}

			Command executingCommand = matchingCommands.Single();

			this.CurrentOperation = "ensure flag consistency";

			EnsureFlagConsistency(executingCommand, commandName, originalArguments);

			this.CurrentOperation = "read tasks from the task file";

			List<Task> taskList = _readTasks();

			this.CurrentOperation = "sort the task list";

			string sortingSteps = _orderByFlag.IsSet ?
				_orderByFlag.Value :
				_configuration.SortOrder.GetValue();

			taskList.Sort(
				Task.GetComparison(
					ParseHelper.ParseSortOrder(
						sortingSteps)));
	
			this.CurrentOperation = "filter the task list";

			IEnumerable<Task> filteredTasks = taskList;

			if (executingCommand.IsReadUpdateDelete)
			{
				if (!taskList.Any())
				{
					OutputWriteLine(Messages.TaskListIsEmpty);
					return;
				}

				IEnumerable<ITaskFilter> filterFlagsSpecified = _flags
					.Where(flag => flag.IsSet)
					.OfType<ITaskFilter>();

				if (filterFlagsSpecified.Any())
				{
					filteredTasks = filterFlagsSpecified
						.OrderBy(taskFilter => taskFilter.FilterPriority)
						.Aggregate(
							seed: taskList as IEnumerable<Task>, 
							func: (taskSequence, filter) => filter.Filter(taskSequence));

					if (!filteredTasks.Any())
					{
						OutputWriteLine(Messages.NoTasksMatchingGivenConditions);
						return;
					}
				}
			}

			if (executingCommand == _addTaskCommand)
			{
				this.CurrentOperation = "add a new task";

				Task addedTask = AddTask(commandLineArguments, taskList);

				// Only when the user discarded the task 
				// creation in an interactive mode.
				// -
				if (addedTask == null) return;

				_saveTasks(taskList);

				OutputWriteLine(
					Messages.TaskWasAdded,
					addedTask.Description,
					addedTask.ID,
					addedTask.Priority);
			}
			else if (executingCommand == _displayTasksCommand)
			{
				this.CurrentOperation = "display tasks";

				filteredTasks.ForEach(task => DisplayTask(task));
			}
			else if (executingCommand == _deleteTasksCommand)
			{
				this.CurrentOperation = "delete tasks";

				RequireExplicitFiltering(commandName, taskList, filteredTasks);

				if (!ConfirmTaskOperation(filteredTasks, "deleted")) return;

				int totalTasksBefore = taskList.Count;

				taskList = taskList.Except(filteredTasks).ToList();
				taskList.ForEach((task, index) => task.ID = index);

				int totalTasksAfter = taskList.Count;

				_saveTasks(taskList);

				if (!taskList.Any())
				{
					File.Delete(this.CurrentTaskListFile);
				}

				if (filteredTasks.IsSingleton())
				{
					OutputWriteLine(
						Messages.TaskWasDeleted, 
						filteredTasks.Single().ID, 
						filteredTasks.Single().Description);
				}
				else
				{
					OutputWriteLine(
						Messages.TasksWereDeleted,
						totalTasksBefore - totalTasksAfter);
				}
			}
			else if (executingCommand == _updateTasksCommand)
			{
				this.CurrentOperation = "update task parameters";

				RequireExplicitFiltering(commandName, taskList, filteredTasks);

				if (!ConfirmTaskOperation(filteredTasks, "updated")) return;

				UpdateTasks(commandLineArguments, taskList, filteredTasks);
			}
			else if (executingCommand == _completeTasksCommand)
			{
				this.CurrentOperation = "finish tasks";

				RequireExplicitFiltering(commandName, taskList, filteredTasks);

				if (!ConfirmTaskOperation(filteredTasks, "completed")) return;

				int totalTasksFinished = 
					filteredTasks.ForEach(task => task.IsFinished = true);

				_saveTasks(taskList);

				if (filteredTasks.IsSingleton())
				{
					OutputWriteLine(
						Messages.TaskWasFinished, 
						filteredTasks.Single().ID, 
						filteredTasks.Single().Description);
				}
				else
				{
					OutputWriteLine(
						Messages.TasksWereFinished,
						totalTasksFinished);
				}
			}
			else if (executingCommand == _configureCommand)
			{
				this.CurrentOperation = "configure program parameters";

				if (!commandLineArguments.Any())
				{
					throw new TaskManException(
						Messages.NoParameterName,
						commandName);
				}

				string parameterName = commandLineArguments.PopFirst();

				if (!commandLineArguments.Any())
				{
					// When no parameter value is provided, it means
					// we should show the parameter.
					// -
					OutputWriteLine(_configuration.GetParameter(parameterName));
				}
				else
				{
					string parameterValue = commandLineArguments.PopFirst();

					if (!ConfirmOperation(
						Messages.ParameterNameWillBeSetToValue,
						parameterName,
						parameterValue)) 
					{
						return;
					}

					_configuration.SetParameter(
						parameterName,
						parameterValue,
						_configurationGlobalFlag.IsSet && _configurationGlobalFlag.Value);

					OutputWriteLine(Messages.ParameterWasSetToValue, parameterName, parameterValue);
				}
			}
		}

		/// <summary>
		/// Handle global flags that work without an explicit
		/// command name.
		/// </summary>
		/// <returns>
		/// <c>true</c>, if any of the global flags have been handled,
		/// otherwise, <c>false</c>.
		/// </returns>
		bool HandleGlobalFlags()
		{
			if (_displayHelpFlag.IsSet)
			{
				this.CurrentOperation = "display help text";

				_output.WriteLine(Assembly.GetExecutingAssembly().GetResourceText("TaskMan.HELP.txt"));
				_optionSet.WriteOptionDescriptions(_output);

				return true;
			}
			else if (_displayLicenseFlag.IsSet)
			{
				this.CurrentOperation = "display license text";
				_output.WriteLine(Assembly.GetExecutingAssembly().GetResourceText("TaskMan.LICENSE.txt"));

				return true;
			}
			else if (_displayVersionFlag.IsSet)
			{
				this.CurrentOperation = "display the taskman version";

				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				AssemblyName assemblyName = executingAssembly.GetName();

				string productName = executingAssembly
					.GetAssemblyAttributeValue<AssemblyProductAttribute, string>(
						attribute => attribute.Product);

				_output.WriteLine(
					"{0} version {1}.{2}.{3}",
					productName,
					assemblyName.Version.Major,
					assemblyName.Version.Minor,
					assemblyName.Version.Build);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Ensures the command line flag consistency.
		/// </summary>
		/// <param name="executingCommand">Executing command.</param>
		/// <param name="commandName">The provided command name.</param>
		/// <param name="commandLineArguments">All command line arguments.</param>
		void EnsureFlagConsistency(
			Command executingCommand,
			string commandName,
			IEnumerable<string> commandLineArguments)
		{
			IEnumerable<Flag> unsupportedFlagsSpecified = _flags.Where(
				flag => flag.IsSet && 
				!executingCommand.SupportedFlags.Contains(flag));

			IEnumerable<Flag> requiredFlagsUnspecified = _flags.Where(
				flag => !flag.IsSet &&
				executingCommand.RequiredFlags.Contains(flag));

			IEnumerable<Flag> filterFlagsSpecified = _flags.Where(
				flag => flag.IsSet && 
				flag is ITaskFilter);

			if (unsupportedFlagsSpecified.Any())
			{
				throw new TaskManException(
					Messages.EntityDoesNotMakeSenseWithEntity,
					unsupportedFlagsSpecified.First().GetProvidedName(commandLineArguments),
					commandName);
			}

			if (requiredFlagsUnspecified.Any())
			{
				throw new TaskManException(
					Messages.RequiredFlagNotSet,
					requiredFlagsUnspecified.First().Prototype);
			}

			if (filterFlagsSpecified.Any() &&
				_includeAllFlag.IsSet)
			{
				throw new TaskManException(
					Messages.EntityDoesNotMakeSenseWithEntity,
					filterFlagsSpecified.First().GetProvidedName(commandLineArguments),
					_includeAllFlag.GetProvidedName(commandLineArguments));
			}
		}

		/// <summary>
		/// Either require task filtering or setting of the
		/// <see cref="TaskMan._includeAllFlag"/>, or throw an error.
		/// </summary>
		/// <param name="commandName">The executing command name.</param>
		/// <param name="allTasks">All tasks.</param>
		/// <param name="filteredTasks">Filtered tasks.</param>
		void RequireExplicitFiltering(
			string commandName, 
			IEnumerable<Task> allTasks, 
			IEnumerable<Task> filteredTasks)
		{
			if (!_includeAllFlag.IsSet && filteredTasks == allTasks)
			{
				throw new TaskManException(
					Messages.NoFilterConditionsUseAllIfIntended,
					commandName);
			}
		}

		/// <summary>
		/// Encapsulates the task modification logic in one method.
		/// </summary>
		/// <param name="cliArguments">
		/// The command line arguments. The first argument should contain the name
		/// of the parameter to update, the second argument should contain the value
		/// for that parameter. All other values will be ignored.
		/// </param>
		/// <param name="taskList">All tasks.</param>
		/// <param name="tasksToUpdate">The set of tasks that should be updated.</param>
		void UpdateTasks(LinkedList<string> cliArguments, List<Task> taskList, IEnumerable<Task> tasksToUpdate)
		{
			if (!cliArguments.HasAtLeastTwoElements())
			{ 
				throw new TaskManException(Messages.InsufficientSetParameters);
			}
			
			string parameterToChange = cliArguments.PopFirst().ToLower();
			string parameterStringValue;

			// Preserve old task description for better human-readable
			// message in case we're updating a single task's description.
			// -
			string oldTaskDescription = null;

			if (tasksToUpdate.IsSingleton())
			{
				oldTaskDescription = tasksToUpdate.Single().Description;
			}

			int totalTasksUpdated;

			if (TaskSetPriorityRegex.IsMatch(parameterToChange))
			{
				Priority priority = ParseHelper.ParsePriority(cliArguments.PopFirst());
				parameterStringValue = priority.ToString();

				totalTasksUpdated = tasksToUpdate.ForEach(task => task.Priority = priority);
			}
			else if (TaskSetDescriptionRegex.IsMatch(parameterToChange))
			{
				parameterStringValue = string.Join(" ", cliArguments);

				totalTasksUpdated = tasksToUpdate.ForEach(task => task.Description = parameterStringValue);
			}
			else if (TaskSetFinishedRegex.IsMatch(parameterToChange))
			{
				bool isFinished = ParseHelper.ParseBool(cliArguments.PopFirst());
				parameterStringValue = isFinished.ToString();

				totalTasksUpdated = tasksToUpdate.ForEach(task => task.IsFinished = isFinished);
			}
			else
			{
				throw new TaskManException(Messages.InvalidSetParameters);
			}

			this._saveTasks(taskList);

			if (totalTasksUpdated == 1)
			{
				OutputWriteLine(
					Messages.TaskWasUpdated,
					tasksToUpdate.Single().ID,
					oldTaskDescription,
					parameterToChange,
					parameterStringValue);
			}
			else
			{
				OutputWriteLine(
					Messages.TasksWereUpdated, 
					totalTasksUpdated,
					parameterToChange,
					parameterStringValue);
			}
		}

		/// <summary>
		/// Encapsulates the task adding logic in one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		/// <returns>The <see cref="Task"/> object that was added into the <paramref name="taskList"/></returns>
		Task AddTask(LinkedList<string> cliArguments, List<Task> taskList)
		{
			if (!cliArguments.Any())
			{
				throw new TaskManException(Messages.NoDescriptionSpecified);
			}

			string description = string.Join(" ", cliArguments);

			Priority taskPriority = _priorityFlag.IsSet ? 
				ParseHelper.ParsePriority(_priorityFlag.Value) : 
				Priority.Normal;

			Task newTask = new Task(taskList.Count, description, taskPriority);

			if (this.ConfirmTaskOperation(new List<Task> { newTask }, "added"))
			{
				taskList.Add(newTask);
				return newTask;
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Writes the string representation of the current task (followed by a line terminator) into
		/// the standard output stream or explicitly provided <see cref="TextWriter"/> output. 
		/// For console output, optional background and foreground <see cref="ConsoleColor"/>
		/// parameters can be specified to override the standard colouring scheme.
		/// </summary>
		void DisplayTask(Task task, TextWriter output = null)
		{
			output = output ?? _output;

			string taskPrefix = task.IsFinished ?
				_configuration.FinishedPrefix.GetValue() :
				string.Empty;

			string taskSymbol = string.Empty;

			if (task.Priority == Priority.Important)
			{
				taskSymbol = _configuration.ImportantSymbol.GetValue();
			}
			else if (task.Priority == Priority.Critical)
			{
				taskSymbol = _configuration.CriticalSymbol.GetValue();
			}

			if (task.IsFinished)
			{
				taskSymbol = _configuration.FinishedSymbol.GetValue();
			}

			ConsoleColor oldForegroundColor = Console.ForegroundColor;

			if (output == Console.Out)
			{
				Console.ForegroundColor =
					task.IsFinished ?
						ParseHelper.ParseColor(_configuration.FinishedTaskColor.GetValue()) :
						task.Priority == Priority.Critical ?
							ParseHelper.ParseColor(_configuration.CriticalTaskColor.GetValue()) :
							task.Priority == Priority.Important ?
								ParseHelper.ParseColor(_configuration.ImportantTaskColor.GetValue()) :
								ParseHelper.ParseColor(_configuration.NormalTaskColor.GetValue());
			}

			output.WriteLine(
				"{0}{1,-2}{2}{3,-6}{4}", 
				taskPrefix,
				taskSymbol,
				_configuration.IdPrefix.GetValue(),
				task.ID, 
				task.Description);

			Console.ForegroundColor = oldForegroundColor;
		}
	}
}