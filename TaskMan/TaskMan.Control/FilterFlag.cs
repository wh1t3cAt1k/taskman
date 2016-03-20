using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskMan.Control
{
	/// <summary>
	/// A flag that is capable of filtering a task sequence
	/// using the provided predicate function.
	/// </summary>
	public class FilterFlag<T>: Flag<T>, ITaskFilter
	{
		readonly Func<T, Task, bool> _filterPredicate;

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskMan.Control.FilterFlag{T}"/> class.
		/// </summary>
		/// <param name="filterPredicate">
		/// The filter predicate function. Based on the current flag value (first
		/// function argument), and a given <see cref="Task"/> object (second
		/// function argument), must return a boolean value indicating whether 
		/// the task object must be included into the filtered sequence.
		/// </param>
		public FilterFlag(string name, string alias, Func<T, Task, bool> filterPredicate)
			: base(name, alias)
		{
			if (filterPredicate == null)
			{
				throw new ArgumentNullException(nameof(filterPredicate));
			}

			_filterPredicate = filterPredicate;
		}

		public IEnumerable<Task> Filter(IEnumerable<Task> taskSequence)
		{
			return taskSequence.Where(task => _filterPredicate(this.Value, task));
		}
	}
}

