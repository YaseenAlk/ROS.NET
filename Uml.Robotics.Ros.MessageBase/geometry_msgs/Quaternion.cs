using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Uml.Robotics.Ros;


namespace Messages.geometry_msgs
{
    public class Quaternion : RosMessage
    {

            public double x = new double();
            public double y = new double();
            public double z = new double();
            public double w = new double();


        public override string MD5Sum() { return "a779879fadf0160734f906b8c19c7004"; }
        public override bool HasHeader() { return false; }
        public override bool IsMetaType() { return false; }
        public override string MessageDefinition() { return @"float64 x
float64 y
float64 z
float64 w"; }
        public override string MessageType { get { return "geometry_msgs/Quaternion"; } }
        public override bool IsServiceComponent() { return false; }

        public Quaternion()
        {

        }

        public Quaternion(byte[] SERIALIZEDSTUFF)
        {
            Deserialize(SERIALIZEDSTUFF);
        }

        public Quaternion(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            Deserialize(SERIALIZEDSTUFF, ref currentIndex);
        }



        public override void Deserialize(byte[] SERIALIZEDSTUFF, ref int currentIndex)
        {
            int arraylength = -1;
            bool hasmetacomponents = false;
            object __thing;
            int piecesize = 0;
            byte[] thischunk, scratch1, scratch2;
            IntPtr h;

            //x
            piecesize = Marshal.SizeOf(typeof(double));
            h = IntPtr.Zero;
            if (SERIALIZEDSTUFF.Length - currentIndex != 0)
            {
                h = Marshal.AllocHGlobal(piecesize);
                Marshal.Copy(SERIALIZEDSTUFF, currentIndex, h, piecesize);
            }
            if (h == IntPtr.Zero) throw new Exception("Memory allocation failed");
            x = (double)Marshal.PtrToStructure(h, typeof(double));
            Marshal.FreeHGlobal(h);
            currentIndex+= piecesize;
            //y
            piecesize = Marshal.SizeOf(typeof(double));
            h = IntPtr.Zero;
            if (SERIALIZEDSTUFF.Length - currentIndex != 0)
            {
                h = Marshal.AllocHGlobal(piecesize);
                Marshal.Copy(SERIALIZEDSTUFF, currentIndex, h, piecesize);
            }
            if (h == IntPtr.Zero) throw new Exception("Memory allocation failed");
            y = (double)Marshal.PtrToStructure(h, typeof(double));
            Marshal.FreeHGlobal(h);
            currentIndex+= piecesize;
            //z
            piecesize = Marshal.SizeOf(typeof(double));
            h = IntPtr.Zero;
            if (SERIALIZEDSTUFF.Length - currentIndex != 0)
            {
                h = Marshal.AllocHGlobal(piecesize);
                Marshal.Copy(SERIALIZEDSTUFF, currentIndex, h, piecesize);
            }
            if (h == IntPtr.Zero) throw new Exception("Memory allocation failed");
            z = (double)Marshal.PtrToStructure(h, typeof(double));
            Marshal.FreeHGlobal(h);
            currentIndex+= piecesize;
            //w
            piecesize = Marshal.SizeOf(typeof(double));
            h = IntPtr.Zero;
            if (SERIALIZEDSTUFF.Length - currentIndex != 0)
            {
                h = Marshal.AllocHGlobal(piecesize);
                Marshal.Copy(SERIALIZEDSTUFF, currentIndex, h, piecesize);
            }
            if (h == IntPtr.Zero) throw new Exception("Memory allocation failed");
            w = (double)Marshal.PtrToStructure(h, typeof(double));
            Marshal.FreeHGlobal(h);
            currentIndex+= piecesize;
        }

        public override byte[] Serialize(bool partofsomethingelse)
        {
            int currentIndex=0, length=0;
            bool hasmetacomponents = false;
            byte[] thischunk, scratch1, scratch2;
            List<byte[]> pieces = new List<byte[]>();
            GCHandle h;

            //x
            scratch1 = new byte[Marshal.SizeOf(typeof(double))];
            h = GCHandle.Alloc(scratch1, GCHandleType.Pinned);
            Marshal.StructureToPtr(x, h.AddrOfPinnedObject(), false);
            h.Free();
            pieces.Add(scratch1);
            //y
            scratch1 = new byte[Marshal.SizeOf(typeof(double))];
            h = GCHandle.Alloc(scratch1, GCHandleType.Pinned);
            Marshal.StructureToPtr(y, h.AddrOfPinnedObject(), false);
            h.Free();
            pieces.Add(scratch1);
            //z
            scratch1 = new byte[Marshal.SizeOf(typeof(double))];
            h = GCHandle.Alloc(scratch1, GCHandleType.Pinned);
            Marshal.StructureToPtr(z, h.AddrOfPinnedObject(), false);
            h.Free();
            pieces.Add(scratch1);
            //w
            scratch1 = new byte[Marshal.SizeOf(typeof(double))];
            h = GCHandle.Alloc(scratch1, GCHandleType.Pinned);
            Marshal.StructureToPtr(w, h.AddrOfPinnedObject(), false);
            h.Free();
            pieces.Add(scratch1);
            // combine every array in pieces into one array and return it
            int __a_b__f = pieces.Sum((__a_b__c)=>__a_b__c.Length);
            int __a_b__e=0;
            byte[] __a_b__d = new byte[__a_b__f];
            foreach(var __p__ in pieces)
            {
                Array.Copy(__p__,0,__a_b__d,__a_b__e,__p__.Length);
                __a_b__e += __p__.Length;
            }
            return __a_b__d;
        }

        public override void Randomize()
        {
            int arraylength = -1;
            Random rand = new Random();
            int strlength;
            byte[] strbuf, myByte;

            //x
            x = (rand.Next() + rand.NextDouble());
            //y
            y = (rand.Next() + rand.NextDouble());
            //z
            z = (rand.Next() + rand.NextDouble());
            //w
            w = (rand.Next() + rand.NextDouble());
        }

        public override bool Equals(RosMessage ____other)
        {
            if (____other == null)
                return false;
            bool ret = true;
            geometry_msgs.Quaternion other = (Messages.geometry_msgs.Quaternion)____other;

            ret &= x == other.x;
            ret &= y == other.y;
            ret &= z == other.z;
            ret &= w == other.w;
            // for each SingleType st:
            //    ret &= {st.Name} == other.{st.Name};
            return ret;
        }
    }
}