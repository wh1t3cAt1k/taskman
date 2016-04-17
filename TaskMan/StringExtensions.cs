using System;
using System.Text;

namespace TaskMan
{
	public static class StringExtensions
	{
		/// <summary>
		/// Returns the string with the first letter decapitalised.
		/// </summary>
		/// <returns>
		/// The string identical to <paramref name="text"/>, but 
		/// with the first letter decapitalised.
		/// </returns>
		/// <param name="text">The original string.</param>
		public static string DecapitaliseFirstLetter(this string text)
		{
			if (text.Length == 0)
			{
				return text;
			}
			else
			{
				return char.ToLower(text[0]) + text.Substring(1);
			}
		}

		public static string Replicate(this string text, int times)
		{
			if (times < 0) throw new ArgumentOutOfRangeException(nameof(times));

			StringBuilder resultBuilder = new StringBuilder(times);

			for (int counter = 0; counter < times; ++counter)
			{
				resultBuilder.Append(text);
			}

			return resultBuilder.ToString();
		}
	}
}

