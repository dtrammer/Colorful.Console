﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Colorful
{
    /// <summary>
    /// Wraps around the System.Console class, adding enhanced styling functionality.
    /// </summary>
    public static partial class Console
    {
        private static ColorStore colorStore;
        private static ColorManagerFactory colorManagerFactory;
        private static ColorManager colorManager;

        // Limitation of the Windows console window.
        private const int MAX_COLOR_CHANGES = 16;

        // Note that if you set ConsoleColor.Black to a different color, then the background of the
        // console will change as a side-effect!  The index of Black (in the ConsoleColor definition) is 0,
        // so avoid that index.
        private const int INITIAL_COLOR_CHANGE_COUNT_VALUE = 1;

        private static readonly string WRITELINE_TRAILER = "\r\n";
        private static readonly string WRITE_TRAILER = "";

        private static void MapToScreen(IEnumerable<KeyValuePair<string, Color>> styleMap, string trailer)
        {
            int writeCount = 1;
            foreach (KeyValuePair<string, Color> textChunk in styleMap)
            {
                System.Console.ForegroundColor = colorManager.GetConsoleColor(textChunk.Value);

                if (writeCount == styleMap.Count())
                {
                    System.Console.Write(textChunk.Key + trailer);
                }
                else
                {
                    System.Console.Write(textChunk.Key);
                }

                writeCount++;
            }

            System.Console.ResetColor();
        }

        private static void WriteInColor<T>(Action<T> action, T target, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target);
            System.Console.ResetColor();
        }

        private static void WriteChunkInColor(Action<string> action, char[] buffer, int index, int count, Color color)
        {
            string chunk = buffer.AsString().Substring(index, count);

            WriteInColor(action, chunk, color);
        }

        private static void WriteInColorAlternating<T>(Action<T> action, T target, ColorAlternator alternator)
        {
            Color color = alternator.GetNextColor(target.AsString());

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target);
            System.Console.ResetColor();
        }

        private static void WriteChunkInColorAlternating(Action<string> action, char[] buffer, int index, int count, ColorAlternator alternator)
        {
            string chunk = buffer.AsString().Substring(index, count);

            WriteInColorAlternating(action, chunk, alternator);
        }

        private static void WriteInColorStyled<T>(string trailer, T target, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(target.AsString());

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteChunkInColorStyled(string trailer, char[] buffer, int index, int count, StyleSheet styleSheet)
        {
            string chunk = buffer.AsString().Substring(index, count);

            WriteInColorStyled(trailer, chunk, styleSheet);
        }

        private static void WriteInColor<T, U>(Action<T, U> action, T target0, U target1, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1);
            System.Console.ResetColor();
        }

        private static void WriteInColorAlternating<T, U>(Action<T, U> action, T target0, U target1, ColorAlternator alternator)
        {
            string formatted = string.Format(target0.ToString(), target1.Normalize());
            Color color = alternator.GetNextColor(formatted);

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1);
            System.Console.ResetColor();
        }

        private static void WriteInColorStyled<T, U>(string trailer, T target0, U target1, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);

            string formatted = string.Format(target0.ToString(), target1.Normalize());
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(formatted);

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteInColorFormatted<T, U>(string trailer, T target0, U target1, Color styledColor, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), target1.Normalize(), new Color[] { styledColor });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter target1, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new object[] { target1.Target }, new Color[] { target1.Color });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColor<T, U>(Action<T, U, U> action, T target0, U target1, U target2, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2);
            System.Console.ResetColor();
        }

        private static void WriteInColorAlternating<T, U>(Action<T, U, U> action, T target0, U target1, U target2, ColorAlternator alternator)
        {
            string formatted = string.Format(target0.ToString(), target1, target2); // NOT FORMATTING
            Color color = alternator.GetNextColor(formatted);

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2);
            System.Console.ResetColor();
        }

        private static void WriteInColorStyled<T, U>(string trailer, T target0, U target1, U target2, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);

            string formatted = string.Format(target0.ToString(), target1, target2);
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(formatted);

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteInColorFormatted<T, U>(string trailer, T target0, U target1, U target2, Color styledColor, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new U[] { target1, target2 }.Normalize(), new Color[] { styledColor });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter target1, Formatter target2, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new object[] { target1.Target, target2.Target }, new Color[] { target1.Color, target2.Color });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColor<T, U>(Action<T, U, U, U> action, T target0, U target1, U target2, U target3, Color color)
        {
            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2, target3);
            System.Console.ResetColor();
        }

        private static void WriteInColorAlternating<T, U>(Action<T, U, U, U> action, T target0, U target1, U target2, U target3, ColorAlternator alternator)
        {
            string formatted = string.Format(target0.ToString(), target1, target2, target3);
            Color color = alternator.GetNextColor(formatted);

            System.Console.ForegroundColor = colorManager.GetConsoleColor(color);
            action.Invoke(target0, target1, target2, target3);
            System.Console.ResetColor();
        }

        private static void WriteInColorStyled<T, U>(string trailer, T target0, U target1, U target2, U target3, StyleSheet styleSheet)
        {
            TextAnnotator annotator = new TextAnnotator(styleSheet);

            string formatted = string.Format(target0.ToString(), target1, target2, target3);
            List<KeyValuePair<string, Color>> annotationMap = annotator.GetAnnotationMap(formatted);

            MapToScreen(annotationMap, trailer);
        }

        private static void WriteInColorFormatted<T, U>(string trailer, T target0, U target1, U target2, U target3, Color styledColor, Color defaultColor)
        {
            TextFormatter formatter = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = formatter.GetFormatMap(target0.ToString(), new U[] { target1, target2, target3 }.Normalize(), new Color[] { styledColor });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter target1, Formatter target2, Formatter target3, Color defaultColor)
        {
            TextFormatter styler = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = styler.GetFormatMap(target0.ToString(), new object[] { target1.Target, target2.Target, target3.Target }, new Color[] { target1.Color, target2.Color, target3.Color });

            MapToScreen(formatMap, trailer);
        }

        private static void WriteInColorFormatted<T>(string trailer, T target0, Formatter[] targets, Color defaultColor)
        {
            TextFormatter styler = new TextFormatter(defaultColor);
            List<KeyValuePair<string, Color>> formatMap = styler.GetFormatMap(target0.ToString(), targets.Select(formatter => formatter.Target).ToArray(), targets.Select(formatter => formatter.Color).ToArray());

            MapToScreen(formatMap, trailer);
        }

        private static Figlet GetFiglet(FigletFont font = null)
        {
            if (font == null)
            {
                return new Figlet();
            }
            else
            {
                return new Figlet(font);
            }
        }

        private static readonly Color blackEquivalent = Color.FromArgb(0, 0, 0);
        private static readonly Color blueEquivalent = Color.FromArgb(0, 0, 255);
        private static readonly Color cyanEquivalent = Color.FromArgb(0, 255, 255);
        private static readonly Color darkBlueEquivalent = Color.FromArgb(0, 0, 128);
        private static readonly Color darkCyanEquivalent = Color.FromArgb(0, 128, 128);
        private static readonly Color darkGrayEquivalent = Color.FromArgb(128, 128, 128);
        private static readonly Color darkGreenEquivalent = Color.FromArgb(0, 128, 0);
        private static readonly Color darkMagentaEquivalent = Color.FromArgb(128, 0, 128);
        private static readonly Color darkRedEquivalent = Color.FromArgb(128, 0, 0);
        private static readonly Color darkYellowEquivalent = Color.FromArgb(128, 128, 0);
        private static readonly Color grayEquivalent = Color.FromArgb(192, 192, 192);
        private static readonly Color greenEquivalent = Color.FromArgb(0, 255, 0);
        private static readonly Color magentaEquivalent = Color.FromArgb(255, 0, 255);
        private static readonly Color redEquivalent = Color.FromArgb(255, 0, 0);
        private static readonly Color whiteEquivalent = Color.FromArgb(255, 255, 255);
        private static readonly Color yellowEquivalent = Color.FromArgb(255, 255, 0);

        private static ColorStore GetColorStore()
        {
            ConcurrentDictionary<Color, ConsoleColor> colorMap = new ConcurrentDictionary<Color, ConsoleColor>();
            ConcurrentDictionary<ConsoleColor, Color> consoleColorMap = new ConcurrentDictionary<ConsoleColor, Color>();

            consoleColorMap.TryAdd(ConsoleColor.Black, blackEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Blue, blueEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Cyan, cyanEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkBlue, darkBlueEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkCyan, darkCyanEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkGray, darkGrayEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkGreen, darkGreenEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkMagenta, darkMagentaEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkRed, darkRedEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.DarkYellow, darkYellowEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Gray, grayEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Green, greenEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Magenta, magentaEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Red, redEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.White, whiteEquivalent);
            consoleColorMap.TryAdd(ConsoleColor.Yellow, yellowEquivalent);

            return new ColorStore(colorMap, consoleColorMap);
        }
    }
}