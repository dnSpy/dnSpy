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
using dnSpy.Contracts.Metadata;
using Microsoft.VisualStudio.Text;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Method debug info service
	/// </summary>
	public interface IMethodDebugService {
		/// <summary>
		/// Gets the number of <see cref="MethodDebugInfo"/>s
		/// </summary>
		int Count { get; }

		/// <summary>
		/// Gets <see cref="MethodSourceStatement"/>s
		/// </summary>
		/// <param name="textPosition">Text position</param>
		/// <param name="sameMethod">true to only return statements within the method that contains <paramref name="textPosition"/></param>
		/// <returns></returns>
		IList<MethodSourceStatement> FindByTextPosition(int textPosition, bool sameMethod);

		/// <summary>
		/// Gets a code <see cref="MethodSourceStatement"/>
		/// </summary>
		/// <param name="method">Method</param>
		/// <param name="codeOffset">Code offset</param>
		/// <returns></returns>
		MethodSourceStatement? FindByCodeOffset(MethodDef method, uint codeOffset);

		/// <summary>
		/// Gets a code <see cref="MethodSourceStatement"/>
		/// </summary>
		/// <param name="token">Token</param>
		/// <param name="codeOffset">Code offset</param>
		/// <returns></returns>
		MethodSourceStatement? FindByCodeOffset(ModuleTokenId token, uint codeOffset);

		/// <summary>
		/// Gets a <see cref="MethodDebugInfo"/> or null if it doesn't exist
		/// </summary>
		/// <param name="method">Method</param>
		/// <returns></returns>
		MethodDebugInfo TryGetMethodDebugInfo(MethodDef method);

		/// <summary>
		/// Gets a <see cref="MethodDebugInfo"/> or null if it doesn't exist
		/// </summary>
		/// <param name="token">Token</param>
		/// <returns></returns>
		MethodDebugInfo TryGetMethodDebugInfo(ModuleTokenId token);

		/// <summary>
		/// Gets all <see cref="MethodSourceStatement"/>s that intersect a span
		/// </summary>
		/// <param name="span">Span</param>
		/// <returns></returns>
		IEnumerable<MethodSourceStatement> GetStatementsByTextSpan(Span span);
	}

	/// <summary>
	/// Constants
	/// </summary>
	internal static class MethodDebugServiceConstants {
		/// <summary>
		/// <see cref="IMethodDebugService"/> key
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

			int IMethodDebugService.Count => 0;
			IList<MethodSourceStatement> IMethodDebugService.FindByTextPosition(int textPosition, bool sameMethod) => Array.Empty<MethodSourceStatement>();
			MethodSourceStatement? IMethodDebugService.FindByCodeOffset(ModuleTokenId token, uint codeOffset) => null;
			MethodSourceStatement? IMethodDebugService.FindByCodeOffset(MethodDef method, uint codeOffset) => null;
			MethodDebugInfo IMethodDebugService.TryGetMethodDebugInfo(ModuleTokenId token) => null;
			MethodDebugInfo IMethodDebugService.TryGetMethodDebugInfo(MethodDef method) => null;
			IEnumerable<MethodSourceStatement> IMethodDebugService.GetStatementsByTextSpan(Span span) => Array.Empty<MethodSourceStatement>();
		}
	}
}
