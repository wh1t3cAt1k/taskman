using System;

namespace TaskMan
{
	public static class Messages
	{
		public const string ClearConfirmationMessage = "All tasks will be deleted. Confirm (y[es]/n[o]): ";
		public const string CouldNotCreateHelpFile = "An error occurred while trying to create a help file: {0}";
		public const string CouldNotReadTaskList = "An error occurred while trying to read task list: {0}";
		public const string ErrorPerformingOperation = "Cannot {0}: {1}";
		public const string FatalErrorReadingFile = "Fatal error occurred while reading {0}. File can be corrupt.";
		public const string InsufficientSetParameters = "Insufficient parameters in 'set' command.";
		public const string InvalidTaskId = "Invalid task ID provided.";
		public const string InvalidTaskIdRange = "Invalid task ID range: the starting ID should not exceed the ending ID.";
		public const string InvalidSetParameters = "Invalid 'set' command syntax. Type 'taskman help' to view command syntax.";
		public const string MakeHelpSucceeded = "Successfully created a help file.";
		public const string NewTaskList = "New task list:";
		public const string NoDescriptionSpecified = "A task description is missing.";
		public const string NoTaskWithSpecifiedId = "There is no task with the specified ID: {0}.";
		public const string NoTaskIdProvided = "You should specify a task ID to {0}.";
		public const string NoTasksInSpecifiedIdRangeWithCondition = "There are no tasks with ID in the specified range that match the display condition: {0}.";
		public const string TaskWasAdded = "Task [{0}] was added with a priority of {1}.";
		public const string TaskWasFinished = "Task with id {0} [{1}] was marked as finished!";
		public const string TaskListIsEmpty = "The task list is empty.";
		public const string TaskListClearCancelled = "Task list clearing has been cancelled.";
		public const string TaskListCleared = "Task list has been cleared.";
		public const string TaskWithIdWasDeleted = "Task with ID {0} [{1}] was successfully deleted.";
		public const string UnknownBoolValue = "Unknown bool value. Should be true or false.";
		public const string UnknownCommand = "Unknown command format. Please type 'taskman help' for the list of available commands.";
		public const string UnknownDisplayCondition = "Unknown display condition provided.";
		public const string UnknownPriorityLevel = "Unknown priority level {0}. Should be 1-3.";
		public const string TaskWithIdDoesNotMatchTheCondition = "The task with ID {0} does not match the display condition and will not be shown.";
	}
}