﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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
		}

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
				}
			}
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

		/*
		[Test]
		public void Test_LicenseFlag_OutputsLicense()
		{
			using (StringWriter output = new StringWriter())
			{
				Console.SetOut(output);

				RunWithCommand("--license");

				Assert.That(
					output.ToString(),
					Contains.Substring("GNU GENERAL PUBLIC LICENSE"));
			}

			using (StringWriter output = new StringWriter())
			{
				Console.SetOut(output);

				RunWithCommand("/license");

				Assert.That(
					output.ToString(),
					Contains.Substring("GNU GENERAL PUBLIC LICENSE"));
			}

			using (StringWriter output = new StringWriter())
			{
				Console.SetOut(output);

				RunWithCommand("-license");

				Assert.That(
					output.ToString(),
					Contains.Substring("GNU GENERAL PUBLIC LICENSE"));
			}
		}
		*/
	}
}

