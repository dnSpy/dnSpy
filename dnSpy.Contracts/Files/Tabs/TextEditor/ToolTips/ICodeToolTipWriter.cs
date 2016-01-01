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

using dnSpy.Contracts.Highlighting;

namespace dnSpy.Contracts.Files.Tabs.TextEditor.ToolTips {
	/// <summary>
	/// Writes tooltips
	/// </summary>
	public interface ICodeToolTipWriter : ISyntaxHighlightOutput {
		/// <summary>
		/// Writes an XML doc comment. Returns true if it was written, false otherwise
		/// </summary>
		/// <param name="xmlDoc">XML doc</param>
		/// <returns></returns>
		bool WriteXmlDoc(string xmlDoc);

		/// <summary>
		/// Writes an XML doc parameter. Returns true if it was written, false otherwise
		/// </summary>
		/// <param name="xmlDoc">XML doc</param>
		/// <param name="paramName">Name of parameter</param>
		/// <returns></returns>
		bool WriteXmlDocParameter(string xmlDoc, string paramName);

		/// <summary>
		/// Writes an XML doc generic. Returns true if it was written, false otherwise
		/// </summary>
		/// <param name="xmlDoc">XML doc</param>
		/// <param name="gpName">Name of generic parameter</param>
		/// <returns></returns>
		bool WriteXmlDocGeneric(string xmlDoc, string gpName);
	}
}
