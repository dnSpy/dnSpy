using System;
using System.Collections.Generic;
using dnlib.DotNet;

namespace dnSpy.Contracts.Decompiler {
	/// <summary>
	/// Resolves generic arguments
	/// </summary>
	public struct GenericArgumentResolver {
		IList<TypeSig> typeGenArgs;
		IList<TypeSig> methodGenArgs;
		RecursionCounter recursionCounter;

		GenericArgumentResolver(IList<TypeSig>? typeGenArgs, IList<TypeSig>? methodGenArgs) {
			this.typeGenArgs = typeGenArgs ?? Array.Empty<TypeSig>();
			this.methodGenArgs = methodGenArgs ?? Array.Empty<TypeSig>();
			recursionCounter = new RecursionCounter();
		}

		/// <summary>
		/// Resolves the type signature with the specified generic arguments.
		/// </summary>
		/// <param name="typeSig">The type signature.</param>
		/// <param name="typeGenArgs">The type generic arguments.</param>
		/// <param name="methodGenArgs">The method generic arguments.</param>
		/// <returns>Resolved type signature.</returns>
		/// <exception cref="System.ArgumentException">No generic arguments to resolve.</exception>
		public static TypeSig? Resolve(TypeSig? typeSig, IList<TypeSig>? typeGenArgs, IList<TypeSig>? methodGenArgs) {
			if (typeSig is null)
				return typeSig;
			if ((typeGenArgs is null || typeGenArgs.Count == 0) && (methodGenArgs is null || methodGenArgs.Count == 0))
				return typeSig;

			var resolver = new GenericArgumentResolver(typeGenArgs, methodGenArgs);
			return resolver.ResolveGenericArgs(typeSig);
		}

		/// <summary>
		/// Resolves the method signature with the specified generic arguments.
		/// </summary>
		/// <param name="methodSig">The method signature.</param>
		/// <param name="typeGenArgs">The type generic arguments.</param>
		/// <param name="methodGenArgs">The method generic arguments.</param>
		/// <returns>Resolved method signature.</returns>
		/// <exception cref="System.ArgumentException">No generic arguments to resolve.</exception>
		public static MethodBaseSig? Resolve(MethodBaseSig? methodSig, IList<TypeSig>? typeGenArgs, IList<TypeSig>? methodGenArgs) {
			if (methodSig is null)
				return null;
			if ((typeGenArgs is null || typeGenArgs.Count == 0) && (methodGenArgs is null || methodGenArgs.Count == 0))
				return methodSig;

			var resolver = new GenericArgumentResolver(typeGenArgs, methodGenArgs);
			return resolver.ResolveGenericArgs(methodSig);
		}

		bool ReplaceGenericArg(ref TypeSig typeSig) {
			if (typeSig is GenericMVar genericMVar) {
				var newSig = Read(methodGenArgs, genericMVar.Number);
				if (!(newSig is null)) {
					typeSig = newSig;
					return true;
				}
				return false;
			}

			if (typeSig is GenericVar genericVar) {
				var newSig = Read(typeGenArgs, genericVar.Number);
				if (!(newSig is null)) {
					typeSig = newSig;
					return true;
				}
				return false;
			}

			return false;
		}

		static TypeSig? Read(IList<TypeSig> sigs, uint index) {
			if (index < (uint)sigs.Count)
				return sigs[(int)index];
			return null;
		}

