using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Media;
using ICSharpCode.ILSpy.TreeNodes;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Utils;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace ICSharpCode.ILSpy
{
	abstract class AbstractSearchStrategy
	{
		protected string[] searchTerm;
		protected Regex regex;

		protected AbstractSearchStrategy(params string[] terms)
		{
			if (terms.Length == 1 && terms[0].Length > 2) {
				var search = terms[0];
				if (search.StartsWith("/") && search.EndsWith("/") && search.Length > 4)
					regex = SafeNewRegex(search.Substring(1, search.Length - 2));

				terms[0] = search;
			}

			searchTerm = terms;
		}

		protected bool IsMatch(string text)
		{
			if (text == null)
				return false;
			if (regex != null)
				return regex.IsMatch(text);

			for (int i = 0; i < searchTerm.Length; ++i) {
				// How to handle overlapping matches?
				if (text.IndexOf(searchTerm[i], StringComparison.OrdinalIgnoreCase) < 0)
					return false;
			}
			return true;
		}

		protected virtual bool IsMatch(FieldDef field)
		{
			return false;
		}

		protected virtual bool IsMatch(PropertyDef property)
		{
			return false;
		}

		protected virtual bool IsMatch(EventDef ev)
		{
			return false;
		}

		protected virtual bool IsMatch(MethodDef m)
		{
			return false;
		}

		void Add<T>(IEnumerable<T> items, TypeDef type, Language language, Action<SearchResult> addResult, Func<T, bool> matcher, Func<T, ImageSource> image) where T : IMemberRef
		{
			foreach (var item in items) {
				if (matcher(item)) {
					addResult(new SearchResult
					{
						Member = item,
						Image = image(item),
						Name = item.Name,
						LocationImage = TypeTreeNode.GetIcon(type),
						Location = language.TypeToString(type, includeNamespace: true)
					});
				}
			}
		}

		public virtual void Search(TypeDef type, Language language, Action<SearchResult> addResult)
		{
			Add(type.Fields, type, language, addResult, IsMatch, FieldTreeNode.GetIcon);
			Add(type.Properties, type, language, addResult, IsMatch, p => PropertyTreeNode.GetIcon(p));
			Add(type.Events, type, language, addResult, IsMatch, EventTreeNode.GetIcon);
			Add(type.Methods.Where(NotSpecialMethod), type, language, addResult, IsMatch, MethodTreeNode.GetIcon);
		}

		bool NotSpecialMethod(MethodDef arg)
		{
			return (arg.SemanticsAttributes & (
				MethodSemanticsAttributes.Setter
				| MethodSemanticsAttributes.Getter
				| MethodSemanticsAttributes.AddOn
				| MethodSemanticsAttributes.RemoveOn
				| MethodSemanticsAttributes.Fire)) == 0;
		}

		Regex SafeNewRegex(string unsafePattern)
		{
			try {
				return new Regex(unsafePattern, RegexOptions.Compiled);
			} catch (ArgumentException) {
				return null;
			}
		}
	}

	class LiteralSearchStrategy : AbstractSearchStrategy
	{
		readonly TypeCode searchTermLiteralType;
		readonly object searchTermLiteralValue;

		public LiteralSearchStrategy(params string[] terms)
			: base(terms)
		{
			if (1 == searchTerm.Length) {
				var parser = new CSharpParser();
				var pe = parser.ParseExpression(searchTerm[0]) as PrimitiveExpression;

				if (pe != null && pe.Value != null) {
					TypeCode peValueType = Type.GetTypeCode(pe.Value.GetType());
					switch (peValueType) {
					case TypeCode.Byte:
					case TypeCode.SByte:
					case TypeCode.Int16:
					case TypeCode.UInt16:
					case TypeCode.Int32:
					case TypeCode.UInt32:
					case TypeCode.Int64:
					case TypeCode.UInt64:
						searchTermLiteralType = TypeCode.Int64;
						searchTermLiteralValue = CSharpPrimitiveCast.Cast(TypeCode.Int64, pe.Value, false);
						break;
					case TypeCode.Single:
					case TypeCode.Double:
					case TypeCode.String:
						searchTermLiteralType = peValueType;
						searchTermLiteralValue = pe.Value;
						break;
					}
				}
			}
		}

		protected override bool IsMatch(FieldDef field)
		{
			return IsLiteralMatch(field.Constant);
		}

		protected override bool IsMatch(PropertyDef property)
		{
			return MethodIsLiteralMatch(property.GetMethod) || MethodIsLiteralMatch(property.SetMethod);
		}

		protected override bool IsMatch(EventDef ev)
		{
			return MethodIsLiteralMatch(ev.AddMethod) || MethodIsLiteralMatch(ev.RemoveMethod) || MethodIsLiteralMatch(ev.InvokeMethod);
		}

		protected override bool IsMatch(MethodDef m)
		{
			return MethodIsLiteralMatch(m);
		}

		bool IsLiteralMatch(object val)
		{
			if (val == null)
				return false;
			switch (searchTermLiteralType) {
				case TypeCode.Int64:
					TypeCode tc = Type.GetTypeCode(val.GetType());
					if (tc >= TypeCode.SByte && tc <= TypeCode.UInt64)
						return CSharpPrimitiveCast.Cast(TypeCode.Int64, val, false).Equals(searchTermLiteralValue);
					else
						return false;
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.String:
					return searchTermLiteralValue.Equals(val);
				default:
					// substring search with searchTerm
					return IsMatch(val.ToString());
			}
		}

		bool MethodIsLiteralMatch(MethodDef m)
		{
			if (m == null)
				return false;
			var body = m.Body;
			if (body == null)
				return false;
			if (searchTermLiteralType == TypeCode.Int64) {
				long val = (long)searchTermLiteralValue;
				foreach (var inst in body.Instructions) {
					switch (inst.OpCode.Code) {
					case Code.Ldc_I8:
						if (val == (long)inst.Operand)
							return true;
						break;
					case Code.Ldc_I4:
						if (val == (int)inst.Operand)
							return true;
						break;
					case Code.Ldc_I4_S:
						if (val == (sbyte)inst.Operand)
							return true;
						break;
					case Code.Ldc_I4_M1:
						if (val == -1)
							return true;
						break;
					case Code.Ldc_I4_0:
						if (val == 0)
							return true;
						break;
					case Code.Ldc_I4_1:
						if (val == 1)
							return true;
						break;
					case Code.Ldc_I4_2:
						if (val == 2)
							return true;
						break;
					case Code.Ldc_I4_3:
						if (val == 3)
							return true;
						break;
					case Code.Ldc_I4_4:
						if (val == 4)
							return true;
						break;
					case Code.Ldc_I4_5:
						if (val == 5)
							return true;
						break;
					case Code.Ldc_I4_6:
						if (val == 6)
							return true;
						break;
					case Code.Ldc_I4_7:
						if (val == 7)
							return true;
						break;
					case Code.Ldc_I4_8:
						if (val == 8)
							return true;
						break;
					}
				}
			} else if (searchTermLiteralType != TypeCode.Empty) {
				Code expectedCode;
				switch (searchTermLiteralType) {
				case TypeCode.Single:
					expectedCode = Code.Ldc_R4;
					break;
				case TypeCode.Double:
					expectedCode = Code.Ldc_R8;
					break;
				case TypeCode.String:
					expectedCode = Code.Ldstr;
					break;
				default:
					throw new InvalidOperationException();
				}
				foreach (var inst in body.Instructions) {
					if (inst.OpCode.Code == expectedCode && searchTermLiteralValue.Equals(inst.Operand))
						return true;
				}
			} else {
				foreach (var inst in body.Instructions) {
					if (inst.OpCode.Code == Code.Ldstr && IsMatch((string)inst.Operand))
						return true;
				}
			}
			return false;
		}
	}

	class MemberSearchStrategy : AbstractSearchStrategy
	{
		public MemberSearchStrategy(params string[] terms)
			: base(terms)
		{
		}

		protected override bool IsMatch(FieldDef field)
		{
			return IsMatch(field.Name);
		}

		protected override bool IsMatch(PropertyDef property)
		{
			return IsMatch(property.Name);
		}

		protected override bool IsMatch(EventDef ev)
		{
			return IsMatch(ev.Name);
		}

		protected override bool IsMatch(MethodDef m)
		{
			return IsMatch(m.Name);
		}
	}

	class TypeSearchStrategy : AbstractSearchStrategy
	{
		public TypeSearchStrategy(params string[] terms)
			: base(terms)
		{
		}

		public override void Search(TypeDef type, Language language, Action<SearchResult> addResult)
		{
			if (IsMatch(type.Name) || IsMatch(type.FullName)) {
				addResult(new SearchResult {
					Member = type,
					Image = TypeTreeNode.GetIcon(type),
					Name = language.TypeToString(type, includeNamespace: false),
					LocationImage = type.DeclaringType != null ? TypeTreeNode.GetIcon(type.DeclaringType) : Images.Namespace,
					Location = type.DeclaringType != null ? language.TypeToString(type.DeclaringType, includeNamespace: true) : type.Namespace.String
				});
			}

			foreach (TypeDef nestedType in type.NestedTypes) {
				Search(nestedType, language, addResult);
			}
		}
	}

}
