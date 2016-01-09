using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System;

namespace TaskMan
{
	static class Program
	{
		/// <summary>
		/// Gets or sets the current operation performed by the program.
		/// </summary>
		/// <value>The current operation.</value>
		static string CurrentOperation { get; set; }

		/// <summary>
		/// The folder where the task list and app configuration files will be stored,
		/// e.g. '~/.config/TaskMan' or 'c:\users\current_user\AppData\Roaming'
		/// </summary>
		static readonly string APP_DATA_PATH = 
			Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
			Path.DirectorySeparatorChar +
			Assembly.GetEntryAssembly().GetName().Name;

		static readonly string TASKS_FILE = "taskman_tasks.tmf";
		static readonly string TASKS_FULL_NAME = APP_DATA_PATH + Path.DirectorySeparatorChar + TASKS_FILE;

		static readonly Regex ConfirmActionRegex = new Regex(@"^\s*y(es)?\s*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex HelpRequestRegex = new Regex(@"(^/\?$)|(^-?-?help$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex LicenseRequestRegex = new Regex(@"^-?-?license$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex IdRangeRegex = new Regex(@"^([0-9]+)-([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex SingleIdRegex = new Regex(@"^([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex TaskAddRegex = new Regex(@"(^add$)|(^new$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex TaskCompleteRegex = new Regex(@"(^complete$)|(^finish$)|(^accomplish$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex TaskDeleteRegex = new Regex(@"(^delete$)|(^remove$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex TaskDisplayRegex = new Regex(@"^(show|display|view)(p|f|all)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex TaskPriorityRegex = new Regex(@"^\[([0-9]+)\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static readonly Regex VersionRegex = new Regex(@"^--version$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		#region Service Functions

		/// <summary>
		/// Extracts the value from the given assembly attribute.
		/// </summary>
		/// <returns>The value extracted from the assembly attribute.</returns>
		/// <param name="extractValueFunction">The function to extract the value from the attribute.</param>
		/// <typeparam name="T">The type of assembly attribute.</typeparam>
		public static V GetAssemblyAttributeValue<T, V>(Func<T, V> extractValueFunction) where T : Attribute
		{
			T attribute = (T)Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof (T));
			return extractValueFunction.Invoke(attribute);
		}
			
		/// <summary>
		/// Outputs the contents of an embedded resource into the standard output stream.
		/// </summary>
		/// <param name="resourceName">The full name of the embedded resource.</param>
		static void DisplayResourceText(string resourceName)
		{
			using (Stream helpTextStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			{
				using (StreamReader helpTextStreamReader = new StreamReader(helpTextStream))
				{
					Console.WriteLine(helpTextStreamReader.ReadToEnd());
				}
			}
		}

		/// <summary>
		/// Outputs the application help into the standard output stream. 
		/// </summary>
		static void DisplayHelpText()
		{
			DisplayResourceText("TaskMan.HELP.txt");
		}

		/// <summary>
		/// Outputs the application license into the standard output stream.
		/// </summary>
		static void DisplayLicenseText()
		{
			DisplayResourceText("TaskMan.LICENSE.txt");
		}

		/// <summary>
		/// Retrieves the task list from the tasks binary file.
		/// </summary>
		/// <returns>The tasks list read from the file.</returns>
		static List<Task> ReadTasks()
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

		#endregion

		static int Main(params string[] args)
		{
			try
			{
				RunTaskman(new LinkedList<string>(args));
			}
			catch (Exception exception)
			{
				Console.Error.WriteLine(
					Messages.ErrorPerformingOperation,
					Program.CurrentOperation,
					exception.Message.DecapitaliseFirstLetter());
				
				return -1;
			}

			return 0;
		}

		static void RunTaskman(LinkedList<string> arguments)
		{
			if (!arguments.Any()) 
			{
				arguments = new LinkedList<string>(new [] { "showall" }); 
			}

			if (!Directory.Exists(Program.APP_DATA_PATH))
			{
				Program.CurrentOperation = "create the app subdirectory in the application data folder";
				Directory.CreateDirectory(Program.APP_DATA_PATH);
			}

			// Retrieve and pop the command name from the arguments.
			// -
			string commandName = arguments.First.Value;
			arguments.RemoveFirst();

			if (HelpRequestRegex.IsMatch(commandName))
			{
				Program.CurrentOperation = "display help text";
				DisplayHelpText();
				return;
			}
			else if (LicenseRequestRegex.IsMatch(commandName))
			{
				Program.CurrentOperation = "display license text";
				DisplayLicenseText();
				return;
			}

			Program.CurrentOperation = "read tasks from the task file";
			List<Task> taskList = ReadTasks(); 

			if (TaskAddRegex.IsMatch(commandName))
			{
				Program.CurrentOperation = "add a new task";

				Task addedTask = AddTask(arguments, taskList);
				SaveTasksIntoFile(taskList);

				Console.WriteLine(
					Messages.TaskWasAdded, 
					addedTask.Description, 
					(Priority)addedTask.PriorityLevel);
			}
			else if (TaskDisplayRegex.IsMatch(commandName))
			{
				Program.CurrentOperation = "display tasks";

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
				Program.CurrentOperation = "delete tasks";

				Task deletedTask = DeleteTask(arguments, taskList);
				SaveTasksIntoFile(taskList);

				Console.WriteLine(
					Messages.TaskWithIdWasDeleted, 
					deletedTask.ID, 
					deletedTask.Description);
			}
			else if (commandName.Equals("set"))
			{
				Program.CurrentOperation = "set task parameters";
				SetTaskParameters(arguments, taskList);
			}
			else if (commandName.Equals("clear"))
			{
				Program.CurrentOperation = "clear the task list";

				Console.Write(Messages.ClearConfirmationMessage);

				if (ConfirmActionRegex.IsMatch(Console.ReadLine()))
				{
					taskList.Clear();
					SaveTasksIntoFile(taskList);
					Console.WriteLine(Messages.TaskListCleared);
				}
				else
				{
					Console.WriteLine(Messages.TaskListClearCancelled);
				}
			}
			else if (TaskCompleteRegex.IsMatch(commandName))
			{
				Program.CurrentOperation = "finish a task";

				int idToFinish;

				if (!arguments.Any())
				{ 
					return;
				}

				ExtractTaskIdNumber(arguments.First(), out idToFinish);

				Task taskToFinish = TaskWithId(taskList, idToFinish);
				taskToFinish.IsFinished = true;

				SaveTasksIntoFile(taskList);

				Console.WriteLine(Messages.TaskWasFinished, taskToFinish.ID, taskToFinish.Description);
				return;
			}
			else if (VersionRegex.IsMatch(commandName))
			{
				Program.CurrentOperation = "display the taskman version";

				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				AssemblyName assemblyName = executingAssembly.GetName();

				Console.WriteLine(
					"{0} version {1}.{2}.{3}",
					GetAssemblyAttributeValue<AssemblyProductAttribute, string>(attribute => attribute.Product),
					assemblyName.Version.Major,
					assemblyName.Version.Minor,
					assemblyName.Version.Build);
			}
			else 
			{
				Program.CurrentOperation = "recognize the command";
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
		/// Returns a task with the specified id, if it is present
		/// in the task list, throws an exception otherwise.
		/// </summary>
		/// <returns>
		/// Task with the specified ID, if it is present 
		/// in <paramref name="tasks"/> list.
		/// </returns>
		/// <param name="tasks">The task list.</param>
		/// <param name="id">The ID of the task to be returned.</param>
		static Task TaskWithId(this List<Task> tasks, int id)
		{
			try 
			{
				return tasks.Single(task => (task.ID == id));
			}
			catch
			{
				throw new Exception(string.Format(Messages.NoTaskWithSpecifiedId, id));
			}
		}

		/// <summary>
		/// Encapsulates the task displaying logic inside one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		/// <param name="displayCondition">Display condition.</param>
		static void DisplayTasks(
			LinkedList<string> cliArguments, 
			List<Task> taskList, 
			TaskDisplayCondition displayCondition)
		{
			if (!cliArguments.Any())
			{ 
				if (taskList.Count == 0)
				{
					Console.WriteLine(Messages.TaskListIsEmpty);
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
					Console.WriteLine(Messages.TaskWithIdDoesNotMatchTheCondition, taskToDisplayId);
				}

				return;
			}
			else if (idRangeMatch.Success)
			{
				Program.CurrentOperation = "display tasks in the ID range";

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
					Console.WriteLine(
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

		static void SetTaskParameters(LinkedList<string> cliArguments, List<Task> taskList)
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

			if (whatToChange == "priority")
			{
				int priorityLevel;

				if (!int.TryParse(cliArguments.First(), out priorityLevel) || 
					priorityLevel < 1 || 
					priorityLevel > 3)
				{
					throw new Exception(string.Format(Messages.UnknownPriorityLevel, cliArguments.ElementAt(2)));
				}

				taskToUpdate.PriorityLevel = (Priority)priorityLevel;
				SaveTasksIntoFile(taskList);
				Console.WriteLine("Congrats! Task with Id {0} has changed its priority to {1}", taskToUpdate.ID, taskToUpdate.PriorityLevel);
				return;
			}
			else if (whatToChange.StartsWith("desc", StringComparison.CurrentCultureIgnoreCase))
			{
				taskToUpdate.Description = string.Join(" ", cliArguments);

				SaveTasksIntoFile(taskList);
				Console.WriteLine("Congrats! Task with Id {0} has changed its description to [{1}].", taskToUpdate.ID, taskToUpdate.Description);

				return;
			}
			else if (whatToChange == "finished")
			{
				bool finishedFlag;

				if(!bool.TryParse(cliArguments.First().ToLower(), out finishedFlag))
				{
					throw new Exception(Messages.UnknownBoolValue);
				}
				taskToUpdate.IsFinished = finishedFlag;

				SaveTasksIntoFile(taskList);
				Console.WriteLine("Congrats! Task with id {0} has changed its finished state to {1}.", taskToUpdate.ID, taskToUpdate.IsFinished);

				return;
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
		public static Task AddTask(LinkedList<string> cliArguments, List<Task> taskList)
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
		public static Task DeleteTask(LinkedList<string> cliArguments, List<Task> taskList)
		{
			if (!cliArguments.Any())
			{
				throw new Exception(string.Format(Messages.NoTaskIdProvided, Program.CurrentOperation));
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