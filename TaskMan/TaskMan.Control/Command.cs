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
		/// in its regular expression.
		/// </summary>
		/// <example>
		/// For example, if the command's regular expression
		/// is <c>^(create|add|delete)$</c>, this property
		/// will return <c>create</c>.
		/// </example>
		public string ExampleUsage 
		{
			get
			{
				return PrototypeHelper
					.GetComponents(this.Prototype)
					.OrderBy(value => value.Length)
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

		public Command(
			string prototype, 
			bool isReadUpdateDelete, 
			IEnumerable<Flag> supportedFlags = null,
			IEnumerable<Flag> requiredFlags = null)
		{
			this.Prototype = prototype;
			this.IsReadUpdateDelete = isReadUpdateDelete;
			this.SupportedFlags = supportedFlags ?? new Flag[] { };
			this.RequiredFlags = requiredFlags ?? new Flag[] { };
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
			return commands.Where(command => 
			{
				Regex prototypeRegex = new Regex(command.Prototype);
				return prototypeRegex.IsMatch(expression);
			});
		}
	}
}

