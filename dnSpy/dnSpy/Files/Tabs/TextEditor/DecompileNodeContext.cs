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
using System.Windows.Threading;
using dnSpy.Contracts.Files.Tabs.TextEditor;
using dnSpy.Contracts.Languages;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;
using ICSharpCode.AvalonEdit.Highlighting;

namespace dnSpy.Files.Tabs.TextEditor {
	sealed class DecompileNodeContext : IDecompileNodeContext {
		public DecompilationContext DecompilationContext { get; }
		public ILanguage Language { get; }
		public ITextOutput Output { get; }
		public IHighlightingDefinition HighlightingDefinition { get; set; }
		public string HighlightingExtension { get; set; }
		public IContentType ContentType { get; set; }
		public Guid ContentTypeGuid { get; set; }

		readonly Dispatcher dispatcher;

		public DecompileNodeContext(DecompilationContext decompilationContext, ILanguage language, ITextOutput output, Dispatcher dispatcher) {
			if (decompilationContext == null)
				throw new ArgumentNullException();
			if (language == null)
				throw new ArgumentNullException();
			if (output == null)
				throw new ArgumentNullException();
			if (dispatcher == null)
				throw new ArgumentNullException();
			this.DecompilationContext = decompilationContext;
			this.Language = language;
			this.Output = output;
			this.dispatcher = dispatcher;
		}

		public T ExecuteInUIThread<T>(Func<T> func) {
			if (dispatcher.CheckAccess())
				return func();

			return (T)dispatcher.Invoke(DispatcherPriority.Send, func);
		}
	}
}
