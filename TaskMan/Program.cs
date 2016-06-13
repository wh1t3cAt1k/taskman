/*
 * taskman - convenient command line to-do list.
 * 
 * copyright (c) 2016 Pavel Kabir
 * 
 * This file is part of taskman.
 * 
 * taskman is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Script.Serialization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

using CsvHelper;
using Mono.Options;

using TaskMan.Control;
using TaskMan.Objects;

namespace TaskMan
{
	public class Program
	{
		public static void Main(string[] arguments)
		{
			bool shellMode = 
				arguments.Length == 1 &&
				Regex.IsMatch(arguments[0], "^(shell|repl)$", RegexOptions.IgnoreCase);

			if (shellMode)
			{
				Console.WriteLine(Messages.EnteringShell);
			}

			do
			{
				if (shellMode)
				{
					Console.Write(">> ");
					arguments = StringExtensions.SplitCommandLine(Console.ReadLine()).ToArray();

					if (arguments.Any())
					{
						if (Regex.IsMatch(arguments.First(), "^(exit|quit)$", RegexOptions.IgnoreCase))
						{
							Console.WriteLine(Messages.ExitingShell);
							return;
						}
						else if (Regex.IsMatch(arguments.First(), "^cls$", RegexOptions.IgnoreCase))
						{
							Console.Clear();
							continue;
						}
						else if (Regex.IsMatch(arguments.First(), "^taskman$", RegexOptions.IgnoreCase))
						{
							Console.WriteLine(Messages.RecursionIsProhibited);
							continue;
						}
					}
				}

				new TaskMan().RunTaskman(arguments);
			}
			while (shellMode);
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

		Flag<bool> _displayHelpFlag;
		Flag<bool> _displayLicenseFlag;
		Flag<bool> _displayVersionFlag;
		Flag<bool> _configurationGlobalFlag;
		Flag<bool> _interactiveFlag;
		Flag<bool> _verboseFlag;
		Flag<bool> _silentFlag;
		Flag<bool> _includeAllFlag;
		Flag<bool> _pendingFilterFlag;
		Flag<bool> _finishedFilterFlag;
		Flag<bool> _renumberFlag;
		Flag<bool> _defaultFlag;

		Flag<string> _dueDateFlag;
		Flag<string> _dueBeforeFlag;
		Flag<string> _priorityFlag;
		Flag<string> _identityFilterFlag;
		Flag<string> _descriptionFilterFlag;
		Flag<string> _orderByFlag;
		Flag<string> _listOverrideFlag;

		Flag<int> _numberSkipFlag;
		Flag<int> _numberLimitFlag;

		Flag<Format> _formatFlag;
		Flag<ImportBehaviour> _importBehaviourFlag;

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
		Command _importCommand;

		#endregion

		#region Command Aliases

		IEnumerable<Alias> _aliases;

		Alias _switchTaskListAlias;
		Alias _clearTasksAlias;
		Alias _helpAlias;
		Alias _versionAlias;
		Alias _licenseAlias;
		Alias _showPendingAlias;
		Alias _showFinishedAlias;

		#endregion

		#region Program State

		/// <summary>
		/// Mono OptionSet object for command line flag parsing.
		/// </summary>
		private OptionSet _optionSet;

		TextReader _input = Console.In;
		TextWriter _output = Console.Out;
		TextWriter _error = Console.Error;

		Command _executingCommand;
		string _executingCommandName;

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
		LinkedList<string> _parsedArguments;

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
		public bool IsVerbose =>
			this._verboseFlag.IsSet &&
			this._verboseFlag.Value;

		/// <summary>
		/// Gets the error stream used by this instance.
		/// </summary>
		public TextWriter ErrorStream => _error;

		/// <summary>
		/// Gets the full filename of the file that stores
		/// the current task list. Does not guarantee that 
		/// the file exists.
		/// </summary>
		private string CurrentTaskListFile
		{
			get
			{
				string taskListName =
					_listOverrideFlag.IsSet
						? _listOverrideFlag.Value
						: _configuration.CurrentTaskList.Value;

				_configuration.CurrentTaskList.Validate(taskListName);

				return Path.Combine(
					_configuration.UserConfigurationDirectory,
					$"{taskListName}.tmf");
			}
		}

		#endregion
					
		public TaskMan(
			Func<List<Task>> taskReadFunction = null,
			Action<List<Task>> taskSaveFunction = null,
			TextReader inputStream = null,
			TextWriter outputStream = null,
			TextWriter errorStream = null)
		{
			this._optionSet = new OptionSet();

			// Collect non-public instance fields
			// and initialize flags / commands / aliases
			// - 
			IEnumerable<FieldInfo> privateFields = 
				typeof(TaskMan).GetFields(BindingFlags.NonPublic | BindingFlags.Instance);

			InitializeFlags(privateFields);
			InitializeCommands(privateFields);
			InitializeAliases(privateFields);

			// Setup IO
			// -
			_readTasks = taskReadFunction ?? this.ReadTasksFromFile;
			_saveTasks = taskSaveFunction ?? this.SaveTasksIntoFile;

			_input = inputStream ?? _input;
			_output = outputStream ?? _output;
			_error = errorStream ?? _error;
		}

		private void InitializeFlags(IEnumerable<FieldInfo> privateFields)
		{
			_displayHelpFlag = new Flag<bool>(
				"displays TaskMan's help text",
				"?|help");

			_displayLicenseFlag = new Flag<bool>(
				"displays TaskMan's licensing terms",
				"license");

			_displayVersionFlag = new Flag<bool>(
				"displays TaskMan's version",
				"version");

			_configurationGlobalFlag = new Flag<bool>(
				"specifies that the provided configuration parameter should be set globally and not just for the current user",
				"G|global");

			_interactiveFlag = new Flag<bool>(
				"displays a confirmation prompt before executing an operation",
				"I|interactive");

			_verboseFlag = new Flag<bool>(
				"increase program verbosity",
				"v|verbose");

			_silentFlag = new Flag<bool>(
				"do not display any messages except errors",
				"S|silent");

			_includeAllFlag = new Flag<bool>(
				"forces an operation to be executed upon all tasks",
				"A|all");

			_dueDateFlag = new TaskFilterFlag<string>(
				"filters tasks by being due on the specified date or specifies a new task's due date",
				"d=|due=|duedate=",
				filterPriority: 1,
				filterPredicate: (flagValue, task) =>
					task.DueDate == ParseHelper.ParseTaskDueDate(flagValue));

			_dueBeforeFlag = new TaskFilterFlag<string>(
				"filters tasks by being due no later than the specified date",
				"D=|before=",
				filterPriority: 1,
				filterPredicate: (flagValue, task) =>
					task.DueDate <= ParseHelper.ParseTaskDueDate(flagValue));

			_priorityFlag = new TaskFilterFlag<string>(
				"filters tasks by priority or specifies a new task's priority",
				"p=|priority=",
				filterPriority: 1,
				filterPredicate: (flagValue, task) =>
					task.Priority == ParseHelper.ParsePriority(flagValue));

			_identityFilterFlag = new TaskFilterFlag<string>(
				"filters tasks by their ID or ID range",
				"i=|id=",
				filterPriority: 1,
				filterPredicate: (flagValue, task) =>
				{
					IEnumerable<int> allowedIds = ParseHelper.ParseTaskId(flagValue);
					return allowedIds.Contains(task.ID);
				});

			_pendingFilterFlag = new TaskFilterFlag<bool>(
				"filters out any finished tasks",
				"P|pending|unfinished",
				filterPriority: 1,
				filterPredicate: (_, task) => task.IsFinished == false);

			_finishedFilterFlag = new TaskFilterFlag<bool>(
				"filters out any unfinished tasks",
				"F|finished|completed",
				filterPriority: 1,
				filterPredicate: (_, task) => task.IsFinished == true);

			_descriptionFilterFlag = new TaskFilterFlag<string>(
				"filters tasks by their description matching a regex",
				"l=|like=",
				filterPriority: 1,
				filterPredicate: (pattern, task) => Regex.IsMatch(
					task.Description,
					pattern,
					RegexOptions.IgnoreCase));

			_numberSkipFlag = new TaskFilterFlag<int>(
				"skips a given number of tasks when displaying the result",
				"skip=",
				filterPriority: 2,
				filterPredicate: (flagValue, task, taskIndex) => taskIndex + 1 > flagValue);

			_numberLimitFlag = new TaskFilterFlag<int>(
				"limits the total number of tasks displayed",
				"n=|limit=",
				filterPriority: 3,
				filterPredicate: (flagValue, task, taskIndex) => taskIndex < flagValue);

			_orderByFlag = new Flag<string>(
				"orders the tasks by the specified criteria",
				"s=|orderby=|sort=");

			_renumberFlag = new Flag<bool>(
				"before showing tasks, reassign task IDs in the display order",
				"r|renumber");

			_defaultFlag = new Flag<bool>(
				"resets a parameter to its default value",
				"default|reset");

			_formatFlag = new Flag<Format>(
				"specifies the input / output format for tasks: text, csv, json or xml.",
				"format=");

			_importBehaviourFlag = new Flag<ImportBehaviour>(
				"specifies the import behaviour for import command",
				"importbehaviour=");

			_listOverrideFlag = new Flag<string>(
				"explicitly specifies the target task list for the current operation.",
				"L=|list=");

			_flags = privateFields
				.Where(fieldInfo => typeof(Flag).IsAssignableFrom(fieldInfo.FieldType))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Flag>();

			_flags.ForEach(flag => flag.AddToOptionSet(this._optionSet));
		}

		private void InitializeCommands(IEnumerable<FieldInfo> privateFields)
		{
			_configureCommand = new Command(
				"configure program parameters",
				@"^(config|configure)$",
				isReadUpdateDelete: false,
				supportedFlags: new[] { _configurationGlobalFlag, _interactiveFlag, _defaultFlag },
				action: ConfigureProgramParameters);

			_addTaskCommand = new Command(
				"add a new task",
				@"^(add|new|create)$",
				isReadUpdateDelete: false,
				supportedFlags: 
					new Flag[] { _interactiveFlag, _dueDateFlag, _priorityFlag, _silentFlag, _verboseFlag, _listOverrideFlag },
				action: AddTask);

			_deleteTasksCommand = new Command(
				"delete tasks",
				@"^(delete|remove)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag, _listOverrideFlag),
				action: DeleteTasks);

			_completeTasksCommand = new Command(
				"finish tasks",
				@"^(complete|finish|accomplish)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag, _listOverrideFlag),
				action: FinishTasks);

			_reopenTasksCommand = new Command(
				"reopen tasks",
				@"^(uncomplete|unfinish|reopen)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag),
				action: ReopenTasks);

			_displayTasksCommand = new Command(
				"display / output tasks",
				@"^(show|display|write|view)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Concat(_includeAllFlag, _verboseFlag, _orderByFlag, _renumberFlag, _formatFlag, _listOverrideFlag),
				action: DisplayTasks);

			_updateTasksCommand = new Command(
				"update task parameters",
				@"^(update|change|modify|set)$",
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(_numberLimitFlag, _numberSkipFlag)
					.Concat(_interactiveFlag, _includeAllFlag, _silentFlag, _verboseFlag, _listOverrideFlag),
				action: UpdateTasks);

			_listCommand = new Command(
				"view available task lists or change the current list",
				@"^(list)$",
				isReadUpdateDelete: false,
				action: DisplayOrChangeTaskList);

			_renumberCommand = new Command(
				"renumber tasks",
				@"^(renumber)$",
				isReadUpdateDelete: false,
				supportedFlags: new[] { _orderByFlag, _listOverrideFlag });

			_importCommand = new Command(
				"import tasks",
				@"^(import|read)$",
				isReadUpdateDelete: false,
				supportedFlags: new Flag[] { _importBehaviourFlag, _listOverrideFlag },
				requiredFlags: new Flag[] { _formatFlag }, 
				action: ImportTasks);

			_commands = privateFields
				.Where(fieldInfo => fieldInfo.FieldType == typeof(Command))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Command>()
				.Except(_executingCommand);
		}

		private void InitializeAliases(IEnumerable<FieldInfo> privateFields)
		{
			_switchTaskListAlias = new Alias(
				"switch",
				$"{_configureCommand.Usage} {_configuration.CurrentTaskList.Name}");

			_clearTasksAlias = new Alias(
				"clear",
				$"{_deleteTasksCommand.Usage} {_includeAllFlag.Usage} {_interactiveFlag.Usage}");

			_helpAlias = new Alias(
				"help",
				$"{_displayHelpFlag.Usage}");

			_versionAlias = new Alias(
				"version",
				$"{_displayVersionFlag.Usage}");

			_licenseAlias = new Alias(
				"license",
				$"{_displayLicenseFlag.Usage}");

			_showPendingAlias = new Alias(
				"showp",
				$"{_displayTasksCommand.Usage} {_pendingFilterFlag.Usage}");

			_showFinishedAlias = new Alias(
				"showf",
				$"{_displayTasksCommand.Usage} {_pendingFilterFlag.Usage}");

			_aliases = privateFields
				.Where(fieldInfo => fieldInfo.FieldType == typeof(Alias))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Alias>();
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
			if (_silentFlag.IsSet && _silentFlag) return;

			_output.Write(string.Format(text, args));
		}

		/// <summary>
		/// Writes the specified string to the output 
		/// unless the silent flag is set. Breaks the 
		/// line on a whitespace character if it exceeds
		/// the configured output width.
		/// </summary>
		void OutputWriteLine(string text, params object[] args)
		{
			if (_silentFlag.IsSet && _silentFlag) return;

			int outputWidth = int.Parse(_configuration.OutputWidth.Value);

			string
				.Format(text, args)
				.MakeLinesByWhitespace(outputWidth)
				.ForEach(line =>
				{
					OutputWrite(line);
					OutputWrite("\n");
				});
		}

		/// <summary>
		/// Confirms with the user that the described action will
		/// be executed.
		/// </summary>
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
				
				confirmationResult = ConfirmActionRegex.IsMatch(_input.ReadLine());
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
				=> DisplayTaskTabular(task, isFirstTask, isLastTask, actionDescription));

			if (relevantTasks.Skip(3).Any())
			{
				actionDescription.WriteLine(
					Messages.AndNumberMore, 
					_filteredTasks.Skip(3).Count());
			}

			return ConfirmOperation(actionDescription.ToString().TrimEnd('\n'));
		}

		void SignalizeOperationSuccess(
			string whatWasDone, 
			int totalTasksAffected, 
			IEnumerable<Task> relevantTasks)
		{
			if (relevantTasks.IsSingleton())
			{
				OutputWriteLine(
					Messages.TaskWasSomething,
					relevantTasks.Single().ID,
					relevantTasks.Single().Description,
					whatWasDone);
			}
			else
			{
				OutputWriteLine(
					Messages.TasksWereSomething,
					totalTasksAffected,
					whatWasDone);
			}
		}

		public void RunTaskman(IEnumerable<string> arguments)
		{
			try
			{
				Run(arguments);
			}
			catch (Exception exception)
			{
				if (this.IsVerbose)
				{
					_error.WriteLine(
						Messages.ErrorPerformingOperation,
						this.CurrentOperation,
						exception.Message.DecapitaliseFirstLetter());

					_error.WriteLine(Messages.ExceptionStackTrace);
					_error.Write(exception.StackTrace);
				}
				else
				{
					_error.WriteLine(
						Messages.Error,
						exception.Message.DecapitaliseFirstLetter());
				}
			}
		}

		void Run(IEnumerable<string> arguments)
		{
			if (arguments.Any())
			{
				this.CurrentOperation = "expand aliases";

				IEnumerable<Alias> matchingAliases = 
					_aliases.Where(alias => alias.Name == arguments.First());

				if (matchingAliases.Any())
				{
					// Replace the alias with its expansion.
					// -
					arguments = Enumerable.Concat(
						matchingAliases.Single().ExpansionArray,
						arguments.Skip(1));
				}
			}

			this.CurrentOperation = "parse command line arguments";

			_parsedArguments =
				new LinkedList<string>(_optionSet.Parse(arguments));

			_executingCommandName = _parsedArguments.First?.Value;

			if (_executingCommandName == null)
			{
				if (HandleGlobalFlags())
				{
					return;
				}

				// TaskMan operates in the display mode by default.
				// --
				_executingCommandName = _displayTasksCommand.Usage;

				_parsedArguments = new LinkedList<string>(new [] 
				{ 
					_executingCommandName
				});
			}

			_parsedArguments.RemoveFirst();

			this.CurrentOperation = "recognize the command";

			IEnumerable<Command> matchingCommands = _commands.Matching(_executingCommandName);

			if (!matchingCommands.Any())
			{
				IEnumerable<string> similarCommandNames = _commands.SimilarNames(
					_executingCommandName,
					maximumEditDistance: 2);

				string errorMessage = Messages.UnknownCommand;

				if (similarCommandNames.Any())
				{
					errorMessage += " " + string.Format(
						Messages.DidYouMean,
						similarCommandNames.First());
				}

				throw new TaskManException(errorMessage, _executingCommandName);
			}

			if (!matchingCommands.IsSingleton())
			{
				IEnumerable<string> matchingNames = matchingCommands
					.SelectMany(command => PrototypeHelper.GetComponents(command.Prototype))
					.Where(name => name.StartsWith(_executingCommandName, StringComparison.OrdinalIgnoreCase));

				throw new TaskManException(
					Messages.MoreThanOneCommandMatchesInput,
					string.Join(", ", matchingNames.Select(name => $"'{name}'")));
			}

			_executingCommand = matchingCommands.Single();

			if (!PrototypeHelper
			    .GetComponents(_executingCommand.Prototype)
				.Any(name => name.Equals(_executingCommandName, StringComparison.OrdinalIgnoreCase)))
			{
				_executingCommandName = PrototypeHelper
					.GetComponents(_executingCommand.Prototype)
					.First(name => name.StartsWith(_executingCommandName, StringComparison.OrdinalIgnoreCase));

				if (this.IsVerbose)
				{
					OutputWriteLine(Messages.AssumingCommand, _executingCommandName);
				}
			}

			this.CurrentOperation = "ensure flag consistency";

			EnsureFlagConsistency(_executingCommand, _executingCommandName, arguments);

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

			this.CurrentOperation = _renumberCommand.Description;

			if (_executingCommand == _renumberCommand ||
				_renumberFlag.IsSet && _renumberFlag)
			{
				this.CurrentOperation = _executingCommand.Description;
				_allTasks.ForEach((task, index) => task.ID = index);
			}
	
			this.CurrentOperation = "filter the task list";

			_filteredTasks = _allTasks;

			if (_executingCommand.IsReadUpdateDelete)
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

			if (_executingCommand == _renumberCommand)
			{
				RequireNoMoreArguments();

				// Renumbering already happened earlier.
				// Just display the confirmation message 
				// and save the list.
				// -
				_saveTasks(_allTasks);
				OutputWriteLine(Messages.TasksWereRenumbered, _allTasks.Count);
			}
			else
			{
				this.CurrentOperation = _executingCommand.Description;
				_executingCommand.Action();
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
			if (!_parsedArguments.Any())
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

			string parameterName = _parsedArguments.PopFirst();

			if (!_parsedArguments.Any() && _defaultFlag.IsSet && _defaultFlag)
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
			else if (!_parsedArguments.Any() && !_defaultFlag.IsSet)
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
						_configureCommand.Usage,
						_defaultFlag.Usage);
				}

				string parameterValue = _parsedArguments.PopFirst();
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

		void DisplayOrChangeTaskList()
		{
			if (_parsedArguments.Any())
			{
				// If another argument remains, it is the
				// new task list name.
				// -
				string newListName = _parsedArguments.PopFirst();

				RequireNoMoreArguments();

				new TaskMan(_readTasks, _saveTasks, _input, _output, _error)
					.Run(new[]
				{
						_configureCommand.Usage,
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

			IEnumerable<string> availableNonEmptyLists = taskListFiles
				.Select(fileName => Path.GetFileNameWithoutExtension(fileName))
				.OrderBy(listName => listName);

			if (availableNonEmptyLists.Any())
			{
				OutputWriteLine(Messages.AvailableTaskLists);
				availableNonEmptyLists.ForEach(
					(listName, index) => OutputWriteLine($"{index + 1}. {listName}"));
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
				!executingCommand.SupportedFlags.Contains(flag) &&
				!executingCommand.RequiredFlags.Contains(flag));

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
					requiredFlagsUnspecified.First().Usage);
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
		/// <see cref="_includeAllFlag"/>, or throw an error.
		/// </summary>
		void RequireExplicitFiltering()
		{
			if (!_includeAllFlag.IsSet && _filteredTasks == _allTasks)
			{
				throw new TaskManException(
					Messages.NoFilterConditionsUseAllIfIntended,
					_executingCommandName);
			}
		}

		/// <summary>
		/// Encapsulates the task displaying / output logic.
		/// </summary>
		void DisplayTasks()
		{
			RequireNoMoreArguments();

			if (!_formatFlag.IsSet)
			{
				_formatFlag.Set(Format.Text);
			}

			if (_formatFlag.Value == Format.Text)
			{
				_filteredTasks.ForEach((task, isFirstTask, isLastTask)
					=> DisplayTaskTabular(task, isFirstTask, isLastTask));
			}
			else if (_formatFlag.Value == Format.CSV)
			{
				using (CsvWriter writer = new CsvWriter(_output))
				{
					writer.WriteHeader<Task>();
					writer.WriteRecords(_filteredTasks);
				}
			}
			else if (_formatFlag.Value == Format.JSON)
			{
				JavaScriptSerializer serializer = new JavaScriptSerializer();
				_output.WriteLine(serializer.Serialize(_filteredTasks.ToArray()));
			}
			else if (_formatFlag.Value == Format.XML)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Task[]));
				serializer.Serialize(_output, _filteredTasks.ToArray());
				_output.WriteLine();
			}
			else
			{
				throw new TaskManException(Messages.FormatNotSupported, _formatFlag.Value);
			}

			if (_renumberFlag.IsSet && _renumberFlag)
			{
				// Only makes sense to save
				// if renumbering has happened.
				// -
				_saveTasks(_allTasks);
			}
		}

		/// <summary>
		/// Encapsulates the task importing logic.
		/// </summary>
		void ImportTasks()
		{
			RequireNoMoreArguments();

			IEnumerable<Task> importedTasks;

			if (_formatFlag.Value == Format.CSV)
			{
				using (CsvReader reader = new CsvReader(_input))
				{
					importedTasks = reader.GetRecords<Task>();
				}
			}
			else if (_formatFlag.Value == Format.JSON)
			{
				JavaScriptSerializer serializer = new JavaScriptSerializer();
				importedTasks = serializer.Deserialize<Task[]>(_input.ReadToEnd());
			}
			else if (_formatFlag.Value == Format.XML)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(Task[]));
				importedTasks = (Task[])serializer.Deserialize(_input);	
			}
			else
			{
				throw new TaskManException(Messages.FormatNotSupported, _formatFlag.Value);
			}

			ImportBehaviour importBehaviour = _importBehaviourFlag.IsSet ?
				_importBehaviourFlag.Value :
				ImportBehaviour.Replace;

			if (importBehaviour == ImportBehaviour.Replace)
			{
				importedTasks.ForEach((task, index) => task.ID = index);

				_allTasks.Clear();
				_allTasks.AddRange(importedTasks);
			}
			else if (importBehaviour == ImportBehaviour.Append)
			{
				int maximumExistingID = _allTasks.Max(task => task.ID);

				importedTasks.ForEach((task, index)
					=> task.ID = maximumExistingID + index + 1);

				_allTasks.AddRange(importedTasks);
			}
			else
			{
				throw new NotImplementedException();
			}

			_saveTasks(_allTasks);
		}

		/// <summary>
		/// Encapsulates the task adding logic in one method.
		/// </summary>
		void AddTask()
		{
			if (!_parsedArguments.Any())
			{
				throw new TaskManException(Messages.NoDescriptionSpecified);
			}

			string description = string.Join(" ", _parsedArguments);

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

			if (!ConfirmTaskOperation("added", new [] { newTask })) return;

			_allTasks.Add(newTask);
			_saveTasks(_allTasks);

			string taskDueDate =
				newTask.DueDate?.ToString("ddd, yyyy-MM-dd");

			OutputWriteLine(
				Messages.TaskWasAdded,
				newTask.Description,
				newTask.ID,
				newTask.Priority.ToString().ToLower(),
				taskDueDate != null ?
					$", due on {taskDueDate}." :
					".");
		}

		/// <summary>
		/// Encapsulates the task modification logic.
		/// </summary>
		void UpdateTasks()
		{
			RequireExplicitFiltering();

			if (!_parsedArguments.HasAtLeastTwoElements())
			{
				throw new TaskManException(Messages.InsufficientUpdateParameters);
			}

			if (!ConfirmTaskOperation("updated")) return;

			string parameterToChange = _parsedArguments.PopFirst().ToLower();
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
				Priority priority = ParseHelper.ParsePriority(_parsedArguments.PopFirst());
				parameterStringValue = priority.ToString();

				RequireNoMoreArguments();

				totalTasksUpdated = _filteredTasks.ForEach(task => task.Priority = priority);
			}
			else if (TaskSetDescriptionRegex.IsMatch(parameterToChange))
			{
				parameterStringValue = string.Join(" ", _parsedArguments);

				totalTasksUpdated = _filteredTasks.ForEach(task => task.Description = parameterStringValue);
			}
			else if (TaskSetFinishedRegex.IsMatch(parameterToChange))
			{
				bool isFinished = ParseHelper.ParseBool(_parsedArguments.PopFirst());
				parameterStringValue = isFinished.ToString();

				RequireNoMoreArguments();

				totalTasksUpdated = _filteredTasks.ForEach(task => task.IsFinished = isFinished);
			}
			else if (TaskSetDueDateRegex.IsMatch(parameterToChange))
			{
				DateTime dueDate = ParseHelper.ParseTaskDueDate(_parsedArguments.PopFirst());
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
		/// Encapsulates the task deletion logic.
		/// </summary>
		void DeleteTasks()
		{			
			RequireExplicitFiltering();
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

			SignalizeOperationSuccess(
				"deleted",
				totalTasksBefore - totalTasksAfter,
				_filteredTasks);
		}

		/// <summary>
		/// Encapsulates the task finishing logic.
		/// </summary>
		void FinishTasks()
		{
			RequireExplicitFiltering();
			RequireNoMoreArguments();

			if (!ConfirmTaskOperation("completed")) return;

			int totalTasksFinished =
				_filteredTasks.ForEach(task => task.IsFinished = true);

			_saveTasks(_allTasks);

			SignalizeOperationSuccess(
				"completed",
				totalTasksFinished,
				_filteredTasks);
		}

		/// <summary>
		/// Encapsulates the task reopen logic.
		/// </summary>
		void ReopenTasks()
		{
			RequireExplicitFiltering();
			RequireNoMoreArguments();

			if (!ConfirmOperation("reopened")) return;

			int totalTasksReopened =
				_filteredTasks.ForEach(task => task.IsFinished = false);

			_saveTasks(_allTasks);

			SignalizeOperationSuccess(
				"reopened",
				totalTasksReopened,
				_filteredTasks);
		}

		/// <summary>
		/// Displays the given task in a default tabular format, writing it into 
		/// the standard output stream or explicitly provided <see cref="TextWriter"/> 
		/// output.
		/// </summary>
		void DisplayTaskTabular(Task task, bool isFirstTask, bool isLastTask, TextWriter output = null)
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
			if (_parsedArguments.Any())
			{
				throw new TaskManException(
					Messages.UnknownCommandLineArguments,
					string.Join(" ", _parsedArguments));
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