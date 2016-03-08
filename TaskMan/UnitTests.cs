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
		[SetUp]
		public void Setup()
		{
			IEnumerable<string> commands = new string[]
			{
				"add Remember the Milk [2]",
				"add Pay the loan [3]",
				"add Play Super Metroid",
				"add Prepare for the party",
			};

			// commands
			//	.Select(command => command.Split(new [] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
			//	.ForEach(arguments => Program.Main(arguments));
		}
	}
}

