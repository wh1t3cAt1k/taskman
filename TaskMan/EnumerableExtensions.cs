using System;
using System.Collections.Generic;

namespace TaskMan
{
	/// <summary>
	/// A static class providing various extension methods for
	/// <see cref="IEnumerable&lt;T&gt;"/> generic sequences.
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

		/// <summary>
		/// Performs an action upon the first element in a sequence that satisfies a given condition.
		/// </summary>
		/// <param name="sequence">A sequence of <typeparamref>T</typeparamref> elements.</param>
		/// <param name="predicate">
		/// A function indicating whether the action should be performed on a given element.
		/// </param>
		/// <param name="action">
		/// An action to be performed upon the first element of the sequence
		/// that satisfies the <paramref name="predicate"/>.
		/// </param>
		/// <typeparam name="T">The type of elements in the sequence.</typeparam>
		public static void ForFirst<T>(this IEnumerable<T> sequence, Func<T, bool> predicate, Action<T> action)
		{
			foreach (T item in sequence)
			{
				if (predicate(item))
				{
					action(item);
					break;
				}
			}
		}

		/// <summary>
		/// Determines if the specified sequence contains exactly one element.
		/// </summary>
		public static bool IsSingleton<T>(this IEnumerable<T> sequence)
		{
			IEnumerator<T> enumerator = sequence.GetEnumerator();
			return enumerator.MoveNext() && !enumerator.MoveNext();
		}
	}
}

