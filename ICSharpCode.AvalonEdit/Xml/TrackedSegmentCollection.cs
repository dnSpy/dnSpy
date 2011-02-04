// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using ICSharpCode.AvalonEdit.Document;

namespace ICSharpCode.AvalonEdit.Xml
{
	/// <summary>
	/// Holds all objects that need to keep offsets up to date.
	/// </summary>
	class TrackedSegmentCollection
	{
		/// <summary>
		/// Holds all types of objects in one collection.
		/// </summary>
		TextSegmentCollection<TextSegment> segments = new TextSegmentCollection<TextSegment>();
		
		/// <summary>
		/// Is used to identify what memory range was touched by object
		/// The default is (StartOffset, EndOffset + 1) which is not stored
		/// </summary>
		class TouchedRange: TextSegment
		{
			public AXmlObject TouchedByObject { get; set; }
		}
		
		public void UpdateOffsetsAndInvalidate(IEnumerable<DocumentChangeEventArgs> changes)
		{
			foreach(DocumentChangeEventArgs change in changes) {
				// Update offsets of all items
				segments.UpdateOffsets(change);
				
				// Remove any items affected by the change
				AXmlParser.Log("Changed {0}-{1}", change.Offset, change.Offset + change.InsertionLength);
				// Removing will cause one of the ends to be set to change.Offset
				// FindSegmentsContaining includes any segments touching
				// so that conviniently takes care of the +1 byte
				var segmentsContainingOffset = segments.FindOverlappingSegments(change.Offset, change.InsertionLength);
				foreach(AXmlObject obj in segmentsContainingOffset.OfType<AXmlObject>().Where(o => o.IsCached)) {
					InvalidateCache(obj, false);
				}
				foreach(TouchedRange range in segmentsContainingOffset.OfType<TouchedRange>()) {
					AXmlParser.Log("Found that {0} dependeds on ({1}-{2})", range.TouchedByObject, range.StartOffset, range.EndOffset);
					InvalidateCache(range.TouchedByObject, true);
					segments.Remove(range);
				}
			}
		}
		
		/// <summary>
		/// Invlidates all objects.  That is, the whole document has changed.
		/// </summary>
		/// <remarks> We still have to keep the items becuase they might be in the document </remarks>
		public void InvalidateAll()
		{
			AXmlParser.Log("Invalidating all objects");
			foreach(AXmlObject obj in segments.OfType<AXmlObject>()) {
				obj.IsCached = false;
			}
		}
		
		/// <summary> Add object to cache, optionally adding extra memory tracking </summary>
		public void AddParsedObject(AXmlObject obj, int? maxTouchedLocation)
		{
			if (!(obj.Length > 0 || obj is AXmlDocument))
				AXmlParser.Assert(false, string.Format(CultureInfo.InvariantCulture, "Invalid object {0}.  It has zero length.", obj));
//			// Expensive check
//			if (obj is AXmlContainer) {
//				int objStartOffset = obj.StartOffset;
//				int objEndOffset = obj.EndOffset;
//				foreach(AXmlObject child in ((AXmlContainer)obj).Children) {
//					AXmlParser.Assert(objStartOffset <= child.StartOffset && child.EndOffset <= objEndOffset, "Wrong nesting");
//				}
//			}
			segments.Add(obj);
			AddSyntaxErrorsOf(obj);
			obj.IsCached = true;
			if (maxTouchedLocation != null) {
				// location is assumed to be read so the range ends at (location + 1)
				// For example eg for "a_" it is (0-2)
				TouchedRange range = new TouchedRange() {
					StartOffset = obj.StartOffset,
					EndOffset = maxTouchedLocation.Value + 1,
					TouchedByObject = obj
				};
				segments.Add(range);
				AXmlParser.Log("{0} touched range ({1}-{2})", obj, range.StartOffset, range.EndOffset);
			}
		}
		
		/// <summary> Removes object with all of its non-cached children </summary>
		public void RemoveParsedObject(AXmlObject obj)
		{
			// Cached objects may be used in the future - do not remove them
			if (obj.IsCached) return;
			segments.Remove(obj);
			RemoveSyntaxErrorsOf(obj);
			AXmlParser.Log("Stopped tracking {0}", obj);
			
			AXmlContainer container = obj as AXmlContainer;
			if (container != null) {
				foreach (AXmlObject child in container.Children) {
					RemoveParsedObject(child);
				}
			}
		}
		
		public void AddSyntaxErrorsOf(AXmlObject obj)
		{
			foreach(SyntaxError syntaxError in obj.MySyntaxErrors) {
				segments.Add(syntaxError);
			}
		}
		
		public void RemoveSyntaxErrorsOf(AXmlObject obj)
		{
			foreach(SyntaxError syntaxError in obj.MySyntaxErrors) {
				segments.Remove(syntaxError);
			}
		}
		
		IEnumerable<AXmlObject> FindParents(AXmlObject child)
		{
			int childStartOffset = child.StartOffset;
			int childEndOffset = child.EndOffset;
			foreach(AXmlObject parent in segments.FindSegmentsContaining(child.StartOffset).OfType<AXmlObject>()) {
				// Parent is anyone wholy containg the child
				if (parent.StartOffset <= childStartOffset && childEndOffset <= parent.EndOffset && parent != child) {
					yield return parent;
				}
			}
		}
		
		/// <summary> Invalidates items, but keeps tracking them </summary>
		/// <remarks> Can be called redundantly (from range tacking) </remarks>
		void InvalidateCache(AXmlObject obj, bool includeParents)
		{
			if (includeParents) {
				foreach(AXmlObject parent in FindParents(obj)) {
					parent.IsCached = false;
					AXmlParser.Log("Invalidating cached item {0} (it is parent)", parent);
				}
			}
			obj.IsCached = false;
			AXmlParser.Log("Invalidating cached item {0}", obj);
		}
		
		public T GetCachedObject<T>(int offset, int lookaheadCount, Predicate<T> conditon) where T: AXmlObject, new()
		{
			TextSegment obj = segments.FindFirstSegmentWithStartAfter(offset);
			while(obj != null && offset <= obj.StartOffset && obj.StartOffset <= offset + lookaheadCount) {
				if (obj is T && ((AXmlObject)obj).IsCached && conditon((T)obj)) {
					return (T)obj;
				}
				obj = segments.GetNextSegment(obj);
			}
			return null;
		}
	}
}
