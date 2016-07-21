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
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Decompiler.Shared;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Code mappings
	/// </summary>
	public interface ICodeMappings {
		/// <summary>
		/// Number of mappings
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets code mappings
		/// </summary>
		/// <param name="line">Line</param>
		/// <param name="column">Column</param>
		/// <returns></returns>
		IList<SourceCodeMapping> Find(int line, int column);

		/// <summary>
		/// Gets a code mapping
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="ilOffset">IL offset</param>
		/// <returns></returns>
		SourceCodeMapping Find(MethodDef method, uint ilOffset);
	}

	/// <summary>
	/// Constants
	/// </summary>
	public static class CodeMappingsConstants {
		/// <summary>
		/// Code mappings key
		/// </summary>
		public static readonly object CodeMappingsKey = new object();
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class CodeMappingsExtensions {
		/// <summary>
		/// Gets a <see cref="ICodeMappings"/> instance
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static ICodeMappings GetCodeMappings(this IDocumentViewer self) => self.TryGetCodeMappings() ?? EmptyCodeMappings.Instance;

		/// <summary>
		/// Gets a <see cref="ICodeMappings"/> or null if none exists
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static ICodeMappings TryGetCodeMappings(this IDocumentViewer self) {
			if (self == null)
				return null;
			return (ICodeMappings)self.GetContentData(CodeMappingsConstants.CodeMappingsKey);
		}

		sealed class EmptyCodeMappings : ICodeMappings {
			public static readonly EmptyCodeMappings Instance = new EmptyCodeMappings();

			public int Count => 0;
			public IList<SourceCodeMapping> Find(int line, int column) => Array.Empty<SourceCodeMapping>();
			public SourceCodeMapping Find(MethodDef method, uint ilOffset) => null;
		}
	}
}
