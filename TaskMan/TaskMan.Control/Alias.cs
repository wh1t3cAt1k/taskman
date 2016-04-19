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

using System;

namespace TaskMan.Control
{
	/// <summary>
	/// An alias for a combination of a taskman verb command and several flags.
	/// </summary>
	public class Alias
	{
		/// <summary>
		/// Gets the name that the current alias should
		/// capture in the command line arguments.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the expansion that should be substituted
		/// for the current alias' name.
		/// </summary>
		public string Expansion { get; }

		/// <summary>
		/// Same as <see cref="Expansion"/>, but
		/// splits the string using the whitespace 
		/// delimiter and returns the resulting array.
		/// </summary>
		public string[] ExpansionArray { 
			get 
			{
				return Expansion.Split(
					new [] { ' ' }, 
					StringSplitOptions.RemoveEmptyEntries);
			}
		}

		public Alias(string name, string expansion)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (expansion == null) throw new ArgumentNullException(nameof(name));

			this.Name = name;
			this.Expansion = expansion;
		}
	}
}

