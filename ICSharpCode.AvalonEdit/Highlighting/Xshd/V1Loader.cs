// Copyright (c) AlphaSierraPapa for the SharpDevelop Team (for details please see \doc\copyright.txt)
// This code is distributed under the GNU LGPL (for details please see \doc\license.txt)

using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Xml;
using System.Xml.Schema;

using ICSharpCode.AvalonEdit.Utils;

namespace ICSharpCode.AvalonEdit.Highlighting.Xshd
{
	/// <summary>
	/// Loads .xshd files, version 1.0.
	/// </summary>
	sealed class V1Loader
	{
		static XmlSchemaSet schemaSet;
		
		static XmlSchemaSet SchemaSet {
			get {
				if (schemaSet == null) {
					schemaSet = HighlightingLoader.LoadSchemaSet(new XmlTextReader(
						Resources.OpenStream("ModeV1.xsd")));
				}
				return schemaSet;
			}
		}
		
		public static XshdSyntaxDefinition LoadDefinition(XmlReader reader, bool skipValidation)
		{
			reader = HighlightingLoader.GetValidatingReader(reader, false, skipValidation ? null : SchemaSet);
			XmlDocument document = new XmlDocument();
			document.Load(reader);
			V1Loader loader = new V1Loader();
			return loader.ParseDefinition(document.DocumentElement);
		}
		
		XshdSyntaxDefinition ParseDefinition(XmlElement syntaxDefinition)
		{
			XshdSyntaxDefinition def = new XshdSyntaxDefinition();
			def.Name = syntaxDefinition.GetAttributeOrNull("name");
			if (syntaxDefinition.HasAttribute("extensions")) {
				def.Extensions.AddRange(syntaxDefinition.GetAttribute("extensions").Split(';', '|'));
			}
			
			XshdRuleSet mainRuleSetElement = null;
			foreach (XmlElement element in syntaxDefinition.GetElementsByTagName("RuleSet")) {
				XshdRuleSet ruleSet = ImportRuleSet(element);
				def.Elements.Add(ruleSet);
				if (ruleSet.Name == null)
					mainRuleSetElement = ruleSet;
				
				if (syntaxDefinition["Digits"] != null) {
					// create digit highlighting rule
					
					const string optionalExponent = @"([eE][+-]?[0-9]+)?";
					const string floatingPoint = @"\.[0-9]+";
					ruleSet.Elements.Add(
						new XshdRule {
							ColorReference = GetColorReference(syntaxDefinition["Digits"]),
							RegexType = XshdRegexType.IgnorePatternWhitespace,
							Regex = @"\b0[xX][0-9a-fA-F]+"
								+ @"|"
								+ @"(\b\d+(" + floatingPoint + ")?"
								+ @"|" + floatingPoint + ")"
								+ optionalExponent
						});
				}
			}
			
			if (syntaxDefinition.HasAttribute("extends") && mainRuleSetElement != null) {
				// convert 'extends="HTML"' to '<Import ruleSet="HTML/" />' in main rule set.
				mainRuleSetElement.Elements.Add(
					new XshdImport { RuleSetReference = new XshdReference<XshdRuleSet>(
						syntaxDefinition.GetAttribute("extends"), string.Empty
					) });
			}
			return def;
		}
		
		static XshdColor GetColorFromElement(XmlElement element)
		{
			if (!element.HasAttribute("bold") && !element.HasAttribute("italic") && !element.HasAttribute("color") && !element.HasAttribute("bgcolor"))
				return null;
			XshdColor color = new XshdColor();
			if (element.HasAttribute("bold"))
				color.FontWeight = XmlConvert.ToBoolean(element.GetAttribute("bold")) ? FontWeights.Bold : FontWeights.Normal;
			if (element.HasAttribute("italic"))
				color.FontStyle = XmlConvert.ToBoolean(element.GetAttribute("italic")) ? FontStyles.Italic : FontStyles.Normal;
			if (element.HasAttribute("color"))
				color.Foreground = ParseColor(element.GetAttribute("color"));
			if (element.HasAttribute("bgcolor"))
				color.Background = ParseColor(element.GetAttribute("bgcolor"));
			return color;
		}
		
