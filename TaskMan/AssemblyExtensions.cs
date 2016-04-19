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
using System.IO;
using System.Reflection;

namespace TaskMan
{
	/// <summary>
	/// A static class providing various extension methods for <see cref="Assembly"/>.
	/// </summary>
	public static class AssemblyExtensions
	{
		/// <summary>
		/// Extracts the value from a given assembly attribute.
		/// </summary>
		/// <returns>The value extracted from the assembly attribute.</returns>
		/// <param name="assembly">An assembly that has an attribute of type <typeparamref>T</typeparamref></param>
		/// <param name="extractValueFunction">A function to extract the value from the attribute.</param>
		/// <typeparam name="T">The type of assembly attribute.</typeparam>
		public static V GetAssemblyAttributeValue<T, V>(this Assembly assembly, Func<T, V> extractValueFunction) where T : Attribute
		{
			T attribute = (T)Attribute.GetCustomAttribute(assembly, typeof (T));
			return extractValueFunction.Invoke(attribute);
		}

		public static string GetAssemblyAttributeValue<T>(this Assembly Assembly, Func<T, string> extractValueFunction) where T: Attribute
		{
			return GetAssemblyAttributeValue<T, string>(Assembly, extractValueFunction);
		}

		/// <summary>
		/// Returns the contents of an embedded resource as text.
		/// </summary>
		/// <param name="assembly">The assembly that contains the required resource.</param>
		/// <param name="resourceName">The full name of the embedded resource.</param>
		public static string GetResourceText(this Assembly assembly, string resourceName)
		{
			using (Stream helpTextStream = assembly.GetManifestResourceStream(resourceName))
			{
				using (StreamReader helpTextStreamReader = new StreamReader(helpTextStream))
				{
					return helpTextStreamReader.ReadToEnd();
				}
			}
		}
	}
}

