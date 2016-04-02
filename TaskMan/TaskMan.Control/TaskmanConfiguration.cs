using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using TaskMan.Objects;

namespace TaskMan.Control
{
	public class TaskmanParameter
	{
		/// <summary>
		/// Gets the parameter name that should serve as a key in the
		/// configuration file.
		/// </summary>
		public string Name { get; private set; }

		/// <summary>
		/// Gets the default value for the current parameter.
		/// </summary>
		public string DefaultValue { get; private set; }

		/// <summary>
		/// Gets the validation regex for the current parameter.
		/// </summary>
		public Regex ValidationRegex { get; private set; }

		/// <summary>
		/// Gets a value indicating whether the current parameter can be
		/// set only in the user scope.
		/// </summary>
		/// <value>
		/// <c>true</c> if this parameter is user scope only; 
		/// otherwise, <c>false</c>.
		/// </value>
		public bool IsUserScoped { get; private set; }

		public TaskmanParameter(
			string name, 
			string validationPattern = ".*", 
			string defaultValue = null, 
			bool isUserScoped = false)
		{
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (validationPattern == null) throw new ArgumentNullException(nameof(validationPattern));
			
			this.Name = name;
			this.ValidationRegex = 
				new Regex(validationPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);

			this.IsUserScoped = isUserScoped;

			if (defaultValue != null &&
			    !this.ValidationRegex.IsMatch(defaultValue))
			{
				throw new ArgumentException(
					"The specified default value does not match the given validation expression");
			}
		}
	}

	public class TaskmanConfiguration
	{
		const string CURRENT_TASK_LIST_KEY = "currentlist";

		private Configuration _userConfiguration;
		private Configuration _globalConfiguration;

		/// <summary>
		/// Gets the directory where all task lists of the current
		/// user must be located.
		/// </summary>
		public string TaskListDirectory
		{
			get
			{
				return Path.GetDirectoryName(_userConfiguration.FilePath);
			}
		}

		public TaskmanParameter TaskListName 
			=> new TaskmanParameter("list", "[A-Za-z][A-Za-z0-9]*", "default", true);

		private IEnumerable<TaskmanParameter> _supportedParameters;

		public TaskmanConfiguration()
		{
			_globalConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			_userConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

			_supportedParameters = new [] {
				TaskListName,
			};
		}

		public void SetParameter(string name, string value, bool setGlobally)
		{
			TaskmanParameter matchingParameter = _supportedParameters
				.SingleOrDefault(parameter => parameter.Name == name);

			if (matchingParameter == null)
			{
				throw new TaskManException($"Unknown parameter name '{name}'.");
			}
			else if (matchingParameter.IsUserScoped && setGlobally)
			{
				throw new TaskManException($"Parameter '{name}' can only be set at the user level");
			}

			if (setGlobally)
			{
				_globalConfiguration.AppSettings.Settings.Add(name, value);
			}
			else
			{
				_userConfiguration.AppSettings.Settings.Add(name, value);
			}
		}

		public string GetParameter(string name)
		{
			TaskmanParameter matchingParameter = _supportedParameters
				.SingleOrDefault(parameter => parameter.Name == name);

			if (matchingParameter == null)
			{
				throw new TaskManException($"Unknown parameter name '{name}'.");
			}

			string userValue = _userConfiguration.AppSettings.Settings[name].Value;
			string globalValue = _globalConfiguration.AppSettings.Settings[name].Value;

			if (userValue != null)
			{
				return userValue;
			}
			else if (globalValue != null)
			{
				return globalValue;
			}
			else if (matchingParameter.DefaultValue != null)
			{
				return matchingParameter.DefaultValue;
			}
			else
			{
				throw new TaskManException($"Parameter {matchingParameter.Name} does not have a defaut value and an explicit value is not found in the configuration files."); 
			}
		}
	}
}