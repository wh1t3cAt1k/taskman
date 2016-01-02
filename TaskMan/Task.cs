using System;
using System.IO;
using System.Runtime.Serialization;

namespace TaskMan
{
	/// <summary>
	/// Signifies the priority of a particular task
	/// </summary>
	public enum Priority
	{
		Average = 1,
		Important = 2,
		Critical = 3
	}

	public enum TaskDisplayCondition
	{
		All = 0,
		Current = 1,
		Finished = 2
	}

	/// <summary>
	/// Denotes a particular task to be completed, with a description,
	/// importance level, and completion flag.
	/// </summary>
	[Serializable]
	public class Task: IComparable<Task>
	{
		public int ID { get; set; }
		public bool IsFinished { get; set; }
		public Priority PriorityLevel { get; set; }
		public string Description { get; set; }

		public Task(
			int id = 0,
			string description = "",
			Priority priority = Priority.Average)
		{
			this.ID = id;
			this.Description = description;
			this.PriorityLevel = priority;
		}

		public override string ToString()
		{
			char prioritySymbol;

			switch (this.PriorityLevel)
			{
				case Priority.Average:
					prioritySymbol = ' ';
					break;
				case Priority.Important:
					prioritySymbol = '!';
					break;
				case Priority.Critical:
					prioritySymbol = '#';
					break;
				default:
					prioritySymbol = ' ';
					break;
			}

			if (this.IsFinished)
			{
				prioritySymbol = 'x';
			}

			return String.Format(
				"{0} {3} id. {1}\t {2}", 
				prioritySymbol, 
				ID, 
				Description, 
				(IsFinished ? "--| fin." : "cur."));
		}

		/// <summary>
		/// Return a boolean value signifying whether the task matches
		/// the given display condition.
		/// </summary>
		/// <returns><c>true</c>, if the current task matches the given display condition, <c>false</c> otherwise.</returns>
		/// <param name="displayCondition">The task display condition.</param>
		public bool MatchesDisplayCondition(TaskDisplayCondition displayCondition)
		{
			switch (displayCondition)
			{
				case TaskDisplayCondition.All:
					return true;
				case TaskDisplayCondition.Current:
					return !this.IsFinished;
				case TaskDisplayCondition.Finished:
					return this.IsFinished;
				default:
					throw new InvalidOperationException(Messages.UnknownDisplayCondition);
			}
		}

		public static readonly ConsoleColor NormalTaskColor = Console.ForegroundColor;
		public static readonly ConsoleColor FinishedTaskColor = ConsoleColor.Gray;
		public static readonly ConsoleColor ImportantTaskColor = ConsoleColor.Cyan;
		public static readonly ConsoleColor CriticalTaskColor = ConsoleColor.Yellow;

		/// <summary>
		/// Gets the foreground color with which the Task would be output
		/// into the console.
		/// </summary>
		/// <value>The color of the console output.</value>
		public ConsoleColor ConsoleOutputColor
		{
			get
			{
				if (this.IsFinished)
				{
					return Task.FinishedTaskColor;
				}
				else if (this.PriorityLevel == Priority.Important)
				{
					return Task.ImportantTaskColor;
				}
				else if (this.PriorityLevel == Priority.Critical)
				{
					return Task.CriticalTaskColor;
				}
				else
				{
					return Task.NormalTaskColor;
				}
			}
		}

		/// <summary>
		/// Writes the string representation of the current task (followed by a line terminator) into
		/// the standard output stream or explicitly provided <see cref="TextWriter"/> output. 
		/// For console output, optional background and foreground <see cref="ConsoleColor"/>
		/// parameters can be specified to override the standard colouring scheme.
		/// </summary>
		public void Display(
			TextWriter outputStream = null,
			ConsoleColor? backgroundColor = null,
			ConsoleColor? foregroundColor = null)
		{
			if (outputStream == null)
			{
				outputStream = Console.Out;
			}

			if (!backgroundColor.HasValue)
			{
				backgroundColor = Console.BackgroundColor;
			}

			if (!foregroundColor.HasValue)
			{
				foregroundColor = this.ConsoleOutputColor;
			}

			ConsoleColor originalBackgroundColor = Console.BackgroundColor;
			ConsoleColor originalForegroundColor = Console.ForegroundColor;

			Console.BackgroundColor = backgroundColor.Value;
			Console.ForegroundColor = foregroundColor.Value;

			outputStream.WriteLine(this);

			Console.BackgroundColor = originalBackgroundColor;
			Console.ForegroundColor = originalForegroundColor;
		}

		public static int CompareTasks(Task firstTask, Task secondTask)
		{
			if (object.ReferenceEquals(firstTask, secondTask))
			{
				return 0;
			}
			else if (firstTask == null || secondTask == null)
			{
				return (firstTask == null ? 1 : -1);
			}
			else if (firstTask.IsFinished != secondTask.IsFinished)
			{
				return (firstTask.IsFinished ? 1 : -1);
			}
			else if (firstTask.PriorityLevel != secondTask.PriorityLevel)
			{
				return firstTask.PriorityLevel - secondTask.PriorityLevel;
			}
			else
			{
				return firstTask.Description.CompareTo(secondTask.Description);
			}
		}

		public static Comparison<Task> TaskComparison = new Comparison<Task>(CompareTasks);

		public int CompareTo(Task otherTask)
		{
			return Task.CompareTasks(this, otherTask);
		}
	}
}