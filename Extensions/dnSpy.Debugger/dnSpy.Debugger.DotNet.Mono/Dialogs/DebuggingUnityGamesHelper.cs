/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using System.Diagnostics;
using dnSpy.Debugger.DotNet.Mono.Properties;

namespace dnSpy.Debugger.DotNet.Mono.Dialogs {
	static class DebuggingUnityGamesHelper {
		public const string DebuggingUnityGamesUrl = "https://github.com/dnSpy/dnSpy/wiki/Debugging-Unity-Games";
		public static string DebuggingUnityGamesText => dnSpy_Debugger_DotNet_Mono_Resources.DebuggingUnityGamesText;
		public static void OpenDebuggingUnityGames() => OpenWebPage(DebuggingUnityGamesUrl);

		static void OpenWebPage(string url) {
			try {
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			catch {
			}
		}
	}
}
