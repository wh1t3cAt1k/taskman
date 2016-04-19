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
using System.Configuration;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Reflection;

using TaskMan.Objects;

namespace TaskMan.Control
{
	public class TaskmanParameter
	{
		TaskmanConfiguration _configuration;

		/// <summary>
		/// Gets the parameter name that should serve as a key in the
		/// configuration file.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Gets the human-friendly description of the parameter.
		/// </summary>
		public string Description { get; }

		/// <summary>
		/// Gets the default value for the current parameter.
		/// </summary>
		public string DefaultValue { get; }

		/// <summary>
		/// Gets the validation regex for the current parameter.
		/// </summary>
		public Regex ValidationRegex { get; }

		/// <summary>
		/// Gets a value indicating whether the current parameter can be
		/// set only in the user scope.
		/// </summary>
		/// <value>
		/// <c>true</c> if this parameter is user scope only; 
		/// otherwise, <c>false</c>.
		/// </value>
		public bool IsUserScoped { get; }

		public TaskmanParameter(
			TaskmanConfiguration configuration,
			string name,
			string description,
			string defaultValue,
			Regex validationRegex,
			bool isUserScoped = false)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (description == null) throw new ArgumentNullException(nameof(description));
			if (validationRegex == null) throw new ArgumentNullException(nameof(validationRegex));
			if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));

			this._configuration = configuration;

			this.Name = name;
			this.Description = description;
			this.ValidationRegex = validationRegex;
			this.IsUserScoped = isUserScoped;

			if (!this.ValidationRegex.IsMatch(defaultValue))
			{
				throw new ArgumentException(
					"The specified default value does not match the given validation expression");
			}

			this.DefaultValue = defaultValue;
		}

		public TaskmanParameter(
			TaskmanConfiguration configuration,
			string name, 
			string description,
			string defaultValue, 
			string validationPattern = ".*", 
			bool isUserScoped = false)
			: this(
				configuration,
				name,
				description,
				defaultValue,
				new Regex(validationPattern),
				isUserScoped)
		{ }

		/// <summary>
		/// Gets the value of this parameter using the
		/// configuration object that this parameter
		/// belongs to.
		/// </summary>
		public string Value => _configuration.GetValue(this.Name);
	}

	public class TaskmanConfiguration
	{
		private Configuration _userConfiguration;
		private Configuration _globalConfiguration;

		/// <summary>
		/// Gets the directory where all configuration files for the
		/// current user must be located.
		/// </summary>
		public string UserConfigurationDirectory
		{
			get
			{
				return Path.GetDirectoryName(_userConfiguration.FilePath);
			}
		}

		const string IDENTIFIER_PATTERN = @"[A-Za-z][A-Za-z0-9]*";
		const string ENUM_VALUE_PATTERN = @"[A-Za-z][A-Za-z0-9]*|[0-9]+";
		const string ANY_PATTERN = @".*";
		const string SYMBOL_PATTERN = @"\S*";

		public TaskmanParameter CurrentTaskList => new TaskmanParameter(
			this, 
			"list",
			"the name of the task list currently used",
			"inbox", 
			IDENTIFIER_PATTERN, 
			true);

		public TaskmanParameter ImportantSymbol => new TaskmanParameter(
			this, 
			"importantsymbol",
			"symbol that precedes important tasks",
			"!",
			SYMBOL_PATTERN); 

		public TaskmanParameter CriticalSymbol => new TaskmanParameter(
			this, 
			"criticalsymbol",
			"symbol that precedes critical tasks", 
			"!!",
			SYMBOL_PATTERN);

		public TaskmanParameter FinishedSymbol => new TaskmanParameter(
			this, 
			"finishedsymbol", 
			"symbol that precedes tasks that are finished",
			"x",
			SYMBOL_PATTERN);

		public TaskmanParameter NormalTaskColor => new TaskmanParameter(
			this,
			"normalcolor",
			"color that is used for normal priority tasks",
			Console.ForegroundColor.ToString(),
			ENUM_VALUE_PATTERN);

		public TaskmanParameter FinishedTaskColor => new TaskmanParameter(
			this,
			"finishedcolor",
			"color that is used for finished tasks",
			ConsoleColor.Gray.ToString(),
			ENUM_VALUE_PATTERN);

		public TaskmanParameter ImportantTaskColor => new TaskmanParameter(
			this,
			"importantcolor",
			"color that is used for important tasks",
			ConsoleColor.Green.ToString(),
			ENUM_VALUE_PATTERN);

		public TaskmanParameter CriticalTaskColor => new TaskmanParameter(
			this,
			"criticalcolor",
			"color that is used for critical tasks",
			ConsoleColor.Yellow.ToString(),
			ENUM_VALUE_PATTERN);

		public TaskmanParameter SortOrder => new TaskmanParameter(
			this,
			"sortorder",
			"defines the default tasks sorting order in the output",
			"id+",
			ParseHelper.SortOrderRegex);

		public IEnumerable<TaskmanParameter> SupportedParameters => _supportedParameters;

		IEnumerable<TaskmanParameter> _supportedParameters;

		public TaskmanConfiguration()
		{
			_globalConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			_userConfiguration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);

			_supportedParameters = typeof(TaskmanConfiguration)
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.Where(propertyInfo => propertyInfo.PropertyType == typeof(TaskmanParameter))
				.Select(propertyInfo => propertyInfo.GetValue(this))
				.Cast<TaskmanParameter>();

			if (!Directory.Exists(this.UserConfigurationDirectory))
			{
				Directory.CreateDirectory(this.UserConfigurationDirectory);
			}
		}

		public void SetParameter(string name, string value, bool setGlobally)
		{
			TaskmanParameter matchingParameter = GetParameter(name);

			if (matchingParameter.IsUserScoped && setGlobally)
			{
				throw new TaskManException($"Parameter '{name}' can only be set at the user level");
			}

			Configuration configuration = setGlobally ?
				_globalConfiguration :
				_userConfiguration;

			configuration.AppSettings.Settings.Add(name, value);
			configuration.Save(ConfigurationSaveMode.Full);
		}

		public string GetDefaultValue(string name)
		{
			return GetParameter(name).DefaultValue;
		}

		public TaskmanParameter GetParameter(string name)
		{
			TaskmanParameter matchingParameter = _supportedParameters
				.SingleOrDefault(parameter => parameter.Name == name);

			if (matchingParameter == null)
			{
				throw new TaskManException(Messages.UnknownParameterName, name);
			}

			return matchingParameter;
		}

		public string GetValue(string name, bool forceGetGlobal = false)
		{
			TaskmanParameter matchingParameter = GetParameter(name);

			string userValue = _userConfiguration.AppSettings.Settings[name]?.Value;
			string globalValue = _globalConfiguration.AppSettings.Settings[name]?.Value;

			if (userValue != null && !forceGetGlobal)
			{
				return userValue;
			}
			else if (globalValue != null)
			{
				return globalValue;
			}
			else if (matchingParameter.IsUserScoped && forceGetGlobal)
			{
				return null;
			}
			else
			{
				return matchingParameter.DefaultValue;
			}
		}
	}
}