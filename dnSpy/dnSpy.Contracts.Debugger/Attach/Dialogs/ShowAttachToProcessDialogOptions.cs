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

namespace dnSpy.Contracts.Debugger.Attach.Dialogs {
	/// <summary>
	/// Attach to Process dialog options
	/// </summary>
	public sealed class ShowAttachToProcessDialogOptions {
		/// <summary>
		/// Gets the title or null to use the default title
		/// </summary>
		public string Title { get; set; }

		/// <summary>
		/// Type of processes that can be attached to. Shown in the title bar, eg. "Unity" or null to not show anything
		/// </summary>
		public string ProcessType { get; set; }

		/// <summary>
		/// Text shown at the bottom of the dialog box between the buttons or null to use the default value
		/// </summary>
		public string Message { get; set; }

		/// <summary>
		/// <see cref="AttachProgramOptionsProviderFactory"/> names, see <see cref="PredefinedAttachProgramOptionsProviderNames"/>
		/// or null to check every provider
		/// </summary>
		public string[] ProviderNames { get; set; }

		/// <summary>
		/// Link button info shown next to the OK button
		/// </summary>
		public AttachToProcessLinkInfo? InfoLink { get; set; }
	}

	/// <summary>
	/// Link info
	/// </summary>
	public struct AttachToProcessLinkInfo {
		/// <summary>
		/// Tooltip message
		/// </summary>
		public string ToolTipMessage { get; set; }

		/// <summary>
		/// URL to go to when button is clicked
		/// </summary>
		public string Url { get; set; }
	}
}
