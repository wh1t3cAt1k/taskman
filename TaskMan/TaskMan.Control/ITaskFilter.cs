using System;
using System.Linq;
using System.Collections.Generic;

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
	}
}

