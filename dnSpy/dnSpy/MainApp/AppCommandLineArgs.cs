/*
    Copyright (C) 2014-2017 de4dot@gmail.com

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
using System.IO;
using System.Linq;
using dnSpy.Contracts.App;

namespace dnSpy.MainApp {
	sealed class AppCommandLineArgs : IAppCommandLineArgs {
		const char ARG_SEP = ':';

		public string SettingsFilename { get; }
		public IEnumerable<string> Filenames => filenames;
		public bool SingleInstance { get; }
		public bool Activate { get; }
		public string Language { get; }
		public string Culture { get; }
		public string SelectMember { get; }
		public bool NewTab { get; }
		public string SearchText { get; }
		public string SearchFor { get; }
		public string SearchIn { get; }
		public string Theme { get; }
		public bool LoadFiles { get; }
		public bool? FullScreen { get; }
		public string ShowToolWindow { get; }
		public string HideToolWindow { get; }

		readonly Dictionary<string, string> userArgs = new Dictionary<string, string>();
		readonly List<string> filenames = new List<string>();

		public AppCommandLineArgs()
			: this(Environment.GetCommandLineArgs().Skip(1).ToArray()) {
		}

		public AppCommandLineArgs(string[] args) {
			SettingsFilename = null;
			SingleInstance = true;
			Activate = true;
			Language = string.Empty;
			Culture = string.Empty;
			SelectMember = string.Empty;
			NewTab = false;
			SearchText = null;
			SearchFor = string.Empty;
			SearchIn = string.Empty;
			Theme = string.Empty;
			LoadFiles = true;
			FullScreen = null;
			ShowToolWindow = string.Empty;
			HideToolWindow = string.Empty;

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
						SettingsFilename = GetFullPath(next);
						i++;
						break;

					case "--multiple":
						SingleInstance = false;
						break;

					case "--dont-activate":
					case "--no-activate":
						Activate = false;
						break;

					case "-l":
					case "--language":
						Language = next;
						i++;
						break;

					case "--culture":
						Culture = next;
						i++;
						break;

					case "--select":
						SelectMember = next;
						i++;
						break;

					case "--new-tab":
						NewTab = true;
						break;

					case "--search":
						SearchText = next;
						i++;
						break;

					case "--search-for":
						SearchFor = next;
						i++;
						break;

					case "--search-in":
						SearchIn = next;
						i++;
						break;

					case "--theme":
						Theme = next;
						i++;
						break;

					case "--dont-load-files":
					case "--no-load-files":
						LoadFiles = false;
						break;

					case "--full-screen":
						FullScreen = true;
						break;

					case "--not-full-screen":
						FullScreen = false;
						break;

					case "--show-tool-window":
						ShowToolWindow = next;
						i++;
						break;

					case "--hide-tool-window":
						HideToolWindow = next;
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
					filenames.Add(GetFullPath(arg));
			}
		}

		static string GetFullPath(string file) {
			try {
				return Path.GetFullPath(file);
			}
			catch {
			}
			return file;
		}

		public bool HasArgument(string argName) => userArgs.ContainsKey(argName);

		public string GetArgumentValue(string argName) {
			string value;
			userArgs.TryGetValue(argName, out value);
			return value;
		}

		public IEnumerable<Tuple<string, string>> GetArguments() => userArgs.Select(a => Tuple.Create(a.Key, a.Value));
	}
}
