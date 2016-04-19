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
using System.Linq;

namespace TaskMan
{
	/// <summary>
	/// A static class providing various extension methods for
	/// <see cref="LinkedList&lt;T&gt;"/> generic sequences.
	/// </summary>
	public static class LinkedListExtensions
	{
		/// <summary>
		/// Removes and returns the first element from a linked list.
		/// </summary>
		/// <returns>The first element in a linked list prior to its removal.</returns>
		/// <param name="linkedList">The linked list to remove the first element from.</param>
		/// <typeparam name="T">The type of elements in the list.</typeparam>
		public static T PopFirst<T>(this LinkedList<T> linkedList)
		{
			T returnValue = linkedList.First();
			linkedList.RemoveFirst();

			return returnValue;
		}
	}
}

