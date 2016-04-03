using System;

namespace TaskMan.Objects
{
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
			int id = -1,
			string description = "",
			Priority priority = Priority.Normal)
		{
			this.ID = id;
			this.Description = description;
			this.Priority = priority;
		}

		public override string ToString()
		{
			return $"{ID} {Description}";
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