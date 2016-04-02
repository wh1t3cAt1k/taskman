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

