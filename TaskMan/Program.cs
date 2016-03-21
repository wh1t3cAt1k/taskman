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
		Flag<bool> _interactiveFlag = new Flag<bool>(nameof(_interactiveFlag), "i|interactive");

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
			filterPredicate: (flagValue, task) => 
				task.PriorityLevel == TaskMan.ParsePriority(flagValue));

		/// <summary>
		/// Filters tasks by their ID or ID range.
		/// </summary>
		Flag<string> _identityFilterFlag = new TaskFilterFlag<string>(
            nameof(_identityFilterFlag),
            "I=|id=",
			filterPredicate: (flagValue, task) => { throw new NotImplementedException(); });

		/// <summary>
		/// Filters tasks, keeps only pending tasks.
		/// </summary>
		Flag<bool> _pendingFilterFlag = new TaskFilterFlag<bool>(
			nameof(_pendingFilterFlag), 
			"P|pending|unfinished",
			filterPredicate: (_, task) => task.IsFinished == false);

		/// <summary>
		/// Filters tasks, keeps only finished tasks.
		/// </summary>
		Flag<bool> _finishedFilterFlag = new TaskFilterFlag<bool>(
			nameof(_finishedFilterFlag), 
			"F|finished|completed",
			filterPredicate: (_, task) => task.IsFinished == true);

		/// <summary>
		/// Filters tasks by regex on description.
		/// </summary>
		Flag<string> _descriptionFilterFlag = new TaskFilterFlag<string>(
			nameof(_descriptionFilterFlag), 
			"r=|like=",
			filterPredicate: (pattern, task) => Regex.IsMatch(
				task.Description, 
				pattern, 
				RegexOptions.IgnoreCase));

		IEnumerable<Command> _commands;

		Command _addTask;
		Command _deleteTasks; 
		Command _completeTasks;
		Command _displayTasks;
		Command _updateTasks;
		Command _clearTasks;

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
				supportedFlags: _flags.OfType<ITaskFilter>().Cast<Flag>());

			_completeTasks = new Command(
				nameof(_completeTasks), 
				TaskCompleteRegex,
				isReadUpdateDelete: true,
				supportedFlags: _flags.OfType<ITaskFilter>().Cast<Flag>());
			
			_displayTasks = new Command(
				nameof(_displayTasks), 
				TaskDisplayRegex,
				isReadUpdateDelete: true,
				supportedFlags: _flags.OfType<ITaskFilter>().Cast<Flag>());
			
			_updateTasks = new Command(
				nameof(_updateTasks), 
				TaskUpdateRegex,
				isReadUpdateDelete: true,
				supportedFlags: new Flag[] { });

			_clearTasks = new Command(
				nameof(_clearTasks),
				TaskClearRegex,
				isReadUpdateDelete: false,
				supportedFlags: new Flag[] { });

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
		static readonly Regex IdRangeRegex = new Regex(@"^([0-9]+)-([0-9]+)$", StandardRegexOptions);
		static readonly Regex SingleIdRegex = new Regex(@"^([0-9]+)$", StandardRegexOptions);
		static readonly Regex TaskAddRegex = new Regex(@"^(add|new|create)$", StandardRegexOptions);
		static readonly Regex TaskCompleteRegex = new Regex(@"^(complete|finish|accomplish)$", StandardRegexOptions);
		static readonly Regex TaskDeleteRegex = new Regex(@"^(delete|remove)$", StandardRegexOptions);
		static readonly Regex TaskClearRegex = new Regex(@"^(clear)$", StandardRegexOptions);
		static readonly Regex TaskDisplayRegex = new Regex(@"^(show|display|view)$", StandardRegexOptions);
		static readonly Regex TaskPriorityRegex = new Regex(@"^\[([0-9]+)\]$", StandardRegexOptions);
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
		/// Outputs the application help into the standard output stream. 
		/// </summary>
		void DisplayHelpText()
		{
			_output.WriteLine(Assembly.GetExecutingAssembly().GetResourceText("TaskMan.HELP.txt"));
		}

		/// <summary>
		/// Outputs the application license into the standard output stream.
		/// </summary>
		void DisplayLicenseText()
		{
			_output.WriteLine(Assembly.GetExecutingAssembly().GetResourceText("TaskMan.LICENSE.txt"));
		}

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
				throw new Exception(string.Format(
					Messages.UnknownPriorityLevel,
					priorityString));
			}

			return priority;
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

			// Handle global flags that work without 
			// an explicit command name.
			// -
			if (commandName == null && _displayHelpFlag.IsSet)
			{
				this.CurrentOperation = "display help text";
				DisplayHelpText();

				return;
			}
			else if (commandName == null && _displayLicenseFlag.IsSet)
			{
				this.CurrentOperation = "display license text";
				DisplayLicenseText();

				return;
			}
			else if (commandName == null && _displayVersionFlag.IsSet)
			{
				this.CurrentOperation = "display the taskman version";

				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				AssemblyName assemblyName = executingAssembly.GetName();

				string productName = executingAssembly
					.GetAssemblyAttributeValue<AssemblyProductAttribute, string>(attribute => attribute.Product);

				_output.WriteLine(
					"{0} version {1}.{2}.{3}",
					productName,
					assemblyName.Version.Major,
					assemblyName.Version.Minor,
					assemblyName.Version.Build);

				return;
			}

			this.CurrentOperation = "recognize the command";

			if (commandName == null)
			{
				// TaskMan operates as "show" by default.
				// -
				commandName = "show";
				arguments = new LinkedList<string>(new [] { commandName }); 
			}

			IEnumerable<Command> matchingCommands = _commands.Matching(commandName);

			if (!matchingCommands.Any())
			{
				throw new Exception(Messages.UnknownCommand);
			}
			else if (!matchingCommands.IsSingleton())
			{
				throw new Exception(Messages.MoreThanOneCommandMatchesInput);
			}

			Command command = matchingCommands.Single();

			this.CurrentOperation = "ensure flag consistency";

			IEnumerable<Flag> unsupportedFlags = _flags.Where(
				flag => flag.IsSet && 
				!command.SupportedFlags.Contains(flag));

			if (unsupportedFlags.Any())
			{
				throw new Exception(string.Format(
					Messages.EntityDoesNotMakeSenseWithEntity,
					unsupportedFlags.First().Alias,
					commandName));
			}

			arguments.RemoveFirst();

			this.CurrentOperation = "read tasks from the task file";

			List<Task> taskList = _readTasks();

			IEnumerable<Task> filteredTasks = taskList;

			if (command.IsReadUpdateDelete)
			{
				this.CurrentOperation = "filter the task list";

				if (!filteredTasks.Any())
				{
					_output.WriteLine(Messages.TaskListIsEmpty);
					return;
				}

				IEnumerable<ITaskFilter> filterFlags = command
					.SupportedFlags
					.Where(flag => flag.IsSet)
					.OfType<ITaskFilter>();
			
				if (filterFlags.Any())
				{
					filteredTasks = command
						.SupportedFlags
						.Where(flag => flag.IsSet && flag is ITaskFilter)
						.Cast<ITaskFilter>()
						.Aggregate(
							taskList as IEnumerable<Task>, 
							(sequence, filter) => filter.Filter(sequence));

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
			else if (command == _clearTasks)
			{
				this.CurrentOperation = "clear the task list";

				_output.Write(Messages.ClearConfirmationMessage);

				if (ConfirmActionRegex.IsMatch(Console.ReadLine()))
				{
					taskList.Clear();
					File.Delete(TASKS_FULL_NAME);
					_output.WriteLine(Messages.TaskListCleared);
				}
				else
				{
					_output.WriteLine(Messages.TaskListClearCancelled);
				}
			}
			else if (command == _completeTasks)
			{
				this.CurrentOperation = "finish tasks";

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
		/// Parses the given string into a task ID number, or 
		/// throws an exception if parse operation is failed.
		/// </summary>
		/// <param name="taskIdString">Task identifier string.</param>
		/// <param name="taskId">Task identifier.</param>
		static void ExtractTaskIdNumber(string taskIdString, out int taskId)
		{
			if (!int.TryParse(taskIdString, out taskId))
			{
				throw new Exception(Messages.InvalidTaskId);
			}
		}

		/// <summary>
		/// Encapsulates the task modification logic in one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		void SetTaskParameters(LinkedList<string> cliArguments, List<Task> taskList)
		{
			if (!cliArguments.Any())
			{
				DisplayHelpText();
				return;
			}

			if (cliArguments.Count < 3)
			{ 
				throw new Exception(Messages.InsufficientSetParameters);
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
					throw new Exception(string.Format(Messages.UnknownPriorityLevel, cliArguments.First()));
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
			else if (TaskSetFinishedRegex.IsMatch(whatToChange))
			{
				bool finishedFlag;

				if(!bool.TryParse(cliArguments.First().ToLower(), out finishedFlag))
				{
					throw new Exception(Messages.UnknownBoolValue);
				}

				taskToUpdate.IsFinished = finishedFlag;

				_saveTasks(taskList);
				_output.WriteLine(
					Messages.TaskWithIdChangedParameter,
					taskToUpdate.ID,
					taskToUpdate.Description,
					"finished state",
					taskToUpdate.IsFinished);
			}
			else
			{
				throw new Exception(Messages.InvalidSetParameters);
			}
		}

		/// <summary>
		/// Encapsulates the task adding logic in one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		/// <returns>The <see cref="Task"/> object that was added into the <paramref name="taskList"/></returns>
		static Task AddTask(LinkedList<string> cliArguments, List<Task> taskList)
		{
			if (!cliArguments.Any())
			{
				throw new Exception(Messages.NoDescriptionSpecified);
			}

			string description = string.Join(
				" ", 
				cliArguments
				.Where(argument => !TaskPriorityRegex.IsMatch(argument)));

			int priorityLevel = 1;
			string priorityArgument = cliArguments.FirstOrDefault(TaskPriorityRegex.IsMatch);

			if (priorityArgument != null)
			{
				string priorityValueString = TaskPriorityRegex.Match(priorityArgument).Groups[1].ToString();

				if (!int.TryParse(priorityValueString, out priorityLevel) ||
					priorityLevel < 1 || 
					priorityLevel > 3)
				{
					throw new Exception(string.Format(
						Messages.UnknownPriorityLevel, 
						priorityArgument)); 
				}
			}

			Task newTask = new Task(taskList.Count, description, (Priority)priorityLevel);
			taskList.Add(newTask);

			return newTask;
		}
	}
}