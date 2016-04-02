using System;
using System.Linq;

namespace TaskMan.Control
{
	public static class PrototypeHelper
	{
		/// <summary>
		/// Gets the command or flag's prototype components.
		/// </summary>
		public static string[] GetComponents(string prototype)
		{
			return
				new string(prototype.Where(character => "^$():=".Contains(character) == false).ToArray())
					.Split(new [] { '|' });
		}
	}
}

