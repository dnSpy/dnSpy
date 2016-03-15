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

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace dnSpy.Contracts.App {
	/// <summary>
	/// Application directories
	/// </summary>
	public static class AppDirectories {
		const string DNSPY_SETTINGS_FILENAME = "dnSpy.xml";

		/// <summary>
		/// Base directory of dnSpy binaries
		/// </summary>
		public static string BinDirectory {
			get { return binDir; }
		}
		static readonly string binDir;

		/// <summary>
		/// Base directory of data directory. Usually %APPDATA%\dnSpy but could be identical to
		/// <see cref="BinDirectory"/>.
		/// </summary>
		public static string DataDirectory {
			get { return appDataDir; }
		}
		static readonly string appDataDir;

		/// <summary>
		/// dnSpy settings filename
		/// </summary>
		public static string SettingsFilename {
			get { return settingsFilename; }
		}
		static string settingsFilename;

		/// <summary>
		/// Don't call this method. It's called by dnSpy to initialize <see cref="SettingsFilename"/>
		/// </summary>
		/// <param name="filename">Settings filename</param>
		/// <returns></returns>
		[EditorBrowsable(EditorBrowsableState.Never)]
		public static void __SetSettingsFilename(string filename) {
			if (hasCalledSetSettingsFilename)
				throw new InvalidOperationException();
			hasCalledSetSettingsFilename = true;
			if (!string.IsNullOrEmpty(filename))
				settingsFilename = filename;
		}
		static bool hasCalledSetSettingsFilename = false;

		static AppDirectories() {
			binDir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			settingsFilename = Path.Combine(binDir, DNSPY_SETTINGS_FILENAME);
			if (File.Exists(settingsFilename))
				appDataDir = binDir;
			else {
				appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dnSpy");
				settingsFilename = Path.Combine(appDataDir, DNSPY_SETTINGS_FILENAME);
			}
		}

		/// <summary>
		/// Returns directories relative to <see cref="BinDirectory"/> and <see cref="DataDirectory"/>
		/// in that order. If they're identical, only one path is returned.
		/// </summary>
		/// <param name="subDir">Sub directory</param>
		/// <returns></returns>
		public static IEnumerable<string> GetDirectories(string subDir) {
			yield return Path.Combine(BinDirectory, subDir);
			if (!StringComparer.OrdinalIgnoreCase.Equals(BinDirectory, DataDirectory))
				yield return Path.Combine(DataDirectory, subDir);
		}
	}
}
