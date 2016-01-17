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
using System.Threading;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnSpy.Decompiler.Shared;

namespace ICSharpCode.Decompiler.Disassembler {
	public class DisassemblerOptions
	{
		public DisassemblerOptions(CancellationToken cancellationToken, ModuleDef ownerModule)
		{
			this.CancellationToken = cancellationToken;
			this.OwnerModule = ownerModule;
		}

		public readonly ModuleDef OwnerModule;

		public readonly CancellationToken CancellationToken;

		/// <summary>
		/// null if we shouldn't add opcode documentation. It returns null if no doc was found
		/// </summary>
		public Func<OpCode, string> GetOpCodeDocumentation;

		/// <summary>
		/// null if we shouldn't add XML doc comments.
		/// </summary>
		public Func<IMemberRef, IEnumerable<string>> GetXmlDocComments;

		/// <summary>
		/// Creates a <see cref="IInstructionBytesReader"/> instance
		/// </summary>
		public Func<MethodDef, IInstructionBytesReader> CreateInstructionBytesReader;

		/// <summary>
		/// Show tokens, RVAs, file offsets
		/// </summary>
		public bool ShowTokenAndRvaComments;

		/// <summary>
		/// Show IL instruction bytes
		/// </summary>
		public bool ShowILBytes;

		/// <summary>
		/// Sort members if true
		/// </summary>
		public bool SortMembers;
	}

	/// <summary>
	/// Disassembles type and member definitions.
	/// </summary>
	public sealed class ReflectionDisassembler
	{
		readonly ITextOutput output;
		readonly DisassemblerOptions options;
		bool isInType; // whether we are currently disassembling a whole type (-> defaultCollapsed for foldings)
		MethodBodyDisassembler methodBodyDisassembler;
		IMemberDef currentMember;
		
