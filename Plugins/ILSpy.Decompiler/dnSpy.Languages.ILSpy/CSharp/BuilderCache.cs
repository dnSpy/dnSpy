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
using System.Text;
using dnSpy.Contracts.Languages;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;

namespace dnSpy.Languages.ILSpy.CSharp {
	/// <summary>
	/// State for one decompiler thread. There should be at most one of these per CPU. This class
	/// is not thread safe and must only be accessed by the owner thread.
	/// </summary>
	sealed class AstBuilderState {
		public readonly AstBuilder AstBuilder;

		/// <summary>
		/// <see cref="StringBuilder"/> instance used by XML doc code. This is always in a random
		/// state (random text) and caller must Clear() it before use.
		/// </summary>
		public readonly StringBuilder XmlDoc_StringBuilder;

		public AstBuilderState() {
			this.AstBuilder = new AstBuilder(new DecompilerContext(null, true));
			this.XmlDoc_StringBuilder = new StringBuilder();
		}

		/// <summary>
		/// Called to re-use this instance for another decompilation. Only the fields that need
		/// resetting will be reset.
		/// </summary>
		public void Reset() {
			AstBuilder.Reset();
		}
	}

	/// <summary>
	/// One instance is created and stored in <see cref="DecompilationContext"/>. It's used by the
	/// decompiler threads to get an <see cref="AstBuilderState"/> instance.
	/// </summary>
	sealed class BuilderCache {
		readonly ThreadSafeObjectPool<AstBuilderState> astBuilderStatePool;

		public BuilderCache() {
			this.astBuilderStatePool = new ThreadSafeObjectPool<AstBuilderState>(Environment.ProcessorCount, createAstBuilderState, resetAstBuilderState);
		}

		static readonly Func<AstBuilderState> createAstBuilderState = () => new AstBuilderState();
		static readonly Action<AstBuilderState> resetAstBuilderState = abs => abs.Reset();

		public AstBuilderState AllocateAstBuilderState() {
			return astBuilderStatePool.Allocate();
		}

		public void Free(AstBuilderState state) {
			astBuilderStatePool.Free(state);
		}
	}
}
