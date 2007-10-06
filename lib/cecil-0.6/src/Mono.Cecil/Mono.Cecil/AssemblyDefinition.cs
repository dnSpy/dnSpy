//
// AssemblyDefinition.cs
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

	using System;
	using System.Collections;

	using Mono.Cecil.Metadata;

	public class AssemblyDefinition : ICustomAttributeProvider,
		IHasSecurity, IAnnotationProvider, IReflectionStructureVisitable {

		MetadataToken m_token;
		AssemblyNameDefinition m_asmName;
		ModuleDefinitionCollection m_modules;
		SecurityDeclarationCollection m_secDecls;
		CustomAttributeCollection m_customAttrs;
		MethodDefinition m_ep;
		TargetRuntime m_runtime;
		AssemblyKind m_kind;

		ModuleDefinition m_mainModule;
		StructureReader m_reader;

		IAssemblyResolver m_resolver;
		IDictionary m_annotations;

		public MetadataToken MetadataToken {
			get { return m_token; }
			set { m_token = value; }
		}

		public AssemblyNameDefinition Name {
			get { return m_asmName; }
		}

		public ModuleDefinitionCollection Modules {
			get { return m_modules; }
		}

		public SecurityDeclarationCollection SecurityDeclarations {
			get {
				if (m_secDecls == null)
					m_secDecls = new SecurityDeclarationCollection (this);

				return m_secDecls;
			}
		}

		public CustomAttributeCollection CustomAttributes {
			get {
				if (m_customAttrs == null)
					m_customAttrs = new CustomAttributeCollection (this);

				return m_customAttrs;
			}
		}

		public MethodDefinition EntryPoint {
			get { return m_ep; }
			set { m_ep = value; }
		}

		public TargetRuntime Runtime {
			get { return m_runtime; }
			set { m_runtime = value; }
		}

		public AssemblyKind Kind {
			get { return m_kind; }
			set { m_kind = value; }
		}

		public ModuleDefinition MainModule {
			get {
				if (m_mainModule == null)
					foreach (ModuleDefinition module in m_modules)
						if (module.Main)
							m_mainModule = module;

				return m_mainModule;
			}
		}

		internal StructureReader Reader {
			get { return m_reader; }
		}

		public IAssemblyResolver Resolver {
			get { return m_resolver; }
			set { m_resolver = value; }
		}

		IDictionary IAnnotationProvider.Annotations {
			get {
				if (m_annotations == null)
					m_annotations = new Hashtable ();
				return m_annotations;
			}
		}

		internal AssemblyDefinition (AssemblyNameDefinition name)
		{
			if (name == null)
				throw new ArgumentNullException ("name");

			m_asmName = name;
			m_modules = new ModuleDefinitionCollection (this);
			m_resolver = new DefaultAssemblyResolver ();
		}

		internal AssemblyDefinition (AssemblyNameDefinition name, StructureReader reader) : this (name)
		{
			m_reader = reader;
		}

		public void Accept (IReflectionStructureVisitor visitor)
		{
			visitor.VisitAssemblyDefinition (this);

			m_asmName.Accept (visitor);
			m_modules.Accept (visitor);

			visitor.TerminateAssemblyDefinition (this);
		}

		public override string ToString ()
		{
			return m_asmName.FullName;
		}
	}
}
