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

namespace dnSpy.Contracts.Text.Editor {
	/// <summary>
	/// Predefined <see cref="ITextView"/> roles
	/// </summary>
	public static class PredefinedTextViewRoles {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public const string Analyzable = "0244C9B6-7BDE-4573-AE80-205652358AC3";
		public const string Debuggable = "20C44446-6FA0-4F5B-AFC0-58700674E369";
		public const string Document = "8D05496D-CA83-4638-8EC6-661BEDCEC747";
		public const string Editable = "E802B8B8-3D97-495E-A45A-DEA80F68BDE4";
		public const string Interactive = "ECB71DF9-3FDF-48EF-A3C9-947C85E15915";
		public const string PrimaryDocument = "A2BD0BCD-5854-4D84-BDB5-F6BC35A50940";
		public const string Structured = "F83768FA-2E36-4B39-A28D-ADFD272C4C57";
		public const string Zoomable = "142FE126-123D-4A6F-A996-ED7AC1D75926";
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
