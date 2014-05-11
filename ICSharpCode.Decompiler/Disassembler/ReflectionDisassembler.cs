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
using dnlib.DotNet;

namespace ICSharpCode.Decompiler.Disassembler
{
	/// <summary>
	/// Disassembles type and member definitions.
	/// </summary>
	public sealed class ReflectionDisassembler
	{
		ITextOutput output;
		CancellationToken cancellationToken;
		bool isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
		MethodBodyDisassembler methodBodyDisassembler;
		IMemberDef currentMember;
		
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
		
		EnumNameCollection<CallingConvention> callingConvention = new EnumNameCollection<CallingConvention>() {
			{ CallingConvention.C, "unmanaged cdecl" },
			{ CallingConvention.StdCall, "unmanaged stdcall" },
			{ CallingConvention.ThisCall, "unmanaged thiscall" },
			{ CallingConvention.FastCall, "unmanaged fastcall" },
			{ CallingConvention.VarArg, "vararg" },
			{ CallingConvention.Generic, null },
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
		
		public void DisassembleMethod(MethodDef method)
		{
			// set current member
			currentMember = method;
			
			// write method header
			output.WriteDefinition(".method ", method);
			DisassembleMethodInternal(method);
		}
		
		void DisassembleMethodInternal(MethodDef method)
		{
			//    .method public hidebysig  specialname
			//               instance default class [mscorlib]System.IO.TextWriter get_BaseWriter ()  cil managed
			//
			
			//emit flags
			WriteEnum(method.Attributes & MethodAttributes.MemberAccessMask, methodVisibility);
			WriteFlags(method.Attributes & ~MethodAttributes.MemberAccessMask, methodAttributeFlags);
			if(method.IsCompilerControlled) output.Write("privatescope ");
			
			if ((method.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl) {
				output.Write("pinvokeimpl");
				if (method.HasImplMap) {
					ImplMap info = method.ImplMap;
					output.Write("(\"" + NRefactory.CSharp.CSharpOutputVisitor.ConvertString(info.Module.Name) + "\"");
					
					if (!string.IsNullOrEmpty(info.Name) && info.Name != method.Name)
						output.Write(" as \"" + NRefactory.CSharp.CSharpOutputVisitor.ConvertString(info.Name) + "\"");
					
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
					else if (info.IsCallConvStdcall)
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
			WriteEnum(method.CallingConvention & (CallingConvention)0x1f, callingConvention);
			
			//return type
			method.ReturnType.WriteTo(output);
			output.Write(' ');
			if (method.Parameters.ReturnParameter.HasParamDef && method.Parameters.ReturnParameter.ParamDef.HasMarshalType) {
				WriteMarshalInfo(method.Parameters.ReturnParameter.ParamDef.MarshalType);
			}
			
			if (method.IsCompilerControlled) {
				output.Write(DisassemblerHelpers.Escape(method.Name + "$PST" + method.MDToken.ToInt32().ToString("X8")));
			} else {
				output.Write(DisassemblerHelpers.Escape(method.Name));
			}
			
			WriteTypeParameters(output, method);
			
			//( params )
			output.Write(" (");
			if (method.MethodSig.GetParams().Count > 0) {
				output.WriteLine();
				output.Indent();
				WriteParameters(method, method.Parameters);
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
					methodOverride.MethodDeclaration.WriteMethodTo(output);
					output.WriteLine();
				}
			}
			foreach (var p in method.Parameters) {
				WriteParameterAttributes(p);
			}
			WriteSecurityDeclarations(method);
			
			if (method.HasBody) {
				// create IL code mappings - used in debugger
				MemberMapping methodMapping = new MemberMapping(method);
				methodBodyDisassembler.Disassemble(method, methodMapping);
				output.AddDebuggerMemberMapping(methodMapping);
			}
			
			CloseBlock("end of method " + DisassemblerHelpers.Escape(method.DeclaringType.Name) + "::" + DisassemblerHelpers.Escape(method.Name));
		}
		
		#region Write Security Declarations
		void WriteSecurityDeclarations(IHasDeclSecurity secDeclProvider)
		{
			if (!secDeclProvider.HasDeclSecurities)
				return;
			foreach (var secdecl in secDeclProvider.DeclSecurities) {
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
						output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(sa.AttributeType.ToTypeSig())));
					} else {
						sa.AttributeType.WriteTo(output, ILNameSyntax.TypeName);
					}
					output.Write(" = {");
					if (sa.HasNamedArguments) {
						output.WriteLine();
						output.Indent();
						
						foreach (var na in sa.Fields) {
							output.Write("field ");
							WriteSecurityDeclarationArgument(na);
							output.WriteLine();
						}
						
						foreach (var na in sa.Properties) {
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
		
		void WriteSecurityDeclarationArgument(CANamedArgument na)
		{
			TypeSig type = na.Argument.Type;
			if (type.ElementType == ElementType.Class || type.ElementType == ElementType.ValueType) {
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
				output.Write("string('{0}')", NRefactory.CSharp.CSharpOutputVisitor.ConvertString((string)na.Argument.Value).Replace("'", "\'"));
			} else {
				WriteConstant(na.Argument.Value);
			}
		}
		
		string GetAssemblyQualifiedName(TypeSig type)
		{
			IAssembly anr = type.Scope as IAssembly;
			if (anr == null) {
				ModuleDef md = type.Scope as ModuleDef;
				if (md != null) {
					anr = md.Assembly;
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
		void WriteMarshalInfo(MarshalType marshalInfo)
		{
			output.Write("marshal(");
			WriteNativeType(marshalInfo.NativeType, marshalInfo);
			output.Write(") ");
		}
		
		void WriteNativeType(NativeType nativeType, MarshalType marshalInfo = null)
		{
			switch (nativeType) {
				case NativeType.NotInitialized:
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
					ArrayMarshalType ami = (ArrayMarshalType)marshalInfo;
					if (ami == null)
						goto default;
					if (ami.ElementType != NativeType.Max)
						WriteNativeType(ami.ElementType);
					output.Write('[');
					if (ami.Flags == 0) {
						output.Write(ami.Size.ToString());
					} else {
						if (ami.Size >= 0)
							output.Write(ami.Size.ToString());
						output.Write(" + ");
						output.Write(ami.ParamNumber.ToString());
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
					output.Write("fixed sysstring[{0}]", ((FixedSysStringMarshalType)marshalInfo).Size);
					break;
				case NativeType.IUnknown:
					output.Write("iunknown");
					//TODO: Print InterfaceMarshalType.IsIidParamIndexValid
					break;
				case NativeType.IDispatch:
					output.Write("idispatch");
					//TODO: Print InterfaceMarshalType.IsIidParamIndexValid
					break;
				case NativeType.Struct:
					output.Write("struct");
					break;
				case NativeType.IntF:
					output.Write("interface");
					//TODO: Print InterfaceMarshalType.IsIidParamIndexValid
					break;
				case NativeType.SafeArray:
					output.Write("safearray ");
					SafeArrayMarshalType sami = marshalInfo as SafeArrayMarshalType;
					if (sami != null) {
						switch (sami.VariantType) {
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
								output.Write(sami.VariantType.ToString());
								break;
						}
						if (sami.IsUserDefinedSubTypeValid) {
							//TODO:
						}
					}
					break;
				case NativeType.FixedArray:
					output.Write("fixed array");
					FixedArrayMarshalType fami = marshalInfo as FixedArrayMarshalType;
					if (fami != null) {
						output.Write("[{0}]", fami.Size);
						if (fami.IsElementTypeValid) {
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
					CustomMarshalType cmi = marshalInfo as CustomMarshalType;
					if (cmi == null)
						goto default;
					output.Write("custom(\"{0}\", \"{1}\"",
								 NRefactory.CSharp.CSharpOutputVisitor.ConvertString(cmi.CustomMarshaler.FullName),
					             NRefactory.CSharp.CSharpOutputVisitor.ConvertString(cmi.Cookie));
					if (!UTF8String.IsNullOrEmpty(cmi.Guid) || !UTF8String.IsNullOrEmpty(cmi.NativeTypeName)) {
						output.Write(", \"{0}\", \"{1}\"", NRefactory.CSharp.CSharpOutputVisitor.ConvertString(cmi.Guid), NRefactory.CSharp.CSharpOutputVisitor.ConvertString(cmi.NativeTypeName));
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
		
		void WriteParameters(MethodDef method, ParameterList parameters)
		{
			for (int i = 0; i < parameters.Count; i++) {
				var p = parameters[i];
				if (p.IsHiddenThisParameter)
					continue;
				var paramDef = p.ParamDef;
				if (paramDef != null) {
					if (paramDef.IsIn)
						output.Write("[in] ");
					if (paramDef.IsOut)
						output.Write("[out] ");
					if (paramDef.IsOptional)
						output.Write("[opt] ");
				}
				p.Type.WriteTo(output);
				output.Write(' ');
				if (paramDef != null && paramDef.HasFieldMarshal) {
					WriteMarshalInfo(paramDef.MarshalType);
				}
				output.WriteDefinition(DisassemblerHelpers.Escape(p.Name), p);
				if (i < parameters.Count - 1)
					output.Write(',');
				output.WriteLine();
			}
		}
		
		void WriteParameters(TypeDef type, IList<TypeSig> parameters) {
			for (int i = 0; i < parameters.Count; i++) {
				if (i != 0)
					output.Write(", ");
				parameters[i].WriteTo(output);
			}
		}
		
		bool HasParameterAttributes(Parameter p)
		{
			return p.ParamDef != null && (p.ParamDef.HasConstant || p.ParamDef.HasCustomAttributes);
		}
		
		void WriteParameterAttributes(Parameter p)
		{
			if (!HasParameterAttributes(p))
				return;
			output.Write(".param [{0}]", p.Index + 1);
			if (p.ParamDef.HasConstant) {
				output.Write(" = ");
				WriteConstant(p.ParamDef.Constant.Value);
			}
			output.WriteLine();
			WriteAttributes(p.ParamDef.CustomAttributes);
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
		
		public void DisassembleField(FieldDef field)
		{
			output.WriteDefinition(".field ", field);
			if (field.HasLayoutInfo) {
				output.Write("[" + field.FieldOffset + "] ");
			}
			WriteEnum(field.Attributes & FieldAttributes.FieldAccessMask, fieldVisibility);
			const FieldAttributes hasXAttributes = FieldAttributes.HasDefault | FieldAttributes.HasFieldMarshal | FieldAttributes.HasFieldRVA;
			WriteFlags(field.Attributes & ~(FieldAttributes.FieldAccessMask | hasXAttributes), fieldAttributes);
			if (field.HasMarshalType) {
				WriteMarshalInfo(field.MarshalType);
			}
			field.FieldType.WriteTo(output);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(field.Name));
			if ((field.Attributes & FieldAttributes.HasFieldRVA) == FieldAttributes.HasFieldRVA) {
				output.Write(" at I_{0:x8}", field.RVA);
			}
			if (field.HasConstant) {
				output.Write(" = ");
				WriteConstant(field.Constant.Value);
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
		
		public void DisassembleProperty(PropertyDef property)
		{
			// set current member
			currentMember = property;
			
			output.WriteDefinition(".property ", property);
			WriteFlags(property.Attributes, propertyAttributes);
			if (property.PropertySig.HasThis)
				output.Write("instance ");
			property.PropertySig.RetType.WriteTo(output);
			output.Write(' ');
			output.Write(DisassemblerHelpers.Escape(property.Name));
			
			output.Write("(");
			WriteParameters(property.DeclaringType, property.PropertySig.GetParameters());
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
		
		void WriteNestedMethod(string keyword, MethodDef method)
		{
			if (method == null)
				return;
			
			output.Write(keyword);
			output.Write(' ');
			method.WriteMethodTo(output);
			output.WriteLine();
		}
		#endregion
		
		#region Disassemble Event
		EnumNameCollection<EventAttributes> eventAttributes = new EnumNameCollection<EventAttributes>() {
			{ EventAttributes.SpecialName, "specialname" },
			{ EventAttributes.RTSpecialName, "rtspecialname" },
		};
		
		public void DisassembleEvent(EventDef ev)
		{
			// set current member
			currentMember = ev;
			
			output.WriteDefinition(".event ", ev);
			WriteFlags(ev.Attributes, eventAttributes);
			ev.EventType.ToTypeSig().WriteTo(output, ILNameSyntax.TypeName);
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
		
		public void DisassembleType(TypeDef type)
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
			
			output.Write(DisassemblerHelpers.Escape(type.DeclaringType != null ? type.Name.String : type.FullName));
			WriteTypeParameters(output, type);
			output.MarkFoldStart(defaultCollapsed: isInType);
			output.WriteLine();
			
			if (type.BaseType != null) {
				output.Indent();
				output.Write("extends ");
				type.BaseType.ToTypeSig().WriteTo(output, ILNameSyntax.TypeName);
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
					type.Interfaces[index].Interface.ToTypeSig().WriteTo(output, ILNameSyntax.TypeName);
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
			if (type.HasClassLayout) {
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
			CloseBlock("end of class " + (type.DeclaringType != null ? type.Name.String : type.FullName));
			isInType = oldIsInType;
		}
		
		void WriteTypeParameters(ITextOutput output, ITypeOrMethodDef p)
		{
			if (p.HasGenericParameters) {
				output.Write('<');
				for (int i = 0; i < p.GenericParameters.Count; i++) {
					if (i > 0)
						output.Write(", ");
					GenericParam gp = p.GenericParameters[i];
					if (gp.HasReferenceTypeConstraint) {
						output.Write("class ");
					} else if (gp.HasNotNullableValueTypeConstraint) {
						output.Write("valuetype ");
					}
					if (gp.HasDefaultConstructorConstraint) {
						output.Write(".ctor ");
					}
					if (gp.HasGenericParamConstraints) {
						output.Write('(');
						for (int j = 0; j < gp.GenericParamConstraints.Count; j++) {
							if (j > 0)
								output.Write(", ");
							gp.GenericParamConstraints[j].Constraint.WriteTo(output, ILNameSyntax.TypeName);
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
		void WriteAttributes(CustomAttributeCollection attributes)
		{
			foreach (CustomAttribute a in attributes) {
				output.Write(".custom ");
				a.Constructor.WriteMethodTo(output);
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
		
		public void DisassembleNamespace(string nameSpace, IEnumerable<TypeDef> types)
		{
			if (!string.IsNullOrEmpty(nameSpace)) {
				output.Write(".namespace " + DisassemblerHelpers.Escape(nameSpace));
				OpenBlock(false);
			}
			bool oldIsInType = isInType;
			isInType = true;
			foreach (TypeDef td in types) {
				cancellationToken.ThrowIfCancellationRequested();
				DisassembleType(td);
				output.WriteLine();
			}
			if (!string.IsNullOrEmpty(nameSpace)) {
				CloseBlock();
				isInType = oldIsInType;
			}
		}
		
		public void WriteAssemblyHeader(AssemblyDef asm)
		{
			output.Write(".assembly ");
			if (asm.IsContentTypeWindowsRuntime)
				output.Write("windowsruntime ");
			output.Write(DisassemblerHelpers.Escape(asm.Name));
			OpenBlock(false);
			WriteAttributes(asm.CustomAttributes);
			WriteSecurityDeclarations(asm);
			if (asm.PublicKey != null && !asm.PublicKey.IsNullOrEmpty) {
				output.Write(".publickey = ");
				WriteBlob(asm.PublicKey.Data);
				output.WriteLine();
			}
			if (asm.HashAlgorithm != AssemblyHashAlgorithm.None) {
				output.Write(".hash algorithm 0x{0:x8}", (int)asm.HashAlgorithm);
				if (asm.HashAlgorithm == AssemblyHashAlgorithm.SHA1)
					output.Write(" // SHA1");
				output.WriteLine();
			}
			Version v = asm.Version;
			if (v != null) {
				output.WriteLine(".ver {0}:{1}:{2}:{3}", v.Major, v.Minor, v.Build, v.Revision);
			}
			CloseBlock();
		}
		
		public void WriteModuleHeader(ModuleDef module)
		{
			if (module.HasExportedTypes) {
				foreach (ExportedType exportedType in module.ExportedTypes) {
					output.Write(".class extern ");
					if (exportedType.IsForwarder)
						output.Write("forwarder ");
					output.Write(exportedType.DeclaringType != null ? exportedType.TypeName.String : exportedType.FullName);
					OpenBlock(false);
					if (exportedType.DeclaringType != null)
						output.WriteLine(".class extern {0}", DisassemblerHelpers.Escape(exportedType.DeclaringType.FullName));
					else
						output.WriteLine(".assembly extern {0}", DisassemblerHelpers.Escape(exportedType.Scope.GetScopeName()));
					CloseBlock();
				}
			}
			
			output.WriteLine(".module {0}", module.Name);
			if (module.Mvid.HasValue)
				output.WriteLine("// MVID: {0}", module.Mvid.Value.ToString("B").ToUpperInvariant());
			// TODO: imagebase, file alignment, stackreserve, subsystem
			output.WriteLine(".corflags 0x{0:x} // {1}", module.Cor20HeaderFlags, module.Cor20HeaderFlags.ToString());
			
			WriteAttributes(module.CustomAttributes);
		}
		
		public void WriteModuleContents(ModuleDef module)
		{
			foreach (TypeDef td in module.Types) {
				DisassembleType(td);
				output.WriteLine();
			}
		}
	}
}
