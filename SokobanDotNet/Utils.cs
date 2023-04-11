using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SokobanDotNet
{
    internal static class Utils
    {
        public static List<List<TileType>> DuplicateMap(List<List<TileType>> tiles)
        {
            List<List<TileType>> newList = new();
            tiles.ForEach(item => newList.Add(new(item)));

            return newList;
        }

        public static List<Tuple<int, int>> DuplicateLocations(List<Tuple<int, int>> locations)
        {
            List<Tuple<int, int>> newLocations = new();
            locations.ForEach(item => newLocations.Add(new(item.Item1, item.Item2)));
            return newLocations;
        }

        public static List<List<int>> Permute(int n)
        {
            List<List<int>> result = new List<List<int>>();
            List<int> sequence = new List<int>();
            for (int i = 0; i < n; i++) sequence.Add(i);
            PermuteHelper(result, sequence, 0);
            return result;
        }

        public static int ManhattanDistance(Tuple<int, int> from, Tuple<int, int> To) => Math.Abs(from.Item1- To.Item1) + Math.Abs(from.Item1- To.Item2);

        private static void PermuteHelper(List<List<int>> result, List<int> sequence, int index)
        {
            if (index == sequence.Count - 1)
            {
                result.Add(new List<int>(sequence));
                return;
            }
            for (int i = index; i < sequence.Count; i++)
            {
                Swap(sequence, index, i);
                PermuteHelper(result, sequence, index + 1);
                Swap(sequence, index, i);
            }
        }

        static void Swap(List<int> sequence, int i, int j)
        {
            int temp = sequence[i];
            sequence[i] = sequence[j];
            sequence[j] = temp;
        }

        public static string ActionsToString(List<PlayerAction> actions) => "[ " + actions.Aggregate("", (current, s) => current + (s + ", ")) + " ]";

        //public static void WriteLineWithColoredChars(string text, List<int> coloredIndices, List<Color> coloredColors)
        //{
        //    int startIndex = 0;
        //    for (int i = 0; i < coloredIndices.Count; i++)
        //    {
        //        int endIndex = coloredIndices[i];
        //        int length = endIndex - startIndex;
        //        Console.Write(text.Substring(startIndex, length));
        //        Console.ForegroundColor = ToConsoleColor(coloredColors[i]);
        //        Console.Write(text.Substring(endIndex, 1));
        //        Console.ResetColor();
        //        startIndex = endIndex + 1;
        //    }
        //    Console.WriteLine(text.Substring(startIndex));
        //}

        //private static ConsoleColor ToConsoleColor(Color color)
        //{
        //    ConsoleColor[] consoleColors = (ConsoleColor[])Enum.GetValues(typeof(ConsoleColor));
        //    double rRatio = (double)color.R / 255;
        //    double gRatio = (double)color.G / 255;
        //    double bRatio = (double)color.B / 255;
        //    double brightness = (rRatio * 0.299) + (gRatio * 0.587) + (bRatio * 0.114);
        //    if (brightness > 0.5)
        //    {
        //        return consoleColors[(int)ConsoleColor.Black];
        //    }
        //    else
        //    {
        //        return consoleColors[(int)ConsoleColor.White];
        //    }
        //}
    }
}
