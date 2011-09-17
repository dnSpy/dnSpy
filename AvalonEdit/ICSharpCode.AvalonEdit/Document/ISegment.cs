// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Diagnostics;
using ICSharpCode.AvalonEdit.Utils;
using System.Globalization;

namespace ICSharpCode.AvalonEdit.Document
{
	/// <summary>
	/// An (Offset,Length)-pair.
	/// </summary>
	/// <seealso cref="TextSegment"/>
	/// <seealso cref="AnchorSegment"/>
	public interface ISegment
	{
		/// <summary>
		/// Gets the start offset of the segment.
		/// </summary>
		int Offset { get; }
		
		/// <summary>
		/// Gets the length of the segment.
		/// </summary>
		/// <remarks>Must not be negative.</remarks>
		int Length { get; }
		
		/// <summary>
		/// Gets the end offset of the segment.
		/// </summary>
		/// <remarks>EndOffset = Offset + Length;</remarks>
		int EndOffset { get; }
	}
	
	static class SegmentExtensions
	{
		/// <summary>
		/// Gets whether the segment contains the offset.
		/// </summary>
		/// <returns>
		/// True, if offset is between segment.Start and segment.End (inclusive); otherwise, false.
		/// </returns>
		public static bool Contains(this ISegment segment, int offset)
		{
			int start = segment.Offset;
			int end = start + segment.Length;
			return offset >= start && offset <= end;
		}
		
		/// <summary>
		/// Gets the overlapping portion of the segments.
		/// Returns SimpleSegment.Invalid if the segments don't overlap.
		/// </summary>
		public static SimpleSegment GetOverlap(this ISegment segment, ISegment other)
		{
			int start = Math.Max(segment.Offset, other.Offset);
			int end = Math.Min(segment.EndOffset, other.EndOffset);
			if (end < start)
				return SimpleSegment.Invalid;
			else
				return new SimpleSegment(start, end - start);
		}
	}
	
	/// <summary>
	/// Represents a simple segment (Offset,Length pair) that is not automatically updated
	/// on document changes.
	/// </summary>
	struct SimpleSegment : IEquatable<SimpleSegment>, ISegment
	{
		public static readonly SimpleSegment Invalid = new SimpleSegment(-1, -1);
		
		public readonly int Offset, Length;
		
		int ISegment.Offset {
			get { return Offset; }
		}
		
		int ISegment.Length {
			get { return Length; }
		}
		
		public int EndOffset {
			get {
				return Offset + Length;
			}
		}
		
		public SimpleSegment(int offset, int length)
		{
			this.Offset = offset;
			this.Length = length;
		}
		
		public SimpleSegment(ISegment segment)
		{
			Debug.Assert(segment != null);
			this.Offset = segment.Offset;
			this.Length = segment.Length;
		}
		
		public override int GetHashCode()
		{
			unchecked {
				return Offset + 10301 * Length;
			}
		}
		
		public override bool Equals(object obj)
		{
			return (obj is SimpleSegment) && Equals((SimpleSegment)obj);
		}
		
		public bool Equals(SimpleSegment other)
		{
			return this.Offset == other.Offset && this.Length == other.Length;
		}
		
		public static bool operator ==(SimpleSegment left, SimpleSegment right)
		{
			return left.Equals(right);
		}
		
		public static bool operator !=(SimpleSegment left, SimpleSegment right)
		{
			return !left.Equals(right);
		}
		
		public override string ToString()
		{
			return "[Offset=" + Offset.ToString(CultureInfo.InvariantCulture) + ", Length=" + Length.ToString(CultureInfo.InvariantCulture) + "]";
		}
	}
	
	/// <summary>
	/// A segment using <see cref="TextAnchor"/>s as start and end positions.
	/// </summary>
	/// <remarks>
	/// <para>
	/// For the constructors creating new anchors, the start position will be AfterInsertion and the end position will be BeforeInsertion.
	/// Should the end position move before the start position, the segment will have length 0.
	/// </para>
	/// </remarks>
	/// <seealso cref="ISegment"/>
	/// <seealso cref="TextSegment"/>
	public sealed class AnchorSegment : ISegment
	{
		readonly TextAnchor start, end;
		
		/// <inheritdoc/>
		public int Offset {
			get { return start.Offset; }
		}
		
		/// <inheritdoc/>
		public int Length {
			get {
				// Math.Max takes care of the fact that end.Offset might move before start.Offset.
				return Math.Max(0, end.Offset - start.Offset);
			}
		}
		
		/// <inheritdoc/>
		public int EndOffset {
			get {
				// Math.Max takes care of the fact that end.Offset might move before start.Offset.
				return Math.Max(start.Offset, end.Offset);
			}
		}
		
		/// <summary>
		/// Creates a new AnchorSegment using the specified anchors.
		/// The anchors must have <see cref="TextAnchor.SurviveDeletion"/> set to true.
		/// </summary>
		public AnchorSegment(TextAnchor start, TextAnchor end)
		{
			if (start == null)
				throw new ArgumentNullException("start");
			if (end == null)
				throw new ArgumentNullException("end");
			if (!start.SurviveDeletion)
				throw new ArgumentException("Anchors for AnchorSegment must use SurviveDeletion", "start");
			if (!end.SurviveDeletion)
				throw new ArgumentException("Anchors for AnchorSegment must use SurviveDeletion", "end");
			this.start = start;
			this.end = end;
		}
		
		/// <summary>
		/// Creates a new AnchorSegment that creates new anchors.
		/// </summary>
		public AnchorSegment(TextDocument document, ISegment segment)
			: this(document, ThrowUtil.CheckNotNull(segment, "segment").Offset, segment.Length)
		{
		}
		
		/// <summary>
		/// Creates a new AnchorSegment that creates new anchors.
		/// </summary>
		public AnchorSegment(TextDocument document, int offset, int length)
		{
			if (document == null)
				throw new ArgumentNullException("document");
			this.start = document.CreateAnchor(offset);
			this.start.SurviveDeletion = true;
			this.start.MovementType = AnchorMovementType.AfterInsertion;
			this.end = document.CreateAnchor(offset + length);
			this.end.SurviveDeletion = true;
			this.end.MovementType = AnchorMovementType.BeforeInsertion;
		}
		
		/// <inheritdoc/>
		public override string ToString()
		{
			return "[Offset=" + Offset.ToString(CultureInfo.InvariantCulture) + ", EndOffset=" + EndOffset.ToString(CultureInfo.InvariantCulture) + "]";
		}
	}
}
