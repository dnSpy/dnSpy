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
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Disassembles type and member definitions.
	/// </summary>
	public sealed class ReflectionDisassembler
	{
		readonly ITextOutput output;
		CancellationToken cancellationToken;
		bool isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
		MethodBodyDisassembler methodBodyDisassembler;
		MemberReference currentMember;
		
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
			{ MethodAttributes.PInvokeImpl, null }, // handled separately
			{ MethodAttributes.UnmanagedExport, "export" },
			{ MethodAttributes.RTSpecialName, "rtspecialname" },
			{ MethodAttributes.RequireSecObject, "reqsecobj" },
			{ MethodAttributes.NewSlot, "newslot" },
			{ MethodAttributes.CheckAccessOnOverride, "strict" },
			{ MethodAttributes.Abstract, "abstract" },
			{ MethodAttributes.Virtual, "virtual" },
			{ MethodAttributes.Static, "static" },
			{ MethodAttributes.HasSecurity, null }, // ?? also invisible in ILDasm
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
			{ MethodCallingConvention.Generic, null },
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
			{ MethodImplAttributes.PreserveSig, "preservesig" },
			{ MethodImplAttributes.InternalCall, "internalcall" },
			{ MethodImplAttributes.ForwardRef, "forwardref" },
		};
		
		public void DisassembleMethod(MethodDefinition method)
		{
			// set current member
			currentMember = method;
			
			// write method header
			output.WriteDefinition(".method ", method);
			DisassembleMethodInternal(method);
		}
		
		void DisassembleMethodInternal(MethodDefinition method)
		{
			//    .method public hidebysig  specialname
			//               instance default class [mscorlib]System.IO.TextWriter get_BaseWriter ()  cil managed
			//
			
			TextLocation startLocation = output.Location;
			
			//emit flags
			WriteEnum(method.Attributes & MethodAttributes.MemberAccessMask, methodVisibility);
			WriteFlags(method.Attributes & ~MethodAttributes.MemberAccessMask, methodAttributeFlags);
			if(method.IsCompilerControlled) output.Write("privatescope ");
			
			if ((method.Attributes & MethodAttributes.PInvokeImpl) == MethodAttributes.PInvokeImpl) {
				output.Write("pinvokeimpl");
				if (method.HasPInvokeInfo && method.PInvokeInfo != null) {
					PInvokeInfo info = method.PInvokeInfo;
					output.Write("(\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(info.Module.Name) + "\"");
					
					if (!string.IsNullOrEmpty(info.EntryPoint) && info.EntryPoint != method.Name)
						output.Write(" as \"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(info.EntryPoint) + "\"");
					
					if (info.IsNoMangle)
						output.Write(" nomangle");
					
					if (info.IsCharSetAnsi)
						output.Write(" ansi");
					else if (info.IsCharSetAuto)
						output.Write(" autochar");
					else if (info.IsCharSetUnicode)
						output.Write(" unicode");
					
					if (info.SupportsLastError)
						output.Write(" lasterr");
					
					if (info.IsCallConvCdecl)
						output.Write(" cdecl");
					else if (info.IsCallConvFastcall)
						output.Write(" fastcall");
					else if (info.IsCallConvStdCall)
						output.Write(" stdcall");
					else if (info.IsCallConvThiscall)
						output.Write(" thiscall");
					else if (info.IsCallConvWinapi)
						output.Write(" winapi");
					
					output.Write(')');
				}
				output.Write(' ');
			}
			
			output.WriteLine();
			output.Indent();
			if (method.ExplicitThis) {
				output.Write("instance explicit ");
			} else if (method.HasThis) {
				output.Write("instance ");
			}
			
			//call convention
			WriteEnum(method.CallingConvention & (MethodCallingConvention)0x1f, callingConvention);
			
			//return type
			method.ReturnType.WriteTo(output);
			output.Write(' ');
			if (method.MethodReturnType.HasMarshalInfo) {
				WriteMarshalInfo(method.MethodReturnType.MarshalInfo);
			}
			
			if (method.IsCompilerControlled) {
				output.Write(DisassemblerHelpers.Escape(method.Name + "$PST" + method.MetadataToken.ToInt32().ToString("X8")));
			} else {
				output.Write(DisassemblerHelpers.Escape(method.Name));
			}
			
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
			OpenBlock(defaultCollapsed: isInType);
			WriteAttributes(method.CustomAttributes);
			if (method.HasOverrides) {
				foreach (var methodOverride in method.Overrides) {
					output.Write(".override method ");
					methodOverride.WriteTo(output);
					output.WriteLine();
				}
			}
			WriteParameterAttributes(0, method.MethodReturnType, method.MethodReturnType);
			foreach (var p in method.Parameters) {
				WriteParameterAttributes(p.Index + 1, p, p);
			}
			WriteSecurityDeclarations(method);
			
			if (method.HasBody) {
				// create IL code mappings - used in debugger
				MethodDebugSymbols debugSymbols = new MethodDebugSymbols(method);
				debugSymbols.StartLocation = startLocation;
				methodBodyDisassembler.Disassemble(method.Body, debugSymbols);
				debugSymbols.EndLocation = output.Location;
				output.AddDebugSymbols(debugSymbols);
			}
			
			CloseBlock("end of method " + DisassemblerHelpers.Escape(method.DeclaringType.Name) + "::" + DisassemblerHelpers.Escape(method.Name));
		}
		
		#region Write Security Declarations
		void WriteSecurityDeclarations(ISecurityDeclarationProvider secDeclProvider)
		{
			if (!secDeclProvider.HasSecurityDeclarations)
				return;
			foreach (var secdecl in secDeclProvider.SecurityDeclarations) {
				output.Write(".permissionset ");
				switch (secdecl.Action) {
					case SecurityAction.Request:
						output.Write("request");
						break;
					case SecurityAction.Demand:
						output.Write("demand");
						break;
					case SecurityAction.Assert:
						output.Write("assert");
						break;
					case SecurityAction.Deny:
						output.Write("deny");
						break;
					case SecurityAction.PermitOnly:
						output.Write("permitonly");
						break;
					case SecurityAction.LinkDemand:
						output.Write("linkcheck");
						break;
					case SecurityAction.InheritDemand:
						output.Write("inheritcheck");
						break;
					case SecurityAction.RequestMinimum:
						output.Write("reqmin");
						break;
					case SecurityAction.RequestOptional:
						output.Write("reqopt");
						break;
					case SecurityAction.RequestRefuse:
						output.Write("reqrefuse");
						break;
					case SecurityAction.PreJitGrant:
						output.Write("prejitgrant");
						break;
					case SecurityAction.PreJitDeny:
						output.Write("prejitdeny");
						break;
					case SecurityAction.NonCasDemand:
						output.Write("noncasdemand");
						break;
					case SecurityAction.NonCasLinkDemand:
						output.Write("noncaslinkdemand");
						break;
					case SecurityAction.NonCasInheritance:
						output.Write("noncasinheritance");
						break;
					default:
						output.Write(secdecl.Action.ToString());
						break;
				}
				output.WriteLine(" = {");
				output.Indent();
				for (int i = 0; i < secdecl.SecurityAttributes.Count; i++) {
					SecurityAttribute sa = secdecl.SecurityAttributes[i];
					if (sa.AttributeType.Scope == sa.AttributeType.Module) {
						output.Write("class ");
						output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(sa.AttributeType)));
					} else {
						sa.AttributeType.WriteTo(output, ILNameSyntax.TypeName);
					}
					output.Write(" = {");
					if (sa.HasFields || sa.HasProperties) {
						output.WriteLine();
						output.Indent();
						
						foreach (CustomAttributeNamedArgument na in sa.Fields) {
							output.Write("field ");
							WriteSecurityDeclarationArgument(na);
							output.WriteLine();
						}
						
						foreach (CustomAttributeNamedArgument na in sa.Properties) {
							output.Write("property ");
							WriteSecurityDeclarationArgument(na);
							output.WriteLine();
						}
						
						output.Unindent();
					}
					output.Write('}');
					
					if (i + 1< secdecl.SecurityAttributes.Count)
						output.Write(',');
					output.WriteLine();
				}
				output.Unindent();
				output.WriteLine("}");
			}
		}
		
		void WriteSecurityDeclarationArgument(CustomAttributeNamedArgument na)
		{
			TypeReference type = na.Argument.Type;
			if (type.MetadataType == MetadataType.Class || type.MetadataType == MetadataType.ValueType) {
				output.Write("enum ");
				if (type.Scope != type.Module) {
					output.Write("class ");
					output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(type)));
				} else {
					type.WriteTo(output, ILNameSyntax.TypeName);
				}
			} else {
				type.WriteTo(output);
			}
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(na.Name));
			output.Write(" = ");
			if (na.Argument.Value is string) {
				// secdecls use special syntax for strings
				output.Write("string('{0}')", NRefactory.CSharp.TextWriterTokenWriter.ConvertString((string)na.Argument.Value).Replace("'", "\'"));
			} else {
				WriteConstant(na.Argument.Value);
			}
		}
		
		string GetAssemblyQualifiedName(TypeReference type)
		{
			AssemblyNameReference anr = type.Scope as AssemblyNameReference;
			if (anr == null) {
				ModuleDefinition md = type.Scope as ModuleDefinition;
				if (md != null) {
					anr = md.Assembly.Name;
				}
			}
			if (anr != null) {
				return type.FullName + ", " + anr.FullName;
			} else {
				return type.FullName;
			}
		}
		#endregion
		
		#region WriteMarshalInfo
		void WriteMarshalInfo(MarshalInfo marshalInfo)
		{
			output.Write("marshal(");
			WriteNativeType(marshalInfo.NativeType, marshalInfo);
			output.Write(") ");
		}
		
		void WriteNativeType(NativeType nativeType, MarshalInfo marshalInfo = null)
		{
			switch (nativeType) {
				case NativeType.None:
					break;
				case NativeType.Boolean:
					output.Write("bool");
					break;
				case NativeType.I1:
					output.Write("int8");
					break;
				case NativeType.U1:
					output.Write("unsigned int8");
					break;
				case NativeType.I2:
					output.Write("int16");
					break;
				case NativeType.U2:
					output.Write("unsigned int16");
					break;
				case NativeType.I4:
					output.Write("int32");
					break;
				case NativeType.U4:
					output.Write("unsigned int32");
					break;
				case NativeType.I8:
					output.Write("int64");
					break;
				case NativeType.U8:
					output.Write("unsigned int64");
					break;
				case NativeType.R4:
					output.Write("float32");
					break;
				case NativeType.R8:
					output.Write("float64");
					break;
				case NativeType.LPStr:
					output.Write("lpstr");
					break;
				case NativeType.Int:
					output.Write("int");
					break;
				case NativeType.UInt:
					output.Write("unsigned int");
					break;
				case NativeType.Func:
					goto default; // ??
				case NativeType.Array:
					ArrayMarshalInfo ami = (ArrayMarshalInfo)marshalInfo;
					if (ami == null)
						goto default;
					if (ami.ElementType != NativeType.Max)
						WriteNativeType(ami.ElementType);
					output.Write('[');
					if (ami.SizeParameterMultiplier == 0) {
						output.Write(ami.Size.ToString());
					} else {
						if (ami.Size >= 0)
							output.Write(ami.Size.ToString());
						output.Write(" + ");
						output.Write(ami.SizeParameterIndex.ToString());
					}
					output.Write(']');
					break;
				case NativeType.Currency:
					output.Write("currency");
					break;
				case NativeType.BStr:
					output.Write("bstr");
					break;
				case NativeType.LPWStr:
					output.Write("lpwstr");
					break;
				case NativeType.LPTStr:
					output.Write("lptstr");
					break;
				case NativeType.FixedSysString:
					output.Write("fixed sysstring[{0}]", ((FixedSysStringMarshalInfo)marshalInfo).Size);
					break;
				case NativeType.IUnknown:
					output.Write("iunknown");
					break;
				case NativeType.IDispatch:
					output.Write("idispatch");
					break;
				case NativeType.Struct:
					output.Write("struct");
					break;
				case NativeType.IntF:
					output.Write("interface");
					break;
				case NativeType.SafeArray:
					output.Write("safearray ");
					SafeArrayMarshalInfo sami = marshalInfo as SafeArrayMarshalInfo;
					if (sami != null) {
						switch (sami.ElementType) {
							case VariantType.None:
								break;
							case VariantType.I2:
								output.Write("int16");
								break;
							case VariantType.I4:
								output.Write("int32");
								break;
							case VariantType.R4:
								output.Write("float32");
								break;
							case VariantType.R8:
								output.Write("float64");
								break;
							case VariantType.CY:
								output.Write("currency");
								break;
							case VariantType.Date:
								output.Write("date");
								break;
							case VariantType.BStr:
								output.Write("bstr");
								break;
							case VariantType.Dispatch:
								output.Write("idispatch");
								break;
							case VariantType.Error:
								output.Write("error");
								break;
							case VariantType.Bool:
								output.Write("bool");
								break;
							case VariantType.Variant:
								output.Write("variant");
								break;
							case VariantType.Unknown:
								output.Write("iunknown");
								break;
							case VariantType.Decimal:
								output.Write("decimal");
								break;
							case VariantType.I1:
								output.Write("int8");
								break;
							case VariantType.UI1:
								output.Write("unsigned int8");
								break;
							case VariantType.UI2:
								output.Write("unsigned int16");
								break;
							case VariantType.UI4:
								output.Write("unsigned int32");
								break;
							case VariantType.Int:
								output.Write("int");
								break;
							case VariantType.UInt:
								output.Write("unsigned int");
								break;
							default:
								output.Write(sami.ElementType.ToString());
								break;
						}
					}
					break;
				case NativeType.FixedArray:
					output.Write("fixed array");
					FixedArrayMarshalInfo fami = marshalInfo as FixedArrayMarshalInfo;
					if (fami != null) {
						output.Write("[{0}]", fami.Size);
						if (fami.ElementType != NativeType.None) {
							output.Write(' ');
							WriteNativeType(fami.ElementType);
						}
					}
					break;
				case NativeType.ByValStr:
					output.Write("byvalstr");
					break;
				case NativeType.ANSIBStr:
					output.Write("ansi bstr");
					break;
				case NativeType.TBStr:
					output.Write("tbstr");
					break;
				case NativeType.VariantBool:
					output.Write("variant bool");
					break;
				case NativeType.ASAny:
					output.Write("as any");
					break;
				case NativeType.LPStruct:
					output.Write("lpstruct");
					break;
				case NativeType.CustomMarshaler:
					CustomMarshalInfo cmi = marshalInfo as CustomMarshalInfo;
					if (cmi == null)
						goto default;
					output.Write("custom(\"{0}\", \"{1}\"",
					             NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.ManagedType.FullName),
					             NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.Cookie));
					if (cmi.Guid != Guid.Empty || !string.IsNullOrEmpty(cmi.UnmanagedType)) {
						output.Write(", \"{0}\", \"{1}\"", cmi.Guid.ToString(), NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.UnmanagedType));
					}
					output.Write(')');
					break;
				case NativeType.Error:
					output.Write("error");
					break;
				default:
					output.Write(nativeType.ToString());
					break;
			}
		}
		#endregion
		
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
				if (p.HasMarshalInfo) {
					WriteMarshalInfo(p.MarshalInfo);
				}
				output.WriteDefinition(DisassemblerHelpers.Escape(p.Name), p);
				if (i < parameters.Count - 1)
					output.Write(',');
				output.WriteLine();
			}
		}
		
		void WriteParameterAttributes(int index, IConstantProvider cp, ICustomAttributeProvider cap)
		{
			if (!cp.HasConstant && !cap.HasCustomAttributes)
				return;
			output.Write(".param [{0}]", index);
			if (cp.HasConstant) {
				output.Write(" = ");
				WriteConstant(cp.Constant);
			}
			output.WriteLine();
			WriteAttributes(cap.CustomAttributes);
		}
		
		void WriteConstant(object constant)
		{
			if (constant == null) {
				output.Write("nullref");
			} else {
				string typeName = DisassemblerHelpers.PrimitiveTypeName(constant.GetType().FullName);
				if (typeName != null && typeName != "string") {
					output.Write(typeName);
					output.Write('(');
					float? cf = constant as float?;
					double? cd = constant as double?;
					if (cf.HasValue && (float.IsNaN(cf.Value) || float.IsInfinity(cf.Value))) {
						output.Write("0x{0:x8}", BitConverter.ToInt32(BitConverter.GetBytes(cf.Value), 0));
					} else if (cd.HasValue && (double.IsNaN(cd.Value) || double.IsInfinity(cd.Value))) {
						output.Write("0x{0:x16}", BitConverter.DoubleToInt64Bits(cd.Value));
					} else {
						DisassemblerHelpers.WriteOperand(output, constant);
					}
					output.Write(')');
				} else {
					DisassemblerHelpers.WriteOperand(output, constant);
				}
			}
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
			if (field.HasLayoutInfo) {
				output.Write("[" + field.Offset + "] ");
			}
			WriteEnum(field.Attributes & FieldAttributes.FieldAccessMask, fieldVisibility);
			const FieldAttributes hasXAttributes = FieldAttributes.HasDefault | FieldAttributes.HasFieldMarshal | FieldAttributes.HasFieldRVA;
			WriteFlags(field.Attributes & ~(FieldAttributes.FieldAccessMask | hasXAttributes), fieldAttributes);
			if (field.HasMarshalInfo) {
				WriteMarshalInfo(field.MarshalInfo);
			}
			field.FieldType.WriteTo(output);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(field.Name));
			if ((field.Attributes & FieldAttributes.HasFieldRVA) == FieldAttributes.HasFieldRVA) {
				output.Write(" at I_{0:x8}", field.RVA);
			}
			if (field.HasConstant) {
				output.Write(" = ");
				WriteConstant(field.Constant);
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
			// set current member
			currentMember = property;
			
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
				WriteNestedMethod(".other", method);
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
			// set current member
			currentMember = ev;
			
			output.WriteDefinition(".event ", ev);
			WriteFlags(ev.Attributes, eventAttributes);
			ev.EventType.WriteTo(output, ILNameSyntax.TypeName);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(ev.Name));
			OpenBlock(false);
			WriteAttributes(ev.CustomAttributes);
			WriteNestedMethod(".addon", ev.AddMethod);
			WriteNestedMethod(".removeon", ev.RemoveMethod);
			WriteNestedMethod(".fire", ev.InvokeMethod);
			foreach (var method in ev.OtherMethods) {
				WriteNestedMethod(".other", method);
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
			{ TypeAttributes.WindowsRuntime, "windowsruntime" },
			{ TypeAttributes.BeforeFieldInit, "beforefieldinit" },
			{ TypeAttributes.HasSecurity, null },
		};
		
		public void DisassembleType(TypeDefinition type)
		{
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
			WriteSecurityDeclarations(type);
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
			if (type.HasEvents) {
				output.WriteLine("// Events");
				foreach (var ev in type.Events) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleEvent(ev);
					output.WriteLine();
				}
				output.WriteLine();
			}
			if (type.HasProperties) {
				output.WriteLine("// Properties");
				foreach (var prop in type.Properties) {
					cancellationToken.ThrowIfCancellationRequested();
					DisassembleProperty(prop);
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
					if (gp.HasDefaultConstructorConstraint) {
						output.Write(".ctor ");
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
			output.Write(".assembly ");
			if (asm.Name.IsWindowsRuntime)
				output.Write("windowsruntime ");
			output.Write(DisassemblerHelpers.Escape(asm.Name.Name));
			OpenBlock(false);
			WriteAttributes(asm.CustomAttributes);
			WriteSecurityDeclarations(asm);
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
				output.Write(".assembly extern ");
				if (aref.IsWindowsRuntime)
					output.Write("windowsruntime ");
				output.Write(DisassemblerHelpers.Escape(aref.Name));
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
			if (module.HasExportedTypes) {
				foreach (ExportedType exportedType in module.ExportedTypes) {
					output.Write(".class extern ");
					if (exportedType.IsForwarder)
						output.Write("forwarder ");
					output.Write(exportedType.DeclaringType != null ? exportedType.Name : exportedType.FullName);
					OpenBlock(false);
					if (exportedType.DeclaringType != null)
						output.WriteLine(".class extern {0}", DisassemblerHelpers.Escape(exportedType.DeclaringType.FullName));
					else
						output.WriteLine(".assembly extern {0}", DisassemblerHelpers.Escape(exportedType.Scope.Name));
					CloseBlock();
				}
			}
			
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
	}
}
