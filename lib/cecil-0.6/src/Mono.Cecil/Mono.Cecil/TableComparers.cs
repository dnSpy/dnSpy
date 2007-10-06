//
// TableComparers.cs
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

	class TableComparers {

		public sealed class TypeDef : IComparer {

			public static readonly TypeDef Instance = new TypeDef ();

			public int Compare (object x, object y)
			{
				TypeDefinition a = x as TypeDefinition;
				TypeDefinition b = y as TypeDefinition;

				if (a == null || b == null)
					throw new ReflectionException ("TypeDefComparer can only compare TypeDefinition");

				if (a.Name == Constants.ModuleType && b.Name == Constants.ModuleType)
					return 0;
				else if (a.Name == Constants.ModuleType)
					return -1;
				else if (b.Name == Constants.ModuleType)
					return 1;

				return Comparer.Default.Compare (a.FullName, b.FullName);
			}
		}

		public class TypeRef : IComparer {

			public static readonly TypeRef Instance = new TypeRef ();

			public int Compare (object x, object y)
			{
				TypeReference a = x as TypeReference;
				TypeReference b = y as TypeReference;

				if (a == null || b == null)
					throw new ReflectionException ("TypeRefComparer can only compare TypeReference");

				if (b.DeclaringType == a)
					return -1;
				else if (a.DeclaringType == b)
					return 1;

				return Comparer.Default.Compare (a.FullName, b.FullName);
			}
		}

		public class NestedClass : IComparer {

			public static readonly NestedClass Instance = new NestedClass ();

			public int Compare (object x, object y)
			{
				NestedClassRow a = x as NestedClassRow;
				NestedClassRow b = y as NestedClassRow;

				return Comparer.Default.Compare (a.NestedClass, b.NestedClass);
			}
		}

		public class Constant : IComparer {

			public static readonly Constant Instance = new Constant ();

			public int Compare (object x, object y)
			{
				ConstantRow a = x as ConstantRow;
				ConstantRow b = y as ConstantRow;

				return Comparer.Default.Compare (
					Utilities.CompressMetadataToken (CodedIndex.HasConstant, a.Parent),
					Utilities.CompressMetadataToken (CodedIndex.HasConstant, b.Parent));
			}

		}

		public class InterfaceImpl : IComparer {

			public static readonly InterfaceImpl Instance = new InterfaceImpl ();

			public int Compare (object x, object y)
			{
				InterfaceImplRow a = x as InterfaceImplRow;
				InterfaceImplRow b = y as InterfaceImplRow;

				int klass = Comparer.Default.Compare (a.Class, b.Class);

				if (klass == 0)
					return Comparer.Default.Compare (
						Utilities.CompressMetadataToken (CodedIndex.TypeDefOrRef, a.Interface),
						Utilities.CompressMetadataToken (CodedIndex.TypeDefOrRef, b.Interface));

				return klass;
			}
		}

		public class MethodSem : IComparer {

			public static readonly MethodSem Instance = new MethodSem ();

			public int Compare (object x, object y)
			{
				MethodSemanticsRow a = x as MethodSemanticsRow;
				MethodSemanticsRow b = y as MethodSemanticsRow;

				return Comparer.Default.Compare (
					Utilities.CompressMetadataToken (CodedIndex.HasSemantics, a.Association),
					Utilities.CompressMetadataToken (CodedIndex.HasSemantics, b.Association));
			}
		}

		public class CustomAttribute : IComparer {

			public static readonly CustomAttribute Instance = new CustomAttribute ();

			public int Compare (object x, object y)
			{
				CustomAttributeRow a = x as CustomAttributeRow;
				CustomAttributeRow b = y as CustomAttributeRow;

				return Comparer.Default.Compare (
					Utilities.CompressMetadataToken (CodedIndex.HasCustomAttribute, a.Parent),
					Utilities.CompressMetadataToken (CodedIndex.HasCustomAttribute, b.Parent));
			}
		}

		public class SecurityDeclaration : IComparer {

			public static readonly SecurityDeclaration Instance = new SecurityDeclaration ();

			public int Compare (object x, object y)
			{
				DeclSecurityRow a = x as DeclSecurityRow;
				DeclSecurityRow b = y as DeclSecurityRow;

				return Comparer.Default.Compare (
					Utilities.CompressMetadataToken (CodedIndex.HasDeclSecurity, a.Parent),
					Utilities.CompressMetadataToken (CodedIndex.HasDeclSecurity, b.Parent));
			}
		}

		public class Override : IComparer {

			public static readonly Override Instance = new Override ();

			public int Compare (object x, object y)
			{
				MethodImplRow a = x as MethodImplRow;
				MethodImplRow b = y as MethodImplRow;

				return Comparer.Default.Compare (a.Class, b.Class);
			}
		}

		public class PInvoke : IComparer {

			public static readonly PInvoke Instance = new PInvoke ();

			public int Compare (object x, object y)
			{
				ImplMapRow a = x as ImplMapRow;
				ImplMapRow b = y as ImplMapRow;

				return Comparer.Default.Compare (a.MemberForwarded.RID, b.MemberForwarded.RID);
			}
		}

		public class FieldRVA : IComparer {

			public static readonly FieldRVA Instance = new FieldRVA ();

			public int Compare (object x, object y)
			{
				FieldRVARow a = x as FieldRVARow;
				FieldRVARow b = y as FieldRVARow;

				return Comparer.Default.Compare (a.Field, b.Field);
			}
		}

		public class FieldLayout : IComparer {

			public static readonly FieldLayout Instance = new FieldLayout ();

			public int Compare (object x, object y)
			{
				FieldLayoutRow a = x as FieldLayoutRow;
				FieldLayoutRow b = y as FieldLayoutRow;

				return Comparer.Default.Compare (a.Field, b.Field);
			}
		}

		public class FieldMarshal : IComparer {

			public static readonly FieldMarshal Instance = new FieldMarshal ();

			public int Compare (object x, object y)
			{
				FieldMarshalRow a = x as FieldMarshalRow;
				FieldMarshalRow b = y as FieldMarshalRow;

				return Comparer.Default.Compare (
					Utilities.CompressMetadataToken (CodedIndex.HasFieldMarshal, a.Parent),
					Utilities.CompressMetadataToken (CodedIndex.HasFieldMarshal, b.Parent));
			}
		}

		public class TypeLayout : IComparer {

			public static readonly TypeLayout Instance = new TypeLayout ();

			public int Compare (object x, object y)
			{
				ClassLayoutRow a = x as ClassLayoutRow;
				ClassLayoutRow b = y as ClassLayoutRow;

				return Comparer.Default.Compare (a.Parent, b.Parent);
			}
		}

		public class GenericParam : IComparer {

			public static readonly GenericParam Instance = new GenericParam ();

			public int Compare (object x, object y)
			{
				GenericParameter a = x as GenericParameter;
				GenericParameter b = y as GenericParameter;

				int token = Comparer.Default.Compare (
					Utilities.CompressMetadataToken (CodedIndex.TypeOrMethodDef, a.Owner.MetadataToken),
					Utilities.CompressMetadataToken (CodedIndex.TypeOrMethodDef, b.Owner.MetadataToken));

				if (token == 0)
					return Comparer.Default.Compare (a.Position, b.Position);

				return token;
			}
		}
	}
}
