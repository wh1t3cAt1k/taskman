using System;
using System.Collections.Generic;

using Mono.Options;

namespace TaskMan
{
	/// <summary>
	/// Represents a command line flag (without actual type and value information).
	/// </summary>
	public abstract class Flag
	{
		/// <summary>
		/// Gets the name of the flag.
		/// </summary>
		public string Name { get; private set; }
		/// <summary>
		/// Gets the alias in the format of <see cref="Mono.Options.Option"/> prototype.
		/// </summary>
		public string Alias { get; private set; }
		/// <summary>
		/// Gets the set of commands that this flag makes sense with.
		/// </summary>
		public IEnumerable<string> MakesSenseWith { get; private set; }
		/// <summary>
		/// Gets the value indicating whether the flag has been explicitly set.
		/// </summary>
		public bool IsSet { get; protected set; }

		public Flag(string name, string alias, IEnumerable<string> makesSenseWith = null)
		{
			this.Name = name;
			this.Alias = alias;
			this.IsSet = false;
			this.MakesSenseWith = makesSenseWith ?? new string[0];
		}

		/// <summary>
		/// Register this instance within a <see cref="Mono.Options.OptionSet"/>.
		/// </summary>
		public abstract void AddToOptionSet(OptionSet optionSet);
	}

	public class Flag<T> : Flag
	{
		public T Value { get; private set; }

		public Flag(string name, string alias, IEnumerable<string> makesSenseWith = null)
			: base(name, alias, makesSenseWith)
		{
			this.Value = default(T);
		}

		public static implicit operator T(Flag<T> flag)
		{
			return flag.Value;
		}

		public void Set(T value)
		{
			this.IsSet = true;
			this.Value = value;
		}

		/// <summary>
		/// Register this instance within a <see cref="Mono.Options.OptionSet"/> using
		/// the current flag's alias for the option set prototyp. The action is a simple lambda 
		/// expression: any <typeparamref> value obtained during the parsing is set
		/// as the value for the current flag instance.
		/// </summary>
		public override void AddToOptionSet(OptionSet optionSet)
		{
			if (typeof(T) == typeof(bool))
			{
				// Special case for boolean flags, because
				// Mono.Options parser doesn't actually convert
				// boolean flags to type bool.
				// -
				optionSet.Add(this.Alias, value =>
					this.Set((T)(object)(value != null)));
			}
			else
			{
				optionSet.Add(this.Alias, (T value) => this.Set(value));
			}
		}
	}
}

