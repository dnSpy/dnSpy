
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Xml.Linq;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.NRefactory;

namespace ICSharpCode.ILSpy.dntheme
{
	sealed class Theme
	{
		static readonly Dictionary<string, ColorType> nameToColorType = new Dictionary<string, ColorType>(StringComparer.InvariantCultureIgnoreCase);

		[DebuggerDisplay("{ColorInfo.ColorType}")]
		public sealed class Color {
			/// <summary>
			/// Color info
			/// </summary>
			public readonly ColorInfo ColorInfo;

			/// <summary>
			/// Original color with no inherited properties. If this one or any of its properties
			/// get modified, <see cref="Theme.RecalculateInheritedColorProperties()"/> must be
			/// called.
			/// </summary>
			public HighlightingColor OriginalColor;

			/// <summary>
			/// Color with inherited properties, but doesn't include inherited default text (because
			/// it messes up with selection in text editor). See also <see cref="InheritedColor"/>
			/// </summary>
			public HighlightingColor TextInheritedColor;

			/// <summary>
			/// Color with inherited properties. See also <see cref="TextInheritedColor"/>
			/// </summary>
			public HighlightingColor InheritedColor;

			public Color(ColorInfo colorInfo)
			{
				this.ColorInfo = colorInfo;
			}
		}

		[DebuggerDisplay("{ColorType}, Children={Children.Length}")]
		public sealed class ColorInfo
		{
			public readonly ColorType ColorType;
			public readonly ColorInfo[] Children;
			public ColorInfo Parent;

			public ColorInfo(ColorType colorType, params ColorInfo[] children)
			{
				this.ColorType = colorType;
				this.Children = children;
				foreach (var child in children)
					child.Parent = this;
			}
		}

		// color inheritance
		static readonly ColorInfo[] rootColorInfos = new ColorInfo[] {
			new ColorInfo(ColorType.Selection),
			new ColorInfo(ColorType.SpecialCharacterBox),
			new ColorInfo(ColorType.SearchResultMarker),
			new ColorInfo(ColorType.DefaultText,
				new ColorInfo(ColorType.Text,
					new ColorInfo(ColorType.Punctuation,
						new ColorInfo(ColorType.Brace),
						new ColorInfo(ColorType.Operator)
					),
					new ColorInfo(ColorType.Comment),
					new ColorInfo(ColorType.Xml,
						new ColorInfo(ColorType.XmlDocTag),
						new ColorInfo(ColorType.XmlDocAttribute),
						new ColorInfo(ColorType.XmlDocComment),
						new ColorInfo(ColorType.XmlComment),
						new ColorInfo(ColorType.XmlCData),
						new ColorInfo(ColorType.XmlDocType),
						new ColorInfo(ColorType.XmlDeclaration),
						new ColorInfo(ColorType.XmlTag),
						new ColorInfo(ColorType.XmlAttributeName),
						new ColorInfo(ColorType.XmlAttributeValue),
						new ColorInfo(ColorType.XmlEntity),
						new ColorInfo(ColorType.XmlBrokenEntity)
					),
					new ColorInfo(ColorType.Literal,
						new ColorInfo(ColorType.Number),
						new ColorInfo(ColorType.String),
						new ColorInfo(ColorType.Char)
					),
					new ColorInfo(ColorType.Identifier,
						new ColorInfo(ColorType.Keyword),
						new ColorInfo(ColorType.NamespacePart),
						new ColorInfo(ColorType.Type,
							new ColorInfo(ColorType.StaticType),
							new ColorInfo(ColorType.Delegate),
							new ColorInfo(ColorType.Enum),
							new ColorInfo(ColorType.Interface),
							new ColorInfo(ColorType.ValueType)
						),
						new ColorInfo(ColorType.GenericParameter,
							new ColorInfo(ColorType.TypeGenericParameter),
							new ColorInfo(ColorType.MethodGenericParameter)
						),
						new ColorInfo(ColorType.Method,
							new ColorInfo(ColorType.InstanceMethod),
							new ColorInfo(ColorType.StaticMethod),
							new ColorInfo(ColorType.ExtensionMethod)
						),
						new ColorInfo(ColorType.Field,
							new ColorInfo(ColorType.InstanceField),
							new ColorInfo(ColorType.EnumField),
							new ColorInfo(ColorType.LiteralField),
							new ColorInfo(ColorType.StaticField)
						),
						new ColorInfo(ColorType.Event,
							new ColorInfo(ColorType.InstanceEvent),
							new ColorInfo(ColorType.StaticEvent)
						),
						new ColorInfo(ColorType.Property,
							new ColorInfo(ColorType.InstanceProperty),
							new ColorInfo(ColorType.StaticProperty)
						),
						new ColorInfo(ColorType.Variable,
							new ColorInfo(ColorType.Local),
							new ColorInfo(ColorType.Parameter)
						),
						new ColorInfo(ColorType.Label),
						new ColorInfo(ColorType.OpCode),
						new ColorInfo(ColorType.ILDirective),
						new ColorInfo(ColorType.ILModule)
					),
					new ColorInfo(ColorType.LineNumber),
					new ColorInfo(ColorType.Link),
					new ColorInfo(ColorType.LocalDefinition),
					new ColorInfo(ColorType.LocalReference),
					new ColorInfo(ColorType.CurrentStatement),
					new ColorInfo(ColorType.BreakpointStatement)
				)
			)
		};
		static readonly ColorInfo[] colorInfos = new ColorInfo[(int)ColorType.Last];

