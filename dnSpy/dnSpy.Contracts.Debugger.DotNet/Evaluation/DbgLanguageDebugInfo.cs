/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using dnSpy.Contracts.Debugger.Evaluation;
using dnSpy.Contracts.Decompiler;

namespace dnSpy.Contracts.Debugger.DotNet.Evaluation {
	/// <summary>
	/// Method debug info used by a .NET debugger language. An instance of this class is attached to
	/// a <see cref="DbgEvaluationContext"/>, see <see cref="DbgLanguageDebugInfoExtensions.TryGetLanguageDebugInfo(DbgEvaluationContext)"/>
	/// and <see cref="DbgLanguageDebugInfoExtensions.GetLanguageDebugInfo(DbgEvaluationContext)"/>
	/// </summary>
	public sealed class DbgLanguageDebugInfo {
		/// <summary>
		/// Gets the method debug info
		/// </summary>
		public MethodDebugInfo MethodDebugInfo { get; }

		/// <summary>
		/// Gets the method version number, a 1-based number
		/// </summary>
		public int MethodVersion { get; }

		/// <summary>
		/// Gets the IL offset
		/// </summary>
		public uint ILOffset { get; }

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="methodDebugInfo">Method debug info</param>
		/// <param name="methodVersion">Method version number, a 1-based number</param>
		/// <param name="ilOffset">IL offset</param>
		public DbgLanguageDebugInfo(MethodDebugInfo methodDebugInfo, int methodVersion, uint ilOffset) {
			if (methodVersion < 1)
				throw new ArgumentOutOfRangeException(nameof(methodVersion));
			MethodDebugInfo = methodDebugInfo ?? throw new ArgumentNullException(nameof(methodDebugInfo));
			MethodVersion = methodVersion;
			ILOffset = ilOffset;
		}
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class DbgLanguageDebugInfoExtensions {
		/// <summary>
		/// Gets the debug info or null if there's none
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public static DbgLanguageDebugInfo TryGetLanguageDebugInfo(this DbgEvaluationContext context) {
			if (context.TryGetData<DbgLanguageDebugInfo>(out var info))
				return info;
			return null;
		}

		/// <summary>
		/// Gets the debug info and throws if there is none
		/// </summary>
		/// <param name="context">Context</param>
		/// <returns></returns>
		public static DbgLanguageDebugInfo GetLanguageDebugInfo(this DbgEvaluationContext context) {
			if (context.TryGetData<DbgLanguageDebugInfo>(out var info))
				return info;
			throw new InvalidOperationException();
		}

		/// <summary>
		/// Attaches <paramref name="debugInfo"/> to <paramref name="context"/>
		/// </summary>
		/// <param name="context">Context</param>
		/// <param name="debugInfo">Debug info</param>
		public static void SetLanguageDebugInfo(DbgEvaluationContext context, DbgLanguageDebugInfo debugInfo) =>
			context.GetOrCreateData(() => debugInfo);
	}
}
