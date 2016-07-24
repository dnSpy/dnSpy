/*
    Copyright (C) 2014-2016 de4dot@gmail.com

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
using System.Collections.Generic;
using dnlib.DotNet;
using dnSpy.Contracts.Files.Tabs.DocViewer;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Code mappings
	/// </summary>
	public interface IMethodDebugService {
		/// <summary>
		/// Gets <see cref="MethodSourceStatement"/>s
		/// </summary>
		/// <param name="textPosition">Text position</param>
		/// <returns></returns>
		IList<MethodSourceStatement> FindByTextPosition(int textPosition);

		/// <summary>
		/// Gets a code <see cref="MethodSourceStatement"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="codeOffset">Code offset</param>
		/// <returns></returns>
		MethodSourceStatement? FindByCodeOffset(MethodDef method, uint codeOffset);
	}

	/// <summary>
	/// Constants
	/// </summary>
	internal static class MethodDebugServiceConstants {
		/// <summary>
		/// Code mappings key
		/// </summary>
		public static readonly object MethodDebugServiceKey = new object();
	}

	/// <summary>
	/// Extension methods
	/// </summary>
	public static class MethodDebugServiceExtensions {
		/// <summary>
		/// Gets a <see cref="IMethodDebugService"/> instance
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IMethodDebugService GetMethodDebugService(this IDocumentViewer self) => self.TryGetMethodDebugService() ?? EmptyMethodDebugService.Instance;

		/// <summary>
		/// Gets a <see cref="IMethodDebugService"/> or null if none exists
		/// </summary>
		/// <param name="self">This</param>
		/// <returns></returns>
		public static IMethodDebugService TryGetMethodDebugService(this IDocumentViewer self) {
			if (self == null)
				return null;
			return (IMethodDebugService)self.GetContentData(MethodDebugServiceConstants.MethodDebugServiceKey);
		}

		sealed class EmptyMethodDebugService : IMethodDebugService {
			public static readonly EmptyMethodDebugService Instance = new EmptyMethodDebugService();

			IList<MethodSourceStatement> IMethodDebugService.FindByTextPosition(int textPosition) => Array.Empty<MethodSourceStatement>();
			MethodSourceStatement? IMethodDebugService.FindByCodeOffset(MethodDef method, uint ilOffset) => null;
		}
	}
}
