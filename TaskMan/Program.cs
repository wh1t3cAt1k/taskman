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
				Console.Error.WriteLine(
					Messages.ErrorPerformingOperation,
					program.CurrentOperation,
					exception.Message.DecapitaliseFirstLetter());

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
		/// <summary>
		/// Gets or sets the current operation performed by the program.
		/// </summary>
		/// <value>The current operation.</value>
		public string CurrentOperation { get; private set; }

		IEnumerable<Flag> _flags;

		/// <summary>
		/// Displays the help text for the program or for the requested command.
		/// </summary>
		Flag<bool> _displayHelpFlag = new Flag<bool>(nameof(_displayHelpFlag), "?|help");

		/// <summary>
		/// Displays the TaskMan license text.
		/// </summary>
		Flag<bool> _displayLicenseFlag = new Flag<bool>(nameof(_displayLicenseFlag), "license");

		/// <summary>
		/// Displays the current TaskMan version.
		/// </summary>
		Flag<bool> _displayVersionFlag = new Flag<bool>(nameof(_displayVersionFlag), "version");

		/// <summary>
		/// Displays a confirmation prompt before performing the requested
		/// operation, along with a list of tasks upon which the operation
		/// is going to be performed. 
		/// </summary>
		Flag<bool> _interactiveFlag = new Flag<bool>(nameof(_interactiveFlag), "I|interactive");

		/// <summary>
		/// Specifies that the operation should be performed upon all tasks.
		/// </summary>
		Flag<bool> _includeAllFlag = new Flag<bool>(nameof(_includeAllFlag), "A|all");

		/// <summary>
		/// Specifies the new task's description.
		/// </summary>
		Flag<string> _descriptionFlag = new Flag<string>(nameof(_descriptionFlag), "d|desc|description");

		/// <summary>
		/// When used as an add flag, specifies the new task's priority.
		/// When used as a filter flag, filters tasks by their priority.
		/// </summary>
		Flag<string> _priorityFlag = new TaskFilterFlag<string>(
			nameof(_priorityFlag), 
			"p=|priority=",
			filterPriority: 1,
			filterPredicate: (flagValue, task) => 
				task.PriorityLevel == TaskMan.ParsePriority(flagValue));

		/// <summary>
		/// Filters tasks by their ID or ID range.
		/// </summary>
		Flag<string> _identityFilterFlag = new TaskFilterFlag<string>(
            nameof(_identityFilterFlag),
            "i=|id=",
			filterPriority: 1,
			filterPredicate: (flagValue, task) => 
			{
				IEnumerable<int> allowedIds = ParseId(flagValue);
				return allowedIds.Contains(task.ID);
			});

		/// <summary>
		/// Filters tasks, keeps only pending tasks.
		/// </summary>
		Flag<bool> _pendingFilterFlag = new TaskFilterFlag<bool>(
			nameof(_pendingFilterFlag), 
			"P|pending|unfinished",
			filterPriority: 1,
			filterPredicate: (_, task) => task.IsFinished == false);

		/// <summary>
		/// Filters tasks, keeps only finished tasks.
		/// </summary>
		Flag<bool> _finishedFilterFlag = new TaskFilterFlag<bool>(
			nameof(_finishedFilterFlag), 
			"F|finished|completed",
			filterPriority: 1,
			filterPredicate: (_, task) => task.IsFinished == true);

		/// <summary>
		/// Filters tasks by regex on description.
		/// </summary>
		Flag<string> _descriptionFilterFlag = new TaskFilterFlag<string>(
			nameof(_descriptionFilterFlag), 
			"r=|like=",
			filterPriority: 1,
			filterPredicate: (pattern, task) => Regex.IsMatch(
				task.Description, 
				pattern, 
				RegexOptions.IgnoreCase));

		/// <summary>
		/// Limits the number of tasks displayed.
		/// </summary>
		Flag<int> _numberLimitFlag = new TaskFilterFlag<int>(
			nameof(_numberLimitFlag),
			"n=|limit=",
			filterPriority: 2,
			filterPredicate: (flagValue, task, taskIndex) => taskIndex < flagValue);

		IEnumerable<Command> _commands;

		Command _addTask;
		Command _deleteTasks; 
		Command _completeTasks;
		Command _displayTasks;
		Command _updateTasks;

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

			_flags = typeof(TaskMan)
				.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(fieldInfo => typeof(Flag).IsAssignableFrom(fieldInfo.FieldType))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Flag>();
				
			_flags.ForEach(flag => flag.AddToOptionSet(this._optionSet));

			_addTask = new Command(
				nameof(_addTask), 
				TaskAddRegex, 
				isReadUpdateDelete: false,
				supportedFlags: new [] { _descriptionFlag, _priorityFlag });
			
			_deleteTasks = new Command(
				nameof(_deleteTasks), 
				TaskDeleteRegex,
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(new [] { _numberLimitFlag })
					.Concat(new [] { _includeAllFlag }));

			_completeTasks = new Command(
				nameof(_completeTasks), 
				TaskCompleteRegex,
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(new [] { _numberLimitFlag })
					.Concat(new [] { _includeAllFlag }));
			
			_displayTasks = new Command(
				nameof(_displayTasks), 
				TaskDisplayRegex,
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Concat(new [] { _includeAllFlag }));
			
			_updateTasks = new Command(
				nameof(_updateTasks), 
				TaskUpdateRegex,
				isReadUpdateDelete: true,
				supportedFlags: _flags
					.Where(flag => flag is ITaskFilter)
					.Except(new [] { _numberLimitFlag })
					.Concat(new [] { _includeAllFlag }));

			_commands = typeof(TaskMan)
				.GetFields(BindingFlags.NonPublic | BindingFlags.Instance)
				.Where(fieldInfo => fieldInfo.FieldType == typeof(Command))
				.Select(fieldInfo => fieldInfo.GetValue(this))
				.Cast<Command>();

			this._readTasks = taskReadFunction ?? this._readTasks;
			this._saveTasks = taskSaveFunction ?? this._saveTasks;

			this._output = outputStream ?? this._output;
			this._error = errorStream ?? this._error;
		}

		/// <summary>
		/// The folder where the task list and app configuration files will be stored,
		/// e.g. '~/.config/TaskMan' or 'c:\users\current_user\AppData\Roaming'
		/// </summary>
		static readonly string APP_DATA_PATH = Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
			Assembly.GetExecutingAssembly().GetName().Name);

		static readonly string TASKS_FILE = "taskman_tasks.tmf";
		static readonly string TASKS_FULL_NAME = Path.Combine(APP_DATA_PATH, TASKS_FILE);

		static readonly RegexOptions StandardRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

		static readonly Regex ConfirmActionRegex = new Regex(@"^\s*y(es)?\s*$", StandardRegexOptions);
		static readonly Regex IdSequenceRegex = new Regex(@"^(:?([0-9]+)\s*?,\s*?)*([0-9]+)$", StandardRegexOptions);
		static readonly Regex IdRangeRegex = new Regex(@"^([0-9]+)-([0-9]+)$", StandardRegexOptions);
		static readonly Regex TaskAddRegex = new Regex(@"^(add|new|create)$", StandardRegexOptions);
		static readonly Regex TaskCompleteRegex = new Regex(@"^(complete|finish|accomplish)$", StandardRegexOptions);
		static readonly Regex TaskDeleteRegex = new Regex(@"^(delete|remove)$", StandardRegexOptions);
		static readonly Regex TaskDisplayRegex = new Regex(@"^(show|display|view)$", StandardRegexOptions);
		static readonly Regex TaskSetDescriptionRegex = new Regex(@"^(description)$", StandardRegexOptions);
		static readonly Regex TaskSetFinishedRegex = new Regex(@"^(finished|completed|accomplished)$", StandardRegexOptions);
		static readonly Regex TaskSetPriorityRegex = new Regex(@"^(priority|importance)$", StandardRegexOptions);
		static readonly Regex TaskUpdateRegex = new Regex(@"^(update|modify)$", StandardRegexOptions);

		/// <summary>
		/// Sets the function that would be called to read the task list.
		/// Can be used to override the default function that reads the tasks from file, 
		/// e.g. for the purpose of unit testing.
		/// </summary>
		Func<List<Task>> _readTasks = TaskMan.ReadTasksFromFile;

		/// <summary>
		/// Sets the function that saves the task list.
		/// Can be used to override the default function that saves the tasks into file,
		/// e.g. for the purpose of unit testing.
		/// </summary>
		Action<List<Task>> _saveTasks = TaskMan.SaveTasksIntoFile;

		/// <summary>
		/// Retrieves the task list from the tasks binary file.
		/// </summary>
		/// <returns>The tasks list read from the file.</returns>
		static List<Task> ReadTasksFromFile()
		{
			using (FileStream inputFileStream = new FileStream(TASKS_FULL_NAME, FileMode.OpenOrCreate, FileAccess.Read))
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
		static void SaveTasksIntoFile(List<Task> tasks)
		{
			tasks.Sort();

			FileStream outputFileStream = new FileStream(TASKS_FULL_NAME, FileMode.Create, FileAccess.Write);

			BinaryFormatter binaryFormatter = new BinaryFormatter();
			binaryFormatter.Serialize(outputFileStream, tasks);

			outputFileStream.Close();
		}

		/// <summary>
		/// Tries to parse a string value into a <see cref="Priority"/> value.
		/// If unsuccessful, throws an exception.
		/// </summary>
		static Priority ParsePriority(string priorityString)
		{
			Priority priority;

			if (!Enum.TryParse(priorityString, out priority) ||
				!Enum.GetValues(typeof(Priority)).Cast<Priority>().Contains(priority))
			{
				throw new TaskManException(
					Messages.UnknownPriorityLevel,
					priorityString);
			}

			return priority;
		}

		/// <summary>
		/// Tries to parse a string value into a sequence of task IDs.
		/// Supports: 
		/// 1. Single IDs like '5'
		/// 2. ID ranges like '5-36'
		/// 3. ID lists like '5,6,7'
		/// </summary>
		/// <returns>
		/// If <paramref name="idString"/> denotes a task ID range like 5-36,
		/// 
		/// Otherwise, if <paramref name="idString"/> denotes a single task ID,
		/// returns a tuple with a <c>null</c> second object.
		/// </returns>
		static IEnumerable<int> ParseId(string idString)
		{
			Match idSequenceMatch = IdSequenceRegex.Match(idString);
			Match idRangeMatch = IdRangeRegex.Match(idString);

			if (idSequenceMatch.Success)
			{
				return idString.Split(',').Select(int.Parse);
			}
			else if (idRangeMatch.Success)
			{
				int lowerBoundary = int.Parse(idRangeMatch.Groups[1].Value);
				int upperBoundary = int.Parse(idRangeMatch.Groups[2].Value);

				if (lowerBoundary > upperBoundary)
				{
					throw new TaskManException(Messages.InvalidTaskIdRange);
				}

				return Enumerable.Range(
					lowerBoundary, 
					checked(upperBoundary - lowerBoundary + 1));
			}
			else
			{
				throw new TaskManException(
					Messages.UnknownIdOrIdRange,
					idString);
			}
		}

		public void Run(IEnumerable<string> commandLineArguments)
		{
			if (!Directory.Exists(TaskMan.APP_DATA_PATH))
			{
				this.CurrentOperation = "create the app subdirectory in the application data folder";
				Directory.CreateDirectory(TaskMan.APP_DATA_PATH);
			}

			this.CurrentOperation = "parse command line arguments";

			LinkedList<string> arguments =
				new LinkedList<string>(_optionSet.Parse(commandLineArguments));

			string commandName = arguments.First?.Value;

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
					arguments = new LinkedList<string>(new [] { commandName }); 
				}
			}

			this.CurrentOperation = "recognize the command";

			IEnumerable<Command> matchingCommands = _commands.Matching(commandName);

			if (!matchingCommands.Any())
			{
				throw new TaskManException(Messages.UnknownCommand);
			}
			else if (!matchingCommands.IsSingleton())
			{
				throw new TaskManException(Messages.MoreThanOneCommandMatchesInput);
			}

			Command command = matchingCommands.Single();

			this.CurrentOperation = "ensure flag consistency";

			IEnumerable<Flag> unsupportedFlagsSpecified = _flags.Where(
				flag => flag.IsSet && 
				!command.SupportedFlags.Contains(flag));

			IEnumerable<Flag> requiredFlagsUnspecified = _flags.Where(
				flag => !flag.IsSet &&
				command.RequiredFlags.Contains(flag));

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
					requiredFlagsUnspecified.First().Alias);
			}

			if (filterFlagsSpecified.Any() &&
			    _includeAllFlag.IsSet)
			{
				throw new TaskManException(
					Messages.EntityDoesNotMakeSenseWithEntity,
					filterFlagsSpecified.First().GetProvidedName(commandLineArguments),
					_includeAllFlag.GetProvidedName(commandLineArguments));
			}

			arguments.RemoveFirst();

			this.CurrentOperation = "read tasks from the task file";

			List<Task> taskList = _readTasks();

			this.CurrentOperation = "filter the task list";

			IEnumerable<Task> filteredTasks = taskList;

			if (command.IsReadUpdateDelete)
			{
				if (!taskList.Any())
				{
					_output.WriteLine(Messages.TaskListIsEmpty);
					return;
				}
			
				if (filterFlagsSpecified.Any())
				{
					filteredTasks = filterFlagsSpecified
						.Cast<ITaskFilter>()
						.OrderBy(taskFilter => taskFilter.FilterPriority)
						.Aggregate(
							seed: taskList as IEnumerable<Task>, 
							func: (taskSequence, filter) => filter.Filter(taskSequence));

					if (!filteredTasks.Any())
					{
						_output.WriteLine(Messages.NoTasksMatchingGivenConditions);
						return;
					}
				}
			}

			if (command == _addTask)
			{
				this.CurrentOperation = "add a new task";

				Task addedTask = AddTask(arguments, taskList);
				_saveTasks(taskList);

				_output.WriteLine(
					Messages.TaskWasAdded,
					addedTask.Description,
					addedTask.ID,
					addedTask.PriorityLevel);
			}
			else if (command == _displayTasks)
			{
				this.CurrentOperation = "display tasks";

				filteredTasks.ForEach(task => task.Display());
			}
			else if (command == _deleteTasks)
			{
				this.CurrentOperation = "delete tasks";

				if (!_includeAllFlag.IsSet && filteredTasks == taskList)
				{
					throw new TaskManException(
						Messages.NoFilterConditionsUseAllIfIntended,
						"delete");
				}

				taskList = taskList.Except(filteredTasks).ToList();

				if (taskList.Any())
				{
					_saveTasks(taskList);
				}
				else
				{
					File.Delete(TASKS_FULL_NAME);
				}

				if (filteredTasks.IsSingleton())
				{
					_output.WriteLine(
						Messages.TaskWasDeleted, 
						filteredTasks.Single().ID, 
						filteredTasks.Single().Description);
				}
				else
				{
					_output.WriteLine(
						Messages.TasksWereDeleted,
						filteredTasks.Count());
				}
			}
			else if (command == _updateTasks)
			{
				this.CurrentOperation = "set task parameters";

				SetTaskParameters(arguments, taskList);
			}
			else if (command == _completeTasks)
			{
				this.CurrentOperation = "finish tasks";

				if (!_includeAllFlag.IsSet && filteredTasks == taskList)
				{
					throw new TaskManException(
						Messages.NoFilterConditionsUseAllIfIntended,
						"finish");
				}

				filteredTasks.ForEach(task => task.IsFinished = true);

				_saveTasks(taskList);

				if (filteredTasks.IsSingleton())
				{
					_output.WriteLine(
						Messages.TaskWasFinished, 
						filteredTasks.Single().ID, 
						filteredTasks.Single().Description);
				}
				else
				{
					_output.WriteLine(
						Messages.TasksWereFinished,
						filteredTasks.Count());
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
		/// Parses the given string into a task ID number, or 
		/// throws an exception if parse operation is failed.
		/// </summary>
		/// <param name="taskIdString">Task identifier string.</param>
		/// <param name="taskId">Task identifier.</param>
		static void ExtractTaskIdNumber(string taskIdString, out int taskId)
		{
			if (!int.TryParse(taskIdString, out taskId))
			{
				throw new TaskManException(
					Messages.UnknownIdOrIdRange,
					taskId);
			}
		}

		/// <summary>
		/// Encapsulates the task modification logic in one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		void SetTaskParameters(LinkedList<string> cliArguments, List<Task> taskList)
		{
			if (cliArguments.Count < 3)
			{ 
				throw new TaskManException(Messages.InsufficientSetParameters);
			}

			int taskId;
			ExtractTaskIdNumber(cliArguments.PopFirst(), out taskId);

			string whatToChange = cliArguments.PopFirst().ToLower();

			Task taskToUpdate = taskList.TaskWithId(taskId);

			if (TaskSetPriorityRegex.IsMatch(whatToChange))
			{
				int priorityLevel;

				if (!int.TryParse(cliArguments.First(), out priorityLevel) || 
					priorityLevel < 1 || 
					priorityLevel > 3)
				{
					throw new TaskManException(Messages.UnknownPriorityLevel, cliArguments.First());
				}

				taskToUpdate.PriorityLevel = (Priority)priorityLevel;
				this._saveTasks(taskList);

				_output.WriteLine(
					Messages.TaskWithIdChangedParameter, 
					taskToUpdate.ID,
					taskToUpdate.Description,
					nameof(Priority).DecapitaliseFirstLetter(),
					taskToUpdate.PriorityLevel);
				
				return;
			}
			else if (TaskSetDescriptionRegex.IsMatch(whatToChange))
			{
				string oldDescription = taskToUpdate.Description;
				taskToUpdate.Description = string.Join(" ", cliArguments);

				this._saveTasks(taskList);
				_output.WriteLine(
					Messages.TaskWithIdChangedParameter,
					taskToUpdate.ID,
					oldDescription,
					nameof(Task.Description).DecapitaliseFirstLetter(),
					taskToUpdate.Description);
			}
			else
			{
				throw new TaskManException(Messages.InvalidSetParameters);
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
				ParsePriority(_priorityFlag.Value) : 
				Priority.Normal;

			Task newTask = new Task(taskList.Count, description, taskPriority);
			taskList.Add(newTask);

			return newTask;
		}
	}
}