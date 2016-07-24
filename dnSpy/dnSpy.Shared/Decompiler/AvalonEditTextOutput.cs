// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using dnSpy.Contracts.Files.Tabs.DocViewer;
using dnSpy.Contracts.Text;
using dnSpy.Decompiler.Shared;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;

namespace dnSpy.Shared.Decompiler {
	/// <summary>
	/// A text segment that references some object. Used for hyperlinks in the editor.
	/// </summary>
	public sealed class ReferenceSegment : TextSegment {
		public object Reference;
		public bool IsLocal;
		public bool IsLocalTarget;

		public bool Equals(TextReference textRef) {
			return textRef != null &&
				Reference == textRef.Reference &&
				IsLocal == textRef.IsLocal &&
				IsLocalTarget == textRef.IsDefinition;
		}

		public TextReference ToTextReference() => new TextReference(Reference, IsLocal, IsLocalTarget);
	}

	/// <summary>
	/// Stores the positions of the definitions that were written to the text output.
	/// </summary>
	public sealed class DefinitionLookup {
		readonly Dictionary<object, int> definitions = new Dictionary<object, int>();

		public int GetDefinitionPosition(object definition) {
			int val;
			if (definition != null && definitions.TryGetValue(definition, out val))
				return val;
			else
				return -1;
		}

		public void AddDefinition(object definition, int offset) {
			if (definition != null)
				definitions[definition] = offset;
		}
	}

	/// <summary>
	/// Text output implementation for AvalonEdit.
	/// </summary>
	public sealed class AvalonEditTextOutput : IDecompilerOutput {
		public bool CanBeCached {
			get { return canBeCached; }
			private set { canBeCached = value; }
		}
		bool canBeCached = true;

		public CachedTextTokenColors CachedColors => cachedTextTokenColors;
		readonly CachedTextTokenColors cachedTextTokenColors = new CachedTextTokenColors();

		StringBuilder b = new StringBuilder();

		public readonly List<VisualLineElementGenerator> ElementGenerators = new List<VisualLineElementGenerator>();

		/// <summary>List of all references that were written to the output</summary>
		readonly TextSegmentCollection<ReferenceSegment> references = new TextSegmentCollection<ReferenceSegment>();

		public readonly DefinitionLookup DefinitionLookup = new DefinitionLookup();

		/// <summary>Embedded UIElements, see <see cref="UIElementGenerator"/>.</summary>
		public readonly List<KeyValuePair<int, Lazy<UIElement>>> UIElements = new List<KeyValuePair<int, Lazy<UIElement>>>();

		public AvalonEditTextOutput() {
		}

		/// <summary>
		/// Gets the list of references (hyperlinks).
		/// </summary>
		public TextSegmentCollection<ReferenceSegment> References => references;

		public void AddVisualLineElementGenerator(VisualLineElementGenerator elementGenerator) =>
			ElementGenerators.Add(elementGenerator);

		public int TextLength => b.Length;

		public int Length {
			get {
				throw new NotImplementedException();
			}
		}

		public int NextPosition {
			get {
				throw new NotImplementedException();
			}
		}

		public bool UsesDebugInfo {
			get {
				throw new NotImplementedException();
			}
		}

		public override string ToString() => b.ToString();

		public string GetCachedText() {
			if (cachedText != null)
				return cachedText;
			cachedText = b.ToString();
			b = null;
			return cachedText;
		}

		public void Indent() {
			throw new NotImplementedException();
		}

		public void Unindent() {
			throw new NotImplementedException();
		}

		public void WriteLine() {
			throw new NotImplementedException();
		}

		public void Write(string text, object color) {
			throw new NotImplementedException();
		}

		public void Write(string text, int index, int count, object color) {
			throw new NotImplementedException();
		}

		public void Write(string text, object reference, DecompilerReferenceFlags flags, object color) {
			throw new NotImplementedException();
		}

		public void AddDebugInfo(MethodDebugInfo methodDebugInfo) {
			throw new NotImplementedException();
		}

		string cachedText;
	}
}
