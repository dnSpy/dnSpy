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

namespace dnSpy.Languages.ILSpy.Settings {
	static class SettingsConstants {
		/// <summary>
		/// Redisassemble IL (ILSpy) code
		/// </summary>
		public static readonly Guid REDISASSEMBLE_IL_ILSPY_CODE = new Guid("9F975119-FB7A-461D-BA17-09B82C7AE258");

		/// <summary>
		/// Redecompile ILAst (ILSpy) code
		/// </summary>
		public static readonly Guid REDECOMPILE_ILAST_ILSPY_CODE = new Guid("20D97DB7-EA2D-489D-820F-DCDCE3AB01D3");

		/// <summary>
		/// Redecompile C# (ILSpy) code
		/// </summary>
		public static readonly Guid REDECOMPILE_CSHARP_ILSPY_CODE = new Guid("00C8E462-E6E7-4D1F-8CE0-3385B34EA1FB");

		/// <summary>
		/// Redecompile VB (ILSpy) code
		/// </summary>
		public static readonly Guid REDECOMPILE_VB_ILSPY_CODE = new Guid("818B8011-851F-4868-9674-3AC3A7B0AEC6");
	}
}
