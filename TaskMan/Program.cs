using System.Collections.Generic;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System;

namespace TaskMan
{
	static class Program
	{
		static string currentOperation;

		static readonly string STARTUP_PATH = Application.StartupPath;
		
		static readonly string TASKS_FILE = "taskman_tasks.tmf";
		static readonly string HELP_FILE = "taskman_service.tmf";
		static readonly string HELP_TEXT_FILE = "taskman_input.txt";

		static readonly string TASKS_FULL_NAME = STARTUP_PATH + Path.DirectorySeparatorChar + TASKS_FILE;
		static readonly string HELP_FULL_NAME = STARTUP_PATH + Path.DirectorySeparatorChar + HELP_FILE;
		static readonly string HELP_TEXT_FULL_NAME = STARTUP_PATH + Path.DirectorySeparatorChar + HELP_TEXT_FILE;

		static Regex HelpMakeRegex = new Regex(@"^-mkhelp$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex HelpRequestRegex = new Regex(@"(^/\?$)|(^-?-?h(elp)?$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskAddRegex = new Regex(@"(^add$)|(^new$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskDisplayRegex = new Regex(@"^(show|display|view)(p|f|all)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskDeleteRegex = new Regex(@"(^delete$)|(^remove$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskCompleteRegex = new Regex(@"(^complete$)|(^finish$)|(^accomplish$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex SingleIdRegex = new Regex(@"^([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex IdRangeRegex = new Regex(@"^([0-9]+)-([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex VersionRegex = new Regex(@"^--version$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskPriorityRegex = new Regex(@"^\[([0-9]+)\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		#region Service Functions

		/// <summary>
		/// Extracts the value from the given assembly attribute.
		/// </summary>
		/// <returns>The value extracted from the assembly attribute.</returns>
		/// <param name="extractValueFunction">The function to extract the value from the attribute.</param>
		/// <typeparam name="T">The type of assembly attribute.</typeparam>
		public static string GetAssemblyAttribute<T>(Func<T, string> extractValueFunction) where T : Attribute
		{
			T attribute = (T)Attribute.GetCustomAttribute(Assembly.GetEntryAssembly(), typeof (T));
			return extractValueFunction.Invoke(attribute);
		}

		/// <summary>
		/// Displays the TaskMan help text in the console.
		/// </summary>
		static void DisplayHelpText()
		{
			FileStream inputFileStream = null;
			string[] helpTextLines = null;

			// Generate the help output from the help binary file.
			// -
			try
			{
				inputFileStream = new FileStream(HELP_FULL_NAME, FileMode.Open, FileAccess.Read);
				helpTextLines = (string[])(new BinaryFormatter()).Deserialize(inputFileStream);
			}
			catch 
			{ 
				Console.Error.WriteLine(Messages.FatalErrorReadingFile, HELP_FILE); 
				return; 
			}
			finally 
			{ 
				if (inputFileStream != null) 
				{
					inputFileStream.Close(); 
				}
			}

			helpTextLines.ForEach(action: Console.WriteLine);
		}

		/// <summary>
		/// Makes the help binary file from the input text file.
		/// </summary>
		static void MakeHelp()
		{
			using (StreamReader inputTextReader = new StreamReader(HELP_TEXT_FULL_NAME))
			{
				List<string> inputTextLines = new List<string>();

				while (!inputTextReader.EndOfStream)
				{
					inputTextLines.Add(inputTextReader.ReadLine());
				}

				using (FileStream outputStream = new FileStream(HELP_FULL_NAME, FileMode.Create, FileAccess.Write))
				{
					BinaryFormatter binaryFormatter = new BinaryFormatter();
					binaryFormatter.Serialize(outputStream, inputTextLines.ToArray());

					Console.WriteLine(Messages.MakeHelpSucceeded);
					outputStream.Close();
				}

				inputTextReader.Close();
			}

			return;
		}

		/// <summary>
		/// Retrieves the task list from the tasks file.
		/// </summary>
		/// <returns>The tasks list.</returns>
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
		static void Save(List<Task> tasks)
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
					Program.currentOperation,
					exception.Message.DecapitaliseFirstLetter());
				
				return -1;
			}

			return 0;
		}

		static void RunTaskman(LinkedList<string> arguments)
		{
			if (!arguments.Any()) 
			{
				arguments = new LinkedList<string>(new string[] { "showall" }); 
			}

			// Retrieve and pop the command name from the arguments.
			// -
			string commandName = arguments.First.Value;
			arguments.RemoveFirst();

			if (HelpMakeRegex.IsMatch(commandName))
			{
				Program.currentOperation = "generate help binary file";
				MakeHelp();
				return;
			}
			else if (HelpRequestRegex.IsMatch(commandName))
			{
				Program.currentOperation = "display help text";
				DisplayHelpText();
				return;
			}

			Program.currentOperation = "read tasks from the task file";
			List<Task> taskList = ReadTasks(); 

			if (TaskAddRegex.IsMatch(commandName))
			{
				Program.currentOperation = "add a new task";

				if (!arguments.Any())
				{
					throw new Exception(Messages.NoDescriptionSpecified);
				}

				string description = string.Join(
					" ", 
					arguments
						.Where(argument => !TaskPriorityRegex.IsMatch(argument)));

				int priorityLevel = 1;
				string priorityArgument = arguments.FirstOrDefault(TaskPriorityRegex.IsMatch);

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
					
				taskList.Add(new Task(taskList.Count, description, (Priority)priorityLevel));
				Save(taskList);

				Console.WriteLine(
					Messages.TaskWasAdded,
					description,
					(Priority)priorityLevel);

				return;
			}
			else if (TaskDisplayRegex.IsMatch(commandName))
			{
				Program.currentOperation = "show tasks";

				Match regexMatch = TaskDisplayRegex.Match(commandName);
				string displayEnding = regexMatch.Groups[2].ToString();

				if (displayEnding == "" || displayEnding == "all")
				{
					DisplayTasks(taskList, arguments, TaskDisplayCondition.All);
				}
				else if (displayEnding == "p")
				{
					DisplayTasks(taskList, arguments, TaskDisplayCondition.Current);
				}
				else if (displayEnding == "f")
				{
					DisplayTasks(taskList, arguments, TaskDisplayCondition.Finished);
				}

				return;
			}
			else if (TaskDeleteRegex.IsMatch(commandName))
			{
				Program.currentOperation = "delete tasks";

				if (!arguments.Any())
				{
					return;
				}

				int idToDelete;
				extractTaskIdNumber(arguments.First(), out idToDelete);

				taskList.DeleteTaskWithId(idToDelete);
				return;
			}
			else if (commandName.Equals("set"))
			{
				Program.currentOperation = "set task parameters";
				SetTaskParameters(arguments, taskList);
			}
			else if (commandName.Equals("clear"))
			{
				Program.currentOperation = "clear task list";

				Console.WriteLine(Messages.ClearConfirmationMessage);

				if (Console.ReadKey(true).Key == ConsoleKey.Y)
				{
					taskList.Clear();
					Save(taskList);
					Console.WriteLine("Task list cleared.");
				}
			}
			else if (TaskCompleteRegex.IsMatch(commandName))
			{
				Program.currentOperation = "finish a task";

				int idToFinish;

				if (!arguments.Any())
				{ 
					return;
				}

				extractTaskIdNumber(arguments.First(), out idToFinish);

				Task taskToFinish = TaskWithId(taskList, idToFinish);
				taskToFinish.IsFinished = true;

				Save(taskList);

				Console.WriteLine(Messages.TaskWasFinished, taskToFinish.ID, taskToFinish.Description);
				return;
			}
			else if (VersionRegex.IsMatch(commandName))
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				AssemblyName assemblyName = executingAssembly.GetName();

				Console.WriteLine(
					"{0} version {1}.{2}.{3}",
					GetAssemblyAttribute<AssemblyProductAttribute>(attribute => attribute.Product),
					assemblyName.Version.Major,
					assemblyName.Version.Minor,
					assemblyName.Version.Build);
			}
			else 
			{
				Program.currentOperation = "recognize the command";
				throw new Exception(Messages.UnknownCommand);
			}

			return;
		}

		#region ID_GETTING_FUNCTIONS:

		static void extractTaskIdNumber(string what, out int toId)
		{
			if (!int.TryParse(what, out toId))
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

		#endregion

		static void DisplayTasks(this List<Task> taskList, IEnumerable<string> args, TaskDisplayCondition displayCondition)
		{
			Program.currentOperation = "display tasks";

			if (!args.Any())
			{ 
				ShowAll(taskList, displayCondition);
				return;
			}

			Match singleIdMatch = SingleIdRegex.Match(args.First());
			Match idRangeMatch = IdRangeRegex.Match(args.First());

			if (singleIdMatch.Success)
			{
				Program.currentOperation = "display a single task";

				int taskToDisplayId = int.Parse(singleIdMatch.Groups[1].ToString());
				Task taskToDisplay = taskList.TaskWithId(taskToDisplayId);

				if (!taskToDisplay.MatchesDisplayCondition(displayCondition))
				{
					Console.WriteLine(Messages.TaskWithIdDoesNotMatchTheCondition, taskToDisplayId);
				}

				return;
			}
			else if (idRangeMatch.Success)
			{
				Program.currentOperation = "display tasks in the ID range";

				int startingId = int.Parse(idRangeMatch.Groups[1].ToString());
				int endingId = int.Parse(idRangeMatch.Groups[2].ToString());

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

			throw new Exception(Messages.UnknownCommand);
		}
			
		/// <summary>
		/// Display all tasks in the task that match the specified condition.
		/// </summary>
		/// <param name="taskList">Task list.</param>
		/// <param name="displayCondition">Display condition.</param>
		static void ShowAll(this List<Task> taskList, TaskDisplayCondition displayCondition)
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
		}

		static void SetTaskParameters(IEnumerable<string> args, List<Task> taskList)
		{
			if (args.Count() < 1)
			{
				Console.WriteLine("Syntax:\nDescription:\t\ttaskman set id desc(//ription) new description");
				Console.WriteLine("Priority:\t\ttaskman set id priority 1..3");
				Console.WriteLine("Finish status:\t\ttaskman set id finished true/false");
				return;
			}

			if (args.Count() < 3)
			{ 
				throw new Exception(Messages.InsufficientSetParameters);
			}

			int taskId;
			extractTaskIdNumber(args.First(), out taskId);

			string whatToChange = args.ElementAt(1).ToLower();

			Task taskToUpdate = taskList.TaskWithId(taskId);

			if (whatToChange == "priority")
			{
				int priorityLevel;

				if (!int.TryParse(args.ElementAt(2), out priorityLevel) || 
					priorityLevel < 1 || 
					priorityLevel > 3)
				{
					throw new Exception(string.Format(Messages.UnknownPriorityLevel, args.ElementAt(2)));
				}

				taskToUpdate.PriorityLevel = (Priority)priorityLevel;
				Save(taskList);
				Console.WriteLine("Congrats! Task with Id {0} has changed its priority to {1}", taskToUpdate.ID, taskToUpdate.PriorityLevel);
				return;
			}
			else if (whatToChange.StartsWith("desc", StringComparison.CurrentCultureIgnoreCase))
			{
				string tmp="";
				for (int i = 2; i < args.Count(); i++)
					tmp += args.ElementAt(i) + " ";
				tmp = tmp.Trim();

				taskToUpdate.Description = tmp.Trim();

				Save(taskList);
				Console.WriteLine("Congrats! Task with Id {0} has changed its description to [{1}].", taskToUpdate.ID, tmp);
				return;
			}
			else if (whatToChange == "finished")
			{
				if (args.Count() < 3) return;
				bool arg;

				if(!bool.TryParse(args.ElementAt(2).ToLower(), out arg))
				{
					throw new Exception(Messages.UnknownBoolValue);
				}
				taskToUpdate.IsFinished = arg;
				Save(taskList);
				Console.WriteLine("Congrats! Task with id {0} has changed its finished state to {1}.", taskToUpdate.ID, arg);
				return;
			}
			else
			{
				Console.WriteLine("Unknown set command syntax. First mention the Id of the task,");
				Console.WriteLine("then type what to change (i.e. priority or desc), then depict the change.");
				SetTaskParameters(new string[0], null);
				return;
			}
		}

		static void DeleteTaskWithId(this List<Task> taskList, int idToDelete)
		{
			Task taskToDelete = taskList.TaskWithId(idToDelete);

			taskList.RemoveAll(task => (task.ID == idToDelete));

			taskList
				.Where(task => task.ID > idToDelete)
				.ForEach(task => (task.ID -= 1));
			
			Console.WriteLine(Messages.TaskWithIdWasDeleted, taskToDelete.ID, taskToDelete.Description);

			Save(taskList);

			Console.WriteLine(Messages.NewTaskList);

			ShowAll(taskList, 0);
			return;
		}
	}
}