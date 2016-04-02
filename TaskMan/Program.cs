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

		Flag<bool> _configurationViewFlag = new Flag<bool>(
			"specifies that the provided configuration parameter should be displayed and not set",
			"view");

		Flag<bool> _interactiveFlag = new Flag<bool>(
			"displays a confirmation prompt before executing an operation (not functional yet)", 
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
				task.Priority == TaskHelper.ParsePriority(flagValue));

		Flag<string> _identityFilterFlag = new TaskFilterFlag<string>(
            "filters tasks by their ID or ID range",
            "i=|id=",
			filterPriority: 1,
			filterPredicate: (flagValue, task) => 
			{
				IEnumerable<int> allowedIds = TaskHelper.ParseId(flagValue);
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

		Alias _switchTaskListAlias;
		Alias _clearTaskListAlias;

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
				supportedFlags: new [] { _configurationGlobalFlag, _configurationViewFlag });

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
					.Concat(new [] { _includeAllFlag, _verboseFlag }));
			
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
			_switchTaskListAlias = new Alias(
				"switch",
				$"{_configureCommand.ExampleUsage} {_configuration.CurrentTaskList.Name}");

			_clearTaskListAlias = new Alias(
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
			tasks.Sort();

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
		void WriteLine(string text)
		{
			if (!_silentFlag.IsSet || !_silentFlag.Value)
			{
				_output.WriteLine(text);
			}
		}

		/// <summary>
		/// Writes the specified string to the output 
		/// unless the silent flag is set.
		/// </summary>
		void WriteLine(string text, params object[] args)
		{
			WriteLine(string.Format(text, args));
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

			this.CurrentOperation = "filter the task list";

			IEnumerable<Task> filteredTasks = taskList;

			if (executingCommand.IsReadUpdateDelete)
			{
				if (!taskList.Any())
				{
					WriteLine(Messages.TaskListIsEmpty);
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
						WriteLine(Messages.NoTasksMatchingGivenConditions);
						return;
					}
				}
			}

			if (executingCommand == _addTaskCommand)
			{
				this.CurrentOperation = "add a new task";

				Task addedTask = AddTask(commandLineArguments, taskList);
				_saveTasks(taskList);

				WriteLine(
					Messages.TaskWasAdded,
					addedTask.Description,
					addedTask.ID,
					addedTask.Priority);
			}
			else if (executingCommand == _displayTasksCommand)
			{
				this.CurrentOperation = "display tasks";

				filteredTasks.ForEach(task => task.Display(_output));
			}
			else if (executingCommand == _deleteTasksCommand)
			{
				this.CurrentOperation = "delete tasks";

				RequireExplicitFiltering(commandName, taskList, filteredTasks);

				taskList = taskList.Except(filteredTasks).ToList();

				_saveTasks(taskList);

				if (!taskList.Any())
				{
					File.Delete(this.CurrentTaskListFile);
				}

				if (filteredTasks.IsSingleton())
				{
					WriteLine(
						Messages.TaskWasDeleted, 
						filteredTasks.Single().ID, 
						filteredTasks.Single().Description);
				}
				else
				{
					WriteLine(
						Messages.TasksWereDeleted,
						filteredTasks.Count());
				}
			}
			else if (executingCommand == _updateTasksCommand)
			{
				this.CurrentOperation = "update task parameters";

				RequireExplicitFiltering(commandName, taskList, filteredTasks);

				UpdateTasks(commandLineArguments, taskList, filteredTasks);
			}
			else if (executingCommand == _completeTasksCommand)
			{
				this.CurrentOperation = "finish tasks";

				RequireExplicitFiltering(commandName, taskList, filteredTasks);

				filteredTasks.ForEach(task => task.IsFinished = true);

				_saveTasks(taskList);

				if (filteredTasks.IsSingleton())
				{
					WriteLine(
						Messages.TaskWasFinished, 
						filteredTasks.Single().ID, 
						filteredTasks.Single().Description);
				}
				else
				{
					WriteLine(
						Messages.TasksWereFinished,
						filteredTasks.Count());
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

				if (_configurationViewFlag.IsSet && 
					_configurationViewFlag.Value)
				{
					WriteLine(_configuration.GetParameter(parameterName));
				}
				else
				{
					if (!commandLineArguments.Any())
					{
						throw new TaskManException(
							Messages.NoParameterValue,
							commandName,
							parameterName);
					}

					string parameterValue = commandLineArguments.PopFirst();

					_configuration.SetParameter(
						parameterName,
						parameterValue,
						_configurationGlobalFlag.IsSet && _configurationGlobalFlag.Value);

					WriteLine(Messages.ParameterWasSetToValue, parameterName, parameterValue);
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
				Priority priority = TaskHelper.ParsePriority(cliArguments.PopFirst());
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
				bool isFinished = TaskHelper.ParseFinished(cliArguments.PopFirst());
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
				WriteLine(
					Messages.TaskWasUpdated,
					tasksToUpdate.Single().ID,
					oldTaskDescription,
					parameterToChange,
					parameterStringValue);
			}
			else
			{
				WriteLine(
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
				TaskHelper.ParsePriority(_priorityFlag.Value) : 
				Priority.Normal;

			Task newTask = new Task(taskList.Count, description, taskPriority);
			taskList.Add(newTask);

			return newTask;
		}
	}
}