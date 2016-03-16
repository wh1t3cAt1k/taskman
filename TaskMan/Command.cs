using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TaskMan
{
	/// <summary>
	/// Represents a TaskMan verb command (e.g. add, delete, etc.)
	/// </summary>
	public class Command
	{
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

		public Command(string name, string regexPattern, params Flag[] supportedFlags)
			: this(
				name, 
				new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase), 
				supportedFlags)
		{ }

		public Command(string name, Regex regularExpression, params Flag[] supportedFlags)
		{
			this.Name = name;
			this.RegularExpression = regularExpression;
			this.SupportedFlags = supportedFlags;
		}
	}

	public static class CommandExtensions
	{
		/// <summary>
		/// Returns a set of commands whose regular
		/// expressions make a match with the provided value.
		/// </summary>
		public static IEnumerable<Command> MatchingCommands(this IEnumerable<Command> commands, string expression)
		{
			return
				commands.Where(command => command.RegularExpression.IsMatch(expression));
		}
	}
}

