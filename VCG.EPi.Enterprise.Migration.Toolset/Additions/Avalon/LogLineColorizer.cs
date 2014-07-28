using VCG.EPi.Enterprise.Logging;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Rendering;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;

namespace VCG.EPi.Enterprise.Migration.Toolset.Additions.Avalon
{
    public class LogLineColorizer : DocumentColorizingTransformer
    {
        protected readonly Color DateForegroundColor = new Color() { A = 255, R = 140, G = 140, B = 140 };
        protected readonly Color NameForegroundColor = new Color() { A = 255, R = 78, G = 201, B = 176 };
        protected readonly Color NumberForegroundColor = new Color() { A = 255, R = 212, G = 183, B = 131 };

        protected readonly Regex NameSelector = new Regex("'(?:[^\']+|\\.)*'", RegexOptions.Compiled);
        protected readonly Regex NumberSelector = new Regex("\\s[0-9]{0,}[\\s$]", RegexOptions.Compiled);

        protected readonly Dictionary<string, Color> MessageColors = new Dictionary<string, Color>()
        {
            //{ "Message", new Color() { A = 255, R = 78, G = 201, B = 176 } },
            { "Message", new Color() { A = 255, R = 86, G = 156, B = 214 } },
            { "Warning", new Color() { A = 255, R = 214, G = 157, B = 133 } },
            { "Error", new Color() { A = 255, R = 195, G = 73, B = 64 } }
        };

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStartOffset = line.Offset;
            string text = CurrentContext.Document.GetText(line);
            
            if (text.Length < 18) return;

            int timeIndex = text.IndexOf(':', 17);
            if (timeIndex < 0 || timeIndex > 22) return;

            SetColor(lineStartOffset, lineStartOffset + timeIndex + 1, DateForegroundColor);

            int messageTypeIndex = text.IndexOf(':', timeIndex + 1);
            if (messageTypeIndex < 0) return;

            string messageType = text.Substring(timeIndex + 1, messageTypeIndex - timeIndex - 1).Trim();
            
            if (!MessageColors.ContainsKey(messageType)) return;

            SetColor(lineStartOffset + timeIndex + 1, lineStartOffset + messageTypeIndex + 1, MessageColors[messageType]);

            SetColor(NumberSelector, text, messageTypeIndex + 1, lineStartOffset, NumberForegroundColor);
            SetColor(NameSelector, text, messageTypeIndex + 1, lineStartOffset, NameForegroundColor);
        }

        protected void SetColor(Regex regex, string text, int start, int offset, Color color)
        {
            MatchCollection matches = regex.Matches(text, start);

            foreach (Match match in matches)
            {
                if (match.Success)
                    SetColor(offset + match.Index, offset + match.Index + match.Length, color);
            }

        }

        protected void SetColor(int start, int end, Color color)
        {
            base.ChangeLinePart(start, end, (VisualLineElement element) =>
            {
                Typeface tf = element.TextRunProperties.Typeface;
                element.TextRunProperties.SetForegroundBrush(new SolidColorBrush(color));
            });

        }
    }
}
