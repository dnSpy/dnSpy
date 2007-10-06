//
// BaseStructureVisitor.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2005 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

namespace Mono.Cecil {

	using System.Collections;

	public abstract class BaseStructureVisitor : IReflectionStructureVisitor {

		public virtual void VisitAssemblyDefinition (AssemblyDefinition asm)
		{
		}

		public virtual void VisitAssemblyNameDefinition (AssemblyNameDefinition name)
		{
		}

		public virtual void VisitAssemblyNameReferenceCollection (AssemblyNameReferenceCollection names)
		{
		}

		public virtual void VisitAssemblyNameReference (AssemblyNameReference name)
		{
		}

		public virtual void VisitResourceCollection (ResourceCollection resources)
		{
		}

		public virtual void VisitEmbeddedResource (EmbeddedResource res)
		{
		}

		public virtual void VisitLinkedResource (LinkedResource res)
		{
		}

		public virtual void VisitAssemblyLinkedResource (AssemblyLinkedResource res)
		{
		}

		public virtual void VisitModuleDefinition (ModuleDefinition module)
		{
		}

		public virtual void VisitModuleDefinitionCollection (ModuleDefinitionCollection modules)
		{
		}

		public virtual void VisitModuleReference (ModuleReference module)
		{
		}

		public virtual void VisitModuleReferenceCollection (ModuleReferenceCollection modules)
		{
		}

		public virtual void TerminateAssemblyDefinition (AssemblyDefinition asm)
		{
		}

		protected void VisitCollection (ICollection coll)
		{
			if (coll.Count == 0)
				return;

			foreach (IReflectionStructureVisitable visitable in coll)
				visitable.Accept (this);
		}
	}
}
