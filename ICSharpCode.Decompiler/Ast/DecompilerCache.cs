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

using ICSharpCode.Decompiler.Ast.Cache;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.Decompiler.ILAst;

namespace ICSharpCode.Decompiler.Ast {
	/// <summary>
	/// Shared by all code in the current decompiler thread. It must only be accessed from the
	/// owning thread.
	/// </summary>
	public sealed class DecompilerCache {
		readonly ObjectPool<IAstTransformPoolObject[]> pipelinePool;
		readonly ObjectPool<ILAstBuilder> ilAstBuilderPool;
		readonly ObjectPool<ILAstOptimizer> ilAstOptimizerPool;
		readonly ObjectPool<GotoRemoval> gotoRemovalPool;
		readonly ObjectPool<AstMethodBodyBuilder> astMethodBodyBuilderPool;

		public DecompilerCache(DecompilerContext ctx) {
			this.pipelinePool = new ObjectPool<IAstTransformPoolObject[]>(() => TransformationPipeline.CreatePipeline(ctx), null);
			this.ilAstBuilderPool = new ObjectPool<ILAstBuilder>(() => new ILAstBuilder(), a => a.Reset());
			this.ilAstOptimizerPool = new ObjectPool<ILAstOptimizer>(() => new ILAstOptimizer(), a => a.Reset());
			this.gotoRemovalPool = new ObjectPool<GotoRemoval>(() => new GotoRemoval(ctx), a => a.Reset());
			this.astMethodBodyBuilderPool = new ObjectPool<AstMethodBodyBuilder>(() => new AstMethodBodyBuilder(), a => a.Reset());
		}

		public void Reset() {
			pipelinePool.ReuseAllObjects();
			ilAstBuilderPool.ReuseAllObjects();
			ilAstOptimizerPool.ReuseAllObjects();
			gotoRemovalPool.ReuseAllObjects();
			astMethodBodyBuilderPool.ReuseAllObjects();
		}

		public IAstTransformPoolObject[] GetPipelinePool() {
			return pipelinePool.Allocate();
		}

		public void Return(IAstTransformPoolObject[] pipeline) {
			pipelinePool.Free(pipeline);
		}

		public ILAstBuilder GetILAstBuilder() {
			return ilAstBuilderPool.Allocate();
		}

		public void Return(ILAstBuilder builder) {
			ilAstBuilderPool.Free(builder);
		}

		public ILAstOptimizer GetILAstOptimizer() {
			return ilAstOptimizerPool.Allocate();
		}

		public void Return(ILAstOptimizer opt) {
			ilAstOptimizerPool.Free(opt);
		}

		public GotoRemoval GetGotoRemoval() {
			return gotoRemovalPool.Allocate();
		}

		public void Return(GotoRemoval gr) {
			gotoRemovalPool.Free(gr);
		}

		public AstMethodBodyBuilder GetAstMethodBodyBuilder() {
			return astMethodBodyBuilderPool.Allocate();
		}

		public void Return(AstMethodBodyBuilder builder) {
			astMethodBodyBuilderPool.Free(builder);
		}
	}
}
