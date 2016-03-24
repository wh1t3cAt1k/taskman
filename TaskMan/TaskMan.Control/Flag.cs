using System;
using System.Collections.Generic;
using System.Linq;

using Mono.Options;

namespace TaskMan.Control
{
	/// <summary>
	/// Represents a command line flag 
	/// (without any actual flag value type information).
	/// </summary>
	public abstract class Flag
	{
		/// <summary>
		/// Gets the description of the flag.
		/// </summary>
		public string Description { get; private set; }

		/// <summary>
		/// Gets the flag prototype in the format of 
		/// <see cref="Mono.Options.Option"/> prototype.
		/// </summary>
		public string Prototype { get; private set; }

		/// <summary>
		/// Gets the value indicating whether the flag has been explicitly set.
		/// </summary>
		public bool IsSet { get; protected set; }

		protected Flag(string description, string alias)
		{
			this.Description = description;
			this.Prototype = alias;
			this.IsSet = false;
		}

		/// <summary>
		/// Register this instance within a <see cref="Mono.Options.OptionSet"/>.
		/// </summary>
		public abstract void AddToOptionSet(OptionSet optionSet);

		/// <summary>
		/// Resets the flag so that <see cref="Flag.IsSet"/> 
		/// property returns <c>false</c>.
		/// </summary>
		public abstract void Reset();

		/// <summary>
		/// Gets the actual name of a command line flag
		/// provided by the user in the command line arguments.
		/// </summary>
		public string GetProvidedName(IEnumerable<string> commandLineArguments)
		{
			IEnumerable<string> flagNames = this.Prototype
				.Split('|')
				.Select(value => value.Replace("=", string.Empty).Replace(":", string.Empty))
				.SelectMany(flagName => 
					new string[] 
					{
						$"-{flagName}",
						$"--{flagName}",
						$"/{flagName}"
					});

			return commandLineArguments.First(argument => flagNames.Contains(argument));
		}
	}

	/// <summary>
	/// Represents a command line flag with an explicit type information.
	/// </summary>
	public class Flag<T> : Flag
	{
		T _value;

		/// <summary>
		/// Gets the flag value.
		/// </summary>
		/// <exception cref="System.InvalidOperationException">
		/// Thrown if the flag has not been explicitly set.
		/// </exception>
		public T Value 
		{ 
			get
			{
				if (this.IsSet)
				{
					return _value;
				}
				else
				{
					throw new InvalidOperationException(string.Format(
						Messages.FlagNotSet,
						this.Prototype));
				}
			}
			private set 
			{
				_value = value;
			}
		}

		public Flag(string description, string prototype)
			: base(description, prototype)
		{ }

		public static implicit operator T(Flag<T> flag)
		{
			return flag.Value;
		}

		public void Set(T value)
		{
			this.IsSet = true;
			this.Value = value;
		}

		public override void Reset()
		{
			this.IsSet = false;
			this.Value = default(T);
		}

		/// <summary>
		/// Register this instance within a <see cref="Mono.Options.OptionSet"/>. 
		/// The action passed into the option is a simple lambda expression: 
		/// any <typeparamref> value obtained during the flag parsing is set as 
		/// the value for the current flag instance.
		/// </summary>
		public override void AddToOptionSet(OptionSet optionSet)
		{
			if (typeof(T) == typeof(bool))
			{
				// Special case for boolean flags, because
				// Mono.Options parser doesn't actually convert
				// boolean flags to type bool.
				// -
				optionSet.Add(this.Prototype, this.Description, value =>
					this.Set(
						(T)(object)(value != null)));
			}
			else if (typeof(T).IsSubclassOf(typeof(Enum)))
			{
				// Special case for enum-type flags, because
				// we want to parse both integer and string
				// representations of enum values.
				// -
				optionSet.Add(this.Prototype, this.Description, value => 
					this.Set(
						(T)Enum.Parse(typeof(T), value)));
			}
			else
			{
				optionSet.Add(this.Prototype, this.Description, (T value) => 
					this.Set(value));
			}
		}
	}

	public static class FlagCollectionExtensions
	{
		/// <summary>
		/// Given a flag collection, adds each of the flag to an option set
		/// and return the resulting option set.
		/// </summary>
		public static OptionSet GetOptionSet(this IEnumerable<Flag> flagCollection)
		{
			OptionSet resultOptionSet = new OptionSet();

			flagCollection.ForEach(flag => flag.AddToOptionSet(resultOptionSet));

			return resultOptionSet;
		}
	}
}

