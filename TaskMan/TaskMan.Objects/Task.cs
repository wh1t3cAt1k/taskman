﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace TaskMan.Objects
{
	public enum TaskDisplayCondition
	{
		All = 0,
		Current = 1,
		Finished = 2
	}

	/// <summary>
	/// Denotes a particular task to be done, with a description text,
	/// importance level, and completion flag.
	/// </summary>
	[Serializable]
	public class Task: IComparable<Task>
	{
		public int ID { get; set; }
		public bool IsFinished { get; set; }
		public Priority Priority { get; set; }
		public string Description { get; set; }

		public Task(
			int id = 0,
			string description = "",
			Priority priority = Priority.Normal)
		{
			this.ID = id;
			this.Description = description;
			this.Priority = priority;
		}

		public override string ToString()
		{
			string prioritySymbol;

			switch (this.Priority)
			{
				case Priority.Important:
					prioritySymbol = "!";
					break;
				case Priority.Critical:
					prioritySymbol = "!!";
					break;
				default:
					prioritySymbol = string.Empty;
					break;
			}

			if (this.IsFinished)
			{
				prioritySymbol = "x";
			}

			return String.Format(
				"{0}{1,-2} id. {2,-6}{3}", 
				(this.IsFinished ? "--| " : string.Empty),
				prioritySymbol,
				ID, 
				Description);
		}

		public static readonly ConsoleColor NormalTaskColor = Console.ForegroundColor;
		public static readonly ConsoleColor FinishedTaskColor = ConsoleColor.Gray;
		public static readonly ConsoleColor ImportantTaskColor = ConsoleColor.Green;
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
				else if (this.Priority == Priority.Important)
				{
					return Task.ImportantTaskColor;
				}
				else if (this.Priority == Priority.Critical)
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
			else if (firstTask.Priority != secondTask.Priority)
			{
				return firstTask.Priority - secondTask.Priority;
			}
			else if (firstTask.ID != secondTask.ID)
			{
				return checked(firstTask.ID - secondTask.ID);
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