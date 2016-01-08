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
		static Regex IdRangeRegex = new Regex(@"^([0-9]+)-([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex SingleIdRegex = new Regex(@"^([0-9]+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskAddRegex = new Regex(@"(^add$)|(^new$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskCompleteRegex = new Regex(@"(^complete$)|(^finish$)|(^accomplish$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskDeleteRegex = new Regex(@"(^delete$)|(^remove$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskDisplayRegex = new Regex(@"^(show|display|view)(p|f|all)?$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex TaskPriorityRegex = new Regex(@"^\[([0-9]+)\]$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		static Regex VersionRegex = new Regex(@"^--version$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

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
				arguments = new LinkedList<string>(new [] { "showall" }); 
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
				Program.currentOperation = "display tasks";

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

				return;
			}
			else if (TaskDeleteRegex.IsMatch(commandName))
			{
				Program.currentOperation = "delete tasks";
				DeleteTasks(arguments, taskList);
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

				ExtractTaskIdNumber(arguments.First(), out idToFinish);

				Task taskToFinish = TaskWithId(taskList, idToFinish);
				taskToFinish.IsFinished = true;

				Save(taskList);

				Console.WriteLine(Messages.TaskWasFinished, taskToFinish.ID, taskToFinish.Description);
				return;
			}
			else if (VersionRegex.IsMatch(commandName))
			{
				Program.currentOperation = "display the taskman version";

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
				Program.currentOperation = "recognize the command";
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
				Program.currentOperation = "display tasks in the ID range";

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
			if (cliArguments.Count < 1)
			{
				Console.WriteLine("Syntax:\nDescription:\t\ttaskman set id desc(//ription) new description");
				Console.WriteLine("Priority:\t\ttaskman set id priority 1..3");
				Console.WriteLine("Finish status:\t\ttaskman set id finished true/false");
				return;
			}

			if (cliArguments.Count < 3)
			{ 
				throw new Exception(Messages.InsufficientSetParameters);
			}

			int taskId;
			ExtractTaskIdNumber(cliArguments.First(), out taskId);

			string whatToChange = cliArguments.ElementAt(1).ToLower();

			Task taskToUpdate = taskList.TaskWithId(taskId);

			if (whatToChange == "priority")
			{
				int priorityLevel;

				if (!int.TryParse(cliArguments.ElementAt(2), out priorityLevel) || 
					priorityLevel < 1 || 
					priorityLevel > 3)
				{
					throw new Exception(string.Format(Messages.UnknownPriorityLevel, cliArguments.ElementAt(2)));
				}

				taskToUpdate.PriorityLevel = (Priority)priorityLevel;
				Save(taskList);
				Console.WriteLine("Congrats! Task with Id {0} has changed its priority to {1}", taskToUpdate.ID, taskToUpdate.PriorityLevel);
				return;
			}
			else if (whatToChange.StartsWith("desc", StringComparison.CurrentCultureIgnoreCase))
			{
				string tmp="";
				for (int i = 2; i < cliArguments.Count(); i++)
					tmp += cliArguments.ElementAt(i) + " ";
				tmp = tmp.Trim();

				taskToUpdate.Description = tmp.Trim();

				Save(taskList);
				Console.WriteLine("Congrats! Task with Id {0} has changed its description to [{1}].", taskToUpdate.ID, tmp);
				return;
			}
			else if (whatToChange == "finished")
			{
				if (cliArguments.Count() < 3) return;
				bool arg;

				if(!bool.TryParse(cliArguments.ElementAt(2).ToLower(), out arg))
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
				SetTaskParameters(new LinkedList<string>(), null);
				return;
			}
		}

		/// <summary>
		/// Encapsulates the task deletion logic in one method.
		/// </summary>
		/// <param name="cliArguments">Command line arguments.</param>
		/// <param name="taskList">Task list.</param>
		public static void DeleteTasks(LinkedList<string> cliArguments, List<Task> taskList)
		{
			if (!cliArguments.Any())
			{
				Console.WriteLine(Messages.NoTaskIdProvided, Program.currentOperation);
				return;
			}

			int idToDelete;
			ExtractTaskIdNumber(cliArguments.First(), out idToDelete);

			Task taskToDelete = taskList.TaskWithId(idToDelete);

			taskList.RemoveAll(task => (task.ID == idToDelete));

			taskList
				.Where(task => task.ID > idToDelete)
				.ForEach(task => (task.ID -= 1));
			
			Console.WriteLine(Messages.TaskWithIdWasDeleted, taskToDelete.ID, taskToDelete.Description);

			Save(taskList);

			Console.WriteLine(Messages.NewTaskList);

			DisplayTasks(new LinkedList<string>(), taskList, TaskDisplayCondition.All);

			return;
		}
	}
}