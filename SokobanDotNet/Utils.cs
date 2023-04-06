using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SokobanDotNet
{
    internal static class Utils
    {
        public static List<List<int>> Permute(int n)
        {
            List<List<int>> result = new List<List<int>>();
            List<int> sequence = new List<int>();
            for (int i = 0; i < n; i++) sequence.Add(i);
            PermuteHelper(result, sequence, 0);
            return result;
        }

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
    }
}
