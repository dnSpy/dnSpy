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
using dnSpy.Contracts.Decompiler;
using dnSpy.Contracts.Text;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Files.Tabs.DocViewer {
	/// <summary>
	/// Reference info
	/// </summary>
	public struct ReferenceInfo : IEquatable<ReferenceInfo> {
		/// <summary>
		/// Gets the reference or null
		/// </summary>
		public object Reference { get; }

		/// <summary>
		/// Gets the flags
		/// </summary>
		public DecompilerReferenceFlags Flags { get; }

		/// <summary>
		/// true if it's a local, parameter, or label
		/// </summary>
		public bool IsLocal => (Flags & DecompilerReferenceFlags.Local) != 0;

		/// <summary>
		/// true if it's a definition
		/// </summary>
		public bool IsDefinition => (Flags & DecompilerReferenceFlags.Definition) != 0;

		/// <summary>
		/// true if it's a write to a reference
		/// </summary>
		public bool IsWrite => (Flags & DecompilerReferenceFlags.IsWrite) != 0;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="reference">Reference or null</param>
		/// <param name="flags">Flags</param>
		public ReferenceInfo(object reference, DecompilerReferenceFlags flags) {
			Reference = reference;
			Flags = flags;
		}

		/// <summary>
		/// Creates a <see cref="TextReference"/> instance
		/// </summary>
		/// <returns></returns>
		public TextReference ToTextReference() => new TextReference(Reference, Flags);

		/// <summary>
		/// Creates a <see cref="TextReference"/> instance
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		public TextReference ToTextReference(Span span) => new TextReference(Reference, Flags, span);

		/// <summary>
		/// operator ==()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(ReferenceInfo a, ReferenceInfo b) => a.Equals(b);

		/// <summary>
		/// operator !=()
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(ReferenceInfo a, ReferenceInfo b) => !a.Equals(b);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(ReferenceInfo other) => Flags == other.Flags && Equals(Reference, other.Reference);

		/// <summary>
		/// Equals()
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj) => obj is ReferenceInfo && Equals((ReferenceInfo)obj);

		/// <summary>
		/// GetHashCode()
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode() => (Reference?.GetHashCode() ?? int.MinValue) ^ (int)Flags;
	}

	/// <summary>
	/// <see cref="SpanData{TData}"/> extensions
	/// </summary>
	public static class SpanDataReferenceInfoExtensions {
		/// <summary>
		/// Creates a <see cref="TextReference"/>
		/// </summary>
		/// <param name="spanData">Instance</param>
		/// <returns></returns>
		public static TextReference ToTextReference(this SpanData<ReferenceInfo> spanData) => spanData.Data.ToTextReference(spanData.Span);
	}
}