		static Theme()
		{
			for (int i = 0; i < (int)TextTokenType.Last; i++) {
				var tt = ((TextTokenType)i).ToString();
				var ct = ((ColorType)i).ToString();
				if (tt != ct) {
					Debug.Fail("Token type is not a sub set of color type or order is not correct");
					throw new Exception("Token type is not a sub set of color type or order is not correct");
				}
			}

			foreach (var fi in typeof(ColorType).GetFields()) {
				if (!fi.IsLiteral)
					continue;
				var val = (ColorType)fi.GetValue(null);
				if (val == ColorType.Last)
					continue;
				nameToColorType[fi.Name] = val;
			}

			InitColorInfos(rootColorInfos);
			for (int i = 0; i < colorInfos.Length; i++) {
				var colorType = (ColorType)i;
				if (colorInfos[i] == null) {
					Debug.Fail(string.Format("Missing info: {0}", colorType));
					throw new Exception(string.Format("Missing info: {0}", colorType));
				}
			}
		}

		static void InitColorInfos(ColorInfo[] infos)
		{
			foreach (var info in infos) {
				int i = (int)info.ColorType;
				if (colorInfos[i] != null) {
					Debug.Fail("Duplicate");
					throw new Exception("Duplicate");
				}
				colorInfos[i] = info;
				InitColorInfos(info.Children);
			}
		}

		public Color[] Colors {
			get { return hlColors; }
		}
		Color[] hlColors = new Color[(int)ColorType.Last];

		public string Name { get; private set; }
		public string MenuName { get; private set; }
		public int Sort { get; private set; }

		public Theme(XElement root)
		{
			var name = root.Attribute("name");
			if (name == null || string.IsNullOrEmpty(name.Value))
				throw new Exception("Missing or empty name attribute");
			this.Name = name.Value;

			var menuName = root.Attribute("menu-name");
			if (menuName == null || string.IsNullOrEmpty(menuName.Value))
				throw new Exception("Missing or empty menu-name attribute");
			this.MenuName = menuName.Value;

			var sort = root.Attribute("sort");
			this.Sort = sort == null ? 1 : (int)sort;

			for (int i = 0; i < hlColors.Length; i++)
				hlColors[i] = new Color(colorInfos[i]);

			var colors = root.Element("colors");
			if (colors != null) {
				foreach (var color in colors.Elements("color")) {
					ColorType colorType = 0;
					var hl = ReadColor(color, ref colorType);
					if (hl == null)
						continue;
					hlColors[(int)colorType].OriginalColor = hl;
				}
			}
			for (int i = 0; i < hlColors.Length; i++) {
				if (hlColors[i].OriginalColor == null)
					hlColors[i].OriginalColor = new HighlightingColor { Name = ((ColorType)i).ToString() };
				hlColors[i].TextInheritedColor = new HighlightingColor { Name = hlColors[i].OriginalColor.Name };
				hlColors[i].InheritedColor = new HighlightingColor { Name = hlColors[i].OriginalColor.Name };
			}

			// Make sure default text always has a background and a foreground color
			var defaultText = hlColors[(int)ColorType.DefaultText];
			if (defaultText.OriginalColor.Background == null)
				defaultText.OriginalColor.Background = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
			if (defaultText.OriginalColor.Foreground == null)
				defaultText.OriginalColor.Foreground = new SimpleHighlightingBrush(System.Windows.Media.Color.FromRgb(0x00, 0x00, 0x00));

			RecalculateInheritedColorProperties();
		}

