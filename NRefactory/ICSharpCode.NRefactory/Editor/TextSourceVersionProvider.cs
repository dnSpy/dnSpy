// Copyright (c) 2010-2013 AlphaSierraPapa for the SharpDevelop Team
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

namespace ICSharpCode.NRefactory.Editor
{
	/// <summary>
	/// Provides ITextSourceVersion instances.
	/// </summary>
	public class TextSourceVersionProvider
	{
		Version currentVersion;
		
		public TextSourceVersionProvider()
		{
			this.currentVersion = new Version(this);
		}
		
		/// <summary>
		/// Gets the current version.
		/// </summary>
		public ITextSourceVersion CurrentVersion {
			get { return currentVersion; }
		}
		
		/// <summary>
		/// Replaces the current version with a new version.
		/// </summary>
		/// <param name="change">Change from current version to new version</param>
		public void AppendChange(TextChangeEventArgs change)
		{
			if (change == null)
				throw new ArgumentNullException("change");
			currentVersion.change = change;
			currentVersion.next = new Version(currentVersion);
			currentVersion = currentVersion.next;
		}
		
		[DebuggerDisplay("Version #{id}")]
		sealed class Version : ITextSourceVersion
		{
			// Reference back to the provider.
			// Used to determine if two checkpoints belong to the same document.
			readonly TextSourceVersionProvider provider;
			// ID used for CompareAge()
			readonly int id;
			
			// the change from this version to the next version
			internal TextChangeEventArgs change;
			internal Version next;
			
			internal Version(TextSourceVersionProvider provider)
			{
				this.provider = provider;
			}
			
			internal Version(Version prev)
			{
				this.provider = prev.provider;
				this.id = unchecked( prev.id + 1 );
			}
			
			public bool BelongsToSameDocumentAs(ITextSourceVersion other)
			{
				Version o = other as Version;
				return o != null && provider == o.provider;
			}
			
			public int CompareAge(ITextSourceVersion other)
			{
				if (other == null)
					throw new ArgumentNullException("other");
				Version o = other as Version;
				if (o == null || provider != o.provider)
					throw new ArgumentException("Versions do not belong to the same document.");
				// We will allow overflows, but assume that the maximum distance between checkpoints is 2^31-1.
				// This is guaranteed on x86 because so many checkpoints don't fit into memory.
				return Math.Sign(unchecked( this.id - o.id ));
			}
			
			public IEnumerable<TextChangeEventArgs> GetChangesTo(ITextSourceVersion other)
			{
				int result = CompareAge(other);
				Version o = (Version)other;
				if (result < 0)
					return GetForwardChanges(o);
				else if (result > 0)
					return o.GetForwardChanges(this).Reverse().Select(change => change.Invert());
				else
					return EmptyList<TextChangeEventArgs>.Instance;
			}
			
			IEnumerable<TextChangeEventArgs> GetForwardChanges(Version other)
			{
				// Return changes from this(inclusive) to other(exclusive).
				for (Version node = this; node != other; node = node.next) {
					yield return node.change;
				}
			}
			
			public int MoveOffsetTo(ITextSourceVersion other, int oldOffset, AnchorMovementType movement)
			{
				int offset = oldOffset;
				foreach (var e in GetChangesTo(other)) {
					offset = e.GetNewOffset(offset, movement);
				}
				return offset;
			}
		}
	}
}
