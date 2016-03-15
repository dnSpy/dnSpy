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
using System.Linq;
using dnSpy.Contracts.App;

namespace dnSpy.MainApp {
	sealed class AppCommandLineArgs : IAppCommandLineArgs {
		const char ARG_SEP = ':';

		public string SettingsFilename {
			get { return settingsFilename; }
		}
		readonly string settingsFilename = null;

		public IEnumerable<string> Filenames {
			get { return filenames.AsEnumerable(); }
		}
		readonly List<string> filenames = new List<string>();

		public bool SingleInstance {
			get { return singleInstance; }
		}
		readonly bool singleInstance = true;

		public bool Activate {
			get { return activate; }
		}
		readonly bool activate = true;

		public string Language {
			get { return language; }
		}
		readonly string language = string.Empty;

		public string Culture {
			get { return culture; }
		}
		readonly string culture = string.Empty;

		public string SelectMember {
			get { return selectMember; }
		}
		readonly string selectMember = string.Empty;

		public bool NewTab {
			get { return newTab; }
		}
		readonly bool newTab = false;

		public string SearchText {
			get { return searchText; }
		}
		readonly string searchText = null;

		public string SearchFor {
			get { return searchFor; }
		}
		readonly string searchFor = string.Empty;

		public string SearchIn {
			get { return searchIn; }
		}
		readonly string searchIn = string.Empty;

		public string Theme {
			get { return theme; }
		}
		readonly string theme = string.Empty;

		public bool LoadFiles {
			get { return loadFiles; }
		}
		readonly bool loadFiles = true;

		public bool? FullScreen {
			get { return fullScreen; }
		}
		bool? fullScreen = null;

		public string ShowToolWindow {
			get { return showToolWindow; }
		}
		readonly string showToolWindow = string.Empty;

		public string HideToolWindow {
			get { return hideToolWindow; }
		}
		readonly string hideToolWindow = string.Empty;

		readonly Dictionary<string, string> userArgs = new Dictionary<string, string>();

		public AppCommandLineArgs()
			: this(Environment.GetCommandLineArgs().Skip(1).ToArray()) {
		}

		public AppCommandLineArgs(string[] args) {
			bool canParseCommands = true;
			for (int i = 0; i < args.Length; i++) {
				var arg = args[i];
				var next = i + 1 < args.Length ? args[i + 1] : string.Empty;

				if (canParseCommands && arg.Length > 0 && arg[0] == '-') {
					switch (arg) {
					case "--":
						canParseCommands = false;
						break;

					case "--settings-file":
						settingsFilename = next;
						i++;
						break;

					case "--multiple":
						singleInstance = false;
						break;

					case "--dont-activate":
					case "--no-activate":
						activate = false;
						break;

					case "-l":
					case "--language":
						language = next;
						i++;
						break;

					case "--culture":
						culture = next;
						i++;
						break;

					case "--select":
						selectMember = next;
						i++;
						break;

					case "--new-tab":
						newTab = true;
						break;

					case "--search":
						searchText = next;
						i++;
						break;

					case "--search-for":
						searchFor = next;
						i++;
						break;

					case "--search-in":
						searchIn = next;
						i++;
						break;

					case "--theme":
						theme = next;
						i++;
						break;

					case "--dont-load-files":
					case "--no-load-files":
						loadFiles = false;
						break;

					case "--full-screen":
						fullScreen = true;
						break;

					case "--not-full-screen":
						fullScreen = false;
						break;

					case "--show-tool-window":
						showToolWindow = next;
						i++;
						break;

					case "--hide-tool-window":
						hideToolWindow = next;
						i++;
						break;

					default:
						int sepIndex = arg.IndexOf(ARG_SEP);
						string argName, argValue;
						if (sepIndex < 0) {
							argName = arg;
							argValue = string.Empty;
						}
						else {
							argName = arg.Substring(0, sepIndex);
							argValue = arg.Substring(sepIndex + 1);
						}
						if (!userArgs.ContainsKey(argName))
							userArgs.Add(argName, argValue);
						break;
					}
				}
				else
					filenames.Add(arg);
			}
		}

		public bool HasArgument(string argName) {
			return userArgs.ContainsKey(argName);
		}

		public string GetArgumentValue(string argName) {
			string value;
			userArgs.TryGetValue(argName, out value);
			return value;
		}

		public IEnumerable<Tuple<string, string>> GetArguments() {
			return userArgs.Select(a => Tuple.Create(a.Key, a.Value));
		}
	}
}
