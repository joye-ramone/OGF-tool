﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OGF_tool
{
    public class MotionRefs
    {
        public long pos;
        public List<string> refs;
        public bool soc;
        public int old_size;

        public MotionRefs()
        {
            this.pos = 0;
            this.old_size = 0;
            this.refs = new List<string>();
            this.soc = false;
        }

        public uint chunk_size(bool v3)
        {
            uint temp = (uint)(v3 ? 0 : 4);
            foreach (var text in refs)
                temp += (uint)text.Length + 1;
            return temp;
        }
        public byte[] count()
        {
            return BitConverter.GetBytes(refs.Count);
        }
        public byte[] data(bool v3)
        {
            List<byte> temp = new List<byte>();

            if (!v3)
            {
                foreach (var str in refs)
                {
                    temp.AddRange(Encoding.Default.GetBytes(str));
                    temp.Add(0);
                }
            }
            else
            {
                string strref = refs[0];
                if (refs.Count > 1)
                {
                    strref += ",";
                    for (int i = 1; i < refs.Count; i++)
                        strref += refs[i] + ",";
                }

                temp.AddRange(Encoding.Default.GetBytes(strref));
                temp.Add(0);
            }

            return temp.ToArray();
        }
    }
}