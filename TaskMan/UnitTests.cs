using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;
using Moq;

namespace TaskMan
{
	[TestFixture]
	public class UnitTests
	{
		List<Task> _savedTasks = new List<Task>();

		[SetUp]
		public void Setup()
		{
			Program.TaskReadFunction = 
				() => this._savedTasks;

			Program.TaskSaveFunction = 
				taskList => this._savedTasks = taskList;
		}

		public void RunWithCommand(string command)
		{
			Program.Main(
				command.Split(new [] { " " }, 
				StringSplitOptions.RemoveEmptyEntries));
		}

		[Test]
		public void Test_SaveFunctionSubstitution_WorksProperly()
		{
			// Action
			// -
			RunWithCommand("add Remember the Milk");

			// Assert
			// -
			Assert.IsNotEmpty(_savedTasks);
		}
	}
}

