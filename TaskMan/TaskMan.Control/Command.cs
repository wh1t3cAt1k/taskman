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
		public bool IsReadUpdateDelete { get; private set; }

		/// <summary>
		/// Gets the command name.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the regular expression corresponding to the command.
		/// </summary>
		public Regex RegularExpression { get; private set; }

		/// <summary>
		/// Gets the set of flags supported by this command.
		/// </summary>
		public IEnumerable<Flag> SupportedFlags { get; private set; }

		public Command(string name, Regex regularExpression, bool isReadUpdateDelete, IEnumerable<Flag> supportedFlags)
		{
			this.Name = name;
			this.RegularExpression = regularExpression;
			this.SupportedFlags = supportedFlags;
			this.IsReadUpdateDelete = isReadUpdateDelete;
		}
	}

	public static class CommandExtensions
	{
		/// <summary>
		/// Returns a set of commands whose regular
		/// expressions make a match with the provided value.
		/// </summary>
		public static IEnumerable<Command> Matching(this IEnumerable<Command> commands, string expression)
		{
			return
				commands.Where(command => command.RegularExpression.IsMatch(expression));
		}
	}
}

