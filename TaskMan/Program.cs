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
		static readonly Regex TaskSetDueDateRegex = new Regex(@"^(due|duedate)$", StandardRegexOptions);

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

		Flag<string> _dueDateFlag = new TaskFilterFlag<string>(
			"filters tasks by being due on the specified date or specifies a new task's due date",
			"d=|due=|duedate=",
			filterPriority: 1,
			filterPredicate: (flagValue, task) =>
				task.DueDate == ParseHelper.ParseTaskDueDate(flagValue));

		Flag<string> _dueBeforeFlag = new TaskFilterFlag<string>(
			"filters tasks by being due no later than the specified date",
			"D=|before=",
			filterPriority: 1,
			filterPredicate: (flagValue, task) =>
				task.DueDate <= ParseHelper.ParseTaskDueDate(flagValue));

		Flag<string> _priorityFlag = new TaskFilterFlag<string>(
			"filters tasks by priority or specifies a new task's priority", 
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
			"l=|like=",
			filterPriority: 1,
			filterPredicate: (pattern, task) => Regex.IsMatch(
				task.Description, 
				pattern, 
				RegexOptions.IgnoreCase));

		Flag<int> _numberSkipFlag = new TaskFilterFlag<int>(
            "skips a given number of tasks when displaying the result",
            "skip=",
            filterPriority: 2,
            filterPredicate: (flagValue, task, taskIndex) => taskIndex + 1 > flagValue);

		Flag<int> _numberLimitFlag = new TaskFilterFlag<int>(
			"limits the total number of tasks displayed",
			"n=|limit=",
			filterPriority: 3,
			filterPredicate: (flagValue, task, taskIndex) => taskIndex < flagValue);

		Flag<string> _orderByFlag = new Flag<string>(
			"orders the tasks by the specified criteria",
			"s=|orderby=|sort=");

		Flag<bool> _renumberFlag = new Flag<bool>(
			"before showing tasks, reassign task IDs in the display order",
			"r|renumber");

		Flag<bool> _defaultFlag = new Flag<bool>(
			"resets a parameter to its default value",
			"default|reset");

		#endregion

		#region Command Verbs

		IEnumerable<Command> _commands;

		Command _addTaskCommand;
		Command _deleteTasksCommand; 
		Command _completeTasksCommand;
		Command _reopenTasksCommand;
		Command _displayTasksCommand;
		Command _updateTasksCommand;
		Command _configureCommand;
		Command _listCommand;
		Command _renumberCommand;

		#endregion

		#region Command Aliases

		IEnumerable<Alias> _aliases;

		Alias _switchTaskListAlias;
		Alias _clearTasksAlias;

		#endregion

		#region Program State

		/// <summary>
		/// Mono OptionSet object for command line flag parsing.
		/// </summary>
		private OptionSet _optionSet;

		TextWriter _output = Console.Out;
		TextWriter _error = Console.Error;

		/// <summary>
		/// Sets the function that would be called to read the task list.
		/// Can be used to override the default function that reads the tasks from file, 
		/// e.g. for the purpose of unit testing.
		/// </summary>
		Func<List<Task>> _readTasks;

		/// <summary>
		/// Sets the function that saves the task list.
		/// Can be used to override the default function that saves 
		/// the tasks into file, e.g. for the purpose of unit testing.
		/// </summary>
		Action<List<Task>> _saveTasks;

		TaskmanConfiguration _configuration = new TaskmanConfiguration();

		/// <summary>
		/// Represents the unparsed remainder of the command 
		/// line arguments.
		/// </summary>
		LinkedList<string> _commandLineArguments;

		/// <summary>
		/// Represents the subset of tasks relevant for the
		/// current operation.
		/// </summary>
		IEnumerable<Task> _filteredTasks;

		/// <summary>
		/// Represents the whole task list as read from
		/// the task list file.
		/// </summary>
		List<Task> _allTasks;

		#endregion

		#region Properties

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
					_configuration.GetValue(_configuration.CurrentTaskList.Name) + ".tmf");
			}
		}

		#endregion
					
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
				supportedFlags: new [] { _configurationGlobalFlag, _interactiveFlag, _defaultFlag });

			_addTaskCommand = new Command(
				@"^(add|new|create)$", 
				isReadUpdateDelete: false,
				supportedFlags: 
					new Flag[] { _interactiveFlag, _dueDateFlag, _priorityFlag, _silentFlag, _verboseFlag });

			_deleteTasksCommand = new Command(
				@"^(delete|remove)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag));

			_completeTasksCommand = new Command(
				@"^(complete|finish|accomplish)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag));

			_reopenTasksCommand = new Command(
				@"^(uncomplete|unfinish|reopen)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag));

			_displayTasksCommand = new Command(
				@"^(show|display|view)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Concat(_includeAllFlag, _verboseFlag, _orderByFlag, _renumberFlag));
			
			_updateTasksCommand = new Command(
				@"^(update|change|modify|set)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag));

			_listCommand = new Command(
				@"^(list)$",
				isReadUpdateDelete: false,
				supportedFlags: new Flag[0]);

			_renumberCommand = new Command(
				@"^(renumber)$",
				isReadUpdateDelete: false,
				supportedFlags: new [] { _orderByFlag });

			_commands = privateFields
				.Where(fieldInfo => fieldInfo.FieldType == typeof(Command))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Command>();

			// Setup program aliases
			// -
			_switchTaskListAlias = new Alias(
				"switch",
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
		bool ConfirmTaskOperation(string willBe, IEnumerable<Task> relevantTasks = null)
		{
			relevantTasks = relevantTasks ?? _filteredTasks;

			StringWriter actionDescription = new StringWriter();

			actionDescription.WriteLine(
				Messages.TheFollowingObjectWillBeAction,
				"tasks",
				willBe);

			relevantTasks.Take(3).ForEach((task, isFirstTask, isLastTask) 
				=> DisplayTask(task, isFirstTask, isLastTask, actionDescription));

			if (relevantTasks.Skip(3).Any())
			{
				actionDescription.WriteLine(
					Messages.AndNumberMore, 
					_filteredTasks.Skip(3).Count());
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

			_commandLineArguments =
				new LinkedList<string>(_optionSet.Parse(originalArguments));

			string commandName = _commandLineArguments.First?.Value;

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
					_commandLineArguments = new LinkedList<string>(new [] { commandName }); 
				}
			}

			_commandLineArguments.RemoveFirst();

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

			_allTasks = _readTasks();

			this.CurrentOperation = "sort the task list";

			string sortingSteps = _orderByFlag.IsSet ?
				_orderByFlag.Value :
				_configuration.SortOrder.Value;

			_allTasks.Sort(
				Task.GetComparison(
					ParseHelper.ParseTaskSortOrder(
						sortingSteps)));

			this.CurrentOperation = "renumber tasks";

			if (executingCommand == _renumberCommand ||
				_renumberFlag.IsSet && _renumberFlag)
			{
				_allTasks.ForEach((task, index) => task.ID = index);
			}
	
			this.CurrentOperation = "filter the task list";

			_filteredTasks = _allTasks;

			if (executingCommand.IsReadUpdateDelete)
			{
				if (!_allTasks.Any())
				{
					OutputWriteLine(Messages.TaskListIsEmpty);
					return;
				}

				IEnumerable<ITaskFilter> filterFlagsSpecified = _flags
					.Where(flag => flag.IsSet)
					.OfType<ITaskFilter>();

				if (filterFlagsSpecified.Any())
				{
					_filteredTasks = filterFlagsSpecified
						.OrderBy(taskFilter => taskFilter.FilterPriority)
						.Aggregate(
							seed: _allTasks as IEnumerable<Task>, 
							func: (taskSequence, filter) => filter.Filter(taskSequence));

					if (!_filteredTasks.Any())
					{
						OutputWriteLine(Messages.NoTasksMatchingGivenConditions);
						return;
					}
				}
			}

			if (executingCommand == _addTaskCommand)
			{
				this.CurrentOperation = "add a new task";

				Task addedTask = AddTask();

				// Only when the user discarded the task 
				// creation in an interactive mode.
				// -
				if (addedTask == null) return;

				_saveTasks(_allTasks);

				string taskDueDate =
					addedTask.DueDate?.ToString("ddd, yyyy-MM-dd");

				OutputWriteLine(
					Messages.TaskWasAdded,
					addedTask.Description,
					addedTask.ID,
					addedTask.Priority.ToString().ToLower(),
					taskDueDate != null ?
						$", due on {taskDueDate}." :
						".");
			}
			else if (executingCommand == _displayTasksCommand)
			{
				this.CurrentOperation = "display tasks";

				RequireNoMoreArguments();

				_filteredTasks.ForEach((task, isFirstTask, isLastTask)
					=> DisplayTask(task, isFirstTask, isLastTask));

				if (_renumberFlag.IsSet && _renumberFlag)
				{
					// Only makes sense to save
					// if renumbering happened.
					// -
					_saveTasks(_allTasks);
				}
			}
			else if (executingCommand == _deleteTasksCommand)
			{
				this.CurrentOperation = "delete tasks";

				RequireExplicitFiltering(commandName);
				RequireNoMoreArguments();

				if (!ConfirmTaskOperation("deleted")) return;

				int totalTasksBefore = _allTasks.Count;

				_allTasks = _allTasks.Except(_filteredTasks).ToList();
				_allTasks.ForEach((task, index) => task.ID = index);

				int totalTasksAfter = _allTasks.Count;

				_saveTasks(_allTasks);

				if (!_allTasks.Any())
				{
					File.Delete(this.CurrentTaskListFile);
				}

				if (_filteredTasks.IsSingleton())
				{
					OutputWriteLine(
						Messages.TaskWasDeleted,
						_filteredTasks.Single().ID,
						_filteredTasks.Single().Description);
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

				RequireExplicitFiltering(commandName);

				if (!ConfirmTaskOperation("updated")) return;

				UpdateTasks();
			}
			else if (executingCommand == _completeTasksCommand)
			{
				this.CurrentOperation = "finish tasks";

				RequireExplicitFiltering(commandName);
				RequireNoMoreArguments();

				if (!ConfirmTaskOperation("completed")) return;

				int totalTasksFinished =
					_filteredTasks.ForEach(task => task.IsFinished = true);

				_saveTasks(_allTasks);

				if (_filteredTasks.IsSingleton())
				{
					OutputWriteLine(
						Messages.TaskWasFinished,
						_filteredTasks.Single().ID,
						_filteredTasks.Single().Description);
				}
				else
				{
					OutputWriteLine(
						Messages.TasksWereFinished,
						totalTasksFinished);
				}
			}
			else if (executingCommand == _reopenTasksCommand)
			{
				this.CurrentOperation = "reopen tasks";

				RequireExplicitFiltering(commandName);
				RequireNoMoreArguments();

				if (!ConfirmOperation("reopened")) return;

				int totalTasksReopened =
					_filteredTasks.ForEach(task => task.IsFinished = false);

				_saveTasks(_allTasks);

				if (_filteredTasks.IsSingleton())
				{
					OutputWriteLine(
						Messages.TaskWasReopened,
						_filteredTasks.Single().ID,
						_filteredTasks.Single().Description);
				}
				else
				{
					OutputWriteLine(
						Messages.TasksWereReopened,
						totalTasksReopened);
				}
			}
			else if (executingCommand == _configureCommand)
			{
				this.CurrentOperation = "configure program parameters";

				ConfigureProgramParameters();
			}
			else if (executingCommand == _listCommand)
			{
				this.CurrentOperation = "display available task lists";

				if (_commandLineArguments.Any())
				{
					// If another argument remains, it is the
					// new task list name.
					// -.
					string newListName = _commandLineArguments.PopFirst();

					RequireNoMoreArguments();

					this.Run(new []
					{
						_configureCommand.ExampleUsage,
						_configuration.CurrentTaskList.Name,
						newListName
					});

					return;
				}

				IEnumerable<string> taskListFiles = Directory.EnumerateFiles(
					_configuration.UserConfigurationDirectory,
					"*.tmf",
					SearchOption.TopDirectoryOnly);

				OutputWriteLine(
					Messages.CurrentTaskList,
					_configuration.CurrentTaskList.Value);

				OutputWriteLine(Messages.AvailableTaskLists);

				taskListFiles
					.Select(fileName => Path.GetFileNameWithoutExtension(fileName))
					.OrderBy(listName => listName)
					.ForEach((listName, index) => OutputWriteLine($"{index + 1}. {listName}"));
			}
			else if (executingCommand == _renumberCommand)
			{
				RequireNoMoreArguments();

				// Renumbering already happened earlier.
				// Just display the confirmation message 
				// and save the list.
				// -
				_saveTasks(_allTasks);
				OutputWriteLine(Messages.TasksWereRenumbered, _allTasks.Count);
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

				StringWriter optionDescriptions = new StringWriter();
				_optionSet.WriteOptionDescriptions(optionDescriptions);

				_output.WriteLine(
					Assembly.GetExecutingAssembly().GetResourceText("TaskMan.HELP.txt"),
					optionDescriptions);

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

		void ConfigureProgramParameters()
		{
			if (!_commandLineArguments.Any())
			{
				OutputWriteLine("Available configuration parameters: ");

				TableWriter tableWriter = new TableWriter(
					_output,
					TableBorders.None,
					new FieldRule(2, paddingRight: 2),
					new FieldRule(16, paddingRight: 2),
					new FieldRule(40, lineBreaking: LineBreaking.Whitespace, paddingRight: 2),
					new FieldRule(15, lineBreaking: LineBreaking.Whitespace));

				_configuration.SupportedParameters.OrderBy(parameter => parameter.Name).ForEach(
					(parameter, index, isLast) =>
				{
					tableWriter.WriteLine(
						index == 0,
						isLast,
						index + 1,
						parameter.Name,
						parameter.Description,
						$"'{parameter.Value}'");
				});

				return;
			}

			string parameterName = _commandLineArguments.PopFirst();

			if (!_commandLineArguments.Any() && _defaultFlag.IsSet && _defaultFlag)
			{
				string defaultValue = _configuration.GetDefaultValue(parameterName);

				if (!ConfirmOperation(
					Messages.ParameterNameWillBeSetToValue,
					parameterName,
					defaultValue))
				{
					return;
				}


				// No parameter value, but default flag
				// is set, which means we should reset the 
				// parameter to its default value.
				// -
				_configuration.SetParameter(
					parameterName,
					defaultValue,
					_configurationGlobalFlag.IsSet && _configurationGlobalFlag);

				OutputWriteLine(
					Messages.ParameterResetToDefault,
					parameterName,
					defaultValue);
			}
			else if (!_commandLineArguments.Any() && !_defaultFlag.IsSet)
			{
				// Just show the parameter.
				// 
				OutputWriteLine(
					Messages.CurrentUserValueOfParameter,
					parameterName,
					_configuration.GetValue(parameterName));
				
				OutputWriteLine(
					Messages.CurrentGlobalValueOfParameter,
					parameterName,
					_configuration.GetValue(parameterName, forceGetGlobal: true) ?? "N/A");

				OutputWriteLine(
					Messages.DefaultValueOfParameter,
					_configuration.GetDefaultValue(parameterName));
			}
			else
			{
				// Set the parameter value explicitly
				// provided by the user.
				// -
				if (_defaultFlag.IsSet)
				{
					throw new TaskManException(
						Messages.EntityDoesNotMakeSenseWithEntity,
						_configureCommand.ExampleUsage,
						_defaultFlag.ExampleUsage);
				}

				string parameterValue = _commandLineArguments.PopFirst();
				RequireNoMoreArguments();

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
		void RequireExplicitFiltering(string commandName)
		{
			if (!_includeAllFlag.IsSet && _filteredTasks == _allTasks)
			{
				throw new TaskManException(
					Messages.NoFilterConditionsUseAllIfIntended,
					commandName);
			}
		}

		/// <summary>
		/// Encapsulates the task modification logic in one method.
		/// </summary>
		void UpdateTasks()
		{
			if (!_commandLineArguments.HasAtLeastTwoElements())
			{ 
				throw new TaskManException(Messages.InsufficientUpdateParameters);
			}
			
			string parameterToChange = _commandLineArguments.PopFirst().ToLower();
			string parameterStringValue;

			// Preserve old task description for better human-readable
			// message in case we're updating a single task's description.
			// -
			string oldTaskDescription = null;

			if (_filteredTasks.IsSingleton())
			{
				oldTaskDescription = _filteredTasks.Single().Description;
			}

			int totalTasksUpdated;

			if (TaskSetPriorityRegex.IsMatch(parameterToChange))
			{
				Priority priority = ParseHelper.ParsePriority(_commandLineArguments.PopFirst());
				parameterStringValue = priority.ToString();

				RequireNoMoreArguments();

				totalTasksUpdated = _filteredTasks.ForEach(task => task.Priority = priority);
			}
			else if (TaskSetDescriptionRegex.IsMatch(parameterToChange))
			{
				parameterStringValue = string.Join(" ", _commandLineArguments);

				totalTasksUpdated = _filteredTasks.ForEach(task => task.Description = parameterStringValue);
			}
			else if (TaskSetFinishedRegex.IsMatch(parameterToChange))
			{
				bool isFinished = ParseHelper.ParseBool(_commandLineArguments.PopFirst());
				parameterStringValue = isFinished.ToString();

				RequireNoMoreArguments();

				totalTasksUpdated = _filteredTasks.ForEach(task => task.IsFinished = isFinished);
			}
			else if (TaskSetDueDateRegex.IsMatch(parameterToChange))
			{
				DateTime dueDate = ParseHelper.ParseTaskDueDate(_commandLineArguments.PopFirst());
				parameterStringValue = dueDate.ToString("MMMMM dd, yyyy");

				RequireNoMoreArguments();

				totalTasksUpdated = _filteredTasks.ForEach(task => task.DueDate = dueDate);
			}
			else
			{
				throw new TaskManException(Messages.InvalidSetParameters);
			}

			this._saveTasks(_allTasks);

			if (totalTasksUpdated == 1)
			{
				OutputWriteLine(
					Messages.TaskWasUpdated,
					_filteredTasks.Single().ID,
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
		Task AddTask()
		{
			if (!_commandLineArguments.Any())
			{
				throw new TaskManException(Messages.NoDescriptionSpecified);
			}

			string description = string.Join(" ", _commandLineArguments);

			Priority priority = _priorityFlag.IsSet ? 
				ParseHelper.ParsePriority(_priorityFlag.Value) : 
				Priority.Normal;

			DateTime? dueDate = _dueDateFlag.IsSet ?
				ParseHelper.ParseTaskDueDate(_dueDateFlag.Value) :
                null as DateTime?;

			Task newTask = new Task(
				_allTasks.Count, 
				description, 
				priority, 
				dueDate);

			if (this.ConfirmTaskOperation("added", new List<Task> { newTask }))
			{
				_allTasks.Add(newTask);
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
		void DisplayTask(Task task, bool isFirstTask, bool isLastTask, TextWriter output = null)
		{
			output = output ?? _output;

			string taskPrefix =
				task.IsFinished ?
					_configuration.FinishedSymbol.Value :
					task.Priority == Priority.Important ?
						_configuration.ImportantSymbol.Value :
						task.Priority == Priority.Critical ?
							_configuration.CriticalSymbol.Value :
							string.Empty;

			ConsoleColor oldForegroundColor = Console.ForegroundColor;

			if (output == Console.Out)
			{
				Console.ForegroundColor =
					task.IsFinished ?
						ParseHelper.ParseColor(_configuration.FinishedTaskColor.Value) :
						task.Priority == Priority.Critical ?
							ParseHelper.ParseColor(_configuration.CriticalTaskColor.Value) :
							task.Priority == Priority.Important ?
								ParseHelper.ParseColor(_configuration.ImportantTaskColor.Value) :
								ParseHelper.ParseColor(_configuration.NormalTaskColor.Value);
			}

			TableWriter tableWriter = new TableWriter(
				output,
				TableBorders.None,
				new FieldRule(2, LineBreaking.Anywhere, Align.Left, paddingLeft: 0, paddingRight: 1),
				new FieldRule(5, LineBreaking.Anywhere, Align.Left, paddingLeft: 0, paddingRight: 1),
				new FieldRule(45, LineBreaking.Whitespace, Align.Left, paddingLeft: 1, paddingRight: 1),
				new FieldRule(20, LineBreaking.Whitespace, Align.Left, paddingLeft: 1, paddingRight: 1));

			tableWriter.WriteLine(
				isFirstTask,
				isLastTask,
				taskPrefix,
				task.ID,
				task.Description,
				GetDueDateRepresentation(task.DueDate));

			Console.ForegroundColor = oldForegroundColor;
		}

		/// <summary>
		/// Asserts that command line arguments
		/// remainder is empty, or throws exception.
		/// </summary>
		void RequireNoMoreArguments()
		{
			if (_commandLineArguments.Any())
			{
				throw new TaskManException(
					Messages.UnknownCommandLineArguments,
					string.Join(" ", _commandLineArguments));
			}
		}

		/// <summary>
		/// Gets the due date representation for a given
		/// task, using a special string value for today
		/// or tomorrow.
		/// </summary>
		static string GetDueDateRepresentation(DateTime? dueDate)
		{
			if (dueDate == null) return string.Empty;
			if (dueDate.Value.Date == DateTime.Today) return "Today";
			if (dueDate.Value.Date == DateTime.Today.AddDays(1)) return "Tomorrow";

			return dueDate.Value.ToString("MMMMM d, yyyy");
		}
	}
}