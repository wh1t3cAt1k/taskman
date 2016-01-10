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

