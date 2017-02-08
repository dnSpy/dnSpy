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
using Microsoft.VisualStudio.Text.Editor;

namespace dnSpy.Contracts.Settings.Groups {
	/// <summary>
	/// Provides group names. Use <see cref="ExportTextViewOptionsGroupNameProviderAttribute"/> to
	/// export an instance.
	/// </summary>
	public interface ITextViewOptionsGroupNameProvider {
		/// <summary>
		/// Returns a group name, eg. <see cref="PredefinedTextViewGroupNames.CodeEditor"/>, or null
		/// </summary>
		/// <param name="textView">Text view</param>
		/// <returns></returns>
		string TryGetGroupName(IWpfTextView textView);
	}

	/// <summary>Metadata</summary>
	public interface ITextViewOptionsGroupNameProviderMetadata {
		/// <summary>See <see cref="ExportTextViewOptionsGroupNameProviderAttribute.Order"/></summary>
		double Order { get; }
	}

	/// <summary>
	/// Exports a <see cref="ITextViewOptionsGroupNameProvider"/> instance
	/// </summary>
	[MetadataAttribute, AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class ExportTextViewOptionsGroupNameProviderAttribute : ExportAttribute, ITextViewOptionsGroupNameProviderMetadata {
		/// <summary>Constructor</summary>
		/// <param name="order">Order of this instanec</param>
		public ExportTextViewOptionsGroupNameProviderAttribute(double order = double.MaxValue)
			: base(typeof(ITextViewOptionsGroupNameProvider)) {
			Order = order;
		}

		/// <summary>
		/// Order of this instance
		/// </summary>
		public double Order { get; }
	}
}
