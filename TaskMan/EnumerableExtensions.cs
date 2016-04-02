using System;
using System.Collections.Generic;

namespace TaskMan
{
	/// <summary>
	/// A static class providing various extension methods for
	/// <see cref="IEnumerable&lt;T&gt;"/> generic sequences.
	/// </summary>
	public static class EnumerableExtensions
	{
		/// <summary>
		/// Peforms an action upon each member of a sequence.
		/// </summary>
		/// <param name="sequence">A sequence of <typeparamref>T<typeparamref> elements.</param>
		/// <param name="action">An action to be performed upon each element of the sequence.</param>
		/// <typeparam name="T">The type of elements in the sequence.</typeparam>
		/// <returns>The number of times the action has been performed.</returns>
		public static int ForEach<T>(this IEnumerable<T> sequence, Action<T> action)
		{
			return ForEach(sequence, (T value, int index) => action(value));
		}

		/// <summary>
		/// Performs an action upon each memeber of a sequence by incorporating
		/// each element's zero-based index.
		/// </summary>
		/// <param name="sequence">A sequence of <typeparamref>T<typeparamref> elements.</param>
		/// <param name="action">An action to be performed upon each element of the sequence.</param>
		/// <typeparam name="T">The type of elements in the sequence.</typeparam>
		/// <returns>The number of times the action has been performed.</returns>
		public static int ForEach<T>(this IEnumerable<T> sequence, Action<T, int> action)
		{
			int currentIndex = 0;

			foreach (T item in sequence)
			{
				action(item, currentIndex);
				++currentIndex;
			}

			return currentIndex;
		}

		/// <summary>
		/// Determines if the specified sequence contains exactly one element.
		/// </summary>
		public static bool IsSingleton<T>(this IEnumerable<T> sequence)
		{
			IEnumerator<T> enumerator = sequence.GetEnumerator();
			return enumerator.MoveNext() && !enumerator.MoveNext();
		}

		/// <summary>
		/// Determines if the specified sequence has at least two elements.
		/// </summary>
		public static bool HasAtLeastTwoElements<T>(this IEnumerable<T> sequence)
		{
			IEnumerator<T> enumerator = sequence.GetEnumerator();
			return enumerator.MoveNext() && enumerator.MoveNext();
		}
	}
}

