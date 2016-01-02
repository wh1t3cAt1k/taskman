using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;

namespace TaskMan
{
	/// <summary>
	/// A static class providing various extension methods for
	/// <see cref="IEnumerable"/> generic sequences.
	/// </summary>
	public static class IEnumerableExtensions
	{
		/// <summary>
		/// Peforms an action upon each member of a sequence.
		/// </summary>
		/// <param name="sequence">A sequence of <typeparamref>T/<typeparamref> elements.</param>
		/// <param name="action">An action to be performed upon each element of the sequence.</param>
		/// <typeparam name="T">The type of elements in the sequence.</typeparam>
		public static void ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			foreach (T item in sequence)
			{
				action(item);
			}
		}
	}
}

