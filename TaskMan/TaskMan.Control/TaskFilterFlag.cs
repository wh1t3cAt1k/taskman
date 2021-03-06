﻿/*
 * taskman - convenient command line to-do list.
 * 
 * copyright (c) 2016 Pavel Kabir
 * 
 * This file is part of taskman.
 * 
 * taskman is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;

using TaskMan.Objects;

namespace TaskMan.Control
{
	/// <summary>
	/// A flag that is capable of filtering a task sequence
	/// using the provided predicate function.
	/// </summary>
	public class TaskFilterFlag<T>: Flag<T>, ITaskFilter
	{
		readonly Func<T, Task, int, bool> _filterPredicate;

		/// <summary>
		/// Initializes a new instance of the <see cref="TaskMan.Control.FilterFlag{T}"/> class.
		/// </summary>
		/// <param name="filterPredicate">
		/// The filter predicate function. Based on the current flag value (first
		/// function argument), and a given <see cref="Task"/> object (second
		/// function argument), and the current object's index (third function
		/// argument), must return a boolean value indicating whether 
		/// the task object must be included into the filtered sequence.
		/// </param>
		public TaskFilterFlag(string name, string alias, int filterPriority, Func<T, Task, int, bool> filterPredicate)
			: base(name, alias)
		{
			if (filterPredicate == null)
			{
				throw new ArgumentNullException(nameof(filterPredicate));
			}

			_filterPredicate = filterPredicate;
			this.FilterPriority = filterPriority;
		}

		public TaskFilterFlag(string name, string alias, int filterPriority, Func<T, Task, bool> filterPredicate)
			: this(name, alias, filterPriority, (flagValue, task, taskIndex) => filterPredicate(flagValue, task))
		{ }

		public IEnumerable<Task> Filter(IEnumerable<Task> taskSequence)
		{
			return taskSequence.Where(
				(task, taskIndex) => _filterPredicate(this.Value, task, taskIndex));
		}

		public int FilterPriority { get; private set; }
	}
}