		public ReflectionDisassembler(ITextOutput output, bool detectControlStructure, DisassemblerOptions options)
		{
			if (output == null)
				throw new ArgumentNullException("output");
			this.output = output;
			this.options = options;
			this.methodBodyDisassembler = new MethodBodyDisassembler(output, detectControlStructure, options);
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
			{ CallingConvention.NativeVarArg, "nativevararg" },
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

		void WriteXmlDocComment(IMemberDef mr) {
			if (options.GetXmlDocComments == null)
				return;
			foreach (var line in options.GetXmlDocComments(mr)) {
				output.Write("///", TextTokenKind.XmlDocTag);
				output.WriteXmlDoc(line);
				output.WriteLine();
			}
		}
		
		public void DisassembleMethod(MethodDef method)
		{
			// set current member
			currentMember = method;
			
			// write method header
			WriteXmlDocComment(method);
			AddComment(method);
			output.WriteDefinition(".method", method, TextTokenKind.ILDirective, false);
			output.WriteSpace();
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
			if (method.IsCompilerControlled) {
				output.Write("privatescope", TextTokenKind.Keyword);
				output.WriteSpace();
			}
			
			if ((method.Attributes & MethodAttributes.PinvokeImpl) == MethodAttributes.PinvokeImpl) {
				output.Write("pinvokeimpl", TextTokenKind.Keyword);
				if (method.HasImplMap) {
					ImplMap info = method.ImplMap;
					output.Write("(", TextTokenKind.Operator);
					output.Write("\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(info.Module == null ? string.Empty : info.Module.Name.String) + "\"", TextTokenKind.String);

					if (!string.IsNullOrEmpty(info.Name) && info.Name != method.Name) {
						output.WriteSpace();
						output.Write("as", TextTokenKind.Keyword);
						output.WriteSpace();
						output.Write("\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(info.Name) + "\"", TextTokenKind.String);
					}

					if (info.IsNoMangle) {
						output.WriteSpace();
						output.Write("nomangle", TextTokenKind.Keyword);
					}

					if (info.IsCharSetAnsi) {
						output.WriteSpace();
						output.Write("ansi", TextTokenKind.Keyword);
					}
					else if (info.IsCharSetAuto) {
						output.WriteSpace();
						output.Write("autochar", TextTokenKind.Keyword);
					}
					else if (info.IsCharSetUnicode) {
						output.WriteSpace();
						output.Write("unicode", TextTokenKind.Keyword);
					}

					if (info.SupportsLastError) {
						output.WriteSpace();
						output.Write("lasterr", TextTokenKind.Keyword);
					}

					if (info.IsCallConvCdecl) {
						output.WriteSpace();
						output.Write("cdecl", TextTokenKind.Keyword);
					}
					else if (info.IsCallConvFastcall) {
						output.WriteSpace();
						output.Write("fastcall", TextTokenKind.Keyword);
					}
					else if (info.IsCallConvStdcall) {
						output.WriteSpace();
						output.Write("stdcall", TextTokenKind.Keyword);
					}
					else if (info.IsCallConvThiscall) {
						output.WriteSpace();
						output.Write("thiscall", TextTokenKind.Keyword);
					}
					else if (info.IsCallConvWinapi) {
						output.WriteSpace();
						output.Write("winapi", TextTokenKind.Keyword);
					}

					output.Write(")", TextTokenKind.Operator);
				}
				output.WriteSpace();
			}
			
			output.WriteLine();
			output.Indent();
			if (method.ExplicitThis) {
				output.Write("instance", TextTokenKind.Keyword);
				output.WriteSpace();
				output.Write("explicit", TextTokenKind.Keyword);
				output.WriteSpace();
			} else if (method.HasThis) {
				output.Write("instance", TextTokenKind.Keyword);
				output.WriteSpace();
			}
			
			//call convention
			WriteEnum(method.CallingConvention & (CallingConvention)0x1f, callingConvention);
			
			//return type
			method.ReturnType.WriteTo(output);
			output.WriteSpace();
			if (method.Parameters.ReturnParameter.HasParamDef && method.Parameters.ReturnParameter.ParamDef.HasMarshalType) {
				WriteMarshalInfo(method.Parameters.ReturnParameter.ParamDef.MarshalType);
			}
			
			if (method.IsCompilerControlled) {
				output.Write(DisassemblerHelpers.Escape(method.Name + "$PST" + method.MDToken.ToInt32().ToString("X8")), TextTokenKindUtils.GetTextTokenType(method));
			} else {
				output.Write(DisassemblerHelpers.Escape(method.Name), TextTokenKindUtils.GetTextTokenType(method));
			}
			
			WriteTypeParameters(output, method);
			
			//( params )
			output.WriteSpace();
			output.Write("(", TextTokenKind.Operator);
			if (method.Parameters.GetNumberOfNormalParameters() > 0) {
				output.WriteLine();
				output.Indent();
				WriteParameters(method.Parameters);
				output.Unindent();
			}
			output.Write(")", TextTokenKind.Operator);
			output.WriteSpace();
			//cil managed
			WriteEnum(method.ImplAttributes & MethodImplAttributes.CodeTypeMask, methodCodeType);
			if ((method.ImplAttributes & MethodImplAttributes.ManagedMask) == MethodImplAttributes.Managed)
				output.Write("managed", TextTokenKind.Keyword);
			else
				output.Write("unmanaged", TextTokenKind.Keyword);
			output.WriteSpace();
			WriteFlags(method.ImplAttributes & ~(MethodImplAttributes.CodeTypeMask | MethodImplAttributes.ManagedMask), methodImpl);
			
			output.Unindent();
			OpenBlock(defaultCollapsed: isInType);
			WriteAttributes(method.CustomAttributes);
			if (method.HasOverrides) {
				foreach (var methodOverride in method.Overrides) {
					output.Write(".override", TextTokenKind.ILDirective);
					output.WriteSpace();
					output.Write("method", TextTokenKind.Keyword);
					output.WriteSpace();
					methodOverride.MethodDeclaration.WriteMethodTo(output);
					output.WriteLine();
				}
			}
			WriteParameterAttributes(0, method.Parameters.ReturnParameter);
			foreach (var p in method.Parameters) {
				if (p.IsHiddenThisParameter)
					continue;
				WriteParameterAttributes(p.MethodSigIndex + 1, p);
			}
			WriteSecurityDeclarations(method);
			
			if (method.HasBody) {
				// create IL code mappings - used in debugger
				MemberMapping debugSymbols = new MemberMapping(method);
				methodBodyDisassembler.Disassemble(method, debugSymbols);
				output.AddDebugSymbols(debugSymbols);
			}
			
			CloseBlock("end of method " + DisassemblerHelpers.Escape(method.DeclaringType.Name) + "::" + DisassemblerHelpers.Escape(method.Name));
		}
		
		#region Write Security Declarations
		void WriteSecurityDeclarations(IHasDeclSecurity secDeclProvider)
		{
			if (!secDeclProvider.HasDeclSecurities)
				return;
			foreach (var secdecl in secDeclProvider.DeclSecurities) {
				output.Write(".permissionset", TextTokenKind.ILDirective);
				output.WriteSpace();
				switch (secdecl.Action) {
					case SecurityAction.Request:
					output.Write("request", TextTokenKind.Keyword);
						break;
					case SecurityAction.Demand:
						output.Write("demand", TextTokenKind.Keyword);
						break;
					case SecurityAction.Assert:
						output.Write("assert", TextTokenKind.Keyword);
						break;
					case SecurityAction.Deny:
						output.Write("deny", TextTokenKind.Keyword);
						break;
					case SecurityAction.PermitOnly:
						output.Write("permitonly", TextTokenKind.Keyword);
						break;
					case SecurityAction.LinkDemand:
						output.Write("linkcheck", TextTokenKind.Keyword);
						break;
					case SecurityAction.InheritDemand:
						output.Write("inheritcheck", TextTokenKind.Keyword);
						break;
					case SecurityAction.RequestMinimum:
						output.Write("reqmin", TextTokenKind.Keyword);
						break;
					case SecurityAction.RequestOptional:
						output.Write("reqopt", TextTokenKind.Keyword);
						break;
					case SecurityAction.RequestRefuse:
						output.Write("reqrefuse", TextTokenKind.Keyword);
						break;
					case SecurityAction.PreJitGrant:
						output.Write("prejitgrant", TextTokenKind.Keyword);
						break;
					case SecurityAction.PreJitDeny:
						output.Write("prejitdeny", TextTokenKind.Keyword);
						break;
					case SecurityAction.NonCasDemand:
						output.Write("noncasdemand", TextTokenKind.Keyword);
						break;
					case SecurityAction.NonCasLinkDemand:
						output.Write("noncaslinkdemand", TextTokenKind.Keyword);
						break;
					case SecurityAction.NonCasInheritance:
						output.Write("noncasinheritance", TextTokenKind.Keyword);
						break;
					default:
						output.Write(secdecl.Action.ToString(), TextTokenKind.Keyword);
						break;
				}
				output.WriteSpace();
				output.Write("=", TextTokenKind.Operator);
				output.WriteSpace();
				output.WriteLineLeftBrace();
				output.Indent();
				for (int i = 0; i < secdecl.SecurityAttributes.Count; i++) {
					SecurityAttribute sa = secdecl.SecurityAttributes[i];
					if (sa.AttributeType != null && sa.AttributeType.Scope == sa.AttributeType.Module) {
						output.Write("class", TextTokenKind.Keyword);
						output.WriteSpace();
						output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(sa.AttributeType)), TextTokenKind.Text);
					} else {
						sa.AttributeType.WriteTo(output, ILNameSyntax.TypeName);
					}
					output.WriteSpace();
					output.Write("=", TextTokenKind.Operator);
					output.WriteSpace();
					output.WriteLeftBrace();
					if (sa.HasNamedArguments) {
						output.WriteLine();
						output.Indent();
						
						var attrType = sa.AttributeType.ResolveTypeDef();
						foreach (var na in sa.Fields) {
							output.Write("field", TextTokenKind.Keyword);
							output.WriteSpace();
							WriteSecurityDeclarationArgument(attrType, na);
							output.WriteLine();
						}
						
						foreach (var na in sa.Properties) {
							output.Write("property", TextTokenKind.Keyword);
							output.WriteSpace();
							WriteSecurityDeclarationArgument(attrType, na);
							output.WriteLine();
						}
						
						output.Unindent();
					}
					output.WriteRightBrace();
					
					if (i + 1< secdecl.SecurityAttributes.Count)
						output.Write(",", TextTokenKind.Operator);
					output.WriteLine();
				}
				output.Unindent();
				output.WriteLineRightBrace();
			}
		}
		
		void WriteSecurityDeclarationArgument(TypeDef attrType, CANamedArgument na)
		{
			object reference = null;
			if (attrType != null) {
				if (na.IsField)
					reference = attrType.FindField(na.Name, new FieldSig(na.Type));
				else
					reference = attrType.FindProperty(na.Name, PropertySig.CreateInstance(na.Type));
			}

			TypeSig type = na.Argument.Type;
			if (type != null && (type.ElementType == ElementType.Class || type.ElementType == ElementType.ValueType)) {
				output.Write("enum", TextTokenKind.Keyword);
				output.WriteSpace();
				if (type.Scope != type.Module) {
					output.Write("class", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write(DisassemblerHelpers.Escape(GetAssemblyQualifiedName(type)), TextTokenKind.Text);
				} else {
					type.WriteTo(output, ILNameSyntax.TypeName);
				}
			} else {
				type.WriteTo(output);
			}
			output.WriteSpace();
			output.WriteReference(DisassemblerHelpers.Escape(na.Name), reference, na.IsField ? TextTokenKind.InstanceField : TextTokenKind.InstanceProperty);
			output.WriteSpace();
			output.Write("=", TextTokenKind.Operator);
			output.WriteSpace();
			if (na.Argument.Value is UTF8String) {
				// secdecls use special syntax for strings
				output.Write("string", TextTokenKind.Keyword);
				output.Write("(", TextTokenKind.Operator);
				output.Write(string.Format("'{0}'", NRefactory.CSharp.TextWriterTokenWriter.ConvertString((UTF8String)na.Argument.Value).Replace("'", "\'")), TextTokenKind.String);
				output.Write(")", TextTokenKind.Operator);
			} else {
				WriteConstant(na.Argument.Value);
			}
		}
		
		string GetAssemblyQualifiedName(IType type)
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
			output.Write("marshal", TextTokenKind.Keyword);
			output.Write("(", TextTokenKind.Operator);
			if (marshalInfo != null)
				WriteNativeType(marshalInfo.NativeType, marshalInfo);
			output.Write(")", TextTokenKind.Operator);
			output.WriteSpace();
		}
		
