// Copyright (c) 2009-2013 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.NRefactory.Xml
{
	/// <summary>
	/// Encapsulates the state of the incremental tag soup parser.
	/// </summary>
	public class IncrementalParserState
	{
		internal readonly int TextLength;
		internal readonly ITextSourceVersion Version;
		internal readonly InternalObject[] Objects;
		
		internal IncrementalParserState(int textLength, ITextSourceVersion version, InternalObject[] objects)
		{
			this.TextLength = textLength;
			this.Version = version;
			this.Objects = objects;
		}
		
		internal List<UnchangedSegment> GetReuseMapTo(ITextSourceVersion newVersion)
		{
			ITextSourceVersion oldVersion = this.Version;
			if (oldVersion == null || newVersion == null)
				return null;
			if (!oldVersion.BelongsToSameDocumentAs(newVersion))
				return null;
			List<UnchangedSegment> reuseMap = new List<UnchangedSegment>();
			reuseMap.Add(new UnchangedSegment(0, 0, this.TextLength));
			foreach (var change in oldVersion.GetChangesTo(newVersion)) {
				bool needsSegmentRemoval = false;
				for (int i = 0; i < reuseMap.Count; i++) {
					UnchangedSegment segment = reuseMap[i];
					if (segment.NewOffset + segment.Length <= change.Offset) {
						// change is completely after this segment
						continue;
					}
					if (change.Offset + change.RemovalLength <= segment.NewOffset) {
						// change is completely before this segment
						segment.NewOffset += change.InsertionLength - change.RemovalLength;
						reuseMap[i] = segment;
						continue;
					}
					// Change is overlapping segment.
					// Split segment into two parts: the part before change, and the part after change.
					var segmentBefore = new UnchangedSegment(segment.OldOffset, segment.NewOffset, change.Offset - segment.NewOffset);
					Debug.Assert(segmentBefore.Length < segment.Length);
					
					int lengthAtEnd = segment.NewOffset + segment.Length - (change.Offset + change.RemovalLength);
					var segmentAfter = new UnchangedSegment(
						segment.OldOffset + segment.Length - lengthAtEnd,
						change.Offset + change.InsertionLength,
						lengthAtEnd);
					Debug.Assert(segmentAfter.Length < segment.Length);
					Debug.Assert(segmentBefore.Length + segmentAfter.Length <= segment.Length);
					Debug.Assert(segmentBefore.NewOffset + segmentBefore.Length <= segmentAfter.NewOffset);
					Debug.Assert(segmentBefore.OldOffset + segmentBefore.Length <= segmentAfter.OldOffset);
					if (segmentBefore.Length > 0 && segmentAfter.Length > 0) {
						reuseMap[i] = segmentBefore;
						reuseMap.Insert(++i, segmentAfter);
					} else if (segmentBefore.Length > 0) {
						reuseMap[i] = segmentBefore;
					} else {
						reuseMap[i] = segmentAfter;
						if (segmentAfter.Length <= 0)
							needsSegmentRemoval = true;
					}
				}
				if (needsSegmentRemoval)
					reuseMap.RemoveAll(s => s.Length <= 0);
			}
			return reuseMap;
		}
	}
	
	struct UnchangedSegment
	{
		public int OldOffset;
		public int NewOffset;
		public int Length;
		
		public UnchangedSegment(int oldOffset, int newOffset, int length)
		{
			this.OldOffset = oldOffset;
			this.NewOffset = newOffset;
			this.Length = length;
		}
		
		public override string ToString()
		{
			return string.Format("[UnchangedSegment OldOffset={0}, NewOffset={1}, Length={2}]", OldOffset, NewOffset, Length);
		}
	}
}
