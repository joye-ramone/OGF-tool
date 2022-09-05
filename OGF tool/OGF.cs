using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OGF_tool
{
    public enum OGF
    {
        OGF4_HEADER = 1,
        OGF4_TEXTURE = 2,
        OGF4_VERTICES = 3,
        OGF4_INDICES = 4,
        OGF4_P_MAP = 5,  //---------------------- unused
        OGF4_SWIDATA = 6,
        OGF4_VCONTAINER = 7, // not used ??
        OGF4_ICONTAINER = 8, // not used ??
        OGF4_CHILDREN = 9,   // * For skeletons only
        OGF4_CHILDREN_L = 10,    // Link to child visuals
        OGF4_LODDEF2 = 11,   // + 5 channel data
        OGF4_TREEDEF2 = 12,  // + 5 channel data
        OGF4_S_BONE_NAMES = 13,  // * For skeletons only
        OGF4_S_MOTIONS = 14, // * For skeletons only
        OGF4_S_SMPARAMS = 15,    // * For skeletons only
        OGF4_S_IKDATA = 16,  // * For skeletons only
        OGF4_S_USERDATA = 17,    // * For skeletons only (Ini-file)
        OGF4_S_DESC = 18,    // * For skeletons only
        OGF4_S_MOTION_REFS = 19, // * For skeletons only
        OGF4_SWICONTAINER = 20,  // * SlidingWindowItem record container
        OGF4_GCONTAINER = 21,    // * both VB&IB
        OGF4_FASTPATH = 22,  // * extended/fast geometry
        OGF4_S_LODS = 23,    // * For skeletons only (Ini-file)
        OGF4_S_MOTION_REFS2 = 24,    // * changes in format
        OGF4_COLLISION_VERTICES = 25,
        OGF4_COLLISION_INDICES = 26,
    };

    public enum OGF_SkeletonVertType
    {
        OGF_VERTEXFORMAT_FVF_1L = 1*0x12071980,
        OGF_VERTEXFORMAT_FVF_2L = 2*0x12071980,
        OGF_VERTEXFORMAT_FVF_3L = 4*0x12071980,
        OGF_VERTEXFORMAT_FVF_4L = 5*0x12071980,
        OGF_VERTEXFORMAT_FVF_NL = 3*0x12071980,
    };

    public enum MotionKeyFlags
    {
        flTKeyPresent = (1<<0),
        flRKeyAbsent  = (1<<1),
        flTKey16IsBit = (1<<2),
        flTKeyFFT_Bit = (1<<3),
    };

    public class XRayLoader
    {
        public long chunk_pos = 0;

        uint CHUNK_COMPRESSED = 0x80000000;

        public MemoryStream mem_stream;
        public BinaryReader reader;


        public void Destroy()
        {
            mem_stream.Dispose();
            reader.Dispose();
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public int ReadInt32()
        {
            return reader.ReadInt32();
        }

        public long ReadInt64()
        {
            return reader.ReadInt64();
        }

        public float ReadFloat()
        {
            return reader.ReadSingle();
        }

        public float[] ReadVector()
        {
            float[] vec = new float[3];

            vec[0] = reader.ReadSingle();
            vec[1] = reader.ReadSingle();
            vec[2] = reader.ReadSingle();

            return vec;
        }

        public float[] ReadVector2()
        {
            float[] vec = new float[2];

            vec[0] = reader.ReadSingle();
            vec[1] = reader.ReadSingle();

            return vec;
        }

        public uint ReadUInt16()
        {
            return reader.ReadUInt16();
        }

        public uint ReadUInt32()
        {
            return reader.ReadUInt32();
        }

        public byte[] ReadBytes(int count)
        {
            return reader.ReadBytes(count);
        }

        public bool SetData(byte[] input)
        {
            if (input == null) return false;
            mem_stream = new MemoryStream(input);
            reader = new BinaryReader(mem_stream);
            return true;
        }

        public void SetStream(Stream stream)
        {
            reader = new BinaryReader(stream);
        }

        public void SetReader(BinaryReader rd)
        {
            reader = rd;
        }

        public bool find_chunk(int chunkId, bool skip = false, bool reset = false)
        {
            return find_chunkSize(chunkId, skip, reset) != 0;
        }

        public byte[] find_and_return_chunk_in_chunk(int chunkId, bool skip = false, bool reset = false)
        {
            int size = (int)find_chunkSize(chunkId, skip, reset);

            if (size > 0)
            {
                return ReadBytes(size);
            }
            else
                return null;
        }

        public uint find_chunkSize(int chunkId, bool skip = false, bool reset = false)
        {
            chunk_pos = 0;

            if (reset) reader.BaseStream.Position = 0;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                if (reader.BaseStream.Position + 8 > reader.BaseStream.Length)
                    return 0;

                uint dwType = reader.ReadUInt32();
                uint dwSize = reader.ReadUInt32();

                if (dwType == chunkId || (dwType ^ CHUNK_COMPRESSED) == chunkId)
                {
                    chunk_pos = reader.BaseStream.Position - 8;
                    return dwSize;
                }
                else
                {
                    if (reader.BaseStream.Position + dwSize < reader.BaseStream.Length)
                        reader.BaseStream.Position += dwSize;
                    else if (reader.BaseStream.Position + 8 < reader.BaseStream.Length)
                        reader.BaseStream.Position += 4;
                    else
                        return 0;
                }
            }

            return 0;
        }

        public void open_chunk(BinaryWriter w, int chunkId)
        {
            w.Write(chunkId);
            chunk_pos = w.BaseStream.Position;
            w.Write(0);     // the place for 'size'
        }

        public void close_chunk(BinaryWriter w)
        {
            if (chunk_pos == 0)
            {
                throw new InvalidOperationException("no chunk!");
            }

            long pos = w.BaseStream.Position;
            w.BaseStream.Position = chunk_pos;
            w.Write((int)(pos - chunk_pos - 4));
            w.BaseStream.Position = pos;
            chunk_pos = 0;
        }

        public string read_stringData(ref bool data)
        {
            string str = "";
            data = false;

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte[] one = { reader.ReadByte() };
                if (one[0] != 0 && one[0] != 0xA && one[0] != 0xD)
                {
                    str += Encoding.Default.GetString(one);
                }
                else
                {
                    if (one[0] == 0xD)
                    {
                        reader.ReadByte();
                        data = true;
                    }
                    break;
                }
            }
            return str;
        }

        public string read_stringZ()
        {
            string str = "";

            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                byte[] one = { reader.ReadByte() };
                if (one[0] != 0)
                {
                    str += Encoding.Default.GetString(one);
                }
                else
                {
                    break;
                }
            }
            return str;
        }

        public void write_stringZ(BinaryWriter w, string str)
        {
            List<byte> temp = new List<byte>();

            temp.AddRange(Encoding.Default.GetBytes(str));
            temp.Add(0);

            w.Write(temp.ToArray());
        }

        public void write_u32(BinaryWriter w, uint num)
        {
            w.Write(num);
        }
    }

    public class OGF_Children
    {
        public byte m_version;
        public byte m_model_type; // 1 - Without bones, 3 - Animated, 10 - Rigid
        public bool IsDM;
        public uint BrokenType;
        public bool IsCopModel;

        public Description description;
        public List<OGF_Child> childs;
        public BoneData bones;
        public IK_Data ikdata;
        public UserData userdata;
        public Lod lod;
        public MotionRefs motion_refs;
        public string motions;

        public uint chunk_size;
        public long pos;

        public OGF_Children()
        {
            this.pos = 0;
            this.chunk_size = 0;
            this.BrokenType = 0;
            this.motions = "";
            this.IsDM = false;
            this.m_model_type = 0;
            this.m_version = 0;
            this.description = null;
            this.childs = new List<OGF_Child>();
            this.bones = null;
            this.ikdata = null;
            this.userdata = null;
            this.lod = null;
            this.motion_refs = null;
            this.IsCopModel = false;
        }

        public bool IsSkeleton()
        {
            return m_model_type == 3 || m_model_type == 10;
        }

        public bool IsAnimated()
        {
            return m_model_type == 3;
        }
    }

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

    public class UserData
    {
        public long pos;
        public int old_size;
        public string userdata;

        public UserData()
        {
            this.pos = 0;
            this.old_size = 0;
            this.userdata = "";
        }

        public byte[] data()
        {
            List<byte> temp = new List<byte>();

            temp.AddRange(Encoding.Default.GetBytes(userdata));
            temp.Add(0);

            return temp.ToArray();
        }

        public uint chunk_size()
        {
            return (uint)userdata.Length + 1;
        }
    }

    public class Lod
    {
        public long pos;
        public int old_size;
        public string lod_path;
        public bool data_str;

        public Lod()
        {
            this.pos = 0;
            this.old_size = 0;
            this.lod_path = "";
            this.data_str = true;
        }

        public byte[] data()
        {
            List<byte> temp = new List<byte>();

            temp.AddRange(Encoding.Default.GetBytes(lod_path));
            if (data_str)
            {
                temp.Add(0xD);
                temp.Add(0xA);
            }
            else
                temp.Add(0);

            return temp.ToArray();
        }

        public uint chunk_size()
        {
            return (uint)lod_path.Length + (uint)(data_str ? 2 : 1);
        }
    }

    public class BoneData
    {
        public long pos;
        public int old_size;

        public List<string> bone_names;
        public List<string> parent_bone_names;
        public List<byte[]> fobb;
        public List<List<int>> bone_childs;

        public BoneData()
        {
            this.pos = 0;
            this.old_size = 0;
            this.bone_names = new List<string>();
            this.parent_bone_names = new List<string>();
            this.fobb = new List<byte[]>();
            this.bone_childs = new List<List<int>>();
        }

        public uint chunk_size()
        {
            uint temp = 4;                                  // count byte

            for (int i = 0; i < bone_names.Count; i++)
            {
                temp += (uint)bone_names[i].Length + 1;          // bone name
                temp += (uint)parent_bone_names[i].Length + 1;   // parent bone name
                temp += 60;                                 // obb
            }

            return temp;
        }

        public byte[] data(bool repair)
        {
            List<byte> temp = new List<byte>();

            temp.AddRange(BitConverter.GetBytes(bone_names.Count));

            for (int i = 0; i < bone_names.Count; i++)
            {
                temp.AddRange(Encoding.Default.GetBytes(bone_names[i]));       // bone name
                temp.Add(0);
                temp.AddRange(Encoding.Default.GetBytes(parent_bone_names[i]));// parent bone name
                temp.Add(0);

                if (repair)
                {
                    for (int j = 0; j < 60; j++)
                        temp.Add(0);
                }
                else
                    temp.AddRange(fobb[i]);                               // obb
            }

            return temp.ToArray();
        }
    }

    public class IK_Data
    {
        public int old_size;

        public List<string> materials;
        public List<float> mass;
        public List<uint> version;
        public List<float[]> center_mass;
        public List<float[]> position;
        public List<float[]> rotation;
        public List<List<byte[]>> bytes_1;

        public IK_Data()
        {
            this.old_size = 0;
            this.materials = new List<string>();
            this.mass = new List<float>();
            this.version = new List<uint>();
            this.center_mass = new List<float[]>();
            this.position = new List<float[]>();
            this.rotation = new List<float[]>();
            this.bytes_1 = new List<List<byte[]>>();
        }

        public uint chunk_size()
        {
            uint temp = 0;
            for (int i = 0; i < materials.Count; i++)
            {
                temp += 4;
                temp += (uint)materials[i].Length + 1;       // bone name
                temp += 112;

                temp += 4;
                temp += 16 * 3;
                temp += 4;
                temp += 4;
                temp += 4;
                temp += 4;
                temp += 4;

                if (version[i] > 0)
                    temp += 4;

                temp += 12;
                temp += 12;
                temp += 4;
                temp += 12;
            }

            return temp;
        }

        public byte[] data()
        {
            List<byte> temp = new List<byte>();

            for (int i = 0; i < materials.Count; i++)
            {
                temp.AddRange(BitConverter.GetBytes(version[i]));
                temp.AddRange(Encoding.Default.GetBytes(materials[i]));
                temp.Add(0);
                for (int j = 0; j < bytes_1[i].Count; j++)
                    temp.AddRange(bytes_1[i][j]);

                temp.AddRange(BitConverter.GetBytes(rotation[i][0]));
                temp.AddRange(BitConverter.GetBytes(rotation[i][1]));
                temp.AddRange(BitConverter.GetBytes(rotation[i][2]));

                temp.AddRange(BitConverter.GetBytes(position[i][0]));
                temp.AddRange(BitConverter.GetBytes(position[i][1]));
                temp.AddRange(BitConverter.GetBytes(position[i][2]));

                temp.AddRange(BitConverter.GetBytes(mass[i]));

                temp.AddRange(BitConverter.GetBytes(center_mass[i][0]));
                temp.AddRange(BitConverter.GetBytes(center_mass[i][1]));
                temp.AddRange(BitConverter.GetBytes(center_mass[i][2]));
            }

            return temp.ToArray();
        }
    }

    public class Description
    {
        public long pos;
        public int old_size;
        public bool four_byte;

        public string m_source;
        public string m_export_tool;
        public long m_export_time;
        public string m_owner_name;
        public long m_creation_time;
        public string m_export_modif_name_tool;
        public long m_modified_time;

        public Description()
        {
            this.pos = 0;
            this.old_size = 0;
            this.four_byte = false;
        }

        public byte[] data()
        {
            List<byte> temp = new List<byte>();

            temp.AddRange(Encoding.Default.GetBytes(m_source));
            temp.Add(0);
            temp.AddRange(Encoding.Default.GetBytes(m_export_tool));
            temp.Add(0);
            if (!four_byte)
                temp.AddRange(BitConverter.GetBytes(m_export_time));
            else
                temp.AddRange(BitConverter.GetBytes((uint)m_export_time));
            temp.AddRange(Encoding.Default.GetBytes(m_owner_name));
            temp.Add(0);
            if (!four_byte)
                temp.AddRange(BitConverter.GetBytes(m_creation_time));
            else
                temp.AddRange(BitConverter.GetBytes((uint)m_creation_time));
            temp.AddRange(Encoding.Default.GetBytes(m_export_modif_name_tool));
            temp.Add(0);
            if (!four_byte)
                temp.AddRange(BitConverter.GetBytes(m_modified_time));
            else
                temp.AddRange(BitConverter.GetBytes((uint)m_modified_time));

            return temp.ToArray();
        }

        public uint chunk_size()
        {
            uint time_size = (uint)(four_byte ? 4 : 8);
            uint size = 0;
            size += (uint)m_source.Length + 1;
            size += (uint)m_export_tool.Length + 1;
            size += time_size;
            size += (uint)m_owner_name.Length + 1;
            size += time_size;
            size += (uint)m_export_modif_name_tool.Length + 1;
            size += time_size;
            return size;
        }
    }

    public class SSkelVert
    {
        public float[] uv;
        public float[] offs;
        public float[] norm;
        public float[] tang;
        public float[] binorm;

        public SSkelVert()
        {
            uv = new float[2];
            offs = new float[3];
            norm = new float[3];
            tang = new float[3];
            binorm = new float[3];
        }
    };

    public class SSkelFace
    {
	    public ushort[] v;
        public SSkelFace()
        {
            v = new ushort[3];
        }
    };

    public class OGF_Child
    {
        public string m_texture;
        public string m_shader;

        public OGF_Child(long _pos, long _parent_pos, uint _chunk_size, int _old_size, string texture, string shader)
        {
            Vertices = new List<SSkelVert>();
            Faces = new List<SSkelFace>();
            pos = _pos;
            parent_pos = _parent_pos;
            m_texture = texture;
            m_shader = shader;
            old_size = _old_size;
            chunk_size = _chunk_size;
            link_type = 0;
            faces = 0;
            verts = 0;
            to_delete = false;
        }

        public long pos;
        public long parent_pos;
        public uint chunk_size;
        public int old_size;

        public uint link_type;
        public long faces, verts;
        public bool to_delete;

        public List<SSkelVert> Vertices;
        public List<SSkelFace> Faces;

        public uint NewSize()
        {
            return (uint)(m_texture.Length + m_shader.Length + 2 - old_size);
        }

        public byte[] data()
        {
            List<byte> temp = new List<byte>();

            temp.AddRange(BitConverter.GetBytes(2));
            temp.AddRange(BitConverter.GetBytes(m_texture.Length + m_shader.Length + 2));

            temp.AddRange(Encoding.Default.GetBytes(m_texture));
            temp.Add(0);
            temp.AddRange(Encoding.Default.GetBytes(m_shader));
            temp.Add(0);

            return temp.ToArray();

        }
    }
}
