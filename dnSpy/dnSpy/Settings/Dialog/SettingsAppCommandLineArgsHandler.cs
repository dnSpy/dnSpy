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
using System.ComponentModel.Composition;
using System.Windows.Threading;
using dnSpy.Contracts.App;
using dnSpy.Contracts.Settings.Dialog;

namespace dnSpy.Settings.Dialog {
	[Export(typeof(IAppCommandLineArgsHandler))]
	sealed class SettingsAppCommandLineArgsHandler : IAppCommandLineArgsHandler {
		readonly Lazy<IAppSettingsService> appSettingsService;
		const string ARG_NAME = "--settings";

		public double Order => 0;

		[ImportingConstructor]
		SettingsAppCommandLineArgsHandler(Lazy<IAppSettingsService> appSettingsService) {
			this.appSettingsService = appSettingsService;
		}

		public void OnNewArgs(IAppCommandLineArgs args) {
			if (!args.HasArgument(ARG_NAME))
				return;
			Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => {
				var guidString = args.GetArgumentValue(ARG_NAME);
				var guid = TryParse(guidString);
				if (guid != null)
					appSettingsService.Value.Show(guid.Value);
				else
					appSettingsService.Value.Show();
			}));
		}

		static Guid? TryParse(string guidString) {
			if (Guid.TryParse(guidString, out var guid))
				return guid;
			return null;
		}
	}
}
