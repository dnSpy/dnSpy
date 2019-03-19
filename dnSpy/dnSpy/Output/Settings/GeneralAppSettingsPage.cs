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

using System;
using dnSpy.Contracts.Settings.Dialog;
using dnSpy.Text.Settings;

namespace dnSpy.Output.Settings {
	sealed class GeneralAppSettingsPage : GeneralAppSettingsPageBase {
		public override Guid ParentGuid => new Guid(AppSettingsConstants.GUID_OUTPUT);
		public override Guid Guid => new Guid("FB352B1B-A310-480E-B0D6-C3F78CAA2B32");
		public override double Order => AppSettingsConstants.ORDER_OUTPUT_DEFAULT_GENERAL;

		public bool ShowTimestamps {
			get => showTimestamps;
			set {
				if (showTimestamps != value) {
					showTimestamps = value;
					OnPropertyChanged(nameof(ShowTimestamps));
				}
			}
		}
		bool showTimestamps;

		public string TimestampDateTimeFormat {
			get => timestampDateTimeFormat;
			set {
				if (timestampDateTimeFormat != value) {
					timestampDateTimeFormat = value;
					OnPropertyChanged(nameof(TimestampDateTimeFormat));
				}
			}
		}
		string timestampDateTimeFormat;

		readonly IOutputWindowOptions options;

		public GeneralAppSettingsPage(IOutputWindowOptions options)
			: base(options) {
			this.options = options;
			ShowTimestamps = options.ShowTimestamps;
			TimestampDateTimeFormat = options.TimestampDateTimeFormat;
		}

		public override void OnApply() {
			options.ShowTimestamps = ShowTimestamps;
			options.TimestampDateTimeFormat = TimestampDateTimeFormat;
			base.OnApply();
		}
	}
}
