using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CSCom
{
    public class FastLVNetObjConverter
    {
        /// <summary>
        /// Cannot be static since labview will not see it.
        /// </summary>
        public FastLVNetObjConverter()
        {
        }


        public enum DataTypes : int
        {
            Other = 0,
            Int8 = 1,
            Byte = 2,
            Int16 = 3,
            UInt16 = 4,
            Int32 = 5,
            UInt32 = 6,
            Int64 = 7,
            UInt64 = 8,
            Single = 9,
            Double = 10
        }

        public static object CreateVerylargeArrayOfDouble(int cnt, int rank)
        {
            int[] dims = new int[rank];
            for (int i = 0; i < rank; i++)
            {
                dims[i] = cnt;
            }

            return Array.CreateInstance(typeof(double), dims);
        }

        public static int GetArrayConvertType(object o)
        {
            if (o == null)
                return (int)DataTypes.Other;
            Type t = o.GetType();
            if (!t.IsArray)
                return (int)DataTypes.Other;

            t = t.GetElementType();
            if (t == typeof(sbyte))
                return (int)DataTypes.Int8;
            else if (t == typeof(byte))
                return (int)DataTypes.Byte;

            else if (t == typeof(Int16))
                return (int)DataTypes.Int16;
            else if (t == typeof(UInt16))
                return (int)DataTypes.UInt16;

            else if (t == typeof(Int32))
                return (int)DataTypes.Int32;
            else if (t == typeof(UInt32))
                return (int)DataTypes.UInt32;

            else if (t == typeof(Int64))
                return (int)DataTypes.Int64;
            else if (t == typeof(UInt64))
                return (int)DataTypes.UInt64;

            else if (t == typeof(float))
                return (int)DataTypes.Single;
            else if (t == typeof(double))
                return (int)DataTypes.Double;

            else return (int)DataTypes.Other;
        }

        public static T[] ConvertToFlatArray<T>(object o, out int[] dims)
        {
            Array ar = o as Array;
            if (ar == null)
            {
                throw new Exception("Object is of type, " + o.GetType() + " and not an array.");
            }

            dims = new int[ar.Rank];
            for (int i=0;i<dims.Length;i++)
                dims[i] = ar.GetLength(i);

            T[] converted = new T[ar.Length];
            Buffer.BlockCopy(ar, 0, converted, 0, converted.Length * System.Runtime.InteropServices.Marshal.SizeOf(typeof(T)));
            return converted;
        }

        public static SByte[] ConvertToInt8(object o, out int[] dims)
        {
            return ConvertToFlatArray<SByte>(o, out dims);
        }

        public static Byte[] ConvertToByte(object o, out int[] dims)
        {
            return ConvertToFlatArray<Byte>(o, out dims);
        }

        public static Int16[] ConvertToInt16(object o, out int[] dims)
        {
            return ConvertToFlatArray<Int16>(o, out dims);
        }

        public static UInt16[] ConvertToUInt16(object o, out int[] dims)
        {
            return ConvertToFlatArray<UInt16>(o, out dims);
        }

        public static Int32[] ConvertToInt32(object o, out int[] dims)
        {
            return ConvertToFlatArray<Int32>(o, out dims);
        }

        public static UInt32[] ConvertToUInt32(object o, out int[] dims)
        {
            return ConvertToFlatArray<UInt32>(o, out dims);
        }

        public static Int64[] ConvertToInt64(object o, out int[] dims)
        {
            return ConvertToFlatArray<Int64>(o, out dims);
        }

        public static UInt64[] ConvertToUInt64(object o, out int[] dims)
        {
            return ConvertToFlatArray<UInt64>(o, out dims);
        }

        public static Single[] ConvertToSingle(object o, out int[] dims)
        {
            return ConvertToFlatArray<Single>(o, out dims);
        }

        public static Double[] ConvertToDouble(object o, out int[] dims)
        {
            return ConvertToFlatArray<Double>(o, out dims);
        }

    }


}
