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
			public PropertyInfo Property { get; }
			public SortingDirection Direction { get; }

			public SortingStep(
				string propertyName,
				SortingDirection direction = SortingDirection.Ascending)
			{
				this.Property = typeof(Task).GetProperty(propertyName, BindingFlags.IgnoreCase);
				this.Direction = direction;

				if (this.Property == null)
				{
					throw new TaskManException(Messages.CannotSortNoSuchProperty);
				}
			}
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

			foreach (SortingStep sortingStep in sortingSteps)
			{
				comparisonSteps.Add((firstTask, secondTask) =>
				{
					int? comparisonResult =
						(int)sortingStep.Direction *
		                ((IComparable)sortingStep.Property.GetValue(firstTask)).CompareTo(secondTask);
					
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
				int? comparisonResult = null;

				foreach (Func<Task, Task, int?> sortingStep in comparisonSteps)
				{
					comparisonResult = sortingStep(firstTask, secondTask);
					if (comparisonResult.HasValue) return comparisonResult.Value;
				}

				return 0;
			});
		}

		static readonly Comparison<Task> DefaultComparison = GetComparison(new[]
		{
			new SortingStep(nameof(Task.IsFinished), SortingDirection.Ascending),
			new SortingStep(nameof(Task.Priority), SortingDirection.Ascending),
			new SortingStep(nameof(Task.ID), SortingDirection.Ascending),
			new SortingStep(nameof(Task.Description), SortingDirection.Ascending)
		});

		public static int Compare(Task firstTask, Task secondTask)
		{
			return DefaultComparison(firstTask, secondTask);
		}

		public int CompareTo(Task otherTask)
		{
			return Compare(this, otherTask);
		}
	}
}