		MethodSig? ResolveGenericArgs(MethodBaseSig sig) {
			if (sig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			MethodSig result = ResolveGenericArgs(new MethodSig(sig.CallingConvention), sig);

			recursionCounter.Decrement();
			return result;
		}

		MethodSig ResolveGenericArgs(MethodSig sig, MethodBaseSig old) {
			sig.RetType = ResolveGenericArgs(old.RetType);
			foreach (var p in old.Params)
				sig.Params.Add(ResolveGenericArgs(p));
			sig.GenParamCount = old.GenParamCount;
			if (!(sig.ParamsAfterSentinel is null)) {
				foreach (var p in old.ParamsAfterSentinel)
					sig.ParamsAfterSentinel.Add(ResolveGenericArgs(p));
			}
			return sig;
		}

		TypeSig? ResolveGenericArgs(TypeSig typeSig) {
			if (typeSig is null)
				return null;
			if (!recursionCounter.Increment())
				return null;

			if (ReplaceGenericArg(ref typeSig)) {
				recursionCounter.Decrement();
				return typeSig;
			}

			TypeSig result;
			switch (typeSig.ElementType) {
			case ElementType.Ptr:
				result = new PtrSig(ResolveGenericArgs(typeSig.Next));
				break;
			case ElementType.ByRef:
				result = new ByRefSig(ResolveGenericArgs(typeSig.Next));
				break;
			case ElementType.Var:
				result = new GenericVar(((GenericVar)typeSig).Number, ((GenericVar)typeSig).OwnerType);
				break;
			case ElementType.ValueArray:
				result = new ValueArraySig(ResolveGenericArgs(typeSig.Next), ((ValueArraySig)typeSig).Size);
				break;
			case ElementType.SZArray:
				result = new SZArraySig(ResolveGenericArgs(typeSig.Next));
				break;
			case ElementType.MVar:
				result = new GenericMVar(((GenericMVar)typeSig).Number, ((GenericMVar)typeSig).OwnerMethod);
				break;
			case ElementType.CModReqd:
				result = new CModReqdSig(((ModifierSig)typeSig).Modifier, ResolveGenericArgs(typeSig.Next));
				break;
			case ElementType.CModOpt:
				result = new CModOptSig(((ModifierSig)typeSig).Modifier, ResolveGenericArgs(typeSig.Next));
				break;
			case ElementType.Module:
				result = new ModuleSig(((ModuleSig)typeSig).Index, ResolveGenericArgs(typeSig.Next));
				break;
			case ElementType.Pinned:
				result = new PinnedSig(ResolveGenericArgs(typeSig.Next));
				break;
			case ElementType.FnPtr:
				result = new FnPtrSig(ResolveGenericArgs(((FnPtrSig)typeSig).MethodSig));
				break;

			case ElementType.Array:
				ArraySig arraySig = (ArraySig)typeSig;
				List<uint> sizes = new List<uint>(arraySig.Sizes);
				List<int> lbounds = new List<int>(arraySig.LowerBounds);
				result = new ArraySig(ResolveGenericArgs(typeSig.Next), arraySig.Rank, sizes, lbounds);
				break;
			case ElementType.GenericInst:
				GenericInstSig gis = (GenericInstSig)typeSig;
				List<TypeSig?> genArgs = new List<TypeSig?>(gis.GenericArguments.Count);
				foreach (TypeSig ga in gis.GenericArguments) {
					genArgs.Add(ResolveGenericArgs(ga));
				}
				result = new GenericInstSig(ResolveGenericArgs(gis.GenericType as TypeSig) as ClassOrValueTypeSig, genArgs);
				break;

			default:
				result = typeSig;
				break;
			}

			recursionCounter.Decrement();

			return result;
		}

		CallingConventionSig? ResolveGenericArgs(CallingConventionSig sig) {
			if (!recursionCounter.Increment())
				return null;

			CallingConventionSig? result;
			MethodSig? msig;
			FieldSig? fsig;
			LocalSig? lsig;
			PropertySig? psig;
			GenericInstMethodSig? gsig;
			if (!((msig = sig as MethodSig) is null))
				result = ResolveGenericArgs(msig);
			else if (!((fsig = sig as FieldSig) is null))
				result = ResolveGenericArgs(fsig);
			else if (!((lsig = sig as LocalSig) is null))
				result = ResolveGenericArgs(lsig);
			else if (!((psig = sig as PropertySig) is null))
				result = ResolveGenericArgs(psig);
			else if (!((gsig = sig as GenericInstMethodSig) is null))
				result = ResolveGenericArgs(gsig);
			else
				result = null;

			recursionCounter.Decrement();

			return result;
		}

		MethodSig ResolveGenericArgs(MethodSig sig) {
			var msig = ResolveGenericArgs2(new MethodSig(), sig);
			msig.OriginalToken = sig.OriginalToken;
			return msig;
		}

		PropertySig ResolveGenericArgs(PropertySig sig) => ResolveGenericArgs2(new PropertySig(), sig);

		T ResolveGenericArgs2<T>(T outSig, T inSig) where T : MethodBaseSig {
			outSig.RetType = ResolveGenericArgs(inSig.RetType);
			outSig.GenParamCount = inSig.GenParamCount;
			UpdateSigList(outSig.Params, inSig.Params);
			if (!(inSig.ParamsAfterSentinel is null)) {
				outSig.ParamsAfterSentinel = new List<TypeSig>(inSig.ParamsAfterSentinel.Count);
				UpdateSigList(outSig.ParamsAfterSentinel, inSig.ParamsAfterSentinel);
			}
			return outSig;
		}

		void UpdateSigList(IList<TypeSig?> inList, IList<TypeSig> outList) {
			foreach (var arg in outList)
				inList.Add(ResolveGenericArgs(arg));
		}

		FieldSig ResolveGenericArgs(FieldSig sig) => new FieldSig(ResolveGenericArgs(sig.Type));

		LocalSig ResolveGenericArgs(LocalSig sig) {
			var lsig = new LocalSig();
			UpdateSigList(lsig.Locals, sig.Locals);
			return lsig;
		}

		GenericInstMethodSig ResolveGenericArgs(GenericInstMethodSig sig) {
			var gsig = new GenericInstMethodSig();
			UpdateSigList(gsig.GenericArguments, sig.GenericArguments);
			return gsig;
		}
	}

	struct RecursionCounter {
		const int MAX_RECURSION_COUNT = 100;
		int counter;

		public bool Increment() {
			if (counter >= MAX_RECURSION_COUNT)
				return false;
			counter++;
			return true;
		}

		public void Decrement() {
#if DEBUG
			if (counter <= 0)
				throw new InvalidOperationException("recursionCounter <= 0");
#endif
			counter--;
		}
	}
}
