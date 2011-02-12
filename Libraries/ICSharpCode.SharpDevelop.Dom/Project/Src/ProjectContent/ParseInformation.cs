// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;

namespace ICSharpCode.SharpDevelop.Dom
{
	/// <summary>
	/// Holds the parse information for a file.
	/// This class is immutable and thread-safe.
	/// </summary>
	public class ParseInformation : Immutable
	{
		ICompilationUnit unit;
		
		/// <summary>
		/// Gets the compilation unit.
		/// This property never returns null.
		/// </summary>
		public ICompilationUnit CompilationUnit {
			get { return unit; }
		}
		
		/// <summary>
		/// Gets the last compilation unit that was valid (=no parse errors).
		/// This property might be null.
		/// </summary>
		[ObsoleteAttribute]
		public ICompilationUnit ValidCompilationUnit { get { return unit; } }
		
		/// <summary>
		/// Gets the last compilation unit that was invalid (=had parse errors).
		/// This property is null if the most recent compilation unit is valid.
		/// </summary>
		[ObsoleteAttribute]
		public ICompilationUnit DirtyCompilationUnit { get { return unit; } }
		
		/// <summary>
		/// Gets the best compilation unit.
		/// This returns the ValidCompilationUnit if one exists, otherwise
		/// the DirtyCompilationUnit.
		/// </summary>
		[ObsoleteAttribute]
		public ICompilationUnit BestCompilationUnit { get { return unit; } }
		
		/// <summary>
		/// Gets the most recent compilation unit. The unit might be valid or invalid.
		/// </summary>
		[ObsoleteAttribute]
		public ICompilationUnit MostRecentCompilationUnit { get { return unit; } }
		
		public ParseInformation(ICompilationUnit unit)
		{
			if (unit == null)
				throw new ArgumentNullException("unit");
			unit.Freeze();
//			if (!unit.IsFrozen)
//				throw new ArgumentException("unit must be frozen for use in ParseInformation");
			this.unit = unit;
		}
	}
}