		static XshdReference<XshdColor> GetColorReference(XmlElement element)
		{
			XshdColor color = GetColorFromElement(element);
			if (color != null)
				return new XshdReference<XshdColor>(color);
			else
				return new XshdReference<XshdColor>();
		}
		
		static HighlightingBrush ParseColor(string c)
		{
			if (c.StartsWith("#", StringComparison.Ordinal)) {
				int a = 255;
				int offset = 0;
				if (c.Length > 7) {
					offset = 2;
					a = Int32.Parse(c.Substring(1,2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
				}
				
				int r = Int32.Parse(c.Substring(1 + offset,2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
				int g = Int32.Parse(c.Substring(3 + offset,2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
				int b = Int32.Parse(c.Substring(5 + offset,2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
				return new SimpleHighlightingBrush(Color.FromArgb((byte)a, (byte)r, (byte)g, (byte)b));
			} else if (c.StartsWith("SystemColors.", StringComparison.Ordinal)) {
				return V2Loader.GetSystemColorBrush(null, c);
			} else {
				return new SimpleHighlightingBrush((Color)V2Loader.ColorConverter.ConvertFromInvariantString(c));
			}
		}
		
		char ruleSetEscapeCharacter;
		
		XshdRuleSet ImportRuleSet(XmlElement element)
		{
			XshdRuleSet ruleSet = new XshdRuleSet();
			ruleSet.Name = element.GetAttributeOrNull("name");
			
			if (element.HasAttribute("escapecharacter")) {
				ruleSetEscapeCharacter = element.GetAttribute("escapecharacter")[0];
			} else {
				ruleSetEscapeCharacter = '\0';
			}
			
			if (element.HasAttribute("reference")) {
				ruleSet.Elements.Add(
					new XshdImport { RuleSetReference = new XshdReference<XshdRuleSet>(
						element.GetAttribute("reference"), string.Empty
					) });
			}
			ruleSet.IgnoreCase = element.GetBoolAttribute("ignorecase");
			
			foreach (XmlElement el in element.GetElementsByTagName("KeyWords")) {
				XshdKeywords keywords = new XshdKeywords();
				keywords.ColorReference = GetColorReference(el);
				// we have to handle old syntax highlighting definitions that contain
				// empty keywords or empty keyword groups
				foreach (XmlElement node in el.GetElementsByTagName("Key")) {
					string word = node.GetAttribute("word");
					if (!string.IsNullOrEmpty(word))
						keywords.Words.Add(word);
				}
				if (keywords.Words.Count > 0) {
					ruleSet.Elements.Add(keywords);
				}
			}
			
			foreach (XmlElement el in element.GetElementsByTagName("Span")) {
				ruleSet.Elements.Add(ImportSpan(el));
			}
			
			foreach (XmlElement el in element.GetElementsByTagName("MarkPrevious")) {
				ruleSet.Elements.Add(ImportMarkPrevNext(el, false));
			}
			foreach (XmlElement el in element.GetElementsByTagName("MarkFollowing")) {
				ruleSet.Elements.Add(ImportMarkPrevNext(el, true));
			}
			
			return ruleSet;
		}
		
		static XshdRule ImportMarkPrevNext(XmlElement el, bool markFollowing)
		{
			bool markMarker = el.GetBoolAttribute("markmarker") ?? false;
			string what = Regex.Escape(el.InnerText);
			const string identifier = @"[\d\w_]+";
			const string whitespace = @"\s*";
			
			string regex;
			if (markFollowing) {
				if (markMarker) {
					regex = what + whitespace + identifier;
				} else {
					regex = "(?<=(" + what + whitespace + "))" + identifier;
				}
			} else {
				if (markMarker) {
					regex = identifier + whitespace + what;
				} else {
					regex = identifier + "(?=(" + whitespace + what + "))";
				}
			}
			return new XshdRule {
				ColorReference = GetColorReference(el),
				Regex = regex,
				RegexType = XshdRegexType.IgnorePatternWhitespace
			};
		}
		
		XshdSpan ImportSpan(XmlElement element)
		{
			XshdSpan span = new XshdSpan();
			if (element.HasAttribute("rule")) {
				span.RuleSetReference = new XshdReference<XshdRuleSet>(null, element.GetAttribute("rule"));
			}
			char escapeCharacter = ruleSetEscapeCharacter;
			if (element.HasAttribute("escapecharacter")) {
				escapeCharacter = element.GetAttribute("escapecharacter")[0];
			}
			span.Multiline = !(element.GetBoolAttribute("stopateol") ?? false);
			
			span.SpanColorReference = GetColorReference(element);
			
			span.BeginRegexType = XshdRegexType.IgnorePatternWhitespace;
			span.BeginRegex = ImportRegex(element["Begin"].InnerText,
			                              element["Begin"].GetBoolAttribute("singleword") ?? false,
			                              element["Begin"].GetBoolAttribute("startofline"));
			span.BeginColorReference = GetColorReference(element["Begin"]);
			
			string endElementText = string.Empty;
			if (element["End"] != null) {
				span.EndRegexType = XshdRegexType.IgnorePatternWhitespace;
				endElementText = element["End"].InnerText;
				span.EndRegex = ImportRegex(endElementText,
				                            element["End"].GetBoolAttribute("singleword") ?? false,
				                            null);
				span.EndColorReference = GetColorReference(element["End"]);
			}
			
			if (escapeCharacter != '\0') {
				XshdRuleSet ruleSet = new XshdRuleSet();
				if (endElementText.Length == 1 && endElementText[0] == escapeCharacter) {
					// ""-style escape
					ruleSet.Elements.Add(new XshdSpan {
					                     	BeginRegex = Regex.Escape(endElementText + endElementText),
					                     	EndRegex = ""
					                     });
				} else {
					// \"-style escape
					ruleSet.Elements.Add(new XshdSpan {
					                     	BeginRegex = Regex.Escape(escapeCharacter.ToString()),
					                     	EndRegex = "."
					                     });
				}
				if (span.RuleSetReference.ReferencedElement != null) {
					ruleSet.Elements.Add(new XshdImport { RuleSetReference = span.RuleSetReference });
				}
				span.RuleSetReference = new XshdReference<XshdRuleSet>(ruleSet);
			}
			return span;
		}
		
		static string ImportRegex(string expr, bool singleWord, bool? startOfLine)
		{
			StringBuilder b = new StringBuilder();
			if (startOfLine != null) {
				if (startOfLine.Value) {
					b.Append(@"(?<=(^\s*))");
				} else {
					b.Append(@"(?<!(^\s*))");
				}
			} else {
				if (singleWord)
					b.Append(@"\b");
			}
			for (int i = 0; i < expr.Length; i++) {
				char c = expr[i];
				if (c == '@') {
					++i;
					if (i == expr.Length)
						throw new HighlightingDefinitionInvalidException("Unexpected end of @ sequence, use @@ to look for a single @.");
					switch (expr[i]) {
						case 'C': // match whitespace or punctuation
							b.Append(@"[^\w\d_]");
							break;
						case '!': // negative lookahead
							{
								StringBuilder whatmatch = new StringBuilder();
								++i;
								while (i < expr.Length && expr[i] != '@') {
									whatmatch.Append(expr[i++]);
								}
								b.Append("(?!(");
								b.Append(Regex.Escape(whatmatch.ToString()));
								b.Append("))");
							}
							break;
						case '-': // negative lookbehind
							{
								StringBuilder whatmatch = new StringBuilder();
								++i;
								while (i < expr.Length && expr[i] != '@') {
									whatmatch.Append(expr[i++]);
								}
								b.Append("(?<!(");
								b.Append(Regex.Escape(whatmatch.ToString()));
								b.Append("))");
							}
							break;
						case '@':
							b.Append("@");
							break;
						default:
							throw new HighlightingDefinitionInvalidException("Unknown character in @ sequence.");
					}
				} else {
					b.Append(Regex.Escape(c.ToString()));
				}
			}
			if (singleWord)
				b.Append(@"\b");
			return b.ToString();
		}
	}
}