		void WriteNativeType(NativeType nativeType, MarshalType marshalInfo = null)
		{
			switch (nativeType) {
				case NativeType.NotInitialized:
					break;
				case NativeType.Boolean:
					output.Write("bool", TextTokenKind.Keyword);
					break;
				case NativeType.I1:
					output.Write("int8", TextTokenKind.Keyword);
					break;
				case NativeType.U1:
					output.Write("unsigned", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("int8", TextTokenKind.Keyword);
					break;
				case NativeType.I2:
					output.Write("int16", TextTokenKind.Keyword);
					break;
				case NativeType.U2:
					output.Write("unsigned", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("int16", TextTokenKind.Keyword);
					break;
				case NativeType.I4:
					output.Write("int32", TextTokenKind.Keyword);
					break;
				case NativeType.U4:
					output.Write("unsigned", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("int32", TextTokenKind.Keyword);
					break;
				case NativeType.I8:
					output.Write("int64", TextTokenKind.Keyword);
					break;
				case NativeType.U8:
					output.Write("unsigned", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("int64", TextTokenKind.Keyword);
					break;
				case NativeType.R4:
					output.Write("float32", TextTokenKind.Keyword);
					break;
				case NativeType.R8:
					output.Write("float64", TextTokenKind.Keyword);
					break;
				case NativeType.LPStr:
					output.Write("lpstr", TextTokenKind.Keyword);
					break;
				case NativeType.Int:
					output.Write("int", TextTokenKind.Keyword);
					break;
				case NativeType.UInt:
					output.Write("unsigned", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("int", TextTokenKind.Keyword);
					break;
				case NativeType.Func:
					output.Write("method", TextTokenKind.Keyword);
					break;
				case NativeType.Array:
					ArrayMarshalType ami = marshalInfo as ArrayMarshalType;
					if (ami == null)
						goto default;
					if (ami.ElementType != NativeType.Max)
						WriteNativeType(ami.ElementType);
					output.Write("[", TextTokenKind.Operator);
					if (ami.Flags == 0) {
						output.Write(ami.Size.ToString(), TextTokenKind.Number);
					} else {
						if (ami.Size >= 0)
							output.Write(ami.Size.ToString(), TextTokenKind.Number);
						output.WriteSpace();
						output.Write("+", TextTokenKind.Operator);
						output.WriteSpace();
						output.Write(ami.ParamNumber.ToString(), TextTokenKind.Number);
					}
					output.Write("]", TextTokenKind.Operator);
					break;
				case NativeType.Currency:
					output.Write("currency", TextTokenKind.Keyword);
					break;
				case NativeType.BStr:
					output.Write("bstr", TextTokenKind.Keyword);
					break;
				case NativeType.LPWStr:
					output.Write("lpwstr", TextTokenKind.Keyword);
					break;
				case NativeType.LPTStr:
					output.Write("lptstr", TextTokenKind.Keyword);
					break;
				case NativeType.FixedSysString:
					var fsmi = marshalInfo as FixedSysStringMarshalType;
					output.Write("fixed", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("sysstring", TextTokenKind.Keyword);
					if (fsmi != null && fsmi.IsSizeValid) {
						output.Write("[", TextTokenKind.Operator);
						output.Write(string.Format("{0}", fsmi.Size), TextTokenKind.Number);
						output.Write("]", TextTokenKind.Operator);
					}
					break;
				case NativeType.IUnknown:
				case NativeType.IDispatch:
				case NativeType.IntF:
					if (nativeType == NativeType.IUnknown)
						output.Write("iunknown", TextTokenKind.Keyword);
					else if (nativeType == NativeType.IDispatch)
						output.Write("idispatch", TextTokenKind.Keyword);
					else if (nativeType == NativeType.IntF)
						output.Write("interface", TextTokenKind.Keyword);
					else
						throw new InvalidOperationException();
					var imti = marshalInfo as InterfaceMarshalType;
					if (imti != null && imti.IsIidParamIndexValid) {
						output.Write("(", TextTokenKind.Operator);
						output.Write("iidparam", TextTokenKind.Keyword);
						output.WriteSpace();
						output.Write("=", TextTokenKind.Operator);
						output.WriteSpace();
						output.Write(imti.IidParamIndex.ToString(), TextTokenKind.Number);
						output.Write(")", TextTokenKind.Operator);
					}
					break;
				case NativeType.Struct:
					output.Write("struct", TextTokenKind.Keyword);
					break;
				case NativeType.SafeArray:
					output.Write("safearray", TextTokenKind.Keyword);
					output.WriteSpace();
					SafeArrayMarshalType sami = marshalInfo as SafeArrayMarshalType;
					if (sami != null && sami.IsVariantTypeValid) {
						switch (sami.VariantType & VariantType.TypeMask) {
							case VariantType.None:
								break;
							case VariantType.Null:
								output.Write("null", TextTokenKind.Keyword);
								break;
							case VariantType.I2:
								output.Write("int16", TextTokenKind.Keyword);
								break;
							case VariantType.I4:
								output.Write("int32", TextTokenKind.Keyword);
								break;
							case VariantType.R4:
								output.Write("float32", TextTokenKind.Keyword);
								break;
							case VariantType.R8:
								output.Write("float64", TextTokenKind.Keyword);
								break;
							case VariantType.CY:
								output.Write("currency", TextTokenKind.Keyword);
								break;
							case VariantType.Date:
								output.Write("date", TextTokenKind.Keyword);
								break;
							case VariantType.BStr:
								output.Write("bstr", TextTokenKind.Keyword);
								break;
							case VariantType.Dispatch:
								output.Write("idispatch", TextTokenKind.Keyword);
								break;
							case VariantType.Error:
								output.Write("error", TextTokenKind.Keyword);
								break;
							case VariantType.Bool:
								output.Write("bool", TextTokenKind.Keyword);
								break;
							case VariantType.Variant:
								output.Write("variant", TextTokenKind.Keyword);
								break;
							case VariantType.Unknown:
								output.Write("iunknown", TextTokenKind.Keyword);
								break;
							case VariantType.Decimal:
								output.Write("decimal", TextTokenKind.Keyword);
								break;
							case VariantType.I1:
								output.Write("int8", TextTokenKind.Keyword);
								break;
							case VariantType.UI1:
								output.Write("unsigned", TextTokenKind.Keyword);
								output.WriteSpace();
								output.Write("int8", TextTokenKind.Keyword);
								break;
							case VariantType.UI2:
								output.Write("unsigned", TextTokenKind.Keyword);
								output.WriteSpace();
								output.Write("int16", TextTokenKind.Keyword);
								break;
							case VariantType.UI4:
								output.Write("unsigned", TextTokenKind.Keyword);
								output.WriteSpace();
								output.Write("int32", TextTokenKind.Keyword);
								break;
							case VariantType.I8:
								output.Write("int64", TextTokenKind.Keyword);
								break;
							case VariantType.UI8:
								output.Write("unsigned", TextTokenKind.Keyword);
								output.WriteSpace();
								output.Write("int64", TextTokenKind.Keyword);
								break;
							case VariantType.Int:
								output.Write("int", TextTokenKind.Keyword);
								break;
							case VariantType.UInt:
								output.Write("unsigned", TextTokenKind.Keyword);
								output.WriteSpace();
								output.Write("int", TextTokenKind.Keyword);
								break;
							case VariantType.Void:
								output.Write("void", TextTokenKind.Keyword);
								break;
							case VariantType.HResult:
								output.Write("hresult", TextTokenKind.Keyword);
								break;
							case VariantType.Ptr:
								output.Write("*", TextTokenKind.Operator);
								break;
							case VariantType.SafeArray:
								output.Write("safearray", TextTokenKind.Keyword);
								break;
							case VariantType.CArray:
								output.Write("carray", TextTokenKind.Keyword);
								break;
							case VariantType.UserDefined:
								output.Write("userdefined", TextTokenKind.Keyword);
								break;
							case VariantType.LPStr:
								output.Write("lpstr", TextTokenKind.Keyword);
								break;
							case VariantType.LPWStr:
								output.Write("lpwstr", TextTokenKind.Keyword);
								break;
							case VariantType.Record:
								output.Write("record", TextTokenKind.Keyword);
								break;
							case VariantType.FileTime:
								output.Write("filetime", TextTokenKind.Keyword);
								break;
							case VariantType.Blob:
								output.Write("blob", TextTokenKind.Keyword);
								break;
							case VariantType.Stream:
								output.Write("stream", TextTokenKind.Keyword);
								break;
							case VariantType.Storage:
								output.Write("storage", TextTokenKind.Keyword);
								break;
							case VariantType.StreamedObject:
								output.Write("streamed_object", TextTokenKind.Keyword);
								break;
							case VariantType.StoredObject:
								output.Write("stored_object", TextTokenKind.Keyword);
								break;
							case VariantType.BlobObject:
								output.Write("blob_object", TextTokenKind.Keyword);
								break;
							case VariantType.CF:
								output.Write("cf", TextTokenKind.Keyword);
								break;
							case VariantType.CLSID:
								output.Write("clsid", TextTokenKind.Keyword);
								break;
							case VariantType.IntPtr:
							case VariantType.UIntPtr:
							case VariantType.VersionedStream:
							case VariantType.BStrBlob:
							default:
								output.Write((sami.VariantType & VariantType.TypeMask).ToString(), TextTokenKind.Keyword);
								break;
						}
						if ((sami.VariantType & VariantType.ByRef) != 0)
							output.Write("&", TextTokenKind.Operator);
						if ((sami.VariantType & VariantType.Array) != 0)
							output.Write("[]", TextTokenKind.Operator);
						if ((sami.VariantType & VariantType.Vector) != 0) {
							output.WriteSpace();
							output.Write("vector", TextTokenKind.Keyword);
						}
						if (sami.IsUserDefinedSubTypeValid) {
							output.Write(",", TextTokenKind.Operator);
							output.WriteSpace();
							output.Write("\"" + NRefactory.CSharp.TextWriterTokenWriter.ConvertString(sami.UserDefinedSubType.FullName) + "\"", TextTokenKind.String);
						}
					}
					break;
				case NativeType.FixedArray:
					output.Write("fixed", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("array", TextTokenKind.Keyword);
					FixedArrayMarshalType fami = marshalInfo as FixedArrayMarshalType;
					if (fami != null) {
						if (fami.IsSizeValid) {
							output.Write("[", TextTokenKind.Operator);
							output.Write(fami.Size.ToString(), TextTokenKind.Number);
							output.Write("]", TextTokenKind.Operator);
						}
						if (fami.IsElementTypeValid) {
							output.WriteSpace();
							WriteNativeType(fami.ElementType);
						}
					}
					break;
				case NativeType.ByValStr:
					output.Write("byvalstr", TextTokenKind.Keyword);
					break;
				case NativeType.ANSIBStr:
					output.Write("ansi", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("bstr", TextTokenKind.Keyword);
					break;
				case NativeType.TBStr:
					output.Write("tbstr", TextTokenKind.Keyword);
					break;
				case NativeType.VariantBool:
					output.Write("variant", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("bool", TextTokenKind.Keyword);
					break;
				case NativeType.ASAny:
					output.Write("as", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("any", TextTokenKind.Keyword);
					break;
				case NativeType.LPStruct:
					output.Write("lpstruct", TextTokenKind.Keyword);
					break;
				case NativeType.CustomMarshaler:
					CustomMarshalType cmi = marshalInfo as CustomMarshalType;
					if (cmi == null)
						goto default;
					output.Write("custom", TextTokenKind.Keyword);
					output.Write("(", TextTokenKind.Operator);
					output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.CustomMarshaler == null ? string.Empty : cmi.CustomMarshaler.FullName)), TextTokenKind.String);
					output.Write(",", TextTokenKind.Operator);
					output.WriteSpace();
					output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.Cookie)), TextTokenKind.String);
					if (!UTF8String.IsNullOrEmpty(cmi.Guid) || !UTF8String.IsNullOrEmpty(cmi.NativeTypeName)) {
						output.Write(",", TextTokenKind.Operator);
						output.WriteSpace();
						output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.Guid)), TextTokenKind.String);
						output.Write(",", TextTokenKind.Operator);
						output.WriteSpace();
						output.Write(string.Format("\"{0}\"", NRefactory.CSharp.TextWriterTokenWriter.ConvertString(cmi.NativeTypeName)), TextTokenKind.String);
					}
					output.Write(")", TextTokenKind.Operator);
					break;
				case NativeType.Error:
					output.Write("error", TextTokenKind.Keyword);
					break;
				case NativeType.Void:
					output.Write("void", TextTokenKind.Keyword);
					break;
				case NativeType.SysChar:
					output.Write("syschar", TextTokenKind.Keyword);
					break;
				case NativeType.Variant:
					output.Write("variant", TextTokenKind.Keyword);
					break;
				case NativeType.Decimal:
					output.Write("decimal", TextTokenKind.Keyword);
					break;
				case NativeType.Date:
					output.Write("date", TextTokenKind.Keyword);
					break;
				case NativeType.ObjectRef:
					output.Write("objectref", TextTokenKind.Keyword);
					break;
				case NativeType.NestedStruct:
					output.Write("nested", TextTokenKind.Keyword);
					output.WriteSpace();
					output.Write("struct", TextTokenKind.Keyword);
					break;
				case NativeType.Ptr:
				case NativeType.IInspectable:
				case NativeType.HString:
				default:
					output.Write(nativeType.ToString(), TextTokenKind.Keyword);
					break;
			}
		}
		#endregion
		
		void WriteParameters(IList<Parameter> parameters)
		{
			for (int i = 0; i < parameters.Count; i++) {
				var p = parameters[i];
				if (p.IsHiddenThisParameter)
					continue;
				var paramDef = p.ParamDef;
				if (paramDef != null) {
					if (paramDef.IsIn) {
						output.Write("[", TextTokenKind.Operator);
						output.Write("in", TextTokenKind.Keyword);
						output.Write("]", TextTokenKind.Operator);
						output.WriteSpace();
					}
					if (paramDef.IsOut) {
						output.Write("[", TextTokenKind.Operator);
						output.Write("out", TextTokenKind.Keyword);
						output.Write("]", TextTokenKind.Operator);
						output.WriteSpace();
					}
					if (paramDef.IsOptional) {
						output.Write("[", TextTokenKind.Operator);
						output.Write("opt", TextTokenKind.Keyword);
						output.Write("]", TextTokenKind.Operator);
						output.WriteSpace();
					}
				}
				p.Type.WriteTo(output);
				output.WriteSpace();
				if (paramDef != null && paramDef.MarshalType != null) {
					WriteMarshalInfo(paramDef.MarshalType);
				}
				output.WriteDefinition(DisassemblerHelpers.Escape(p.Name), p, TextTokenKind.Parameter);
				if (i < parameters.Count - 1)
					output.Write(",", TextTokenKind.Operator);
				output.WriteLine();
			}
		}
		
		bool HasParameterAttributes(Parameter p)
		{
			return p.ParamDef != null && (p.ParamDef.HasConstant || p.ParamDef.HasCustomAttributes);
		}
		
		void WriteParameterAttributes(int index, Parameter p)
		{
			if (!HasParameterAttributes(p))
				return;
			output.Write(".param", TextTokenKind.ILDirective);
			output.WriteSpace();
			output.Write("[", TextTokenKind.Operator);
			output.Write(string.Format("{0}", index), TextTokenKind.Number);
			output.Write("]", TextTokenKind.Operator);
			if (p.HasParamDef && p.ParamDef.HasConstant) {
				output.WriteSpace();
				output.Write("=", TextTokenKind.Operator);
				output.WriteSpace();
				WriteConstant(p.ParamDef.Constant.Value);
			}
			output.WriteLine();
			if (p.HasParamDef)
				WriteAttributes(p.ParamDef.CustomAttributes);
		}
		
		void WriteConstant(object constant)
		{
			if (constant == null) {
				output.Write("nullref", TextTokenKind.Keyword);
			} else {
				TypeSig typeSig;
				string typeName = DisassemblerHelpers.PrimitiveTypeName(constant.GetType().FullName, options.OwnerModule, out typeSig);
				if (typeName != null && typeName != "string") {
					DisassemblerHelpers.WriteKeyword(output, typeName, typeSig.ToTypeDefOrRef());
					output.Write("(", TextTokenKind.Operator);
					float? cf = constant as float?;
					double? cd = constant as double?;
					if (cf.HasValue && (float.IsNaN(cf.Value) || float.IsInfinity(cf.Value))) {
						output.Write(string.Format("0x{0:x8}", BitConverter.ToInt32(BitConverter.GetBytes(cf.Value), 0)), TextTokenKind.Number);
					} else if (cd.HasValue && (double.IsNaN(cd.Value) || double.IsInfinity(cd.Value))) {
						output.Write(string.Format("0x{0:x16}", BitConverter.DoubleToInt64Bits(cd.Value)), TextTokenKind.Number);
					} else {
						DisassemblerHelpers.WriteOperand(output, constant);
					}
					output.Write(")", TextTokenKind.Operator);
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
			WriteXmlDocComment(field);
			AddComment(field);
			output.WriteDefinition(".field", field, TextTokenKind.ILDirective, false);
			output.WriteSpace();
			if (field.HasLayoutInfo && field.FieldOffset.HasValue) {
				output.Write("[", TextTokenKind.Operator);
				output.Write(string.Format("{0}", field.FieldOffset), TextTokenKind.Number);
				output.Write("]", TextTokenKind.Operator);
				output.WriteSpace();
			}
			WriteEnum(field.Attributes & FieldAttributes.FieldAccessMask, fieldVisibility);
			const FieldAttributes hasXAttributes = FieldAttributes.HasDefault | FieldAttributes.HasFieldMarshal | FieldAttributes.HasFieldRVA;
			WriteFlags(field.Attributes & ~(FieldAttributes.FieldAccessMask | hasXAttributes), fieldAttributes);
			if (field.HasMarshalType) {
				WriteMarshalInfo(field.MarshalType);
			}
			field.FieldType.WriteTo(output);
			output.WriteSpace();
			output.Write(DisassemblerHelpers.Escape(field.Name), TextTokenKindUtils.GetTextTokenType(field));
			if ((field.Attributes & FieldAttributes.HasFieldRVA) == FieldAttributes.HasFieldRVA) {
				output.WriteSpace();
				output.Write("at", TextTokenKind.Keyword);
				output.WriteSpace();
				output.Write(string.Format("I_{0:x8}", (uint)field.RVA), TextTokenKind.Text);
				uint fieldSize;
				if (field.GetFieldSize(out fieldSize)) {
					output.WriteSpace();
					output.Write(string.Format("// {0} (0x{0:x4}) bytes", fieldSize), TextTokenKind.Comment);
				}
			}
			if (field.HasConstant) {
				output.WriteSpace();
				output.Write("=", TextTokenKind.Operator);
				output.WriteSpace();
				WriteConstant(field.Constant.Value);
			}
			output.WriteLine();
			if (field.HasCustomAttributes) {
				WriteAttributes(field.CustomAttributes);
			}
		}
		#endregion
		
		#region Disassemble Property
		EnumNameCollection<PropertyAttributes> propertyAttributes = new EnumNameCollection<PropertyAttributes>() {
			{ PropertyAttributes.SpecialName, "specialname" },
			{ PropertyAttributes.RTSpecialName, "rtspecialname" },
			{ PropertyAttributes.HasDefault, "hasdefault" },
		};
		
		public void DisassembleProperty(PropertyDef property, bool full = true)
		{
			// set current member
			currentMember = property;
			
			WriteXmlDocComment(property);
			AddComment(property);
			output.WriteDefinition(".property", property, TextTokenKind.ILDirective, false);
			output.WriteSpace();
			WriteFlags(property.Attributes, propertyAttributes);
			if (property.PropertySig != null && property.PropertySig.HasThis) {
				output.Write("instance", TextTokenKind.Keyword);
				output.WriteSpace();
			}
			property.PropertySig.GetRetType().WriteTo(output);
			output.WriteSpace();
			output.Write(DisassemblerHelpers.Escape(property.Name), TextTokenKindUtils.GetTextTokenType(property));
			
			output.Write("(", TextTokenKind.Operator);
			var parameters = new List<Parameter>(property.GetParameters());
			if (parameters.GetNumberOfNormalParameters() > 0) {
				output.WriteLine();
				output.Indent();
				WriteParameters(parameters);
				output.Unindent();
			}
			output.Write(")", TextTokenKind.Operator);

			if (full) {
				OpenBlock(false);
				WriteAttributes(property.CustomAttributes);

				foreach (var method in property.GetMethods)
					WriteNestedMethod(".get", method);
				foreach (var method in property.SetMethods)
					WriteNestedMethod(".set", method);
				foreach (var method in property.OtherMethods)
					WriteNestedMethod(".other", method);
				CloseBlock();
			}
			else {
				output.WriteSpace();
				output.WriteLeftBrace();

				if (property.GetMethods.Count > 0) {
					output.WriteSpace();
					output.Write(".get", TextTokenKind.Keyword);
					output.Write(";", TextTokenKind.Operator);
				}

				if (property.SetMethods.Count > 0) {
					output.WriteSpace();
					output.Write(".set", TextTokenKind.Keyword);
					output.Write(";", TextTokenKind.Operator);
				}

				output.WriteSpace();
				output.WriteRightBrace();
			}
		}
		
		void WriteNestedMethod(string keyword, MethodDef method)
		{
			if (method == null)
				return;
			
			AddComment(method);
			output.Write(keyword, TextTokenKind.ILDirective);
			output.WriteSpace();
			method.WriteMethodTo(output);
			output.WriteLine();
		}
		#endregion
		
		#region Disassemble Event
		EnumNameCollection<EventAttributes> eventAttributes = new EnumNameCollection<EventAttributes>() {
			{ EventAttributes.SpecialName, "specialname" },
			{ EventAttributes.RTSpecialName, "rtspecialname" },
		};
		
		public void DisassembleEvent(EventDef ev, bool full = true)
		{
			// set current member
			currentMember = ev;
			
			WriteXmlDocComment(ev);
			AddComment(ev);
			output.WriteDefinition(".event", ev, TextTokenKind.ILDirective, false);
			output.WriteSpace();
			WriteFlags(ev.Attributes, eventAttributes);
			ev.EventType.WriteTo(output, ILNameSyntax.TypeName);
			output.WriteSpace();
			output.Write(DisassemblerHelpers.Escape(ev.Name), TextTokenKindUtils.GetTextTokenType(ev));

			if (full) {
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
			else {
				output.WriteSpace();
				output.WriteLeftBrace();

				if (ev.AddMethod != null) {
					output.WriteSpace();
					output.Write(".addon", TextTokenKind.Keyword);
					output.Write(";", TextTokenKind.Operator);
				}

				if (ev.RemoveMethod != null) {
					output.WriteSpace();
					output.Write(".removeon", TextTokenKind.Keyword);
					output.Write(";", TextTokenKind.Operator);
				}

				if (ev.InvokeMethod != null) {
					output.WriteSpace();
					output.Write(".fire", TextTokenKind.Keyword);
					output.Write(";", TextTokenKind.Operator);
				}

				output.WriteSpace();
				output.WriteRightBrace();
			}
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

		void AddTokenComment(IMDTokenProvider member, string extra = null)
		{
			if (!options.ShowTokenAndRvaComments)
				return;

			StartComment();
			WriteToken(member);
			output.WriteLine();
		}

		void StartComment()
		{
			output.Write("//", TextTokenKind.Comment);
		}

		void WriteToken(IMDTokenProvider member)
		{
			output.Write(" Token: ", TextTokenKind.Comment);
			output.WriteReference(string.Format("0x{0:X8}", member.MDToken.Raw), new TokenReference(options.OwnerModule, member.MDToken.Raw), TextTokenKind.Comment, false);
			output.Write(" RID: ", TextTokenKind.Comment);
			output.Write(string.Format("{0}", member.MDToken.Rid), TextTokenKind.Comment);
		}

		void WriteRVA(IMemberDef member)
		{
			uint rva;
			long fileOffset;
			member.GetRVA(out rva, out fileOffset);
			string extra = string.Empty;
			if (rva == 0)
				return;

			var mod = member.Module;
			var filename = mod == null ? null : mod.Location;
			output.Write(" RVA: ", TextTokenKind.Comment);
			output.WriteReference(string.Format("0x{0:X8}", rva), new AddressReference(filename, true, rva, 0), TextTokenKind.Comment, false);
			output.Write(" File Offset: ", TextTokenKind.Comment);
			output.WriteReference(string.Format("0x{0:X8}", fileOffset), new AddressReference(filename, false, (ulong)fileOffset, 0), TextTokenKind.Comment, false);
		}

		void AddComment(IMemberDef member)
		{
			if (!options.ShowTokenAndRvaComments)
				return;

			StartComment();
			WriteToken(member);
			WriteRVA(member);
			output.WriteLine();
		}
		
		public void DisassembleType(TypeDef type)
		{
			// start writing IL
			WriteXmlDocComment(type);
			AddComment(type);
			output.WriteDefinition(".class", type, TextTokenKind.ILDirective, false);
			output.WriteSpace();
			
			if ((type.Attributes & TypeAttributes.ClassSemanticMask) == TypeAttributes.Interface) {
				output.Write("interface", TextTokenKind.Keyword);
				output.WriteSpace();
			}
			WriteEnum(type.Attributes & TypeAttributes.VisibilityMask, typeVisibility);
			WriteEnum(type.Attributes & TypeAttributes.LayoutMask, typeLayout);
			WriteEnum(type.Attributes & TypeAttributes.StringFormatMask, typeStringFormat);
			const TypeAttributes masks = TypeAttributes.ClassSemanticMask | TypeAttributes.VisibilityMask | TypeAttributes.LayoutMask | TypeAttributes.StringFormatMask;
			WriteFlags(type.Attributes & ~masks, typeAttributes);
			
			output.Write(DisassemblerHelpers.Escape(type.DeclaringType != null ? type.Name.String : type.FullName), TextTokenKindUtils.GetTextTokenType(type));
			WriteTypeParameters(output, type);
			output.WriteLine();
			
			if (type.BaseType != null) {
				output.Indent();
				output.Write("extends", TextTokenKind.Keyword);
				output.WriteSpace();
				type.BaseType.WriteTo(output, ILNameSyntax.TypeName);
				output.WriteLine();
				output.Unindent();
			}
			if (type.HasInterfaces) {
				output.Indent();
				for (int index = 0; index < type.Interfaces.Count; index++) {
					if (index > 0)
						output.WriteLine(",", TextTokenKind.Operator);
					if (index == 0) {
						output.Write("implements", TextTokenKind.Keyword);
						output.WriteSpace();
					}
					else
						output.Write("           ", TextTokenKind.Text);
					type.Interfaces[index].Interface.WriteTo(output, ILNameSyntax.TypeName);
				}
				output.WriteLine();
				output.Unindent();
			}
			
			output.WriteLineLeftBrace();
			output.Indent();
			bool oldIsInType = isInType;
			isInType = true;
			WriteAttributes(type.CustomAttributes);
			WriteSecurityDeclarations(type);
			if (type.HasClassLayout) {
				output.Write(".pack", TextTokenKind.ILDirective);
				output.WriteSpace();
				output.WriteLine(string.Format("{0}", type.PackingSize), TextTokenKind.Number);
				output.Write(".size", TextTokenKind.ILDirective);
				output.WriteSpace();
				output.WriteLine(string.Format("{0}", type.ClassSize), TextTokenKind.Number);
				output.WriteLine();
			}
			if (type.HasNestedTypes) {
				output.WriteLine("// Nested Types", TextTokenKind.Comment);
				foreach (var nestedType in type.GetNestedTypes(options.SortMembers)) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleType(nestedType);
					output.WriteLine();
				}
				output.WriteLine();
			}
			if (type.HasFields) {
				output.WriteLine("// Fields", TextTokenKind.Comment);
				foreach (var field in type.GetFields(options.SortMembers)) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleField(field);
				}
				output.WriteLine();
			}
			if (type.HasMethods) {
				output.WriteLine("// Methods", TextTokenKind.Comment);
				foreach (var m in type.GetMethods(options.SortMembers)) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleMethod(m);
					output.WriteLine();
				}
			}
			if (type.HasEvents) {
				output.WriteLine("// Events", TextTokenKind.Comment);
				foreach (var ev in type.GetEvents(options.SortMembers)) {
					options.CancellationToken.ThrowIfCancellationRequested();
					DisassembleEvent(ev);
					output.WriteLine();
				}
			}
			if (type.HasProperties) {
				output.WriteLine("// Properties", TextTokenKind.Comment);
				foreach (var prop in type.GetProperties(options.SortMembers)) {
					options.CancellationToken.ThrowIfCancellationRequested();
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
				output.Write("<", TextTokenKind.Operator);
				for (int i = 0; i < p.GenericParameters.Count; i++) {
					if (i > 0) {
						output.Write(",", TextTokenKind.Operator);
						output.WriteSpace();
					}
					GenericParam gp = p.GenericParameters[i];
					if (gp.HasReferenceTypeConstraint) {
						output.Write("class", TextTokenKind.Keyword);
						output.WriteSpace();
					} else if (gp.HasNotNullableValueTypeConstraint) {
						output.Write("valuetype", TextTokenKind.Keyword);
						output.WriteSpace();
					}
					if (gp.HasDefaultConstructorConstraint) {
						output.Write(".ctor", TextTokenKind.Keyword);
						output.WriteSpace();
					}
					if (gp.HasGenericParamConstraints) {
						output.Write("(", TextTokenKind.Operator);
						for (int j = 0; j < gp.GenericParamConstraints.Count; j++) {
							if (j > 0) {
								output.Write(",", TextTokenKind.Operator);
								output.WriteSpace();
							}
							gp.GenericParamConstraints[j].Constraint.WriteTo(output, ILNameSyntax.TypeName);
						}
						output.Write(")", TextTokenKind.Operator);
						output.WriteSpace();
					}
					if (gp.IsContravariant) {
						output.Write("-", TextTokenKind.Operator);
					} else if (gp.IsCovariant) {
						output.Write("+", TextTokenKind.Operator);
					}
					output.Write(DisassemblerHelpers.Escape(gp.Name), TextTokenKindUtils.GetTextTokenType(gp));
				}
				output.Write(">", TextTokenKind.Operator);
			}
		}
		#endregion

		#region Helper methods
		void WriteAttributes(CustomAttributeCollection attributes)
		{
			foreach (CustomAttribute a in attributes) {
				output.Write(".custom", TextTokenKind.ILDirective);
				output.WriteSpace();
				a.Constructor.WriteMethodTo(output);
				byte[] blob = a.GetBlob();
				if (blob != null) {
					output.WriteSpace();
					output.Write("=", TextTokenKind.Operator);
					output.WriteSpace();
					WriteBlob(blob);
				}
				output.WriteLine();
			}
		}
		
		void WriteBlob(byte[] blob)
		{
			output.Write("(", TextTokenKind.Operator);
			output.Indent();

			for (int i = 0; i < blob.Length; i++) {
				if (i % 16 == 0 && i < blob.Length - 1) {
					output.WriteLine();
				} else {
					output.WriteSpace();
				}
				output.Write(blob[i].ToString("x2"), TextTokenKind.Number);
			}
			
			output.WriteLine();
			output.Unindent();
			output.Write(")", TextTokenKind.Operator);
		}
		
		void OpenBlock(bool defaultCollapsed)
		{
			output.WriteLine();
			output.WriteLineLeftBrace();
			output.Indent();
		}
		
		void CloseBlock(string comment = null)
		{
			output.Unindent();
			output.WriteRightBrace();
			if (comment != null) {
				output.WriteSpace();
				output.Write("// " + comment, TextTokenKind.Comment);
			}
			output.WriteLine();
		}
		
		void WriteFlags<T>(T flags, EnumNameCollection<T> flagNames) where T : struct
		{
			long val = Convert.ToInt64(flags);
			long tested = 0;
			foreach (var pair in flagNames) {
				tested |= pair.Key;
				if ((val & pair.Key) != 0 && pair.Value != null) {
					foreach (var kv in pair.Value.Split(' ')) {
						output.Write(kv, TextTokenKind.Keyword);
						output.WriteSpace();
					}
				}
			}
			if ((val & ~tested) != 0) {
				output.Write(string.Format("flag({0:x4})", val & ~tested), TextTokenKind.Keyword);
				output.WriteSpace();
			}
		}
		
		void WriteEnum<T>(T enumValue, EnumNameCollection<T> enumNames) where T : struct
		{
			long val = Convert.ToInt64(enumValue);
			foreach (var pair in enumNames) {
				if (pair.Key == val) {
					if (pair.Value != null) {
						foreach (var kv in pair.Value.Split(' ')) {
							output.Write(kv, TextTokenKind.Keyword);
							output.WriteSpace();
						}
					}
					return;
				}
			}
			if (val != 0) {
				output.Write(string.Format("flag({0:x4})", val), TextTokenKind.Keyword);
				output.WriteSpace();
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
				output.Write(".namespace", TextTokenKind.ILDirective);
				output.WriteSpace();
				if (DisassemblerHelpers.MustEscape(nameSpace))
					output.Write(DisassemblerHelpers.Escape(nameSpace), TextTokenKind.NamespacePart);
				else
					DisassemblerHelpers.WriteNamespace(output, nameSpace);
				OpenBlock(false);
			}
			bool oldIsInType = isInType;
			isInType = true;
			foreach (TypeDef td in types) {
				options.CancellationToken.ThrowIfCancellationRequested();
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
			output.Write(".assembly", TextTokenKind.ILDirective);
			output.WriteSpace();
			if (asm.IsContentTypeWindowsRuntime) {
				output.Write("windowsruntime", TextTokenKind.Keyword);
				output.WriteSpace();
			}
			output.Write(DisassemblerHelpers.Escape(asm.Name), TextTokenKind.Text);
			OpenBlock(false);
			WriteAttributes(asm.CustomAttributes);
			WriteSecurityDeclarations(asm);
			if (asm.PublicKey != null && !asm.PublicKey.IsNullOrEmpty) {
				output.Write(".publickey", TextTokenKind.ILDirective);
				output.WriteSpace();
				output.Write("=", TextTokenKind.Operator);
				output.WriteSpace();
				WriteBlob(asm.PublicKey.Data);
				output.WriteLine();
			}
			if (asm.HashAlgorithm != AssemblyHashAlgorithm.None) {
				output.Write(".hash", TextTokenKind.ILDirective);
				output.WriteSpace();
				output.Write("algorithm", TextTokenKind.Keyword);
				output.WriteSpace();
				output.Write(string.Format("0x{0:x8}", (uint)asm.HashAlgorithm), TextTokenKind.Number);
				if (asm.HashAlgorithm == AssemblyHashAlgorithm.SHA1) {
					output.WriteSpace();
					output.Write("// SHA1", TextTokenKind.Comment);
				}
				output.WriteLine();
			}
			Version v = asm.Version;
			if (v != null) {
				output.Write(".ver", TextTokenKind.ILDirective);
				output.WriteSpace();
				output.Write(string.Format("{0}", v.Major), TextTokenKind.Number);
				output.Write(":", TextTokenKind.Operator);
				output.Write(string.Format("{0}", v.Minor), TextTokenKind.Number);
				output.Write(":", TextTokenKind.Operator);
				output.Write(string.Format("{0}", v.Build), TextTokenKind.Number);
				output.Write(":", TextTokenKind.Operator);
				output.WriteLine(string.Format("{0}", v.Revision), TextTokenKind.Number);
			}
			CloseBlock();
		}
		
		public void WriteAssemblyReferences(ModuleDef module)
		{
			if (module == null)
				return;
			foreach (var mref in module.GetModuleRefs()) {
				output.Write(".module", TextTokenKind.ILDirective);
				output.WriteSpace();
				output.Write("extern", TextTokenKind.Keyword);
				output.WriteSpace();
				output.WriteLine(DisassemblerHelpers.Escape(mref.Name), TextTokenKind.Text);
			}
			foreach (var aref in module.GetAssemblyRefs()) {
				AddTokenComment(aref);
				output.Write(".assembly", TextTokenKind.ILDirective);
				output.WriteSpace();
				output.Write("extern", TextTokenKind.Keyword);
				output.WriteSpace();
				if (aref.IsContentTypeWindowsRuntime) {
					output.Write("windowsruntime", TextTokenKind.Keyword);
					output.WriteSpace();
				}
				output.Write(DisassemblerHelpers.Escape(aref.Name), TextTokenKind.Text);
				OpenBlock(false);
				if (!PublicKeyBase.IsNullOrEmpty2(aref.PublicKeyOrToken)) {
					output.Write(".publickeytoken", TextTokenKind.ILDirective);
					output.WriteSpace();
					output.Write("=", TextTokenKind.Operator);
					output.WriteSpace();
					WriteBlob(aref.PublicKeyOrToken.Token.Data);
					output.WriteLine();
				}
				if (aref.Version != null) {
					output.Write(".ver", TextTokenKind.ILDirective);
					output.WriteSpace();
					output.Write(string.Format("{0}", aref.Version.Major), TextTokenKind.Number);
					output.Write(":", TextTokenKind.Operator);
					output.Write(string.Format("{0}", aref.Version.Minor), TextTokenKind.Number);
					output.Write(":", TextTokenKind.Operator);
					output.Write(string.Format("{0}", aref.Version.Build), TextTokenKind.Number);
					output.Write(":", TextTokenKind.Operator);
					output.WriteLine(string.Format("{0}", aref.Version.Revision), TextTokenKind.Number);
				}
				CloseBlock();
			}
		}
		
		public void WriteModuleHeader(ModuleDef module)
		{
			if (module.HasExportedTypes) {
				foreach (ExportedType exportedType in module.ExportedTypes) {
					AddTokenComment(exportedType);
					output.Write(".class", TextTokenKind.ILDirective);
					output.WriteSpace();
					output.Write("extern", TextTokenKind.Keyword);
					output.WriteSpace();
					if (exportedType.IsForwarder) {
						output.Write("forwarder", TextTokenKind.Keyword);
						output.WriteSpace();
					}
					output.Write(exportedType.DeclaringType != null ? exportedType.TypeName.String : exportedType.FullName, TextTokenKindUtils.GetTextTokenType(exportedType));
					OpenBlock(false);
					if (exportedType.DeclaringType != null) {
						output.Write(".class", TextTokenKind.ILDirective);
						output.WriteSpace();
						output.Write("extern", TextTokenKind.Keyword);
						output.WriteSpace();
						output.WriteLine(DisassemblerHelpers.Escape(exportedType.DeclaringType.FullName), TextTokenKindUtils.GetTextTokenType(exportedType.DeclaringType));
					}
					else {
						output.Write(".assembly", TextTokenKind.ILDirective);
						output.WriteSpace();
						output.Write("extern", TextTokenKind.Keyword);
						output.WriteSpace();
						output.WriteLine(DisassemblerHelpers.Escape(exportedType.Scope.GetScopeName()), TextTokenKind.Text);
					}
					CloseBlock();
				}
			}
			
			output.Write(".module", TextTokenKind.ILDirective);
			output.WriteSpace();
			output.WriteLine(module.Name, TextTokenKind.Text);
			if (module.Mvid.HasValue)
				output.WriteLine(string.Format("// MVID: {0}", module.Mvid.Value.ToString("B").ToUpperInvariant()), TextTokenKind.Comment);
			// TODO: imagebase, file alignment, stackreserve, subsystem
			output.Write(".corflags", TextTokenKind.ILDirective);
			output.WriteSpace();
			output.Write(string.Format("0x{0:x}", module.Cor20HeaderFlags), TextTokenKind.Number);
			output.WriteSpace();
			output.WriteLine(string.Format("// {0}", module.Cor20HeaderFlags.ToString()), TextTokenKind.Comment);
			
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
