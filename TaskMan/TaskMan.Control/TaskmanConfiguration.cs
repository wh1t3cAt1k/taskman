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
			Regex validationRegex,
			string defaultValue = null,
			bool isUserScoped = false)
		{
			if (configuration == null) throw new ArgumentNullException(nameof(configuration));
			if (name == null) throw new ArgumentNullException(nameof(name));
			if (description == null) throw new ArgumentNullException(nameof(description));
			if (validationRegex == null) throw new ArgumentNullException(nameof(validationRegex));

			this._configuration = configuration;

			this.Name = name;
			this.ValidationRegex = validationRegex;
			this.IsUserScoped = isUserScoped;

			if (defaultValue != null &&
				!this.ValidationRegex.IsMatch(defaultValue))
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
			string validationPattern = ".*", 
			string defaultValue = null, 
			bool isUserScoped = false)
			: this(
				configuration,
				name,
				description,
				new Regex(validationPattern),
				defaultValue,
				isUserScoped)
		{ }

		/// <summary>
		/// Gets the value of this parameter using the
		/// configuration object that this parameter
		/// belongs to.
		/// </summary>
		public string GetValue()
		{
			return _configuration.GetParameter(this.Name);
		}
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

		private const string IDENTIFIER_PATTERN = "[A-Za-z][A-Za-z0-9]*";
		private const string ENUM_VALUE_PATTERN = "[A-Za-z][A-Za-z0-9]*|[0-9]+";
		private const string ANY_PATTERN = ".*";

		public TaskmanParameter CurrentTaskList => new TaskmanParameter(
			this, 
			"list", 
			"the name of the task list currently used",
			IDENTIFIER_PATTERN, 
			"inbox", 
			true);

		public TaskmanParameter ImportantSymbol => new TaskmanParameter(
			this, 
			"importantsymbol",
			"symbol that precedes important tasks",
			ANY_PATTERN, 
			"!");

		public TaskmanParameter CriticalSymbol => new TaskmanParameter(
			this, 
			"symbol that precedes critical tasks", 
			"criticalsymbol",
			ANY_PATTERN, 
			"!!");

		public TaskmanParameter FinishedSymbol => new TaskmanParameter(
			this, 
			"finishedsymbol", 
			"symbol that precedes tasks that are finished",
			ANY_PATTERN, 
			"x");

		public TaskmanParameter IdPrefix => new TaskmanParameter(
			this, 
			"idprefix", 
			"string prefix that precedes the task ID value",
			ANY_PATTERN, 
			"id. ");

		public TaskmanParameter NormalTaskColor => new TaskmanParameter(
			this,
			"normalcolor",
			"color that is used for normal priority tasks",
			ENUM_VALUE_PATTERN,
			Console.ForegroundColor.ToString());

		public TaskmanParameter FinishedTaskColor => new TaskmanParameter(
			this,
			"finishedcolor",
			"color that is used for finished tasks",
			ENUM_VALUE_PATTERN,
			ConsoleColor.Gray.ToString());

		public TaskmanParameter ImportantTaskColor => new TaskmanParameter(
			this,
			"importantcolor",
			"color that is used for important tasks",
			ENUM_VALUE_PATTERN,
			ConsoleColor.Green.ToString());

		public TaskmanParameter CriticalTaskColor => new TaskmanParameter(
			this,
			"criticalcolor",
			"color that is used for critical tasks",
			ENUM_VALUE_PATTERN,
			ConsoleColor.Yellow.ToString());

		public TaskmanParameter SortOrder => new TaskmanParameter(
			this,
			"sortorder",
			"defines the tasks sorting order in the output",
			ParseHelper.SortOrderRegex,
			"is+pr-id+");

		private IEnumerable<TaskmanParameter> _supportedParameters;

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

			Configuration configuration = setGlobally ?
				_globalConfiguration :
				_userConfiguration;

			configuration.AppSettings.Settings.Add(name, value);
			configuration.Save(ConfigurationSaveMode.Full);
		}

		public string GetParameter(string name)
		{
			TaskmanParameter matchingParameter = _supportedParameters
				.SingleOrDefault(parameter => parameter.Name == name);

			if (matchingParameter == null)
			{
				throw new TaskManException($"Unknown parameter name '{name}'.");
			}

			string userValue = _userConfiguration.AppSettings.Settings[name]?.Value;
			string globalValue = _globalConfiguration.AppSettings.Settings[name]?.Value;

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