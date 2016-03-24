using System;
using System.Linq;
using System.Collections.Generic;

using TaskMan.Objects;

namespace TaskMan.Control
{
	public interface ITaskFilter
	{
		/// <summary>
		/// Filters the specified task sequence using the 
		/// internal implementation logic.
		/// </summary>
		/// <param name="sequence">The source unfiltered sequence.</param>
		/// <returns>The filtered task sequence.</returns>
		IEnumerable<Task> Filter(IEnumerable<Task> sequence);

		/// <summary>
		/// Gets the filter's priority. The lower the value,
		/// the sooner the filter is applied. There order of filter
		/// application within the same priority level is undefined.
		/// </summary>
		int FilterPriority { get; }
	}
}

