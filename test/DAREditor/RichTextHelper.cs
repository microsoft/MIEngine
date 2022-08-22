// // Copyright (c) Microsoft. All rights reserved.
// // Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Text;
using System.Windows.Controls;
using System.Windows.Documents;


namespace DAREditor
{
    


    static class RichTextHelper
    {
        public static string GetContent(RichTextBox richTextBox)
        {
            StringBuilder textBuilder = new StringBuilder();

            void ProcessInlines(InlineCollection inlines)
            {
                foreach (var inline in inlines)
                {
                    if (inline is LineBreak)
                        textBuilder.Append(Environment.NewLine);
                    else if (inline is Run run)
                        textBuilder.Append(run.Text);
                    else if (inline is Span span)
                        ProcessInlines(span.Inlines);
                    else
                    {
                        Debug.Fail("ERROR: Unknown Inline type, add an error");
                    }
                }
            }

            foreach (var block in richTextBox.Document.Blocks)
            {
                if (block is Paragraph paragraph)
                {
                    if (textBuilder.Length != 0)
                        textBuilder.Append(Environment.NewLine);
                    ProcessInlines(paragraph.Inlines);

                }
                else
                {
                    Debug.Fail("ERROR: block is not a paragraph, add an error");
                }
            }

            return textBuilder.ToString();
        }

        public static void SetContent(RichTextBox richTextBox, string text)
        {
            var run = new Run(text);
            var paragraph = new Paragraph(run);
            BlockCollection actualBlocks = richTextBox.Document.Blocks;
            actualBlocks.Clear();
            actualBlocks.Add(paragraph);
        }
    }
}