using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

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

		public enum SortingDirection
		{
			Ascending = 1,
			Descending = -1
		}

		/// <summary>
		/// Represents a task sorting step.
		/// </summary>
		public class SortingStep
		{
			public FieldInfo Field { get; }
			public SortingDirection Direction { get; }

			public SortingStep(
				FieldInfo field, 
				SortingDirection direction = SortingDirection.Ascending)
			{
				this.Field = field;
				this.Direction = direction;
			}

			public SortingStep(
				string fieldName,
				SortingDirection direction = SortingDirection.Ascending)
				: this(
					typeof(Task).GetField(fieldName, BindingFlags.IgnoreCase),
					direction)
			{ }
		}

		/// <summary>
		/// Gets the task comparison delegate based on the sorting steps
		/// provided.
		/// </summary>
		public static Comparison<Task> GetComparison(IEnumerable<SortingStep> sortingSteps)
		{
			// Each comparison step can either return a definitive integer value
			// like CompareTo() does, in which case further steps won't be performed,
			// or it can return null, which means "inconclusive".
			// -
			List<Func<Task, Task, int?>> comparisonSteps = 
				new List<Func<Task, Task, int?>>();

			comparisonSteps.Add((firstTask, secondTask) =>
			{
				return object.ReferenceEquals(firstTask, secondTask) ?
					0 :
					null;
			});

			comparisonSteps.Add((firstTask, secondTask) =>
			{
				if (firstTask == null && secondTask == null)
				{
					return 0;
				}
				else if (firstTask == null || secondTask == null)
				{
					return (firstTask == null ? 1 : -1);
				}
				else
				{
					return null;
				}
			});

			foreach (SortingStep sortingStep in sortingSteps)
			{
				comparisonSteps.Add((firstTask, secondTask) =>
				{
					int? comparisonResult =
						(int)sortingStep.Direction *
						((IComparable)sortingStep.Field.GetValue(firstTask)).CompareTo(secondTask);
					
					return (comparisonResult != 0 ? comparisonResult : null);
				});
			}

			return new Comparison<Task>((firstTask, secondTask) =>
				{
					int? comparisonResult = null;

					foreach (Func<Task, Task, int?> sortingStep in comparisonSteps)
					{
						comparisonResult = sortingStep(firstTask, secondTask);
						if (comparisonResult.HasValue) return comparisonResult.Value;
					}

					return 0;
				});
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