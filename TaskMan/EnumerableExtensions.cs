using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskMan
{
	/// <summary>
	/// A static class providing various extension methods for
	/// <see cref="IEnumerable&lt;T&gt;"/> generic sequences.
	/// </summary>
	public static class EnumerableExtensions
	{
		public static IEnumerable<T> Except<T>(this IEnumerable<T> sequence, params T[] arguments)
		{
			return sequence.Except(arguments as IEnumerable<T>);
		}

		public static IEnumerable<T> Concat<T>(this IEnumerable<T> sequence, params T[] arguments)
		{
			return sequence.Concat(arguments as IEnumerable<T>);
		}

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
		/// Performs an action upon each member of a sequence by incorporating
		/// information about whether the current element is a first / last
		/// element of a sequence.
		/// </summary>
		/// <param name="action">
		/// The second / third arguments of the action specify whether the current
		/// element is the first / last element of the sequence, respectively.
		/// </param>
		public static void ForEach<T>(this IEnumerable<T> sequence, Action<T, bool, bool> action)
		{
			sequence.ForEach((element, index, isLast) => action(element, index == 0, isLast));
		}

		/// <summary>
		/// Performs an action upon each member of a sequence by incorporating
		/// the current element's index and a value indicating whether the 
		/// current element is the last element of the sequence.
		/// </summary>
		public static void ForEach<T>(this IEnumerable<T> sequence, Action<T, int, bool> action)
		{
			IEnumerator<T> enumerator = sequence.GetEnumerator();

			if (!enumerator.MoveNext()) return;

			bool isLastElement;

			T current = enumerator.Current;
			int currentIndex = 0;

			isLastElement = !enumerator.MoveNext();
			action(current, currentIndex++, isLastElement);

			while (!isLastElement)
			{
				current = enumerator.Current;

				isLastElement = !enumerator.MoveNext();
				action(current, currentIndex++, isLastElement);
			}
		}

		/// <summary>
		/// Splits the source sequence into subsequences so that
		/// each subsequence has the required maximum number of elements.
		/// </summary>
		public static IEnumerable<IEnumerable<T>> Split<T>(this IEnumerable<T> sequence, int count)
		{
			if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
			int currentElementIndex = 0;

			return sequence.GroupBy(_ => currentElementIndex++ / count);
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

