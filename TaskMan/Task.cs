using System;
using System.Runtime.Serialization;

namespace TaskMan
{
	/// <summary>
	/// Signifies the priority of a particular task
	/// </summary>
	public enum Priority
	{
		Average = 1,
		High = 2,
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
		public bool Finished { get; set; }
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
				case Priority.High:
					prioritySymbol = '!';
					break;
				case Priority.Critical:
					prioritySymbol = '#';
					break;
				default:
					prioritySymbol = ' ';
					break;
			}

			if (this.Finished)
			{
				prioritySymbol = 'x';
			}

			return String.Format(
				"{0} {3} id. {1}\t {2}", 
				prioritySymbol, 
				ID, 
				Description, 
				(Finished ? "--| fin." : "cur."));
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
			else if (firstTask.Finished != secondTask.Finished)
			{
				return (firstTask.Finished ? 1 : -1);
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