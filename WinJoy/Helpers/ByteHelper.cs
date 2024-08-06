using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WinJoy.Helpers
{
    internal static class ByteHelper
    {
        internal static bool IsBitSet(int value, int bit) => (value & bit) != 0;

        internal static ushort[] Decode3ByteGroup(byte[] group)
        {
            if (group.Length % 3 != 0)
            {
                throw new ArgumentException("Invalid byte groups");
            }

            var result = new ushort[group.Length / 3 * 2];
            var resultIndex = 0;
            for (var i = 0; i < group.Length; i += 3)
            {
                result[resultIndex] = (ushort)((group[i + 1] << 8) & 0xF00 | group[i]);
                resultIndex++;
                result[resultIndex] = (ushort)((group[i + 2] << 4) | (group[i + 1] >> 4));
                resultIndex++;
            }

            return result;
        }
    }
}
