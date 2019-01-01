/*
    Copyright (C) 2014-2019 de4dot@gmail.com

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

using System.Diagnostics;
using System.Linq;
using dnlib.DotNet;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;

namespace dnSpy.Decompiler.ILSpy.Core.CSharp {
	sealed class AssemblyInfoTransform : IAstTransform {
		public void Run(AstNode compilationUnit) {
			foreach (var attrSect in compilationUnit.Descendants.OfType<AttributeSection>()) {
				var attr = attrSect.Descendants.OfType<Attribute>().FirstOrDefault();
				Debug.Assert(attr != null);
				if (attr == null)
					continue;
				var ca = attr.Annotation<CustomAttribute>();
				if (ca == null)
					continue;
				if (!Compare(ca.AttributeType, systemRuntimeVersioningString, targetFrameworkAttributeString) &&
					!Compare(ca.AttributeType, systemSecurityString, unverifiableCodeAttributeString))
					continue;
				attrSect.Remove();
			}
		}
		static readonly UTF8String systemRuntimeVersioningString = new UTF8String("System.Runtime.Versioning");
		static readonly UTF8String targetFrameworkAttributeString = new UTF8String("TargetFrameworkAttribute");
		static readonly UTF8String systemSecurityString = new UTF8String("System.Security");
		static readonly UTF8String unverifiableCodeAttributeString = new UTF8String("UnverifiableCodeAttribute");

		static bool Compare(ITypeDefOrRef type, UTF8String expNs, UTF8String expName) {
			if (type == null)
				return false;

			if (type is TypeRef tr)
				return tr.Namespace == expNs && tr.Name == expName;
			if (type is TypeDef td)
				return td.Namespace == expNs && td.Name == expName;

			return false;
		}
	}
}
