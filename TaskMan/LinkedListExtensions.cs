using System.Collections.Generic;
using System.Linq;

namespace TaskMan
{
	/// <summary>
	/// A static class providing various extension methods for
	/// <see cref="LinkedList&lt;T&gt;"/> generic sequences.
	/// </summary>
	public static class LinkedListExtensions
	{
		/// <summary>
		/// Removes and returns the first element in a linked list.
		/// </summary>
		/// <returns>The first element in a linked list prior to its removal.</returns>
		/// <param name="linkedList">The linked list to remove the first element from.</param>
		/// <typeparam name="T">The type of elements in the list.</typeparam>
		public static T PopFirst<T>(this LinkedList<T> linkedList)
		{
			T returnValue = linkedList.First();
			linkedList.RemoveFirst();

			return returnValue;
		}
	}
}

