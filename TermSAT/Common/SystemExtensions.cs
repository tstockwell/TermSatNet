﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace TermSAT.Common
{
    public static class SystemExtensions
    {
        public static string ToHexadecimalString(this BitArray bits)
        {
            StringBuilder sb = new StringBuilder(bits.Length / 4);

            for (int i = 0; i < bits.Length; i += 4)
            {
                int v = (bits[i] ? 8 : 0) |
                        (bits[i + 1] ? 4 : 0) |
                        (bits[i + 2] ? 2 : 0) |
                        (bits[i + 3] ? 1 : 0);

                sb.Append(v.ToString("x1")); // Or "X1"
            }

            return sb.ToString(); 
        }

        public static BitArray ToBitArray(this string hexArray)
        {
            var bits = new BitArray(hexArray.Length*4);
            int i = 0;
            Action<bool, bool, bool, bool> setBits = (bool a, bool b, bool c, bool d) => {
                bits[i++] = a;
                bits[i++] = b;
                bits[i++] = c;
                bits[i++] = d;
            };
            foreach (var c in hexArray.ToLower())
            {
                switch (c)
                {
                    case '0': { setBits(false, false, false, false); } break;
                    case '1': { setBits(false, false, false, true);  } break;
                    case '2': { setBits(false, false, true,  false); } break;
                    case '3': { setBits(false, false, true,  true);  } break;
                    case '4': { setBits(false, true,  false, false); } break;
                    case '5': { setBits(false, true,  false, true);  } break;
                    case '6': { setBits(false, true,  true,  false); } break;
                    case '7': { setBits(false, true,  true,  true);  } break;
                    case '8': { setBits(true,  false, false, false); } break;
                    case '9': { setBits(true,  false, false, true);  } break;
                    case 'a': { setBits(true,  false, true,  false); } break;
                    case 'b': { setBits(true,  false, true,  true);  } break;
                    case 'c': { setBits(true,  true,  false, false); } break;
                    case 'd': { setBits(true,  true,  false, true);  } break;
                    case 'e': { setBits(true,  true,  true,  false); } break;
                    case 'f': { setBits(true,  true,  true,  true);  } break;
                }
            }
            return bits;
        }
    }
}