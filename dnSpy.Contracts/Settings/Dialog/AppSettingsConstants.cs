/*
    Copyright (C) 2014-2015 de4dot@gmail.com

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
using dnSpy.Contracts.Files;
using dnSpy.Contracts.Files.TreeView;
using dnSpy.Contracts.Languages;
using ICSharpCode.Decompiler;

namespace dnSpy.Contracts.Settings.Dialog {
	/// <summary>
	/// Constants
	/// </summary>
	public static class AppSettingsConstants {
		/// <summary>
		/// Code using <see cref="ILanguage.ShowMember(IMemberRef, DecompilerSettings)"/> must be
		/// refreshed.
		/// </summary>
		public static readonly Guid REFRESH_LANGUAGE_SHOWMEMBER = new Guid("64819A35-1DA1-4485-BFF0-C9C702147550");

		/// <summary>
		/// Code using <see cref="DecompilerSettings.DecompilationObjectsArray"/> must be refreshed
		/// </summary>
		public static readonly Guid REFRESH_LANGUAGE_DECOMPILATION_ORDER = new Guid("83018E9D-BD49-472B-93A0-77147CF40943");

		/// <summary>
		/// Redisassemble IL code
		/// </summary>
		public static readonly Guid REDISASSEMBLE_IL_CODE = new Guid("9F975119-FB7A-461D-BA17-09B82C7AE258");

		/// <summary>
		/// Redecompile ILAst code
		/// </summary>
		public static readonly Guid REDECOMPILE_ILAST_CODE = new Guid("20D97DB7-EA2D-489D-820F-DCDCE3AB01D3");

		/// <summary>
		/// Redecompile C# code
		/// </summary>
		public static readonly Guid REDECOMPILE_CSHARP_CODE = new Guid("00C8E462-E6E7-4D1F-8CE0-3385B34EA1FB");

		/// <summary>
		/// Redecompile VB code
		/// </summary>
		public static readonly Guid REDECOMPILE_VB_CODE = new Guid("818B8011-851F-4868-9674-3AC3A7B0AEC6");

		/// <summary>
		/// Disable memory mapped I/O
		/// </summary>
		public static readonly Guid DISABLE_MMAP = new Guid("D34E66D2-524C-4B6C-87CE-ED8ECCC32C59");

		/// <summary>
		/// Order of decompiler settings tab
		/// </summary>
		public const double ORDER_SETTINGS_TAB_DECOMPILER = 1000;

		/// <summary>
		/// Order of display settings tab
		/// </summary>
		public const double ORDER_SETTINGS_TAB_DISPLAY = 3000;

		/// <summary>
		/// Order of misc settings tab
		/// </summary>
		public const double ORDER_SETTINGS_TAB_MISC = 5000;

		/// <summary>
		/// Order of <see cref="IFileManager"/>'s <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_SETTINGS_LISTENER_FILEMANAGER = double.MinValue;	// It must be first since it disables mmap'd I/O

		/// <summary>
		/// Order of decompiler's <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_SETTINGS_LISTENER_DECOMPILER = 1000;

		/// <summary>
		/// Order of <see cref="IFileTreeView"/>'s <see cref="IAppSettingsModifiedListener"/> instance
		/// </summary>
		public const double ORDER_SETTINGS_LISTENER_FILETREEVIEW = 2000;

		/// <summary>
		/// Guid of app settings tab "Misc"
		/// </summary>
		public const string GUID_DYNTAB_MISC = "D32B4501-DDB3-4886-9D51-8DA1255A30DC";

		/// <summary>Misc tab: order of: use mmap'd I/O checkbox</summary>
		public const double ORDER_MISC_USEMMAPDIO = 1000;

		/// <summary>Misc tab: order of: windows explorer integration checkbox</summary>
		public const double ORDER_MISC_EXPLORERINTEGRATION = 2000;

		/// <summary>Misc tab: order of: enable all warnings button</summary>
		public const double ORDER_MISC_ENABLEALLWARNINGS = 3000;

		/// <summary>Misc tab: order of: use new renderer group box</summary>
		public const double ORDER_MISC_USENEWRENDERER = 4000;
	}
}
