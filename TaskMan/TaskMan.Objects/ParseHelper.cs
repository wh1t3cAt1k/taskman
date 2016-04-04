using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Reflection;

namespace TaskMan.Objects
{
	public static class ParseHelper
	{
		static readonly RegexOptions StandardRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

		static readonly Regex IdSequenceRegex = new Regex(@"^(:?([0-9]+)\s*?,\s*?)*([0-9]+)$", StandardRegexOptions);
		static readonly Regex IdRangeRegex = new Regex(@"^([0-9]+)-([0-9]+)$", StandardRegexOptions);

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

			if (!Enum.TryParse(priorityString, out priority) ||
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

			if (!Enum.TryParse(colorString, out color) ||
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
		/// Example: "i+d+p-", which means
		/// "ascending by IsFinished flag,
		/// then ascending by Description,
		/// then descending by Priority".
		/// </summary>
		static readonly Regex SortingStepRegex = new Regex(@"^([A-Za-z][A-Za-z0-9]*?(?:\+|\-))+$", StandardRegexOptions);

		/// <summary>
		/// Tries to parse a string value into a sequence of 
		/// </summary>
		/// <returns>The sorting steps.</returns>
		/// <param name="sortString">Sort string.</param>
		public static IEnumerable<Task.SortingStep> ParseSortingSteps(string sortString)
		{
			Match match = SortingStepRegex.Match(sortString);

			List<Task.SortingStep> sortingSteps = new List<Task.SortingStep>();

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
						sortOrder);
				}
				else
				{
					sortingSteps.Add(new Task.SortingStep(
						propertyName: matchingFields.Single().Name,
						direction:
							sortOrder == '+' ? 
							Task.SortingDirection.Ascending : 
							Task.SortingDirection.Descending));
				}
			}

			return sortingSteps;
		}
	}
}

