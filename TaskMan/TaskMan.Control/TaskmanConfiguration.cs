using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text.RegularExpressions;

using TaskMan.Objects;

namespace TaskMan.Control
{
	public class TaskmanConfiguration
	{
		const string CURRENT_TASK_LIST_KEY = "currentlist";

		private Configuration _configuration;
		private bool _isGlobal;

		public TaskmanConfiguration(ConfigurationUserLevel configurationLevel)
		{
			switch (configurationLevel)
			{
				case ConfigurationUserLevel.None:
					_configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
					_isGlobal = true;
					break;

				case ConfigurationUserLevel.PerUserRoaming:
					_configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoaming);
					_isGlobal = false;
					break;

				default:
					throw new TaskManException("Only per user roaming and none are supported.");
			}
		}

		/// <summary>
		/// Sets the parameter value.
		/// </summary>
		/// <param name="key">The parameter key.</param>
		/// <param name="value">The parameter value.</param>
		/// <param name="allowUserScopeOnly">If <c>true</c>, the setting will never be written to global configuration.</param>
		private void SetParameterValue(string key, string value, bool allowUserScopeOnly)
		{
			if (_isGlobal && allowUserScopeOnly)
			{
				throw new TaskManException($"The {key} setting does not allow global configuration");
			}
		}

		/// <summary>
		/// Gets or sets the current task list name.
		/// </summary>
		public string CurrentTaskList
		{
			set
			{
				Regex identifierRegex = new Regex("[A-Za-z][A-Za-z0-9]*");

				if (!identifierRegex.IsMatch(value))
				{
					throw new TaskManException($"Invalid list name {value} - can consist only of latin alphanumeric characters, and cannot start with a digit.");
				}

				SetParameterValue(CURRENT_TASK_LIST_KEY, value, true);
			}
		}
	}
}