// 
// ITextEditorAdapter.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;

namespace ICSharpCode.NRefactory
{
	public interface ITextEditorAdapter
	{
		bool TabsToSpaces { get; }

		int TabSize { get; }

		string EolMarker { get; }
		
		string Text { get; }

		int Length { get; }

		int LocationToOffset (int line, int col);
		char GetCharAt (int offset);
		string GetTextAt (int offset, int length);
		
		int LineCount { get; }

		int GetEditableLength (int lineNumber);
		string GetIndentation (int lineNumber);
		int GetLineOffset (int lineNumber);
		int GetLineLength (int lineNumber);
		int GetLineEndOffset (int lineNumber);
		
		void Replace (int offset, int count, string text);
	}
	
	/*
	public static class ITextEditorAdapterHelperMethods
	{
		public static void AcceptChanges (this ITextEditorAdapter adapter, List<Change> changes)
		{
			for (int i = 0; i < changes.Count; i++) {
				changes [i].PerformChange (adapter);
				var replaceChange = changes [i];
				for (int j = i + 1; j < changes.Count; j++) {
					var change = changes [j];
					if (replaceChange.Offset >= 0 && change.Offset >= 0) {
						if (replaceChange.Offset < change.Offset) {
							change.Offset -= replaceChange.RemovedChars;
							if (!string.IsNullOrEmpty (replaceChange.InsertedText))
								change.Offset += replaceChange.InsertedText.Length;
						} else if (replaceChange.Offset < change.Offset + change.RemovedChars) {
							change.RemovedChars -= replaceChange.RemovedChars;
							change.Offset = replaceChange.Offset + (!string.IsNullOrEmpty (replaceChange.InsertedText) ? replaceChange.InsertedText.Length : 0);
						}
					}
				}
			}
		}		
	}*/
}

