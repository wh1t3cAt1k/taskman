﻿/*
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
using System.Text.RegularExpressions;
using System.Reflection;

namespace TaskMan.Objects
{
	public static class ParseHelper
	{
		public static readonly RegexOptions StandardRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

		public static readonly Regex IdSequenceRegex = 
			new Regex(@"^(:?([0-9]+)\s*?,\s*?)*([0-9]+)$", StandardRegexOptions);
		
		public static readonly Regex IdRangeRegex = 
			new Regex(@"^([0-9]+)-([0-9]+)$", StandardRegexOptions);

		/// <summary>
		/// Example: "is+desc+pr-", which means
		/// "ascending by IsFinished flag,
		/// then ascending by Description,
		/// then descending by Priority".
		/// </summary>
		public static readonly Regex SortOrderRegex = 
			new Regex(@"^([A-Za-z][A-Za-z0-9]*?(?:\+|\-))+$", StandardRegexOptions);

		/// <summary>
		/// Tries to parse a string value into a boolean value.
		/// 0 and "false" are parsed into <c>false</c>, 
		/// 1 and "true" are parsed into <c>true</c>, case-insensitively.
		/// </summary>
		public static bool ParseBool(string value)
		{
			int integerResult;
			bool booleanResult;

			if (bool.TryParse(value, out booleanResult))
			{
				return booleanResult;
			}
			else if (
				int.TryParse(value, out integerResult) &&
				integerResult == 0 || integerResult == 1)
			{
				return integerResult == 1;
			}
			else
			{
				throw new TaskManException(Messages.UknownBooleanValue, value);
			}
		}

		/// <summary>
		/// Tries to parse a string value into a <see cref="Task.Priority"/> value.
		/// If unsuccessful, throws an exception.
		/// </summary>
		public static Priority ParsePriority(string priorityString)
		{
			Priority priority;

			if (!Enum.TryParse(priorityString, true, out priority) ||
				!Enum.GetValues(typeof(Priority)).Cast<Priority>().Contains(priority))
			{
				throw new TaskManException(
					Messages.UnknownPriorityLevel,
					priorityString);
			}

			return priority;
		}

		public static ConsoleColor ParseColor(string colorString)
		{
			ConsoleColor color;

			if (!Enum.TryParse(colorString, true, out color) ||
			    !Enum.GetValues(typeof(ConsoleColor)).Cast<ConsoleColor>().Contains(color))
			{
				throw new TaskManException(Messages.UnknownColor, colorString);
			}

			return color;
		}

		/// <summary>
		/// Tries to parse a string value into a sequence of <see cref="Task"/> IDs.
		/// Supports: 
		/// 1. Single IDs like '5'
		/// 2. ID ranges like '5-36'
		/// 3. ID lists like '5,6,7'
		/// </summary>
		public static IEnumerable<int> ParseTaskId(string idString)
		{
			Match idSequenceMatch = IdSequenceRegex.Match(idString);
			Match idRangeMatch = IdRangeRegex.Match(idString);

			if (idSequenceMatch.Success)
			{
				return idString.Split(',').Select(int.Parse);
			}
			else if (idRangeMatch.Success)
			{
				int lowerBoundary = int.Parse(idRangeMatch.Groups[1].Value);
				int upperBoundary = int.Parse(idRangeMatch.Groups[2].Value);

				if (lowerBoundary > upperBoundary)
				{
					throw new TaskManException(Messages.InvalidTaskIdRange);
				}

				return Enumerable.Range(
					lowerBoundary, 
					checked(upperBoundary - lowerBoundary + 1));
			}
			else
			{
				throw new TaskManException(
					Messages.UnknownIdOrIdRange,
					idString);
			}
		}

		/// <summary>
		/// Parses a string value into a sequence of comparison
		/// steps that determine the tasks' sorting order.
		/// </summary>
		public static IEnumerable<Task.ComparisonStep> ParseTaskSortOrder(string sortString)
		{
			Match match = SortOrderRegex.Match(sortString);

			if (!match.Success)
			{
				throw new TaskManException(Messages.IncorrectSortingStepsSyntax);
			}

			List<Task.ComparisonStep> sortingSteps = new List<Task.ComparisonStep>();

			foreach (Capture capture in match.Groups[1].Captures)
			{
				string propertyNamePrefix = 
					capture.Value.Substring(0, capture.Value.Length - 1);
				
				char sortOrder = capture.Value[capture.Length - 1];

				IEnumerable<PropertyInfo> matchingFields = typeof(Task)
					.GetProperties(BindingFlags.Public | BindingFlags.Instance)
					.Where(propertyInfo => 
					       propertyInfo.Name.StartsWith(propertyNamePrefix, StringComparison.OrdinalIgnoreCase));

				if (!matchingFields.Any())
				{
					throw new TaskManException(
						Messages.BadSortingStepNoSuchPropertyPrefix,
						propertyNamePrefix,
						sortOrder);
				}
				else if (!matchingFields.IsSingleton())
				{
					throw new TaskManException(
						Messages.BadSortingStepAmbiguousPropertyPrefix,
						propertyNamePrefix,
						sortOrder,
						string.Join(", ", matchingFields.Select(propertyInfo => propertyInfo.Name)));
				}
				else
				{
					sortingSteps.Add(new Task.ComparisonStep(
						propertyName: matchingFields.Single().Name,
						direction:
							sortOrder == '+' ? 
							Task.SortingDirection.Ascending : 
							Task.SortingDirection.Descending));
				}
			}

			return sortingSteps;
		}

		/// <summary>
		/// Shift by the given amount of years, months,
		/// weeks and days relative to the current date
		/// or to the date specified by the user.
		/// </summary>
		public static readonly Regex DueDateShiftRegex = new Regex(
			@"(?:^|::)" +
			@"(?:(\+|\-)([0-9]+)y)?" +
			@"(?:(\+|\-)([0-9]+)m)?" +
			@"(?:(\+|\-)([0-9]+)w)?" +
			@"(?:(\+|\-)([0-9]+)d)?" +
			@"$",
			StandardRegexOptions);

		/// <summary>
		/// Modulo operation, always returns a positive number.
		/// </summary>
		private static int Mod(int first, int second)
		{
			return (first % second + second) % second;
		}

		private static DateTime WeekBeginning(DateTime? day = null)
		{
			day = day ?? DateTime.Today;

			return day.Value.AddDays(
				-Mod(day.Value.DayOfWeek - DayOfWeek.Monday, 7));
		}

		private static DateTime MonthEnd(DateTime? day = null)
		{
			day = day ?? DateTime.Today;

			return new DateTime(
				day.Value.Year,
				day.Value.Month,
				DateTime.DaysInMonth(day.Value.Year, day.Value.Month));
		}

		private static DateTime YearEnd(DateTime? day = null)
		{
			day = day ?? DateTime.Today;

			return new DateTime(
				day.Value.Year,
				12,
				31);
		}

		private static readonly Dictionary<string, Func<DateTime>> NaturalLanguageDueDates =
			new Dictionary<string, Func<DateTime>> {
				{ "today", () => DateTime.Today },
				{ "tomorrow", () => DateTime.Today.AddDays(1) },
				{ "this monday", () => WeekBeginning() },
				{ "this tuesday", () => WeekBeginning().AddDays(1) },
				{ "this wednesday", () => WeekBeginning().AddDays(2) },
				{ "this thursday", () => WeekBeginning().AddDays(3) },
				{ "this friday", () => WeekBeginning().AddDays(4) },
				{ "this saturday", () => WeekBeginning().AddDays(5) },
				{ "this sunday|this week", () => WeekBeginning().AddDays(6) },
				{ "next monday", () => WeekBeginning().AddDays(7) },
				{ "next tuesday", () => WeekBeginning().AddDays(8) },
				{ "next wednesday", () => WeekBeginning().AddDays(9) },
				{ "next thursday", () => WeekBeginning().AddDays(10) },
				{ "next friday", () => WeekBeginning().AddDays(11) },
				{ "next saturday", () => WeekBeginning().AddDays(12) },
				{ "next sunday|next week", () => WeekBeginning().AddDays(13) },
				{ "this month", () => MonthEnd() },
				{ "next month", () => MonthEnd().AddMonths(1) },
				{ "this year", () => YearEnd() },
				{ "next year", () => YearEnd().AddYears(1) },
			};

		/// <summary>
		/// Parses a string value into a task due date.
		/// </summary>
		public static DateTime ParseTaskDueDate(string value)
		{
			DateTime result;

			// Parse explicit datetime value.
			// -
			bool dateParseSuccess = DateTime.TryParse(
				value.Split(
					new string[] { "::" },
					StringSplitOptions.RemoveEmptyEntries).First(),
				out result);

			if (!dateParseSuccess)
			{
				result = DateTime.Today;
			}

			// Parse natural language due date.
			// -
			bool humanReadableParseSuccess = false;

			string matchingKey = NaturalLanguageDueDates
				.Keys
				.FirstOrDefault(key => 
					Regex.IsMatch(key, Regex.Escape(value), RegexOptions.IgnoreCase));

			if (matchingKey != null)
			{
				humanReadableParseSuccess = true;
				result = NaturalLanguageDueDates[matchingKey]();
			}

			// Parse due date shift values, if any.
			// -
			Match dateShiftMatch = DueDateShiftRegex.Match(value);

			bool dateShiftSuccess = dateShiftMatch.Success;

			if (!dateParseSuccess && !humanReadableParseSuccess && !dateShiftSuccess)
			{
				throw new TaskManException(Messages.UnknownDueDate, value);
			}

			if (dateShiftSuccess)
			{
				// Add years
				// -
				if (dateShiftMatch.Groups[1].Success)
				{
					result = result.AddYears(
						int.Parse(dateShiftMatch.Groups[2].Value) *
						(dateShiftMatch.Groups[1].Value == "+" ? 1 : -1));
				}

				// Add months
				// -
				if (dateShiftMatch.Groups[3].Success)
				{
					result = result.AddMonths(
						int.Parse(dateShiftMatch.Groups[4].Value) *
						(dateShiftMatch.Groups[3].Value == "+" ? 1 : -1));
				}

				// Add weeks
				// -
				if (dateShiftMatch.Groups[5].Success)
				{
					result = result.AddDays(
						7 * int.Parse(dateShiftMatch.Groups[6].Value) *
						(dateShiftMatch.Groups[5].Value == "+" ? 1 : -1));
				}

				// Add days
				// -
				if (dateShiftMatch.Groups[7].Success)
				{
					result = result.AddDays(
						int.Parse(dateShiftMatch.Groups[8].Value) *
						(dateShiftMatch.Groups[7].Value == "+" ? 1 : -1));
				}
			}

			return result;
		}
	}
}

