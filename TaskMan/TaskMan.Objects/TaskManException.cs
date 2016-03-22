using System;

namespace TaskMan.Objects
{
	public class TaskManException : Exception
	{
		public TaskManException(string message, params object[] formatArguments)
			: base(string.Format(message, formatArguments))
		{ }
	}
}

