using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TaskMan.Objects
{
	public static class TaskHelper
	{
		static readonly RegexOptions StandardRegexOptions = RegexOptions.Compiled | RegexOptions.IgnoreCase;

		static readonly Regex IdSequenceRegex = new Regex(@"^(:?([0-9]+)\s*?,\s*?)*([0-9]+)$", StandardRegexOptions);
		static readonly Regex IdRangeRegex = new Regex(@"^([0-9]+)-([0-9]+)$", StandardRegexOptions);

		/// <summary>
		/// Tries to parse a string value into a <see cref="Task.Finished"/>
		/// value. 0 and "false" are parsed into <c>false</c>, 1 and "true" 
		/// are parsed into <c>true</c>, case-insensitively.
		/// </summary>
		public static bool ParseFinished(string value)
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
				throw new TaskManException(Messages.UnknownFinishedFlag, value);
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

		/// <summary>
		/// Tries to parse a string value into a sequence of <see cref="Task"/> IDs.
		/// Supports: 
		/// 1. Single IDs like '5'
		/// 2. ID ranges like '5-36'
		/// 3. ID lists like '5,6,7'
		/// </summary>
		public static IEnumerable<int> ParseId(string idString)
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
	}
}

