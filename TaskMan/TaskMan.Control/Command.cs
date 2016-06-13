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
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TaskMan.Control
{
	/// <summary>
	/// Represents a TaskMan verb command (e.g. add, delete, etc.)
	/// </summary>
	public class Command
	{
		/// <summary>
		/// Gets the current command's description.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets a value indicating whether this instance is a 
		/// read / update / delete command.
		/// </summary>
		/// <value>
		/// <c>true</c> if this instance is read / update / delete; 
		/// otherwise, <c>false</c>.
		/// </value>
		public bool IsReadUpdateDelete { get; }

		/// <summary>
		/// Gets the prototype (regular expression) corresponding 
		/// to the command. By convention, should contain allowed 
		/// command names separated by pipe character.
		///  
		/// Can also optionally contain brackets, start line symbol 
		/// and end line symbol.
		/// </summary>
		public string Prototype { get; }

		/// <summary>
		/// Gets the command's example usage as the longest component
		/// in its regular expression prototype.
		/// </summary>
		/// <example>
		/// For example, if the command's prototype is 
		/// <c>^(create|add|delete)$</c>, this property
		/// must return <c>add</c>.
		/// </example>
		public string Usage
		{
			get
			{
				return PrototypeHelper
					.GetComponents(this.Prototype)
					.OrderByDescending(value => value.Length)
					.First();
			}
		}

		/// <summary>
		/// Gets the set of flags supported by this command.
		/// </summary>
		public IEnumerable<Flag> SupportedFlags { get; }

		/// <summary>
		/// Gets the set of flags required by this command.
		/// </summary>
		public IEnumerable<Flag> RequiredFlags { get; }

		/// <summary>
		/// Gets the action corresponding to the current
		/// command.
		/// </summary>
		public Action Action { get; }

		public Command(
			string description,
			string prototype, 
			bool isReadUpdateDelete,
			IEnumerable<Flag> supportedFlags = null,
			IEnumerable<Flag> requiredFlags = null,
			Action action = null)
		{
			this.Description = description;
			this.Prototype = prototype;
			this.IsReadUpdateDelete = isReadUpdateDelete;
			this.SupportedFlags = supportedFlags ?? new Flag[] { };
			this.RequiredFlags = requiredFlags ?? new Flag[] { };
			this.Action = action ?? (() => { throw new NotImplementedException(); });
		}
	}

	public static class CommandExtensions
	{
		/// <summary>
		/// Returns a set of commands whose prototypes make a match 
		/// with the provided value.
		/// </summary>
		public static IEnumerable<Command> Matching(this IEnumerable<Command> commands, string expression)
		{
			IEnumerable<Command> strictMatches = commands.Where(
				command => new Regex(command.Prototype).IsMatch(expression));

			if (strictMatches.Any()) return strictMatches;

			IEnumerable<Command> prefixMatches = commands.Where(command =>
			{
				IEnumerable<string> commandComponents = 
					PrototypeHelper.GetComponents(command.Prototype);

				return commandComponents.Any(
					component => component.StartsWith(
						expression, 
						StringComparison.OrdinalIgnoreCase));
			});

			return prefixMatches;
		}

		/// <summary>
		/// For a given set of commands, extracts command names 
		/// that are similar to the provided expression in terms
		/// of string distance.
		/// </summary>
		public static IEnumerable<string> SimilarNames(
			this IEnumerable<Command> commands, 
			string expression,
			int maximumEditDistance)
		{
			return commands.SelectMany(
				command => PrototypeHelper
					.GetComponents(command.Prototype)
					.Where(name => name.LevenshteinDistance(expression) <= maximumEditDistance));
		}
	}
}

