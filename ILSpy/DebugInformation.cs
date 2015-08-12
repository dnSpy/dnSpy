// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.IO;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger {
	/// <summary>
	/// Contains the data important for debugger from the main application.
	/// </summary>
	public static class DebugInformation
	{
		/// <summary>
		/// Gets or sets the current method key, IL offset and member reference. Used for step in/out.
		/// </summary>
		public static Tuple<MethodKey, int, IMemberRef> DebugStepInformation { get; set; }

		/// <summary>
		/// true if we must call JumpToReference() due to new stack frame
		/// </summary>
		public static bool MustJumpToReference { get; set; }
	}

	public struct MethodKey : IEquatable<MethodKey>
	{
		readonly int token;
		readonly string moduleFullPath;

		public string ModuleFullPath {
			get { return moduleFullPath; }
		}

		public int Token
		{
			get { return token; }
		}

		public MethodKey(int token, string moduleFullPath)
		{
			this.token = token;
			this.moduleFullPath = moduleFullPath;
		}

		public static MethodKey? Create(IMemberRef member)
		{
			if (member == null)
				return null;
			return Create(member.MDToken.ToInt32(), member.Module);
		}

		public static MethodKey? Create(int token, IOwnerModule ownerModule)
		{
			if (ownerModule == null)
				return null;
			return Create(token, ownerModule.Module);
		}

		public static MethodKey? Create(int token, ModuleDef module)
		{
			if (module == null || string.IsNullOrEmpty(module.Location))
				return null;
			return new MethodKey(token, module);
		}

		MethodKey(int token, ModuleDef module)
		{
			this.token = token;
			if (string.IsNullOrEmpty(module.Location))
				throw new ArgumentException("Module has no path");
			this.moduleFullPath = Path.GetFullPath(module.Location);
		}

		public static bool operator ==(MethodKey a, MethodKey b)
		{
			return a.Equals(b);
		}

		public static bool operator !=(MethodKey a, MethodKey b)
		{
			return !a.Equals(b);
		}

		public bool Equals(MethodKey other)
		{
			if (token != other.token)
				return false;
			if (moduleFullPath == other.moduleFullPath)
				return true;
			if (moduleFullPath == null || other.moduleFullPath == null)
				return false;
			return moduleFullPath.Equals(other.moduleFullPath, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			if (!(obj is MethodKey))
				return false;
			return Equals((MethodKey)obj);
		}

		public override int GetHashCode()
		{
			return token ^ (moduleFullPath == null ? 0 : StringComparer.OrdinalIgnoreCase.GetHashCode(moduleFullPath));
		}

		public override string ToString()
		{
			return string.Format("{0:X8} {1}", token, moduleFullPath);
		}

		public bool IsSameModule(string moduleFullPath)
		{
			if (this.moduleFullPath == moduleFullPath)
				return true;
			if (this.moduleFullPath == null)
				return false;
			return this.moduleFullPath.Equals(moduleFullPath, StringComparison.OrdinalIgnoreCase);
		}
	}
}
