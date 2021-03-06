﻿/*
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

namespace TaskMan.Control
{
	/// <summary>
	/// Denotes the tasks display format.
	/// </summary>
	public enum Format
	{
		/// <summary>
		/// Denotes the usual textual format for console output.
		/// </summary>
		Text = 0,
		/// <summary>
		/// Denotes the comma-separated-value format (with header).
		/// </summary>
		CSV,
		/// <summary>
		/// Denotes the JSON format. 
		/// </summary>
		JSON,
		/// <summary>
		/// Denotes the XML format.
		/// </summary>
		XML
	}
}

