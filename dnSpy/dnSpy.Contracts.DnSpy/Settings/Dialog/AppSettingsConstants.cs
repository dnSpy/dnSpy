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
using dnlib.DotNet;
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Documents;
using dnSpy.Contracts.Documents.TreeView;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Constants
	/// </summary>
	public static class AppSettingsConstants {
		/// <summary>
		/// Code using <see cref="IDecompiler.ShowMember(IMemberRef)"/> must be
		/// refreshed.
		/// </summary>
		public static readonly Guid REFRESH_LANGUAGE_SHOWMEMBER = new Guid("64819A35-1DA1-4485-BFF0-C9C702147550");

		/// <summary>
		/// Treeview member order setting got updated
		/// </summary>
		public static readonly Guid REFRESH_TREEVIEW_MEMBER_ORDER = new Guid("CA54455B-6D5D-4E82-86D4-7CE4CA5AC478");

		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		public static readonly Guid DISABLE_MEMORY_MAPPED_IO = new Guid("D34E66D2-524C-4B6C-87CE-ED8ECCC32C59");

		/// <summary>
		/// Order of Environment settings tab
		/// </summary>
		public const double ORDER_TAB_ENVIRONMENT = 0;

		/// <summary>
		/// Order of decompiler settings tab
		/// </summary>
		public const double ORDER_TAB_DECOMPILER = 1000;

		/// <summary>
		/// Order of debugger settings tab
		/// </summary>
		public const double ORDER_TAB_DEBUGGER = 2000;

		/// <summary>
		/// Order of Assembly Explorer settings tab
		/// </summary>
		public const double ORDER_TAB_ASSEMBLY_EXPLORER = 3000;

		/// <summary>
		/// Order of hex editor settings tab
		/// </summary>
		public const double ORDER_TAB_HEXEDITOR = 4000;

		/// <summary>
		/// Order of background image settings tab
		/// </summary>
		public const double ORDER_TAB_BACKGROUNDIMAGE = 5000;

		/// <summary>
		/// Order of baml settings tab
		/// </summary>
		public const double ORDER_TAB_BAML = 6000;

		/// <summary>
		/// Order of <see cref="IDsDocumentService"/>'s <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_LISTENER_DOCUMENTMANAGER = double.MinValue;	// It must be first since it disables mmap'd I/O

		/// <summary>
		/// Order of decompiler's <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_LISTENER_DECOMPILER = 1000;

		/// <summary>
		/// Order of <see cref="IDocumentTreeView"/>'s <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_LISTENER_DOCUMENTTREEVIEW = 2000;

		/// <summary>
		/// Guid of Environment tab settings
		/// </summary>
		public const string GUID_ENVIRONMENT = "66B8E553-3961-4B0D-8948-F399FA78A809";

		/// <summary>
		/// Guid of Decompiler tab settings
		/// </summary>
		public const string GUID_DECOMPILER = "E380FC93-BACB-4125-8AF1-ADFAEA4D1307";

		/// <summary>
		/// Order of Environment / General
		/// </summary>
		public const double ORDER_ENVIRONMENT_GENERAL = 0;

		/// <summary>
		/// Order of Environment / Font
		/// </summary>
		public const double ORDER_ENVIRONMENT_FONT = 1000;

		/// <summary>
		/// Order of Decompiler / ILSpy C#/VB
		/// </summary>
		public const double ORDER_DECOMPILER_SETTINGS_ILSPY_CSHARP = 10000;

		/// <summary>
		/// Order of Decompiler / ILSpy IL
		/// </summary>
		public const double ORDER_DECOMPILER_SETTINGS_ILSPY_IL = 11000;
	}
}
