// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using ICSharpCode.NRefactory;

namespace ICSharpCode.SharpDevelop.Dom
{
	public struct FilePosition : IEquatable<FilePosition>
	{
		string filename;
		Location position;
		ICompilationUnit compilationUnit;
		
		public static readonly FilePosition Empty = new FilePosition(null, Location.Empty);
		
		public FilePosition(ICompilationUnit compilationUnit, int line, int column)
		{
			this.position = new Location(column, line);
			this.compilationUnit = compilationUnit;
			if (compilationUnit != null) {
				this.filename = compilationUnit.FileName;
			} else {
				this.filename = null;
			}
		}
		
		public FilePosition(string filename)
			: this(filename, Location.Empty)
		{
		}
		
		public FilePosition(string filename, int line, int column)
			: this(filename, new Location(column, line))
		{
		}
		
		public FilePosition(string filename, Location position)
		{
			this.compilationUnit = null;
			this.filename = filename;
			this.position = position;
		}
		
		public string FileName {
			get {
				return filename;
			}
		}
		
		public ICompilationUnit CompilationUnit {
			get {
				return compilationUnit;
			}
		}
		
		public Location Position {
			get {
				return position;
			}
		}
		
		public override string ToString()
		{
			return String.Format("{0} : (line {1}, col {2})",
			                     filename,
			                     Line,
			                     Column);
		}
		
		public int Line {
			get {
				return position.Y;
			}
		}
		
		public int Column {
			get {
				return position.X;
			}
		}
		
		public bool IsEmpty {
			get {
				return filename == null;
			}
		}
		
		public override bool Equals(object obj)
		{
			return obj is FilePosition && Equals((FilePosition)obj);
		}
		
		public bool Equals(FilePosition other)
		{
			return this.FileName == other.FileName && this.Position == other.Position;
		}
		
		public override int GetHashCode()
		{
			return filename.GetHashCode() ^ position.GetHashCode();
		}
		
		public static bool operator ==(FilePosition lhs, FilePosition rhs)
		{
			return lhs.Equals(rhs);
		}
		
		public static bool operator !=(FilePosition lhs, FilePosition rhs)
		{
			return !lhs.Equals(rhs);
		}
	}
}
