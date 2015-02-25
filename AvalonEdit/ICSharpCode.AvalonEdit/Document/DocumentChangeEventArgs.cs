// Copyright (c) 2014 AlphaSierraPapa for the SharpDevelop Team
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
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.NRefactory.Editor;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// Describes a change of the document text.
	/// This class is thread-safe.
	/// </summary>
	[Serializable]
	public class DocumentChangeEventArgs : TextChangeEventArgs
	{
		volatile OffsetChangeMap offsetChangeMap;
		
		/// <summary>
		/// Gets the OffsetChangeMap associated with this document change.
		/// </summary>
		/// <remarks>The OffsetChangeMap instance is guaranteed to be frozen and thus thread-safe.</remarks>
		public OffsetChangeMap OffsetChangeMap {
			get {
				OffsetChangeMap map = offsetChangeMap;
				if (map == null) {
					// create OffsetChangeMap on demand
					map = OffsetChangeMap.FromSingleElement(CreateSingleChangeMapEntry());
					offsetChangeMap = map;
				}
				return map;
			}
		}
		
		internal OffsetChangeMapEntry CreateSingleChangeMapEntry()
		{
			return new OffsetChangeMapEntry(this.Offset, this.RemovalLength, this.InsertionLength);
		}
		
		/// <summary>
		/// Gets the OffsetChangeMap, or null if the default offset map (=single replacement) is being used.
		/// </summary>
		internal OffsetChangeMap OffsetChangeMapOrNull {
			get {
				return offsetChangeMap;
			}
		}
		
		/// <summary>
		/// Gets the new offset where the specified offset moves after this document change.
		/// </summary>
		public override int GetNewOffset(int offset, AnchorMovementType movementType = AnchorMovementType.Default)
		{
			if (offsetChangeMap != null)
				return offsetChangeMap.GetNewOffset(offset, movementType);
			else
				return CreateSingleChangeMapEntry().GetNewOffset(offset, movementType);
		}
		
		/// <summary>
		/// Creates a new DocumentChangeEventArgs object.
		/// </summary>
		public DocumentChangeEventArgs(int offset, string removedText, string insertedText)
			: this(offset, removedText, insertedText, null)
		{
		}
		
		/// <summary>
		/// Creates a new DocumentChangeEventArgs object.
		/// </summary>
		public DocumentChangeEventArgs(int offset, string removedText, string insertedText, OffsetChangeMap offsetChangeMap)
			: base(offset, removedText, insertedText)
		{
			SetOffsetChangeMap(offsetChangeMap);
		}
		
		/// <summary>
		/// Creates a new DocumentChangeEventArgs object.
		/// </summary>
		public DocumentChangeEventArgs(int offset, ITextSource removedText, ITextSource insertedText, OffsetChangeMap offsetChangeMap)
			: base(offset, removedText, insertedText)
		{
			SetOffsetChangeMap(offsetChangeMap);
		}
		
		void SetOffsetChangeMap(OffsetChangeMap offsetChangeMap)
		{
			if (offsetChangeMap != null) {
				if (!offsetChangeMap.IsFrozen)
					throw new ArgumentException("The OffsetChangeMap must be frozen before it can be used in DocumentChangeEventArgs");
				if (!offsetChangeMap.IsValidForDocumentChange(this.Offset, this.RemovalLength, this.InsertionLength))
					throw new ArgumentException("OffsetChangeMap is not valid for this document change", "offsetChangeMap");
				this.offsetChangeMap = offsetChangeMap;
			}
		}
		
		/// <inheritdoc/>
		public override TextChangeEventArgs Invert()
		{
			OffsetChangeMap map = this.OffsetChangeMapOrNull;
			if (map != null) {
				map = map.Invert();
				map.Freeze();
			}
			return new DocumentChangeEventArgs(this.Offset, this.InsertedText, this.RemovedText, map);
		}
	}
}
