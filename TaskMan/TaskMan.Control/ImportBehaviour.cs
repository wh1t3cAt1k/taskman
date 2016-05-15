namespace TaskMan.Control
{
	public enum ImportBehaviour
	{
		/// <summary>
		/// Replaces all tasks in the target list by
		/// the imported tasks.
		/// </summary>
		Replace = 0,
		/// <summary>
		/// Appends the imported tasks to the target 
		/// list IDs. 
		/// </summary>
		Append,
		/// <summary>
		/// Currently not supported.
		/// </summary>
		MergePriorityExisting,
		/// <summary>
		/// Currently not supported.
		/// </summary>
		MergePriorityImported,
	}
}

