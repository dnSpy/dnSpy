/*
    Copyright (C) 2014-2016 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace dnSpy.Contracts.Plugin {
	/// <summary>
	/// License information
	/// </summary>
	public sealed class PluginLicense {
		/// <summary>
		/// Path to license file (in the plugin's resources) or null
		/// </summary>
		public string ResourceFilePath { get; set; }

		/// <summary>
		/// Name of license shown in the UI, eg. MIT or GPL v3 etc
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Constructor
		/// </summary>
		public PluginLicense() {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="name">Name, see <see cref="Name"/></param>
		/// <param name="path">Path to file, see <see cref="ResourceFilePath"/></param>
		public PluginLicense(string name, string path = null) {
			this.Name = name;
			this.ResourceFilePath = path;
		}
	}
}
