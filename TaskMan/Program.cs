using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;

using Mono.Options;

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

		bool _displayHelp = false;
		bool _displayLicense = false;
		bool _displayVersion = false;

		private OptionSet OptionSet;

		TextWriter _output = Console.Out;
		TextWriter _error = Console.Error;

		public TaskMan(
			Func<List<Task>> taskReadFunction = null,
			Action<List<Task>> taskSaveFunction = null,
			TextWriter outputStream = null,
			TextWriter errorStream = null)
		{
			this.OptionSet = new OptionSet {
				{ "?|help", value => _displayHelp = (value != null) },
				{ "license", value => _displayLicense = (value != null) },
				{ "version", value => _displayVersion = (value != null) },
			};

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
		static readonly Regex TaskAddRegex = new Regex(@"(^add$)|(^new$)|(^create$)", StandardRegexOptions);
		static readonly Regex TaskCompleteRegex = new Regex(@"(^complete$)|(^finish$)|(^accomplish$)", StandardRegexOptions);
		static readonly Regex TaskDeleteRegex = new Regex(@"(^delete$)|(^remove$)", StandardRegexOptions);
		static readonly Regex TaskDisplayRegex = new Regex(@"^(show|display|view)(p|f|all)?$", StandardRegexOptions);
		static readonly Regex TaskPriorityRegex = new Regex(@"^\[([0-9]+)\]$", StandardRegexOptions);
		static readonly Regex TaskSetDescriptionRegex = new Regex(@"^description$", StandardRegexOptions);
		static readonly Regex TaskSetFinishedRegex = new Regex(@"(^finished$)|(^completed$)|(^accomplished$)", StandardRegexOptions);
		static readonly Regex TaskSetPriorityRegex = new Regex(@"(^priority$)|(^importance$)", StandardRegexOptions);

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

		public void Run(IEnumerable<string> commandLineArguments)
		{
			List<string> optionsRemainder = OptionSet.Parse(commandLineArguments);

			LinkedList<string> arguments = new LinkedList<string>(optionsRemainder);

			if (!arguments.Any()) 
			{
				arguments = new LinkedList<string>(new [] { "showall" }); 
			}

			if (!Directory.Exists(TaskMan.APP_DATA_PATH))
			{
				this.CurrentOperation = "create the app subdirectory in the application data folder";
				Directory.CreateDirectory(TaskMan.APP_DATA_PATH);
			}

			// Retrieve and pop the command name from the arguments.
			// -
			string commandName = arguments.First.Value;
			arguments.RemoveFirst();

			if (this._displayHelp)
			{
				DisplayHelpText();
				this.CurrentOperation = "display help text";

				return;
			}
			else if (this._displayLicense)
			{
				this.CurrentOperation = "display license text";
				DisplayLicenseText();

				return;
			}
			else if (this._displayVersion)
			{
				this.CurrentOperation = "display the taskman version";

				Assembly entryAssembly = Assembly.GetExecutingAssembly();
				AssemblyName assemblyName = entryAssembly.GetName();

				string productName = entryAssembly
					.GetAssemblyAttributeValue<AssemblyProductAttribute, string>(attribute => attribute.Product);

				_output.WriteLine(
					"{0} version {1}.{2}.{3}",
					productName,
					assemblyName.Version.Major,
					assemblyName.Version.Minor,
					assemblyName.Version.Build);

				return;
			}


			this.CurrentOperation = "read tasks from the task file";
			List<Task> taskList = _readTasks();

			if (TaskAddRegex.IsMatch(commandName))
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
			else if (TaskDisplayRegex.IsMatch(commandName))
			{
				this.CurrentOperation = "display tasks";

				Match regexMatch = TaskDisplayRegex.Match(commandName);
				string displayEnding = regexMatch.Groups[2].ToString();

				if (displayEnding == "" || displayEnding == "all")
				{
					DisplayTasks(arguments, taskList, TaskDisplayCondition.All);
				}
				else if (displayEnding == "p")
				{
					DisplayTasks(arguments, taskList, TaskDisplayCondition.Current);
				}
				else if (displayEnding == "f")
				{
					DisplayTasks(arguments, taskList, TaskDisplayCondition.Finished);
				}
			}
			else if (TaskDeleteRegex.IsMatch(commandName))
			{
				this.CurrentOperation = "delete tasks";

				Task deletedTask = DeleteTask(arguments, taskList);

				if (taskList.Any())
				{
					_saveTasks(taskList);
				}
				else
				{
					File.Delete(TASKS_FULL_NAME);
				}

				_output.WriteLine(
					Messages.TaskWithIdWasDeleted, 
					deletedTask.ID, 
					deletedTask.Description);
			}
			else if (commandName.Equals("set"))
			{
				this.CurrentOperation = "set task parameters";
				SetTaskParameters(arguments, taskList);
			}
			else if (commandName.Equals("clear"))
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
			else if (TaskCompleteRegex.IsMatch(commandName))
			{
				this.CurrentOperation = "finish a task";

				int idToFinish;

				if (!arguments.Any())
				{ 
					return;
				}

				ExtractTaskIdNumber(arguments.First(), out idToFinish);

				Task taskToFinish = taskList.TaskWithId(idToFinish);
				taskToFinish.IsFinished = true;

				_saveTasks(taskList);

				_output.WriteLine(Messages.TaskWasFinished, taskToFinish.ID, taskToFinish.Description);
				return;
			}
			else 
			{
				this.CurrentOperation = "recognize the command";
				throw new Exception(Messages.UnknownCommand);
			}

			return;
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
		/// Encapsulates the task displaying logic inside one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		/// <param name="displayCondition">Display condition.</param>
		void DisplayTasks(
			LinkedList<string> cliArguments, 
			List<Task> taskList, 
			TaskDisplayCondition displayCondition)
		{
			if (!cliArguments.Any())
			{ 
				if (taskList.Count == 0)
				{
					_output.WriteLine(Messages.TaskListIsEmpty);
				}
				else
				{
					taskList
						.Where(task => task.MatchesDisplayCondition(displayCondition))
						.ForEach(task => task.Display());
				}

				return;
			}

			Match singleIdMatch = SingleIdRegex.Match(cliArguments.First());
			Match idRangeMatch = IdRangeRegex.Match(cliArguments.First());

			if (singleIdMatch.Success)
			{
				int taskToDisplayId;

				ExtractTaskIdNumber(singleIdMatch.Groups[1].ToString(), out taskToDisplayId);

				Task taskToDisplay = taskList.TaskWithId(taskToDisplayId);

				if (!taskToDisplay.MatchesDisplayCondition(displayCondition))
				{
					_output.WriteLine(Messages.TaskWithIdDoesNotMatchTheCondition, taskToDisplayId);
				}

				return;
			}
			else if (idRangeMatch.Success)
			{
				this.CurrentOperation = "display tasks in the ID range";

				int startingId;
				int endingId;

				ExtractTaskIdNumber(idRangeMatch.Groups[1].ToString(), out startingId);
				ExtractTaskIdNumber(idRangeMatch.Groups[2].ToString(), out endingId);

				if (startingId > endingId)
				{
					throw new Exception(Messages.InvalidTaskIdRange);
				}

				IEnumerable<Task> tasksToDisplay = taskList.Where(task =>
					task.ID >= startingId &&
					task.ID <= endingId &&
					task.MatchesDisplayCondition(displayCondition));
				
				if (tasksToDisplay.Any())
				{
					tasksToDisplay.ForEach(task => task.Display());
				}
				else
				{
					_output.WriteLine(
						Messages.NoTasksInSpecifiedIdRangeWithCondition, 
						displayCondition);
				}

				return;
			}
			else
			{
				throw new Exception(Messages.UnknownCommand);
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

		/// <summary>
		/// Encapsulates the task deletion logic in one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		/// <returns>The <see cref="Task"/> object that has been deleted.</returns>
		public Task DeleteTask(LinkedList<string> cliArguments, List<Task> taskList)
		{
			if (!cliArguments.Any())
			{
				throw new Exception(string.Format(Messages.NoTaskIdProvided, this.CurrentOperation));
			}

			int idToDelete;
			ExtractTaskIdNumber(cliArguments.First(), out idToDelete);

			Task taskToDelete = taskList.TaskWithId(idToDelete);

			taskList.RemoveAll(task => (task.ID == idToDelete));

			taskList
				.Where(task => task.ID > idToDelete)
				.ForEach(task => (task.ID -= 1));

			return taskToDelete;
		}
	}
}