		/// <summary>
		/// Recalculates the inherited color properties and should be called whenever any of the
		/// color properties have been modified.
		/// </summary>
		public void RecalculateInheritedColorProperties()
		{
			for (int i = 0; i < hlColors.Length; i++) {
				var info = colorInfos[i];
				var textColor = hlColors[i].TextInheritedColor;
				var color = hlColors[i].InheritedColor;
				if (info.ColorType == ColorType.DefaultText) {
					color.Foreground = textColor.Foreground = hlColors[(int)info.ColorType].OriginalColor.Foreground;
					color.Background = textColor.Background = hlColors[(int)info.ColorType].OriginalColor.Background;
					color.FontStyle = textColor.FontStyle = hlColors[(int)info.ColorType].OriginalColor.FontStyle;
					color.FontWeight = textColor.FontWeight = hlColors[(int)info.ColorType].OriginalColor.FontWeight;
				}
				else {
					textColor.Foreground = GetForeground(info, false);
					textColor.Background = GetBackground(info, false);
					textColor.FontStyle = GetFontStyle(info, false);
					textColor.FontWeight = GetFontWeight(info, false);

					color.Foreground = GetForeground(info, true);
					color.Background = GetBackground(info, true);
					color.FontStyle = GetFontStyle(info, true);
					color.FontWeight = GetFontWeight(info, true);
				}
			}
		}

		HighlightingBrush GetForeground(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.Foreground;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		HighlightingBrush GetBackground(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.Background;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		FontStyle? GetFontStyle(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.FontStyle;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		FontWeight? GetFontWeight(ColorInfo info, bool canIncludeDefault)
		{
			while (info != null) {
				if (!canIncludeDefault && info.ColorType == ColorType.DefaultText)
					break;
				var color = hlColors[(int)info.ColorType];
				var val = color.OriginalColor.FontWeight;
				if (val != null)
					return val;
				info = info.Parent;
			}
			return null;
		}

		public Color GetColor(TextTokenType tokenType)
		{
			return GetColor((ColorType)tokenType);
		}

		public Color GetColor(ColorType colorType)
		{
			uint i = (uint)colorType;
			if (i >= (uint)hlColors.Length)
				return hlColors[(int)ColorType.DefaultText];
			return hlColors[i];
		}

		HighlightingColor ReadColor(XElement color, ref ColorType colorType)
		{
			var name = color.Attribute("name");
			if (name == null)
				return null;
			colorType = ToColorType(name.Value);
			if (colorType == ColorType.Last)
				return null;

			var hl = new HighlightingColor();
			hl.Name = colorType.ToString();

			var fg = color.Attribute("fg");
			if (fg != null)
				hl.Foreground = CreateColor(fg.Value);

			var bg = color.Attribute("bg");
			if (bg != null)
				hl.Background = CreateColor(bg.Value);

			var italics = color.Attribute("italics") ?? color.Attribute("italic");
			if (italics != null)
				hl.FontStyle = (bool)italics ? FontStyles.Italic : FontStyles.Normal;

			var bold = color.Attribute("bold");
			if (bold != null)
				hl.FontWeight = (bool)bold ? FontWeights.Bold : FontWeights.Normal;

			return hl;
		}

		static readonly ColorConverter colorConverter = new ColorConverter();
		static HighlightingBrush CreateColor(string color)
		{
			if (color.StartsWith("SystemColors.")) {
				string shortName = color.Substring(13);
				var property = typeof(SystemColors).GetProperty(shortName + "Brush");
				if (property == null)
					return null;
				return new SystemColorHighlightingBrush(property);
			}

			var clr = (System.Windows.Media.Color?)colorConverter.ConvertFromInvariantString(color);
			return clr == null ? null : new SimpleHighlightingBrush(clr.Value);
		}

		static ColorType ToColorType(string name)
		{
			ColorType type;
			if (nameToColorType.TryGetValue(name, out type))
				return type;
			return ColorType.Last;
		}

		public override string ToString() {
			return string.Format("Theme: {0}", Name);
		}
	}
}
