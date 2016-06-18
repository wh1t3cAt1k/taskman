/*
 * taskman - convenient command line to-do list.
 * 
 * copyright (c) 2016 Pavel Kabir
 * 
 * This file is part of taskman.
 * 
 * taskman is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program. If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
		/// Splits the specified text into a sequence of lines, each line not
		/// exceeding the specified maximal width, breaking the line (preferably) 
		/// on whitespace characters. If the text contains any line breaks, treats 
		/// the pieces between the line breaks separately.
		/// </summary>
		public static IEnumerable<string> MakeLinesByWhitespace(this string text, int maxLineWidth)
		{
			IEnumerable<string> atomicTexts = text.Split('\n');

			if (atomicTexts.HasAtLeastTwoElements())
			{
				return atomicTexts.SelectMany(
					atomicText => MakeLinesByWhitespace(atomicText, maxLineWidth));
			}

			Queue<string> textParts = new Queue<string>(Regex
				.Split(text, @"(\s)")
				.SelectMany(part =>
				{
					if (part.Length <= maxLineWidth)
					{
						return new[] { part };
					}
					else
					{
						return part
							.Split(maxLineWidth)
							.Select(characters => new string(characters.ToArray()))
							.ToArray();
					}
				}));

			List<string> resultingLines = new List<string>();

			StringBuilder nextLine = new StringBuilder();

			while (textParts.Count > 0)
			{
				string linePart = textParts.Dequeue();

				if (nextLine.Length + linePart.Length > maxLineWidth)
				{
					resultingLines.Add(nextLine.ToString().TrimEnd());
					nextLine = new StringBuilder();
				}

				nextLine.Append(linePart);
			}

			if (nextLine.Length > 0)
			{
				resultingLines.Add(nextLine.ToString().TrimEnd());
			}

			return resultingLines;
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

		/// <summary>
		/// Returns the given string replicated the required
		/// number of times.
		/// </summary>
		public static string Replicate(this string text, int times)
		{
			if (times < 0) throw new ArgumentOutOfRangeException(nameof(times));

			StringBuilder resultBuilder = new StringBuilder(text.Length * times);

			for (int counter = 0; counter < times; ++counter)
			{
				resultBuilder.Append(text);
			}

			return resultBuilder.ToString();
		}

		/// <summary>
		/// Returns the string, filtering out the provided
		/// set of characters. 
		/// </summary>
		public static string FilterCharacters(
			this string text, 
			IEnumerable<char> forbiddenCharacters)
		{
			char[] filteredCharacters = text
				.Where(character => !forbiddenCharacters.Contains(character))
				.ToArray();

			return new string(filteredCharacters);
		}

		/// <summary>
		/// Returns the edit distance between the strings.
		/// </summary>
		public static int LevenshteinDistance(this string first, string second)
		{
			if (string.IsNullOrEmpty(first) ||
				string.IsNullOrEmpty(second))
			{
				return 0;
			}

			int firstLength = first.Length;
			int secondLength = second.Length;

			int[,] distances = new int[firstLength + 1, secondLength + 1];

			for (int i = 0; i <= firstLength; distances[i, 0] = i++) { }
			for (int j = 0; j <= secondLength; distances[0, j] = j++) { }

			for (int i = 1; i <= firstLength; i++)
			{
				for (int j = 1; j <= secondLength; j++)
				{
					int cost = second[j - 1] == first[i - 1] ? 0 : 1;

					distances[i, j] = Math.Min(
						Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
						distances[i - 1, j - 1] + cost);
				}
			}

			return distances[firstLength, secondLength];
		}

		/// <summary>
		/// Checks that a given string contains the given format string,
		/// ignoring any argument placeholders in the latter.
		/// The implementation is currently not precise, but enough
		/// for unit testing purposes.
		/// </summary>
		public static bool ContainsFormat(
			this string text, 
			string formatString, 
			StringComparison comparisonType = StringComparison.OrdinalIgnoreCase)
		{
			return Regex
				.Split(formatString, @"{\d+}")
				.All(part => text.IndexOf(part, comparisonType) >= 0);
		}
	}
}

