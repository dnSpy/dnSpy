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
using System.Diagnostics;
using System.Linq;
using dnSpy.Contracts.App;

namespace dnSpy.MainApp {
	sealed class AppCommandLineArgs : IAppCommandLineArgs {
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

		public AppCommandLineArgs()
			: this(Environment.GetCommandLineArgs().Skip(1).ToArray()) {
		}

		public AppCommandLineArgs(string[] args) {
			for (int i = 0; i < args.Length; i++) {
				var arg = args[i];
				var next = i + 1 < args.Length ? args[i + 1] : string.Empty;

				if (arg.Length > 0 && arg[0] == '-') {
					switch (arg) {
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

					default:
						Debug.Fail(string.Format("Invalid arg: '{0}'", arg));
						break;
					}
				}
				else
					filenames.Add(arg);
			}
		}
	}
}
