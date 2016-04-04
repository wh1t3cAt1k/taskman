namespace TaskMan
{
	public static class Messages
	{
		public const string AndNumberMore = "...and {0} more.";
		public const string YesNoConfirmationPrompt = "Confirm (y[es]/n[o]): ";
		public const string Cancelled = "Cancelled.";
		public const string CouldNotReadTaskList = "An error occurred while trying to read task list: {0}";
		public const string Error = "Error: {0}";
		public const string ErrorPerformingOperation = "Cannot {0}: {1}";
		public const string ExceptionStackTrace = "Exception stack trace: ";
		public const string FlagNotSet = "The {0} flag value has not been set.";
		public const string NoParameterName = "No parameter name specified for '{0}'.";
		public const string ParameterWasSetToValue = "Parameter '{0}' was set to '{1}'.";
		public const string EntityDoesNotMakeSenseWithEntity = "'{0}' does not make sense with '{1}'.";
		public const string InsufficientSetParameters = "Insufficient parameters in 'set' command. " + Messages.TypeHelpForCommandSyntax;
		public const string InvalidTaskIdRange = "Invalid task ID range: the starting ID should not exceed the ending ID.";
		public const string InvalidSetParameters = "Invalid 'set' command syntax. " + Messages.TypeHelpForCommandSyntax;
		public const string MoreThanOneCommandMatchesInput = "More than one command matches the given input. Please be more specific.";
		public const string NoDescriptionSpecified = "A task description is missing.";
		public const string NoTasksMatchingGivenConditions = "There are no tasks matching the given conditions."; 
		public const string NoTaskWithSpecifiedId = "There is no task with the specified ID: '{0}'.";
		public const string CannotSortNoSuchProperty = "Cannot sort by '{0}', there is no such task property.";
		public const string BadSortingStepNoSuchPropertyPrefix = "Invalid sorting step '{0}{1}', there is no task property that starts with '{0}'";
		public const string BadSortingStepAmbiguousPropertyPrefix = "Ambiguous sorting step '{0}{1}', there is more than one task property that starts with '{0}'.";
		public const string TaskWasAdded = "Task [{0}] was added with an ID of {1}, priority: {2}.";
		public const string TaskWasFinished = "Task with ID {0} [{1}] was successfully marked as finished.";
		public const string TasksWereFinished = "{0} tasks were successfully marked as finished.";
		public const string TaskListIsEmpty = "The task list is empty.";
		public const string TaskListClearCancelled = "Task list clearing has been cancelled.";
		public const string TaskListCleared = "Task list has been cleared.";
		public const string TaskWasUpdated = "Task {0} [{1}] has changed its '{2}' value to '{3}'";
		public const string TasksWereUpdated = "Updated {0} task(s) with new '{1}' value of '{2}'.";
		public const string TaskWasDeleted = "Task with ID {0} [{1}] was successfully deleted.";
		public const string TasksWereDeleted = "{0} tasks were successfully deleted.";
		public const string TheFollowingObjectWillBeAction = "The following {0} will be {1}: ";
		public const string TypeHelpForCommandSyntax = "Type 'taskman --help' to view command syntax.";
		public const string ParameterNameWillBeSetToValue = "Parameter '{0}' will be set to '{1}'.";
		public const string RequiredFlagNotSet = "Required flag {0} was not specified";
		public const string UknownBooleanValue = "Cannot parse '{0}' into a boolean value.";
		public const string UnknownCommand = "Unknown command '{0}'. " + Messages.TypeHelpForCommandSyntax;
		public const string UnknownColor = "Unknown console color '{0}'.";
		public const string UnknownPriorityLevel = "Unknown priority level '{0}'.";
		public const string UnknownIdOrIdRange = "Unknown ID or ID range '{0}'.";
		public const string NoFilterConditionsUseAllIfIntended = "No task filter conditions specified. Use the --all flag if you intended to {0} all tasks.";
	}
}