using System;
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace TaskMan
{
	[TestFixture]
	public class UnitTests
	{
		List<Task> _savedTasks;

		[SetUp]
		public void Setup()
		{
			_savedTasks = new List<Task>();

			Program.TaskReadFunction = 
				() => this._savedTasks;
			
			Program.TaskSaveFunction = 
				taskList => this._savedTasks = taskList;
		}

		public void RunWithCommand(string command)
		{
			Program.Main(command.Split(
				new [] { ' ' },
				StringSplitOptions.RemoveEmptyEntries));
		}

		public void RunWithCommands(params string[] commands)
		{
			foreach (string command in commands)
			{
				RunWithCommand(command);
			}
		}

		[Test]
		public void Test_AddWorks()
		{
			// Action
			// -
			RunWithCommand("add Remember the milk");

			// Assert
			// -
			Assert.IsNotEmpty(_savedTasks);
			Assert.That(
				_savedTasks.First().Description, 
				Is.EqualTo("Remember the milk"));
		}

		[Test]
		public void Test_NewIsTheSameAsAdd()
		{
			// Action
			// -
			RunWithCommands(
				"add Remember the milk",
				"new Remember the milk");

			// Assert
			// -
			Assert.That(_savedTasks.Count, Is.EqualTo(2));
			Assert.That(
				_savedTasks.First().Description,
				Is.EqualTo(_savedTasks.Last().Description));
		}

		[Test]
		public void Test_CreateIsTheSameAsAdd()
		{
			// Action
			// -
			RunWithCommands(
				"add Remember the milk",
				"create Remember the milk");

			// Assert
			// -
			Assert.That(_savedTasks.Count, Is.EqualTo(2));
			Assert.That(
				_savedTasks.First().Description, 
				Is.EqualTo(_savedTasks.Last().Description));
		}
	}
}

