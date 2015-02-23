// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.ILAst;
using dnlib.DotNet;

namespace ICSharpCode.ILSpy.Debugger
{
	/// <summary>
	/// Contains the data important for debugger from the main application.
	/// </summary>
	public static class DebugInformation
	{
		/// <summary>
		/// Gets or sets the current code mappings.
		/// </summary>
		public static Dictionary<MethodKey, MemberMapping> CodeMappings { get; set; }
		
		/// <summary>
		/// Gets or sets the current method key, IL offset and member reference. Used for step in/out.
		/// </summary>
		public static Tuple<MethodKey, int, IMemberRef> DebugStepInformation { get; set; }
	}

	public sealed class MethodKey : IEquatable<MethodKey>
	{
		readonly int token;
		readonly string moduleFullPath;

		public int Token
		{
			get { return token; }
		}

		public MethodKey(int token, string moduleFullPath)
		{
			this.token = token;
			this.moduleFullPath = moduleFullPath;
		}

		public MethodKey(IMemberRef member)
			: this(member.MDToken.ToInt32(), member.Module)
		{
		}

		public MethodKey(int token, IOwnerModule ownerModule)
			: this(token, ownerModule.Module)
		{
		}

		public MethodKey(int token, ModuleDef module)
		{
			this.token = token;
			if (string.IsNullOrEmpty(module.Location))
				throw new ArgumentException("Module has no path");
			this.moduleFullPath = Path.GetFullPath(module.Location);
		}

		public bool Equals(MethodKey other)
		{
			if (other == null)
				return false;
			return token == other.token &&
				moduleFullPath.Equals(other.moduleFullPath, StringComparison.OrdinalIgnoreCase);
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as MethodKey);
		}

		public override int GetHashCode()
		{
			return token ^ moduleFullPath.GetHashCode();
		}

		public override string ToString()
		{
			return string.Format("{0:X8} {1}", token, moduleFullPath);
		}

		public bool IsSameModule(string moduleFullPath)
		{
			return this.moduleFullPath.Equals(moduleFullPath, StringComparison.OrdinalIgnoreCase);
		}
	}
}
