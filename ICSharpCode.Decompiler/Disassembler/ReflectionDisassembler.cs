// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Disassembles type and member definitions.
	/// </summary>
	public sealed class ReflectionDisassembler : ICodeMappings
	{
		ITextOutput output;
		CancellationToken cancellationToken;
		bool isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
		MethodBodyDisassembler methodBodyDisassembler;
		
		public ReflectionDisassembler(ITextOutput output, bool detectControlStructure, CancellationToken cancellationToken)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			this.output = output;
			this.cancellationToken = cancellationToken;
			this.methodBodyDisassembler = new MethodBodyDisassembler(output, detectControlStructure, cancellationToken);
		}
		
		#region Disassemble Method
		EnumNameCollection<MethodAttributes> methodAttributeFlags = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Final, "final" },
			{ MethodAttributes.HideBySig, "hidebysig" },
			{ MethodAttributes.SpecialName, "specialname" },
			{ MethodAttributes.PInvokeImpl, "pinvokeimpl" },
			{ MethodAttributes.UnmanagedExport, "export" },
			{ MethodAttributes.RTSpecialName, "rtspecialname" },
			{ MethodAttributes.RequireSecObject, "requiresecobj" },
			{ MethodAttributes.NewSlot, "newslot" },
			{ MethodAttributes.Virtual, "virtual" },
			{ MethodAttributes.Abstract, "abstract" },
			{ MethodAttributes.Static, "static" }
		};
		
		EnumNameCollection<MethodAttributes> methodVisibility = new EnumNameCollection<MethodAttributes>() {
			{ MethodAttributes.Private, "private" },
			{ MethodAttributes.FamANDAssem, "famandassem" },
			{ MethodAttributes.Assembly, "assembly" },
			{ MethodAttributes.Family, "family" },
			{ MethodAttributes.FamORAssem, "famorassem" },
			{ MethodAttributes.Public, "public" },
		};
		
		EnumNameCollection<MethodCallingConvention> callingConvention = new EnumNameCollection<MethodCallingConvention>() {
			{ MethodCallingConvention.C, "unmanaged cdecl" },
			{ MethodCallingConvention.StdCall, "unmanaged stdcall" },
			{ MethodCallingConvention.ThisCall, "unmanaged thiscall" },
			{ MethodCallingConvention.FastCall, "unmanaged fastcall" },
			{ MethodCallingConvention.VarArg, "vararg" },
			{ MethodCallingConvention.Generic, "generic" },
		};
		
		EnumNameCollection<MethodImplAttributes> methodCodeType = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.IL, "cil" },
			{ MethodImplAttributes.Native, "native" },
			{ MethodImplAttributes.OPTIL, "optil" },
			{ MethodImplAttributes.Runtime, "runtime" },
		};
		
		EnumNameCollection<MethodImplAttributes> methodImpl = new EnumNameCollection<MethodImplAttributes>() {
			{ MethodImplAttributes.Synchronized, "synchronized" },
			{ MethodImplAttributes.NoInlining, "noinlining" },
			{ MethodImplAttributes.NoOptimization, "nooptimization" },
		};
		
		public void DisassembleMethod(MethodDefinition method)
		{
			// write method header
			output.WriteDefinition(".method ", method);
			DisassembleMethodInternal(method);
		}
		
		void DisassembleMethodInternal(MethodDefinition method)
		{
			//    .method public hidebysig  specialname
			//               instance default class [mscorlib]System.IO.TextWriter get_BaseWriter ()  cil managed
			//
			
			//emit flags
			WriteEnum(method.Attributes & MethodAttributes.MemberAccessMask, methodVisibility);
			WriteFlags(method.Attributes & ~MethodAttributes.MemberAccessMask, methodAttributeFlags);
			
			output.WriteLine();
			output.Indent();
			
			if (method.HasThis)
				output.Write("instance ");
			
			//call convention
			WriteEnum(method.CallingConvention & (MethodCallingConvention)0x1f, callingConvention);
			
			
			//return type
			method.ReturnType.WriteTo(output);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(method.Name));
			WriteTypeParameters(output, method);
			
			//( params )
			output.Write(" (");
			if (method.HasParameters) {
				output.WriteLine();
				output.Indent();
				WriteParameters(method.Parameters);
				output.Unindent();
			}
			output.Write(") ");
			//cil managed
			WriteEnum(method.ImplAttributes & MethodImplAttributes.CodeTypeMask, methodCodeType);
			if ((method.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed)
				output.Write("managed ");
			else
				output.Write("unmanaged ");
			WriteFlags(method.ImplAttributes & ~(MethodImplAttributes.CodeTypeMask | MethodImplAttributes.ManagedMask), methodImpl);
			
			output.Unindent();
			if (method.HasBody || method.HasCustomAttributes || method.Parameters.Any(HasParameterAttributes)) {
				OpenBlock(defaultCollapsed: isInType);
				WriteAttributes(method.CustomAttributes);
				foreach (var p in method.Parameters) {
					WriteParameterAttributes(p);
				}
				
				if (method.HasBody) {
					// create IL code mappings - used in debugger
					MemberMapping methodMapping = method.CreateCodeMapping(this.CodeMappings);
					methodBodyDisassembler.Disassemble(method.Body, methodMapping);
				}
				
				CloseBlock("end of method " + method.DeclaringType.Name + "::" + method.Name);
			} else {
				output.WriteLine();
			}
		}
		
		void WriteParameters(Collection<ParameterDefinition> parameters)
		{
			for (int i = 0; i < parameters.Count; i++) {
				var p = parameters[i];
				if (p.IsIn)
					output.Write("[in] ");
				if (p.IsOut)
					output.Write("[out] ");
				if (p.IsOptional)
					output.Write("[opt] ");
				p.ParameterType.WriteTo(output);
				output.Write(' ');
				output.WriteDefinition(DisassemblerHelpers.Escape(p.Name), p);
				if (i < parameters.Count - 1)
					output.Write(',');
				output.WriteLine();
			}
		}
		
		bool HasParameterAttributes(ParameterDefinition p)
		{
			return p.HasConstant || p.HasCustomAttributes;
		}
		
		void WriteParameterAttributes(ParameterDefinition p)
		{
			if (!HasParameterAttributes(p))
				return;
			output.Write(".param [{0}]", p.Index);
			if (p.HasConstant) {
				output.Write(" = ");
				if (p.Constant != null)
					DisassemblerHelpers.WriteOperand(output, p.Constant);
				else
					output.Write("nullref");
			}
			output.WriteLine();
			WriteAttributes(p.CustomAttributes);
		}
		#endregion
		
		#region Disassemble Field
		EnumNameCollection<FieldAttributes> fieldVisibility = new EnumNameCollection<FieldAttributes>() {
			{ FieldAttributes.Private, "private" },
			{ FieldAttributes.FamANDAssem, "famandassem" },
			{ FieldAttributes.Assembly, "assembly" },
			{ FieldAttributes.Family, "family" },
			{ FieldAttributes.FamORAssem, "famorassem" },
			{ FieldAttributes.Public, "public" },
		};
		
		EnumNameCollection<FieldAttributes> fieldAttributes = new EnumNameCollection<FieldAttributes>() {
			{ FieldAttributes.Static, "static" },
			{ FieldAttributes.Literal, "literal" },
			{ FieldAttributes.InitOnly, "initonly" },
			{ FieldAttributes.SpecialName, "specialname" },
			{ FieldAttributes.RTSpecialName, "rtspecialname" },
			{ FieldAttributes.NotSerialized, "notserialized" },
		};
		
		public void DisassembleField(FieldDefinition field)
		{
			output.WriteDefinition(".field ", field);
			WriteEnum(field.Attributes & FieldAttributes.FieldAccessMask, fieldVisibility);
			WriteFlags(field.Attributes & ~(FieldAttributes.FieldAccessMask | FieldAttributes.HasDefault), fieldAttributes);
			field.FieldType.WriteTo(output);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(field.Name));
			if (field.HasConstant) {
				output.Write(" = ");
				DisassemblerHelpers.WriteOperand(output, field.Constant);
			}
			output.WriteLine();
			if (field.HasCustomAttributes) {
				output.MarkFoldStart();
				WriteAttributes(field.CustomAttributes);
				output.MarkFoldEnd();
			}
		}
		#endregion
		
		#region Disassemble Property
		EnumNameCollection<PropertyAttributes> propertyAttributes = new EnumNameCollection<PropertyAttributes>() {
			{ PropertyAttributes.SpecialName, "specialname" },
			{ PropertyAttributes.RTSpecialName, "rtspecialname" },
			{ PropertyAttributes.HasDefault, "hasdefault" },
		};
		
		public void DisassembleProperty(PropertyDefinition property)
		{
			output.WriteDefinition(".property ", property);
			WriteFlags(property.Attributes, propertyAttributes);
			if (property.HasThis)
				output.Write("instance ");
			property.PropertyType.WriteTo(output);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(property.Name));
			
			output.Write("(");
			if (property.HasParameters) {
				output.WriteLine();
				output.Indent();
				WriteParameters(property.Parameters);
				output.Unindent();
			}
			output.Write(")");
			
			OpenBlock(false);
			WriteAttributes(property.CustomAttributes);
			WriteNestedMethod(".get", property.GetMethod);
			WriteNestedMethod(".set", property.SetMethod);
			foreach (var method in property.OtherMethods) {
				WriteNestedMethod(".method", method);
			}
			CloseBlock();
		}
		
		void WriteNestedMethod(string keyword, MethodDefinition method)
		{
			if (method == null)
				return;
			output.Write(keyword);
			output.Write(' ');
			method.WriteTo(output);
			output.WriteLine();
		}
		#endregion
		
		#region Disassemble Event
		EnumNameCollection<EventAttributes> eventAttributes = new EnumNameCollection<EventAttributes>() {
			{ EventAttributes.SpecialName, "specialname" },
			{ EventAttributes.RTSpecialName, "rtspecialname" },
		};
		
		public void DisassembleEvent(EventDefinition ev)
		{
			output.WriteDefinition(".event ", ev);
			WriteFlags(ev.Attributes, eventAttributes);
			ev.EventType.WriteTo(output);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(ev.Name));
			OpenBlock(false);
			WriteAttributes(ev.CustomAttributes);
			WriteNestedMethod(".add", ev.AddMethod);
			WriteNestedMethod(".remove", ev.RemoveMethod);
			WriteNestedMethod(".invoke", ev.InvokeMethod);
			foreach (var method in ev.OtherMethods) {
				WriteNestedMethod(".method", method);
			}
			CloseBlock();
		}
		#endregion
		
		#region Disassemble Type
		EnumNameCollection<TypeAttributes> typeVisibility = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.Public, "public" },
			{ TypeAttributes.NotPublic, "private" },
			{ TypeAttributes.NestedPublic, "nested public" },
			{ TypeAttributes.NestedPrivate, "nested private" },
			{ TypeAttributes.NestedAssembly, "nested assembly" },
			{ TypeAttributes.NestedFamily, "nested family" },
			{ TypeAttributes.NestedFamANDAssem, "nested famandassem" },
			{ TypeAttributes.NestedFamORAssem, "nested famorassem" },
		};
		
		EnumNameCollection<TypeAttributes> typeLayout = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.AutoLayout, "auto" },
			{ TypeAttributes.SequentialLayout, "sequential" },
			{ TypeAttributes.ExplicitLayout, "explicit" },
		};
		
		EnumNameCollection<TypeAttributes> typeStringFormat = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.AutoClass, "auto" },
			{ TypeAttributes.AnsiClass, "ansi" },
			{ TypeAttributes.UnicodeClass, "unicode" },
		};
		
		EnumNameCollection<TypeAttributes> typeAttributes = new EnumNameCollection<TypeAttributes>() {
			{ TypeAttributes.Abstract, "abstract" },
			{ TypeAttributes.Sealed, "sealed" },
			{ TypeAttributes.SpecialName, "specialname" },
			{ TypeAttributes.Import, "import" },
			{ TypeAttributes.Serializable, "serializable" },
			{ TypeAttributes.BeforeFieldInit, "beforefieldinit" },
			{ TypeAttributes.HasSecurity, null },
		};
		
		public void DisassembleType(TypeDefinition type)
		{
			// create IL code mappings - used for debugger
			if (this.CodeMappings == null)
				this.CodeMappings = new Tuple<string, List<MemberMapping>>(type.FullName, new List<MemberMapping>());
			
			// start writing IL
			output.WriteDefinition(".class ", type);
			
			if ((type.Attributes & TypeAttributes.ClassSemanticMask) == TypeAttributes.Interface)
				output.Write("interface ");
			WriteEnum(type.Attributes & TypeAttributes.VisibilityMask, typeVisibility);
			WriteEnum(type.Attributes & TypeAttributes.LayoutMask, typeLayout);
			WriteEnum(type.Attributes & TypeAttributes.StringFormatMask, typeStringFormat);
			const TypeAttributes masks = TypeAttributes.ClassSemanticMask | TypeAttributes.VisibilityMask | TypeAttributes.LayoutMask | TypeAttributes.StringFormatMask;
			WriteFlags(type.Attributes & ~masks, typeAttributes);
			
			output.Write(DisassemblerHelpers.Escape(type.DeclaringType != null ? type.Name : type.FullName));
			WriteTypeParameters(output, type);
			output.MarkFoldStart(defaultCollapsed: isInType);
			output.WriteLine();
			
			if (type.BaseType != null) {
				output.Indent();
				output.Write("extends ");
				type.BaseType.WriteTo(output, ILNameSyntax.TypeName);
				output.WriteLine();
				output.Unindent();
			}
			if (type.HasInterfaces) {
				output.Indent();
				for (int index = 0; index < type.Interfaces.Count; index++) {
					if (index > 0)
						output.WriteLine(",");
					if (index == 0)
						output.Write("implements ");
					else
						output.Write("           ");
					type.Interfaces[index].WriteTo(output, ILNameSyntax.TypeName);
				}
				output.WriteLine();
				output.Unindent();
			}
			
			output.WriteLine("{");
			output.Indent();
			bool oldIsInType = isInType;
			isInType = true;
			WriteAttributes(type.CustomAttributes);
			if (type.HasLayoutInfo) {
				output.WriteLine(".pack {0}", type.PackingSize);
				output.WriteLine(".size {0}", type.ClassSize);
				output.WriteLine();
			}
			if (type.HasNestedTypes) {
				output.WriteLine("// Nested Types");
				foreach (var nestedType in type.NestedTypes) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleType(nestedType);
					output.WriteLine();
				}
				output.WriteLine();
			}
			if (type.HasFields) {
				output.WriteLine("// Fields");
				foreach (var field in type.Fields) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleField(field);
				}
				output.WriteLine();
			}
			if (type.HasMethods) {
				output.WriteLine("// Methods");
				foreach (var m in type.Methods) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleMethod(m);
					output.WriteLine();
				}
			}
			if (type.HasProperties) {
				output.WriteLine("// Properties");
				foreach (var prop in type.Properties) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleProperty(prop);
				}
				output.WriteLine();
			}
			if (type.HasEvents) {
				output.WriteLine("// Events");
				foreach (var ev in type.Events) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleEvent(ev);
					output.WriteLine();
				}
				output.WriteLine();
			}
			CloseBlock("end of class " + (type.DeclaringType != null ? type.Name : type.FullName));
			isInType = oldIsInType;
		}
		
		void WriteTypeParameters(ITextOutput output, IGenericParameterProvider p)
		{
			if (p.HasGenericParameters) {
				output.Write('<');
				for (int i = 0; i < p.GenericParameters.Count; i++) {
					if (i > 0)
						output.Write(", ");
					GenericParameter gp = p.GenericParameters[i];
					if (gp.HasReferenceTypeConstraint) {
						output.Write("class ");
					} else if (gp.HasNotNullableValueTypeConstraint) {
						output.Write("valuetype ");
					}
					if (gp.HasConstraints) {
						output.Write('(');
						for (int j = 0; j < gp.Constraints.Count; j++) {
							if (j > 0)
								output.Write(", ");
							gp.Constraints[j].WriteTo(output, ILNameSyntax.TypeName);
						}
						output.Write(") ");
					}
					if (gp.HasDefaultConstructorConstraint) {
						output.Write(".ctor ");
					}
					if (gp.IsContravariant) {
						output.Write('-');
					} else if (gp.IsCovariant) {
						output.Write('+');
					}
					output.Write(DisassemblerHelpers.Escape(gp.Name));
				}
				output.Write('>');
			}
		}
		#endregion

		#region Helper methods
		void WriteAttributes(Collection<CustomAttribute> attributes)
		{
			foreach (CustomAttribute a in attributes) {
				output.Write(".custom ");
				a.Constructor.WriteTo(output);
				byte[] blob = a.GetBlob();
				if (blob != null) {
					output.Write(" = ");
					WriteBlob(blob);
				}
				output.WriteLine();
			}
		}
		
		void WriteBlob(byte[] blob)
		{
			output.Write("(");
			output.Indent();

			for (int i = 0; i < blob.Length; i++) {
				if (i % 16 == 0 && i < blob.Length - 1) {
					output.WriteLine();
				} else {
					output.Write(' ');
				}
				output.Write(blob[i].ToString("x2"));
			}
			
			output.WriteLine();
			output.Unindent();
			output.Write(")");
		}
		
		void OpenBlock(bool defaultCollapsed)
		{
			output.MarkFoldStart(defaultCollapsed: defaultCollapsed);
			output.WriteLine();
			output.WriteLine("{");
			output.Indent();
		}
		
		void CloseBlock(string comment = null)
		{
			output.Unindent();
			output.Write("}");
			if (comment != null)
				output.Write(" // " + comment);
			output.MarkFoldEnd();
			output.WriteLine();
		}
		
		void WriteFlags<T>(T flags, EnumNameCollection<T> flagNames) where T : struct
		{
			long val = Convert.ToInt64(flags);
			long tested = 0;
			foreach (var pair in flagNames) {
				tested |= pair.Key;
				if ((val & pair.Key) != 0 && pair.Value != null) {
					output.Write(pair.Value);
					output.Write(' ');
				}
			}
			if ((val & ~tested) != 0)
				output.Write("flag({0:x4}) ", val & ~tested);
		}
		
		void WriteEnum<T>(T enumValue, EnumNameCollection<T> enumNames) where T : struct
		{
			long val = Convert.ToInt64(enumValue);
			foreach (var pair in enumNames) {
				if (pair.Key == val) {
					if (pair.Value != null) {
						output.Write(pair.Value);
						output.Write(' ');
					}
					return;
				}
			}
			if (val != 0) {
				output.Write("flag({0:x4})", val);
				output.Write(' ');
			}
			
		}
		
		sealed class EnumNameCollection<T> : IEnumerable<KeyValuePair<long, string>> where T : struct
		{
			List<KeyValuePair<long, string>> names = new List<KeyValuePair<long, string>>();
			
			public void Add(T flag, string name)
			{
				this.names.Add(new KeyValuePair<long, string>(Convert.ToInt64(flag), name));
			}
			
			public IEnumerator<KeyValuePair<long, string>> GetEnumerator()
			{
				return names.GetEnumerator();
			}
			
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			{
				return names.GetEnumerator();
			}
		}
		#endregion
		
		public void DisassembleNamespace(string nameSpace, IEnumerable<TypeDefinition> types)
		{
			if (!string.IsNullOrEmpty(nameSpace)) {
				output.Write(".namespace " + DisassemblerHelpers.Escape(nameSpace));
				OpenBlock(false);
			}
			bool oldIsInType = isInType;
			isInType = true;
			foreach (TypeDefinition td in types) {
				cancellationToken.ThrowIfCancellationRequested();
				DisassembleType(td);
				output.WriteLine();
			}
			if (!string.IsNullOrEmpty(nameSpace)) {
				CloseBlock();
				isInType = oldIsInType;
			}
		}
		
		public void WriteAssemblyHeader(AssemblyDefinition asm)
		{
			output.Write(".assembly " + DisassemblerHelpers.Escape(asm.Name.Name));
			OpenBlock(false);
			WriteAttributes(asm.CustomAttributes);
			if (asm.Name.PublicKey != null && asm.Name.PublicKey.Length > 0) {
				output.Write(".publickey = ");
				WriteBlob(asm.Name.PublicKey);
				output.WriteLine();
			}
			if (asm.Name.HashAlgorithm != AssemblyHashAlgorithm.None) {
				output.Write(".hash algorithm 0x{0:x8}", (int)asm.Name.HashAlgorithm);
				if (asm.Name.HashAlgorithm == AssemblyHashAlgorithm.SHA1)
					output.Write(" // SHA1");
				output.WriteLine();
			}
			Version v = asm.Name.Version;
			if (v != null) {
				output.WriteLine(".ver {0}:{1}:{2}:{3}", v.Major, v.Minor, v.Build, v.Revision);
			}
			CloseBlock();
		}
		
		public void WriteAssemblyReferences(ModuleDefinition module)
		{
			foreach (var mref in module.ModuleReferences) {
				output.WriteLine(".module extern {0}", DisassemblerHelpers.Escape(mref.Name));
			}
			foreach (var aref in module.AssemblyReferences) {
				output.Write(".assembly extern {0}", DisassemblerHelpers.Escape(aref.Name));
				OpenBlock(false);
				if (aref.PublicKeyToken != null) {
					output.Write(".publickeytoken = ");
					WriteBlob(aref.PublicKeyToken);
					output.WriteLine();
				}
				if (aref.Version != null) {
					output.WriteLine(".ver {0}:{1}:{2}:{3}", aref.Version.Major, aref.Version.Minor, aref.Version.Build, aref.Version.Revision);
				}
				CloseBlock();
			}
		}
		
		public void WriteModuleHeader(ModuleDefinition module)
		{
			output.WriteLine(".module {0}", module.Name);
			output.WriteLine("// MVID: {0}", module.Mvid.ToString("B").ToUpperInvariant());
			// TODO: imagebase, file alignment, stackreserve, subsystem
			output.WriteLine(".corflags 0x{0:x} // {1}", module.Attributes, module.Attributes.ToString());
			
			WriteAttributes(module.CustomAttributes);
		}
		
		public void WriteModuleContents(ModuleDefinition module)
		{
			foreach (TypeDefinition td in module.Types) {
				DisassembleType(td);
				output.WriteLine();
			}
		}
		
		/// <inheritdoc/>
		public Tuple<string, List<MemberMapping>> CodeMappings {
			get;
			private set;
		}
	}
}
