using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace CSCom
{
    /// <summary>
    /// Implements a data structure for the np object map.
    /// </summary>
    [Serializable]
    public class NPMessage
    {
        #region Construction

        /// <summary>
        /// Make a JMessage from namepaths data
        /// </summary>
        /// <param name="namepaths"></param>
        /// <param name="values"></param>
        public NPMessage(int type, NPMessageNamepathData[] data, string message = null)
            : this((NPMessageType)type, data, message)
        {
        }

        /// <summary>
        /// Make a JMessage from namepaths data
        /// </summary>
        /// <param name="namepaths"></param>
        /// <param name="values"></param>
        public NPMessage(NPMessageType type, NPMessageNamepathData[] data, string message = null)
        {
            if (data == null)
                data = new NPMessageNamepathData[0];

            m_NamePaths = data;
            Text = message;
            MessageType = type;
        }

        #endregion

        #region Properties

        /// <summary>
        /// The message type.
        /// </summary>
        public NPMessageType MessageType { get; private set; } = NPMessageType.Warning;

        /// <summary>
        /// The string message to send.
        /// </summary>
        public string Text { get; private set; } = null;

        /// <summary>
        /// The value to serialize
        /// </summary>
        NPMessageNamepathData[] m_NamePaths;

        /// <summary>
        /// True if has any namepaths
        /// </summary>
        public int NamepathsCount
        {
            get
            {
                if (m_NamePaths == null)
                    return 0;
                return m_NamePaths.Length;
            }
        }

        /// <summary>
        /// The data collection in the form of namepaths.
        /// </summary>
        public NPMessageNamepathData[] NamePaths
        {
            get { return m_NamePaths; }
        }

        #endregion


        #region String representation

        public override string ToString()
        {
            StringBuilder wr = new StringBuilder();
            wr.AppendLine("Message type: " + MessageType + " (" + ((int)MessageType) + ")");
            wr.AppendLine("Message text: " + Text);
            if (NamepathsCount > 0)
                foreach (var npd in NamePaths)
                {
                    wr.AppendLine("\t" + npd.Namepath + ": " + npd.Value);
                }
            return wr.ToString();
        }

        #endregion

        #region Static helpers

        public static NPMessage FromValue(object val, NPMessageType type = NPMessageType.Response, string text = null)
        {
            return new NPMessage(type, new NPMessageNamepathData[]
            {
                new NPMessageNamepathData("",val)
            }, text);
        }

        #endregion
    }

    /// <summary>
    /// Definitions of the compound 
    /// </summary>
    public enum NPMessageType : int
    {
        Error = 1,
        Warning = 2,
        Create = 4,
        Destroy = 8,
        Invoke = 16,
        Get = 32,
        Set = 64,
        Response=128,
    }

    /// <summary>
    /// Implements a specific nampath data.
    /// </summary>
    [Serializable]
    public class NPMessageNamepathData : ISerializable
    {
        public NPMessageNamepathData()
        {
        }

        public NPMessageNamepathData(string namepath, object value = null)
        {
            Namepath = namepath;
            Value = value;
        }


        /// <summary>
        /// The value of the namepath
        /// </summary>
        public object Value;

        /// <summary>
        /// The namepath of the object.
        /// </summary>
        public string Namepath;

        #region special serialization

        protected enum ValueSerializationType : byte
        {
            FastArray = 1,
            Normal = 2
        }

        protected NPMessageNamepathData(SerializationInfo info, StreamingContext context)
        {
            Namepath = (string)info.GetValue("Namepath", typeof(string));
            ValueSerializationType vst = (ValueSerializationType)info.GetValue("vst", typeof(ValueSerializationType));
            Type vtype;
            switch (vst)
            {
                case ValueSerializationType.FastArray:
                    // Reding the dimensions.
                    try
                    {
                        int[] dims = (int[])info.GetValue("dims", typeof(int[]));

                        // reading the value.
                        vtype = (Type)info.GetValue("Value.Type", typeof(Type));
                        Array stored = (Array)info.GetValue("Value", vtype);

                        // converting to new array.
                        Array val = Array.CreateInstance(vtype.GetElementType(), dims);
                        //Array.Copy(stored, val, stored.Length);
                        Buffer.BlockCopy(stored, 0, val, 0, stored.Length * Marshal.SizeOf(vtype.GetElementType()));
                        Value = val;
                    }
                    catch(Exception ex)
                    {
                        throw new Exception("Error at special serialization of NPMessageNamepathInfo",ex);
                    }
                    break;
                case ValueSerializationType.Normal:
                    vtype = (Type)info.GetValue("Value.Type", typeof(Type));
                    if (vtype != null)
                        Value = info.GetValue("Value", vtype);
                    else Value = null;
                    break;
                default:
                    throw new Exception("Cannot find value serialization type.");
            }
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("Namepath", Namepath);
            bool doFastArray = false;
            if (Value != null)
            {
                Type vtype = Value.GetType();
                bool isarray = false;
                bool isSimple = IsSimple(vtype, out isarray);
                doFastArray = isarray && isSimple;
            }
            if (doFastArray)
            {
                // memory copy as byte.
                // see how fast this is.
                Type vtype = Value.GetType();
                info.AddValue("vst", ValueSerializationType.FastArray); // means value at the type (or unknown value type).
                Array ar = Value as Array;
                int[] dims = new int[ar.Rank];
                for(int i=0;i<dims.Length;i++)
                {
                    dims[i] = ar.GetLength(i);
                }
                
                
                Array storeAr = Array.CreateInstance(vtype.GetElementType(), ar.Length);
                Buffer.BlockCopy(ar, 0, storeAr, 0, ar.Length * Marshal.SizeOf(vtype.GetElementType()));
                //Array.Copy(ar, storeAr, ar.Length);

                info.AddValue("dims", dims);
                info.AddValue("Value.Type", storeAr.GetType());
                info.AddValue("Value", storeAr);
            }
            else
            {
                info.AddValue("vst", ValueSerializationType.Normal); // means value at the type (or unknown value type).
                info.AddValue("Value.Type", Value == null ? null : Value.GetType());
                if (Value != null)
                    info.AddValue("Value", Value);
            }
        }

        #endregion

        #region Binary arrays

        //private static byte[] ToByteArray(Array source, Type elType)
        //{
        //    GCHandle handle = GCHandle.Alloc(source, GCHandleType.Pinned);
        //    try
        //    {
        //        IntPtr pointer = handle.AddrOfPinnedObject();
        //        byte[] destination = new byte[source.Length * Marshal.SizeOf(elType)];
        //        Marshal.Copy(pointer, destination, 0, destination.Length);
        //        return destination;
        //    }
        //    finally
        //    {
        //        if (handle.IsAllocated)
        //            handle.Free();
        //    }
        //}

        //private static Array FromByteArray(byte[] source, Type elType, int[] dims)
        //{
        //    Array destination = Array.CreateInstance(elType, dims);
        //    GCHandle handle = GCHandle.Alloc(destination, GCHandleType.Pinned);
        //    try
        //    {
        //        IntPtr pointer = handle.AddrOfPinnedObject();
        //        Marshal.Copy(source, 0, pointer, source.Length);
        //        return destination;
        //    }
        //    finally
        //    {
        //        if (handle.IsAllocated)
        //            handle.Free();
        //    }
        //}

        #endregion

        #region type info

        static bool IsSimple(Type t)
        {
            bool isar = false;
            return IsSimple(t, out isar);
        }

        static bool IsSimple(Type type, out bool isArray)
        {
            isArray = type.IsArray;
            if (isArray)
                return IsSimple(type.GetElementType());
            return type.IsPrimitive
              || type.IsEnum
              || type.Equals(typeof(string))
              || type.Equals(typeof(decimal));
        }

        #endregion
    }
}
