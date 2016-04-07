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

		public DateTime? DueDate { get; set; }

		public Task(
			int id = -1,
			string description = "",
			Priority priority = Priority.Normal,
			DateTime? dueDate = null)
		{
			this.ID = id;
			this.Description = description;
			this.Priority = priority;
			this.DueDate = dueDate;
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
		/// Represents a task comparison step that
		/// is primarily used during task sorting.
		/// </summary>
		public class ComparisonStep
		{
			public PropertyInfo Property { get; }
			public SortingDirection Direction { get; }

			public ComparisonStep(
				string propertyName,
				SortingDirection direction = SortingDirection.Ascending)
			{
				this.Property = typeof(Task).GetProperty(
					propertyName, 
					BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

				this.Direction = direction;

				if (this.Property == null)
				{
					throw new TaskManException(
						Messages.CannotSortNoSuchProperty, 
						propertyName);
				}
			}
		}

		/// <summary>
		/// Gets the task comparison delegate based on the sorting steps
		/// provided.
		/// </summary>
		public static Comparison<Task> GetComparison(IEnumerable<ComparisonStep> sortingSteps)
		{
			// Each comparison step can either return a definitive integer value
			// like CompareTo() does, in which case further steps won't be performed,
			// or it can return null, which means "inconclusive".
			// -
			List<Func<Task, Task, int?>> comparisonSteps = 
				new List<Func<Task, Task, int?>>();

			// The first two steps always check reference equality
			// and 'null' case -- a task that is 'null' is always
			// 'smaller' than a non-null task.
			// -
			comparisonSteps.Add((firstTask, secondTask) =>
			{
				return object.ReferenceEquals(firstTask, secondTask) ?
		        	0 :
					null as int?;
			});

			comparisonSteps.Add((firstTask, secondTask) =>
			{
				if (firstTask == null && secondTask == null)
				{
					return 0;
				}
				else if (firstTask == null || secondTask == null)
				{
					return (firstTask == null ? -1 : 1);
				}
				else
				{
					return null;
				}
			});

			foreach (ComparisonStep sortingStep in sortingSteps)
			{
				comparisonSteps.Add((firstTask, secondTask) =>
				{
					IComparable firstPropertyValue = (IComparable)sortingStep.Property.GetValue(firstTask);
					IComparable secondPropertyValue = (IComparable)sortingStep.Property.GetValue(secondTask);

					int? comparisonResult =
						(int)sortingStep.Direction *
						firstPropertyValue.CompareTo(secondPropertyValue);
					
					return (comparisonResult != 0 ? comparisonResult : null);
				});
			}

			// The result is a comparison that sequentially
			// compares via each comparison step. It stops when
			// any step returns a definite (non-null) value, and
			// returns that value as the overall comparison result.
			// -
			return new Comparison<Task>((firstTask, secondTask) =>
			{
				int? comparisonResult;

				foreach (Func<Task, Task, int?> nextComparison in comparisonSteps)
				{
					comparisonResult = nextComparison(firstTask, secondTask);
					if (comparisonResult.HasValue) return comparisonResult.Value;
				}

				return 0;
			});
		}

		public static int Compare(Task firstTask, Task secondTask)
		{
			Comparison<Task> defaultComparison = GetComparison(new []
			{
				new ComparisonStep(nameof(Task.ID), SortingDirection.Ascending),
			});

			return defaultComparison(firstTask, secondTask);
		}

		public int CompareTo(Task otherTask)
		{
			return Compare(this, otherTask);
		}
	}
}