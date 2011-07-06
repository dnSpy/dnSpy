// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under MIT license (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.Editor
{
	/// <summary>
	/// An (Offset,Length)-pair.
	/// </summary>
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
	
	/// <summary>
	/// Extension methods for <see cref="ISegment"/>.
	/// </summary>
	public static class ISegmentExtensions
	{
		/// <summary>
		/// Gets whether the segment contains the offset.
		/// </summary>
		/// <returns>
		/// True, if offset is between segment.Start and segment.End (inclusive); otherwise, false.
		/// </returns>
		public static bool Contains (this ISegment segment, int offset)
		{
			return segment.Offset <= offset && offset <= segment.EndOffset;
		}
		
		/// <summary>
		/// True, if the segment contains the specified segment, false otherwise.
		/// </summary>
		public static bool Contains (this ISegment thisSegment, ISegment segment)
		{
			return  segment != null && thisSegment.Offset <= segment.Offset && segment.EndOffset <= thisSegment.EndOffset;
		}
	}
}
