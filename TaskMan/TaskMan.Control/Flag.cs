using System;
using System.Collections.Generic;

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
		/// Gets the name of the flag.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the alias in the format of <see cref="Mono.Options.Option"/> prototype.
		/// </summary>
		public string Alias { get; private set; }

		/// <summary>
		/// Gets the value indicating whether the flag has been explicitly set.
		/// </summary>
		public bool IsSet { get; protected set; }

		protected Flag(string name, string alias)
		{
			this.Name = name;
			this.Alias = alias;
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
						this.Name));
				}
			}
			private set 
			{
				_value = value;
			}
		}

		public Flag(string name, string alias)
			: base(name, alias)
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
				optionSet.Add(this.Alias, value => {
					if (value is T) 
					{
						this.Set((T)(object)value);
					}
					else
					{
						throw new NotSupportedException();
					}
				});
			}
		}
	}

	public static class FlagCollectionExtensions
	{
		public static OptionSet GetOptionSet(this IEnumerable<Flag> flagCollection)
		{
			OptionSet resultOptionSet = new OptionSet();

			flagCollection.ForEach(flag => flag.AddToOptionSet(resultOptionSet));

			return resultOptionSet;
		}
	}
}

