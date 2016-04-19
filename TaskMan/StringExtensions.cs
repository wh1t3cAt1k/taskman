using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace TaskMan
{
	public static class StringExtensions
	{
		/// <summary>
		/// Credit goes to http://stackoverflow.com/a/298990/499206
		/// on this one.
		/// </summary>
		public static IEnumerable<string> SplitCommandLine(string commandLine)
		{
			bool isInsideDoubleQuotes = false;
			bool isInsideSingleQuotes = false;

			return commandLine.Split(character =>
			{
				if (character == '\"' && !isInsideSingleQuotes)
				{
					isInsideDoubleQuotes = !isInsideDoubleQuotes;
				}

				if (character == '\'' && !isInsideDoubleQuotes)
				{
					isInsideSingleQuotes = !isInsideSingleQuotes;	
				}

				return 
					!isInsideDoubleQuotes && 
					!isInsideSingleQuotes &&
					char.IsWhiteSpace(character);
			})
			.Select(argument => argument.Trim().TrimMatchingQuotes('\"', '\''))
			.Where(argument => !string.IsNullOrEmpty(argument));
		}

		/// <summary>
		/// Credit goes to http://stackoverflow.com/a/298990/499206
		/// on this one.
		/// </summary>
		public static IEnumerable<string> Split(this string text, Func<char, bool> controller)
		{
			int nextPieceIndex = 0;

			for (int characterIndex = 0; characterIndex < text.Length; characterIndex++)
			{
				if (controller(text[characterIndex]))
				{
					yield return text.Substring(nextPieceIndex, characterIndex - nextPieceIndex);
					nextPieceIndex = characterIndex + 1;
				}
			}

			yield return text.Substring(nextPieceIndex);
		}

		/// <summary>
		/// Credit goes to http://stackoverflow.com/a/298990/499206
		/// on this one.
		/// </summary>
		public static string TrimMatchingQuotes(this string text, params char[] possibleQuotes)
		{
			if (!text.HasAtLeastTwoElements()) return text;

			if (Array.IndexOf(possibleQuotes, text.First()) >= 0 &&
			    Array.IndexOf(possibleQuotes, text.First()) ==
			    Array.IndexOf(possibleQuotes, text.Last()))
			{
				return text.Substring(1, text.Length - 2);
			}

			return text;
		}

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

