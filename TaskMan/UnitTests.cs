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

		public string Input { get; set; } = "";
		public string Output => _output.ToString();
		public string Errors => _errors.ToString();

		public void RunWithCommand(string command)
		{
			using (StringReader inputRedirect = new StringReader(this.Input))
			using (StringWriter outputRedirect = new StringWriter(_output))
			using (StringWriter errorRedirect = new StringWriter(_errors))
			{
				TaskMan program = new TaskMan(
					taskReadFunction: () => this._savedTasks,
					taskSaveFunction: taskList => this._savedTasks = taskList,
					inputStream: inputRedirect,
					outputStream: outputRedirect,
					errorStream: errorRedirect);

				program.RunTaskman(command.Split(
					new[] { ' ' },
					StringSplitOptions.RemoveEmptyEntries));
			}
		}

		public void RunWithCommands(params string[] commands)
		{
			commands.ForEach(RunWithCommand);
		}

		public void AddThreeTasks()
		{
			this.RunWithCommands(
				"add first --silent",
				"add second --silent",
				"add third --silent");
		}
	}

	[TestFixture]
	public class UnitTests
	{
		[Test]
		public void Test_Add_AddsTask()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("add Remember the milk");

			Assert.IsNotEmpty(tester.SavedTasks);
			Assert.That(
				tester.SavedTasks.First().Description, 
				Is.EqualTo("Remember the milk"));
		}

		[Test]
		public void Test_New_IsTheSameAsAdd()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommands(
				"add Remember the milk",
				"new Remember the milk");

			Assert.That(tester.SavedTasks.Count(), Is.EqualTo(2));
			Assert.That(
				tester.SavedTasks.First().Description,
				Is.EqualTo(tester.SavedTasks.Last().Description));
		}

		[Test]
		public void Test_Create_IsTheSameAsAdd()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommands(
				"add Remember the milk",
				"create Remember the milk");

			Assert.That(tester.SavedTasks.Count, Is.EqualTo(2));
			Assert.That(
				tester.SavedTasks.First().Description, 
				Is.EqualTo(tester.SavedTasks.Last().Description));
		}

		[Test]
		public void Test_Complete_CompletesTasks()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommand("complete --all");
			
			Assert.That(
				tester.SavedTasks,
				Is.All.Matches<Task>(task => task.IsFinished));
		}

		[Test]
		public void Test_Delete_DeletesTasks()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommand("delete --all");

			Assert.IsEmpty(tester.SavedTasks);
		}

		[Test]
		public void Test_Update_UpdatesDescription()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommand("update --all description NEW");

			Assert.That(
				tester.SavedTasks,
				Is.All.Matches<Task>(task => task.Description == "NEW"));
		}

		[Test]
		public void Test_Update_UpdatesFinished()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommands("update --all finished true");

			Assert.That(
				tester.SavedTasks,
				Is.All.Matches<Task>(task => task.IsFinished));
		}

		[Test]
		public void Test_Update_UpdatesPriority()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommands("update --all priority Important");

			Assert.That(
				tester.SavedTasks,
				Is.All.Matches<Task>(task => task.Priority == Priority.Important));
		}

		[Test]
		public void Test_PriorityFlag_SetsPriority_When_AddingNewTask()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("add -p Critical first");

			Assert.That(
				tester.SavedTasks.Single().Priority,
				Is.EqualTo(Priority.Critical));
		}

		[Test]
		public void Test_PriorityFlag_FiltersByPriority()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommands(
				"add first",
				"add second -p Important",
				"add third",
				"delete -p Normal");

			Assert.That(
				tester.SavedTasks.Single().Priority, 
				Does.Not.EqualTo(Priority.Normal));
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
		public void Test_SilentFlag_MakesTaskAddingSilent()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("add --silent Remember the milk");

			Assert.That(string.IsNullOrWhiteSpace(tester.Output));
		}

		[Test]
		public void Test_VerboseFlag_IncreasesErrorVerbosity()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("self-destruct --verbose");

			Assert.That(
				tester.Errors.ContainsFormat(Messages.ExceptionStackTrace));
		}

		[Test]
		public void Test_TaskMan_DoesNotPrintStackTrace_When_NoVerboseFlag()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("self-destruct");

			Assert.That(
				!tester.Errors.ContainsFormat(Messages.ExceptionStackTrace));
		}

		[Test]
		public void Test_LimitFlag_HasLowerPriorityThanSkipFlag()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommand("show --limit 1 --skip 2");

			Assert.That(
				tester.Output,
				Does.Not.Contain("first")
				.And.Not.Contain("second")
				.And.Contains("third"));
		}

		[Test]
		public void Test_SplitCommandLine_SplitsArgumentsCorrectly()
		{
			Assert.That(
				StringExtensions.SplitCommandLine(@"show ""hello"""),
				Is.EqualTo(new string[] { "show", "hello" }));

			Assert.That(
				StringExtensions.SplitCommandLine(@"show --orderby a+b-d+"),
				Is.EqualTo(new string[] { "show", "--orderby", "a+b-d+" }));

			Assert.That(
				StringExtensions.SplitCommandLine(@"'show' '--like' 'hello'"),
				Is.EqualTo(new string[] { "show", "--like", "hello" }));

			Assert.That(
				StringExtensions.SplitCommandLine(@"'""show""'"),
				Is.EqualTo(new string[] { @"""show""" }));
		}

		[Test]
		public void Test_TaskMan_SplitsOutputMessagesIntoLines()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("add " + "ABCDE ".Replicate(10000));

			Assert.That(tester.Output.Split('\n').HasAtLeastTwoElements());
		}

		[Test]
		public void Test_DescriptionRegexFlag_FiltersTasksByDescription()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommands(
				"add mario --silent",
				"add wario --silent",
				"add princess --silent",
				"add bowser --silent",
				"show --like ario");

			Assert.That(
				tester.Output,
				Does.Contain("mario")
				.And.Contain("wario")
				.And.Not.Contain("princess")
				.And.Not.Contain("bowser"));
		}

		[Test]
		public void Test_Renumber_RenumbersTasksInGivenOrder()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommands(
				"add 1",
				"add 2",
				"add 3");

			Assert.That(
				tester.SavedTasks.Select(t => t.Description),
				Is.EquivalentTo(new[] { "1", "2", "3" }));

			tester.RunWithCommand("renumber --orderby id-");

			Assert.That(
				tester.SavedTasks.Select(t => t.Description),
				Is.EquivalentTo(new[] { "3", "2", "1" }));
		}

		[Test]
		public void Test_FormatFlag_WorksWithCSVFormat()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommand("show --format csv");

			Assert.That(true);
		}

		[Test]
		public void Test_FormatFlag_WorksWithXMLFormat()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.RunWithCommand("show --format xml");

			Assert.That(true);
		}

		[Test]
		public void Test_InteractiveFlag_AllowsTheUserToAbortOperation()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.Input = "no";

			tester.RunWithCommand("delete --all --interactive");

			Assert.That(() => tester.Output.ContainsFormat(Messages.Cancelled));
			Assert.That(tester.SavedTasks.Count, Is.EqualTo(3));
		}

		[Test]
		public void Test_InteractiveFlag_AllowsTheUserToConfirmOperation()
		{
			TaskManTester tester = new TaskManTester();

			tester.AddThreeTasks();
			tester.Input = "yes";

			tester.RunWithCommand("delete --all --interactive");

			Assert.That(tester.SavedTasks, Is.Empty);
		}

		[Test]
		public void Test_TaskMan_OutputsSimilarCommandOnTypo()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("shom");

			Assert.That(tester.Errors.ContainsFormat(Messages.DidYouMean));
			Assert.That(tester.Errors, Does.Contain("show").IgnoreCase);
		}

		[Test]
		public void Test_TaskMan_OutputsPossibleCommandsOnAmbiguity()
		{
			TaskManTester tester = new TaskManTester();

			tester.RunWithCommand("s");

			Assert.That(
				tester.Errors.ContainsFormat(Messages.MoreThanOneCommandMatchesInput));
		}
	}
}

