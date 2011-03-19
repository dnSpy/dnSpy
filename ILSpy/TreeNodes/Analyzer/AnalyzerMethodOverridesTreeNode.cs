using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using ICSharpCode.NRefactory.Utils;
using ICSharpCode.TreeView;
using Mono.Cecil;

namespace ICSharpCode.ILSpy.TreeNodes.Analyzer
{
	/// <summary>
	/// Searches for overrides of the analyzed method.
	/// </summary>
	class AnalyzerMethodOverridesTreeNode : AnalyzerTreeNode
	{
		readonly MethodDefinition analyzedMethod;
		readonly ThreadingSupport threading;

		/// <summary>
		/// Controls whether overrides of already overriden method should be included.
		/// </summary>
		readonly bool onlyDirectOverrides = false;

		public AnalyzerMethodOverridesTreeNode(MethodDefinition analyzedMethod)
		{
			if (analyzedMethod == null)
				throw new ArgumentNullException("analyzedMethod");

			this.analyzedMethod = analyzedMethod;
			this.threading = new ThreadingSupport();
			this.LazyLoading = true;
		}

		public override object Text
		{
			get { return "Overrided By"; }
		}

		public override object Icon
		{
			get { return Images.Search; }
		}

		protected override void LoadChildren()
		{
			threading.LoadChildren(this, FetchChildren);
		}

		protected override void OnCollapsing()
		{
			if (threading.IsRunning)
			{
				this.LazyLoading = true;
				threading.Cancel();
				this.Children.Clear();
			}
		}

		IEnumerable<SharpTreeNode> FetchChildren(CancellationToken ct)
		{
			return FindReferences(MainWindow.Instance.AssemblyList.GetAssemblies(), ct);
		}

		IEnumerable<SharpTreeNode> FindReferences(LoadedAssembly[] assemblies, CancellationToken ct)
		{
			// use parallelism only on the assembly level (avoid locks within Cecil)
			return assemblies.AsParallel().WithCancellation(ct).SelectMany((LoadedAssembly asm) => FindReferences(asm, ct));
		}

		IEnumerable<SharpTreeNode> FindReferences(LoadedAssembly asm, CancellationToken ct)
		{
			string asmName = asm.AssemblyDefinition.Name.Name;
			string name = analyzedMethod.Name;
			string declTypeName = analyzedMethod.DeclaringType.FullName;
			foreach (TypeDefinition type in TreeTraversal.PreOrder(asm.AssemblyDefinition.MainModule.Types, t => t.NestedTypes))
			{
				ct.ThrowIfCancellationRequested();

				if (!IsDerived(type, analyzedMethod.DeclaringType))
					continue;

				foreach (MethodDefinition method in type.Methods)
				{
					ct.ThrowIfCancellationRequested();

					if (HasCompatibleSpecification(method) && !method.IsNewSlot && DoesOverrideCorrectMethod(method))
					{
						yield return new AnalyzedMethodTreeNode(method);
					}
				}
			}
		}

		/// <summary>
		/// Tests whether the method could override analyzed method by comparing its name, return type and parameters.
		/// </summary>
		/// <param name="method">The method to test.</param>
		/// <returns>true if the method has the same specyfication as analyzed method, otherwise false.</returns>
		private bool HasCompatibleSpecification(MethodDefinition method)
		{
			return method.Name == analyzedMethod.Name
						&& method.IsVirtual
						&& AreSameType(method.ReturnType, analyzedMethod.ReturnType)
						&& HaveTheSameParameters(method);
		}

		/// <summary>
		/// Checks whether between given and analyzed method are overrides with <code>new</code> (newSlot) modifier.
		/// </summary>
		/// <param name="method">The method to test.</param>
		/// <returns>true if the method overrides analyzed method, false if it overrides some other method that hides analyzed method.</returns>
		private bool DoesOverrideCorrectMethod(MethodDefinition method)
		{
			var type = method.DeclaringType.BaseType.Resolve();
			while (type != analyzedMethod.DeclaringType)
			{
				var parentOverride = type.Methods.Where(m => HasCompatibleSpecification(m)).SingleOrDefault();
				if (parentOverride != null)
				{
					if (parentOverride.IsNewSlot)
						return false;
					else
						return !onlyDirectOverrides;
				}
				type = type.BaseType.Resolve();
			}
			return true;
		}

		/// <summary>
		/// Checks whether one type derives (directly or indirectly) from base type.
		/// </summary>
		/// <param name="derivedType">The possible derived type.</param>
		/// <param name="baseType">The base type.</param>
		/// <returns>true if <paramref name="derivedType"/> derives from <paramref name="baseType"/>, overwise false.</returns>
		private static bool IsDerived(TypeDefinition derivedType, TypeDefinition baseType)
		{
			while (derivedType != null && derivedType.BaseType != null)
			{
				if (AreSameType(derivedType.BaseType, baseType))
					return true;
				derivedType = derivedType.BaseType.Resolve();
			}
			return false;
		}

		/// <summary>
		/// Checks whether both <see cref="TypeReference"/> instances references the same type.
		/// </summary>
		/// <param name="ref1">The first type reference.</param>
		/// <param name="ref2">The second type reference.</param>
		/// <returns>true if both instances references the same type, overwise false.</returns>
		private static bool AreSameType(TypeReference ref1, TypeReference ref2)
		{
			if (ref1 == ref2)
				return true;

			if (ref1.Name != ref2.Name || ref1.FullName != ref2.FullName)
				return false;

			return ref1.Resolve() == ref2.Resolve();
		}

		/// <summary>
		/// Checkes whether the given method and the analyzed one has identical lists of parameters.
		/// </summary>
		/// <param name="method">The method to test.</param>
		/// <returns>true if both methods has the same parameters, otherwise false.</returns>
		private bool HaveTheSameParameters(MethodDefinition method)
		{
			if (analyzedMethod.HasParameters)
			{
				return CompareParameterLists(analyzedMethod.Parameters, method.Parameters);
			}
			else
			{
				return !method.HasParameters;
			}
		}

		/// <summary>
		/// Compares the list of method's parameters.
		/// </summary>
		/// <param name="coll1">The first list to compare.</param>
		/// <param name="coll2">The second list to copare.</param>
		/// <returns>true if both list have parameters of the same types at the same positions.</returns>
		private static bool CompareParameterLists(Mono.Collections.Generic.Collection<ParameterDefinition> coll1, Mono.Collections.Generic.Collection<ParameterDefinition> coll2)
		{
			if (coll1.Count != coll2.Count)
				return false;

			for (int index = 0; index < coll1.Count; index++)
			{
				var param1 = coll1[index];
				var param2 = coll2[index];
				if (param1.Attributes != param2.Attributes || !AreSameType(param1.ParameterType, param2.ParameterType))
					return false;
			}
			return true;
		}
	}
}
