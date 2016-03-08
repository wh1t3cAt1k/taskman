using System;
using System.Collections.Generic;
using System.Linq;

namespace TaskMan
{
	public static class TaskExtensions
	{
		/// <summary>
		/// Returns a task with the specified id, if it is present
		/// in the task list, throws an exception otherwise.
		/// </summary>
		/// <returns>
		/// Task with the specified ID, if it is present 
		/// in <paramref name="tasks"/> list.
		/// </returns>
		/// <param name="tasks">The task list.</param>
		/// <param name="id">The ID of the task to be returned.</param>
		public static Task TaskWithId(this List<Task> tasks, int id)
		{
			if (!tasks.Any())
			{
				throw new Exception(Messages.TaskListIsEmpty);
			}

			try
			{
				return tasks.Single(task => (task.ID == id));
			}
			catch
			{
				throw new Exception(string.Format(Messages.NoTaskWithSpecifiedId, id));
			}
		}
	}
}

