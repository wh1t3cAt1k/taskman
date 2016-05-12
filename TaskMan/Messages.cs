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

namespace TaskMan
{
	public static class Messages
	{
		public const string AndNumberMore = "...and {0} more.";
		public const string AvailableTaskLists = "Available non-empty lists: ";
		public const string YesNoConfirmationPrompt = "Confirm (y[es]/n[o]): ";
		public const string ParameterResetToDefault = "Parameter '{0}' was reset to '{1}'.";
		public const string Cancelled = "Cancelled.";
		public const string EnteringShell = "Entering taskman REPL. Type 'quit' or 'exit' when finished.";
		public const string ExitingShell = "Leaving taskman REPL. Goodbye.";
		public const string UnknownCommandLineArguments = "Unknown command line arguments: {0}";
		public const string CouldNotReadTaskList = "An error occurred while trying to read task list: {0}";
		public const string Error = "Error: {0}";
		public const string ErrorPerformingOperation = "Cannot {0}: {1}";
		public const string ExceptionStackTrace = "Exception stack trace: ";
		public const string NoParameterValue = "Parameter {0} does not have a defaut value and an explicit value is not found in the configuration files.";
		public const string FlagNotSet = "The {0} flag value has not been set.";
		public const string NoParameterName = "No parameter name specified for '{0}'.";
		public const string UnknownParameterName = "Unknown parameter name '{0}'.";
		public const string ParameterWasSetToValue = "Parameter '{0}' was set to '{1}'.";
		public const string CurrentUserValueOfParameter = "User-level value: '{1}'.";
		public const string CurrentGlobalValueOfParameter = "Global value: '{1}'.";
		public const string ParameterHasNoDefault = "Parameter has no default.";
		public const string DefaultValueOfParameter = "Default value: '{0}'.";
		public const string CurrentTaskList = "Current list: '{0}'.";
		public const string EntityDoesNotMakeSenseWithEntity = "'{0}' does not make sense with '{1}'.";
		public const string InsufficientUpdateParameters = "Insufficient parameters in 'set' command.";
		public const string InvalidTaskIdRange = "Invalid task ID range: the starting ID should not exceed the ending ID.";
		public const string InvalidSetParameters = "Invalid 'set' command syntax.";
		public const string MoreThanOneCommandMatchesInput = "More than one command matches the given input. Please be more specific.";
		public const string NoDescriptionSpecified = "A task description is missing.";
		public const string NoTasksMatchingGivenConditions = "There are no tasks matching the given conditions."; 
		public const string NoTaskWithSpecifiedId = "There is no task with the specified ID: '{0}'.";
		public const string CannotSortNoSuchProperty = "Cannot sort by '{0}', there is no such task property.";
		public const string BadSortingStepNoSuchPropertyPrefix = "Invalid sorting step '{0}{1}', there is no task property that starts with '{0}'.";
		public const string BadSortingStepAmbiguousPropertyPrefix = "Ambiguous sorting step '{0}{1}', there is more than one task property that starts with '{0}': {2}.";
		public const string TaskWasAdded = "Task [{0}] was added with an ID of {1}, {2} priority{3}";
		public const string TaskWasFinished = "Task with ID {0} [{1}] was marked as finished.";
		public const string TasksWereFinished = "{0} tasks were marked as finished.";
		public const string TaskWasReopened = "Task with ID {0} [{1}] was reopened.";
		public const string TasksWereReopened = "{0} tasks were reopened.";
		public const string TaskListIsEmpty = "The task list is empty.";
		public const string TaskListClearCancelled = "Task list clearing has been cancelled.";
		public const string TaskListCleared = "Task list has been cleared.";
		public const string TaskWasUpdated = "Task {0} [{1}] has changed its '{2}' value to '{3}'";
		public const string TasksWereUpdated = "Updated {0} task(s) with new '{1}' value of '{2}'.";
		public const string TaskWasSomething = "Task with ID {0} [{1}] was {2}.";
		public const string TasksWereSomething = "{0} tasks were {1}.";
		public const string TaskWasDeleted = "Task with ID {0} [{1}] was deleted.";
		public const string TasksWereDeleted = "{0} tasks were deleted.";
		public const string TasksWereRenumbered = "{0} tasks were successfully renumbered.";
		public const string TheFollowingObjectWillBeAction = "The following {0} will be {1}: ";
		public const string TypeHelpForCommandSyntax = "Type 'taskman --help' to view command syntax.";
		public const string ParameterNameWillBeSetToValue = "Parameter '{0}' will be set to '{1}'.";
		public const string RequiredFlagNotSet = "Required flag {0} was not specified";
		public const string UknownBooleanValue = "Cannot parse '{0}' into a boolean value.";
		public const string UnknownCommand = "Unknown command '{0}'. ";
		public const string UnknownColor = "Unknown console color '{0}'.";
		public const string UnknownPriorityLevel = "Unknown priority level '{0}'.";
		public const string UnknownIdOrIdRange = "Unknown ID or ID range '{0}'.";
		public const string UnknownDueDate = "Cannot parse '{0}' into a due date.";
		public const string NoFilterConditionsUseAllIfIntended = "No task filter conditions specified. Use the --all flag if you intended to {0} all tasks.";
		public const string IncorrectSortingStepsSyntax = "Sorting string does not match the required syntax ('property+' or 'property-')";
	}
}