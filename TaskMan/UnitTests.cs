using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using TaskMan.Objects;
using System.Text;

namespace TaskMan
{
	public class TaskManTester
	{
		List<Task> _savedTasks = new List<Task>();
		
		public IEnumerable<Task> SavedTasks { get { return _savedTasks; } }

		StringBuilder _output = new StringBuilder();
		StringBuilder _errors = new StringBuilder();

		public string Output { get { return _output.ToString(); } }
		public string Errors { get { return _errors.ToString(); } }

		public void RunWithCommand(string command)
		{
			using (StringWriter outputRedirect = new StringWriter())
			{
				using (StringWriter errorRedirect = new StringWriter())
				{
					TaskMan program = new TaskMan(
						taskReadFunction: () => this._savedTasks,
						taskSaveFunction: taskList => this._savedTasks = taskList,
						outputStream: outputRedirect,
						errorStream: errorRedirect);
			
					program.Run(command.Split(
						new [] { ' ' },
						StringSplitOptions.RemoveEmptyEntries));

					outputRedirect.Flush();
					errorRedirect.Flush();

					_output.AppendLine(outputRedirect.ToString());
					_errors.AppendLine(errorRedirect.ToString());
				}
			}
		}

		public void RunWithCommands(params string[] commands)
		{
			commands.ForEach(RunWithCommand);
		}
	}

	[TestFixture]
	public class UnitTests
	{
		[Test]
		public void Test_AddWorks()
		{
			TaskManTester tester = new TaskManTester();

			// Action
			// -
			tester.RunWithCommand("add Remember the milk");

			// Assert
			// -
			Assert.IsNotEmpty(tester.SavedTasks);
			Assert.That(
				tester.SavedTasks.First().Description, 
				Is.EqualTo("Remember the milk"));
		}

		[Test]
		public void Test_NewIsTheSameAsAdd()
		{
			TaskManTester tester = new TaskManTester();

			// Action
			// -
			tester.RunWithCommands(
				"add Remember the milk",
				"new Remember the milk");

			// Assert
			// -
			Assert.That(tester.SavedTasks.Count(), Is.EqualTo(2));
			Assert.That(
				tester.SavedTasks.First().Description,
				Is.EqualTo(tester.SavedTasks.Last().Description));
		}

		[Test]
		public void Test_CreateIsTheSameAsAdd()
		{
			TaskManTester tester = new TaskManTester();

			// Action
			// -
			tester.RunWithCommands(
				"add Remember the milk",
				"create Remember the milk");

			// Assert
			// -
			Assert.That(tester.SavedTasks.Count, Is.EqualTo(2));
			Assert.That(
				tester.SavedTasks.First().Description, 
				Is.EqualTo(tester.SavedTasks.Last().Description));
		}

		[Test]
		public void Test_LicenseFlag_OutputsLicense()
		{
			TaskManTester tester = new TaskManTester();

			const string expectedSubstring = "GNU GENERAL PUBLIC LICENSE";

			tester.RunWithCommand("--license");

			Assert.That(
				tester.Output,
				Contains.Substring(expectedSubstring));
		}

		[Test]
		public void Test_VersionFlag_OutputsVersion()
		{
			TaskManTester tester = new TaskManTester();

			const string expectedSubstring = "version";

			tester.RunWithCommand("--version");

			Assert.That(
				tester.Output,
				Contains.Substring(expectedSubstring));
		}

		[Test]
		public void Test_LimitFlag_HasLowerPriorityThanSkipFlag()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommands(
				"add first --silent",
				"add second --silent",
				"add third --silent",
				"show --limit 1 --skip 2");

			Assert.That(
				tester.Output,
				Does.Not.Contain("first")
				.And.Not.Contain("second")
				.And.Contains("third"));
		}
	}
}

