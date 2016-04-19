/*
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

