//
// C#
// dkxce APRS Client Loop Sender
// v 0.4, 05.06.2024
// https://github.com/dkxce/APRSClientLoopSender
// en,ru,1251,utf-8
//

using System;

namespace APRSClientLoopSender
{
    internal class GeoUtils
    {
        // Рассчет расстояния       
        #region LENGTH
        public static float GetLengthMeters(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            // use fastest
            float result = (float)GetLengthMetersD(StartLat, StartLong, EndLat, EndLong, radians);

            if (float.IsNaN(result))
            {
                result = (float)GetLengthMetersC(StartLat, StartLong, EndLat, EndLong, radians);
                if (float.IsNaN(result))
                {
                    result = (float)GetLengthMetersE(StartLat, StartLong, EndLat, EndLong, radians);
                    if (float.IsNaN(result))
                        result = 0;
                };
            };

            return result;
        }

        // Slower
        public static uint GetLengthMetersA(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            double D2R = Math.PI / 180;     // Преобразование градусов в радианы

            double a = 6378137.0000;     // WGS-84 Equatorial Radius (a)
            double f = 1 / 298.257223563;  // WGS-84 Flattening (f)
            double b = (1 - f) * a;      // WGS-84 Polar Radius
            double e2 = (2 - f) * f;      // WGS-84 Квадрат эксцентричности эллипсоида  // 1-(b/a)^2

            // Переменные, используемые для вычисления смещения и расстояния
            double fPhimean;                           // Средняя широта
            double fdLambda;                           // Разница между двумя значениями долготы
            double fdPhi;                           // Разница между двумя значениями широты
            double fAlpha;                           // Смещение
            double fRho;                           // Меридианский радиус кривизны
            double fNu;                           // Поперечный радиус кривизны
            double fR;                           // Радиус сферы Земли
            double fz;                           // Угловое расстояние от центра сфероида
            double fTemp;                           // Временная переменная, использующаяся в вычислениях

            // Вычисляем разницу между двумя долготами и широтами и получаем среднюю широту
            // предположительно что расстояние между точками << радиуса земли
            if (!radians)
            {
                fdLambda = (StartLong - EndLong) * D2R;
                fdPhi = (StartLat - EndLat) * D2R;
                fPhimean = ((StartLat + EndLat) / 2) * D2R;
            }
            else
            {
                fdLambda = StartLong - EndLong;
                fdPhi = StartLat - EndLat;
                fPhimean = (StartLat + EndLat) / 2;
            };

            // Вычисляем меридианные и поперечные радиусы кривизны средней широты
            fTemp = 1 - e2 * (sqr(Math.Sin(fPhimean)));
            fRho = (a * (1 - e2)) / Math.Pow(fTemp, 1.5);
            fNu = a / (Math.Sqrt(1 - e2 * (Math.Sin(fPhimean) * Math.Sin(fPhimean))));

            // Вычисляем угловое расстояние
            if (!radians)
            {
                fz = Math.Sqrt(sqr(Math.Sin(fdPhi / 2.0)) + Math.Cos(EndLat * D2R) * Math.Cos(StartLat * D2R) * sqr(Math.Sin(fdLambda / 2.0)));
            }
            else
            {
                fz = Math.Sqrt(sqr(Math.Sin(fdPhi / 2.0)) + Math.Cos(EndLat) * Math.Cos(StartLat) * sqr(Math.Sin(fdLambda / 2.0)));
            };
            fz = 2 * Math.Asin(fz);

            // Вычисляем смещение
            if (!radians)
            {
                fAlpha = Math.Cos(EndLat * D2R) * Math.Sin(fdLambda) * 1 / Math.Sin(fz);
            }
            else
            {
                fAlpha = Math.Cos(EndLat) * Math.Sin(fdLambda) * 1 / Math.Sin(fz);
            };
            fAlpha = Math.Asin(fAlpha);

            // Вычисляем радиус Земли
            fR = (fRho * fNu) / (fRho * sqr(Math.Sin(fAlpha)) + fNu * sqr(Math.Cos(fAlpha)));
            // Получаем расстояние
            return (uint)Math.Round(Math.Abs(fz * fR));
        }
        
        // Slowest
        public static uint GetLengthMetersB(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            double fPhimean, fdLambda, fdPhi, fAlpha, fRho, fNu, fR, fz, fTemp, Distance,
                D2R = Math.PI / 180,
                a = 6378137.0,
                e2 = 0.006739496742337;
            if (radians) D2R = 1;

            fdLambda = (StartLong - EndLong) * D2R;
            fdPhi = (StartLat - EndLat) * D2R;
            fPhimean = (StartLat + EndLat) / 2.0 * D2R;

            fTemp = 1 - e2 * Math.Pow(Math.Sin(fPhimean), 2);
            fRho = a * (1 - e2) / Math.Pow(fTemp, 1.5);
            fNu = a / Math.Sqrt(1 - e2 * Math.Sin(fPhimean) * Math.Sin(fPhimean));

            fz = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(fdPhi / 2.0), 2) +
              Math.Cos(EndLat * D2R) * Math.Cos(StartLat * D2R) * Math.Pow(Math.Sin(fdLambda / 2.0), 2)));
            fAlpha = Math.Asin(Math.Cos(EndLat * D2R) * Math.Sin(fdLambda) / Math.Sin(fz));
            fR = fRho * fNu / (fRho * Math.Pow(Math.Sin(fAlpha), 2) + fNu * Math.Pow(Math.Cos(fAlpha), 2));
            Distance = fz * fR;

            return (uint)Math.Round(Distance);
        }
        
        // Average
        public static uint GetLengthMetersC(double StartLat, double StartLong, double EndLat, double EndLong, bool radians)
        {
            double D2R = Math.PI / 180;
            if (radians) D2R = 1;
            double dDistance = Double.MinValue;
            double dLat1InRad = StartLat * D2R;
            double dLong1InRad = StartLong * D2R;
            double dLat2InRad = EndLat * D2R;
            double dLong2InRad = EndLong * D2R;

            double dLongitude = dLong2InRad - dLong1InRad;
            double dLatitude = dLat2InRad - dLat1InRad;

            // Intermediate result a.
            double a = Math.Pow(Math.Sin(dLatitude / 2.0), 2.0) +
                       Math.Cos(dLat1InRad) * Math.Cos(dLat2InRad) *
                       Math.Pow(Math.Sin(dLongitude / 2.0), 2.0);

            // Intermediate result c (great circle distance in Radians).
            double c = 2.0 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1.0 - a));

            const double kEarthRadiusKms = 6378137.0000;
            dDistance = kEarthRadiusKms * c;

            return (uint)Math.Round(dDistance);
        }
        
        // Fastest
        public static double GetLengthMetersD(double sLat, double sLon, double eLat, double eLon, bool radians)
        {
            double EarthRadius = 6378137.0;

            double lon1 = radians ? sLon : DegToRad(sLon);
            double lon2 = radians ? eLon : DegToRad(eLon);
            double lat1 = radians ? sLat : DegToRad(sLat);
            double lat2 = radians ? eLat : DegToRad(eLat);

            return EarthRadius * (Math.Acos(Math.Sin(lat1) * Math.Sin(lat2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Cos(lon1 - lon2)));
        }
        
        // Fastest
        public static double GetLengthMetersE(double sLat, double sLon, double eLat, double eLon, bool radians)
        {
            double EarthRadius = 6378137.0;

            double lon1 = radians ? sLon : DegToRad(sLon);
            double lon2 = radians ? eLon : DegToRad(eLon);
            double lat1 = radians ? sLat : DegToRad(sLat);
            double lat2 = radians ? eLat : DegToRad(eLat);

            /* This algorithm is called Sinnott's Formula */
            double dlon = (lon2) - (lon1);
            double dlat = (lat2) - (lat1);
            double a = Math.Pow(Math.Sin(dlat / 2), 2.0) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(dlon / 2), 2.0);
            double c = 2 * Math.Asin(Math.Sqrt(a));
            return EarthRadius * c;
        }

        private static double sqr(double val) => val * val;

        public static double DegToRad(double deg) => (deg / 180.0 * Math.PI);
        #endregion LENGTH

        /// <summary>
        /// Converts base data types to an array of bytes, and an array of bytes to base
        /// data types.
        /// All info taken from the meta data of System.BitConverter. This implementation
        /// allows for Endianness consideration.
        ///</summary>
        public class MyBitConverter
        {
            /// <summary>
            ///     Constructor
            /// </summary>
            public MyBitConverter()
            {

            }

            /// <summary>
            ///     Constructor
            /// </summary>
            /// <param name="IsLittleEndian">Indicates the byte order ("endianess") in which data is stored in this computer architecture.</param>
            public MyBitConverter(bool IsLittleEndian)
            {
                this.isLittleEndian = IsLittleEndian;
            }

            /// <summary>
            ///     Indicates the byte order ("endianess") in which data is stored in this computer
            /// architecture.
            /// </summary>
            private bool isLittleEndian = true;

            /// <summary>
            /// Indicates the byte order ("endianess") in which data is stored in this computer
            /// architecture.
            ///</summary>
            public bool IsLittleEndian { get { return isLittleEndian; } set { isLittleEndian = value; } } // should default to false, which is what we want for Empire

            /// <summary>
            /// Converts the specified double-precision floating point number to a 64-bit
            /// signed integer.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// A 64-bit signed integer whose value is equivalent to value.
            ///</summary>
            public long DoubleToInt64Bits(double value) { throw new NotImplementedException(); }
            ///
            /// <summary>
            /// Returns the specified Boolean value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// A Boolean value.
            ///
            /// Returns:
            /// An array of bytes with length 1.
            ///</summary>
            public byte[] GetBytes(bool value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified Unicode character value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// A character to convert.
            ///
            /// Returns:
            /// An array of bytes with length 2.
            ///</summary>
            public byte[] GetBytes(char value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified double-precision floating point value as an array of
            /// bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 8.
            ///</summary>
            public byte[] GetBytes(double value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified single-precision floating point value as an array of
            /// bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 4.
            ///</summary>
            public byte[] GetBytes(float value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified 32-bit signed integer value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 4.
            ///</summary>
            public byte[] GetBytes(int value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified 64-bit signed integer value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 8.
            ///</summary>
            public byte[] GetBytes(long value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified 16-bit signed integer value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 2.
            ///</summary>
            public byte[] GetBytes(short value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified 32-bit unsigned integer value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 4.
            ///</summary>
            public byte[] GetBytes(uint value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified 64-bit unsigned integer value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 8.
            ///</summary>
            public byte[] GetBytes(ulong value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Returns the specified 16-bit unsigned integer value as an array of bytes.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// An array of bytes with length 2.
            ///</summary>
            public byte[] GetBytes(ushort value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.GetBytes(value);
                }
                else
                {
                    byte[] res = System.BitConverter.GetBytes(value);
                    Array.Reverse(res);
                    return res;
                }
            }
            ///
            /// <summary>
            /// Converts the specified 64-bit signed integer to a double-precision floating
            /// point number.
            ///
            /// Parameters:
            /// value:
            /// The number to convert.
            ///
            /// Returns:
            /// A double-precision floating point number whose value is equivalent to value.
            ///</summary>
            public double Int64BitsToDouble(long value) { throw new NotImplementedException(); }
            ///
            /// <summary>
            /// Returns a Boolean value converted from one byte at a specified position in
            /// a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// true if the byte at startIndex in value is nonzero; otherwise, false.
            ///
            /// Exceptions:
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public bool ToBoolean(byte[] value, int startIndex) { throw new NotImplementedException(); }
            ///
            /// <summary>
            /// Returns a Unicode character converted from two bytes at a specified position
            /// in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A character formed by two bytes beginning at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex equals the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public char ToChar(byte[] value, int startIndex) { throw new NotImplementedException(); }
            ///
            /// <summary>
            /// Returns a double-precision floating point number converted from eight bytes
            /// at a specified position in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A double precision floating point number formed by eight bytes beginning
            /// at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex is greater than or equal to the length of value minus 7, and is
            /// less than or equal to the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public double ToDouble(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToDouble(value, startIndex);
                }
                else
                {
                    byte[] res = new byte[8];
                    Array.Copy(value, startIndex, res, 0, 8);
                    Array.Reverse(res);
                    return System.BitConverter.ToDouble(res, 0);
                }
            }
            ///
            /// <summary>
            /// Returns a 16-bit signed integer converted from two bytes at a specified position
            /// in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A 16-bit signed integer formed by two bytes beginning at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex equals the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public short ToInt16(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToInt16(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToInt16(res, value.Length - sizeof(Int16) - startIndex);
                }
            }
            ///
            /// <summary>
            /// Returns a 32-bit signed integer converted from four bytes at a specified
            /// position in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A 32-bit signed integer formed by four bytes beginning at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex is greater than or equal to the length of value minus 3, and is
            /// less than or equal to the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public int ToInt32(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToInt32(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToInt32(res, value.Length - sizeof(Int32) - startIndex);
                }
            }
            ///
            /// <summary>
            /// Returns a 64-bit signed integer converted from eight bytes at a specified
            /// position in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A 64-bit signed integer formed by eight bytes beginning at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex is greater than or equal to the length of value minus 7, and is
            /// less than or equal to the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public long ToInt64(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToInt64(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToInt64(res, value.Length - sizeof(Int64) - startIndex);
                }
            }
            ///
            /// <summary>
            /// Returns a single-precision floating point number converted from four bytes
            /// at a specified position in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A single-precision floating point number formed by four bytes beginning at
            /// startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex is greater than or equal to the length of value minus 3, and is
            /// less than or equal to the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public float ToSingle(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToSingle(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToSingle(res, value.Length - sizeof(Single) - startIndex);
                }
            }
            ///
            /// <summary>
            /// Converts the numeric value of each element of a specified array of bytes
            /// to its equivalent hexadecimal string representation.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// Returns:
            /// A System.String of hexadecimal pairs separated by hyphens, where each pair
            /// represents the corresponding element in value; for example, "7F-2C-4A".
            ///
            /// Exceptions:
            /// System.ArgumentNullException:
            /// value is null.
            ///</summary>
            public string ToString(byte[] value)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToString(value);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToString(res);
                }
            }
            ///
            /// <summary>
            /// Converts the numeric value of each element of a specified subarray of bytes
            /// to its equivalent hexadecimal string representation.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A System.String of hexadecimal pairs separated by hyphens, where each pair
            /// represents the corresponding element in a subarray of value; for example,
            /// "7F-2C-4A".
            ///
            /// Exceptions:
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public string ToString(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToString(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res, startIndex, value.Length - startIndex);
                    return System.BitConverter.ToString(res, startIndex);
                }
            }
            ///
            /// <summary>
            /// Converts the numeric value of each element of a specified subarray of bytes
            /// to its equivalent hexadecimal string representation.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// length:
            /// The number of array elements in value to convert.
            ///
            /// Returns:
            /// A System.String of hexadecimal pairs separated by hyphens, where each pair
            /// represents the corresponding element in a subarray of value; for example,
            /// "7F-2C-4A".
            ///
            /// Exceptions:
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex or length is less than zero. -or- startIndex is greater than
            /// zero and is greater than or equal to the length of value.
            ///
            /// System.ArgumentException:
            /// The combination of startIndex and length does not specify a position within
            /// value; that is, the startIndex parameter is greater than the length of value
            /// minus the length parameter.
            ///</summary>
            public string ToString(byte[] value, int startIndex, int length)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToString(value, startIndex, length);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res, startIndex, length);
                    return System.BitConverter.ToString(res, startIndex, length);
                }
            }
            ///
            /// <summary>
            /// Returns a 16-bit unsigned integer converted from two bytes at a specified
            /// position in a byte array.
            ///
            /// Parameters:
            /// value:
            /// The array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A 16-bit unsigned integer formed by two bytes beginning at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex equals the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public ushort ToUInt16(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToUInt16(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToUInt16(res, value.Length - sizeof(UInt16) - startIndex);
                }
            }
            ///
            /// <summary>
            /// Returns a 32-bit unsigned integer converted from four bytes at a specified
            /// position in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A 32-bit unsigned integer formed by four bytes beginning at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex is greater than or equal to the length of value minus 3, and is
            /// less than or equal to the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public uint ToUInt32(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToUInt32(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToUInt32(res, value.Length - sizeof(UInt32) - startIndex);
                }
            }
            ///
            /// <summary>
            /// Returns a 64-bit unsigned integer converted from eight bytes at a specified
            /// position in a byte array.
            ///
            /// Parameters:
            /// value:
            /// An array of bytes.
            ///
            /// startIndex:
            /// The starting position within value.
            ///
            /// Returns:
            /// A 64-bit unsigned integer formed by the eight bytes beginning at startIndex.
            ///
            /// Exceptions:
            /// System.ArgumentException:
            /// startIndex is greater than or equal to the length of value minus 7, and is
            /// less than or equal to the length of value minus 1.
            ///
            /// System.ArgumentNullException:
            /// value is null.
            ///
            /// System.ArgumentOutOfRangeException:
            /// startIndex is less than zero or greater than the length of value minus 1.
            ///</summary>
            public ulong ToUInt64(byte[] value, int startIndex)
            {
                if (IsLittleEndian)
                {
                    return System.BitConverter.ToUInt64(value, startIndex);
                }
                else
                {
                    byte[] res = (byte[])value.Clone();
                    Array.Reverse(res);
                    return System.BitConverter.ToUInt64(res, value.Length - sizeof(UInt64) - startIndex);
                }
            }
        }
    }

    internal class Point
    {
        public int X;
        public int Y;
    }

    internal class PointF
    {
        public double X;
        public double Y;

        public PointF(double x, double y) {  X = x; Y = y; }
    }

    internal class PointD
    {
        public double X;
        public double Y;
        public byte Type;

        public PointD() { }

        public PointD(int X, int Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public PointD(int X, int Y, byte Type)
        {
            this.X = X;
            this.Y = Y;
            this.Type = Type;
        }

        public PointD(float X, float Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public PointD(float X, float Y, byte Type)
        {
            this.X = X;
            this.Y = Y;
            this.Type = Type;
        }

        public PointD(double X, double Y)
        {
            this.X = X;
            this.Y = Y;
        }

        public PointD(double X, double Y, byte Type)
        {
            this.X = X;
            this.Y = Y;
            this.Type = Type;
        }

        public PointD(Point point)
        {
            this.X = point.X;
            this.Y = point.Y;
        }

        public PointD(Point point, byte Type)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.Type = Type;
        }

        public PointD(PointF point)
        {
            this.X = point.X;
            this.Y = point.Y;
        }

        public PointD(PointF point, byte Type)
        {
            this.X = point.X;
            this.Y = point.Y;
            this.Type = Type;
        }

        public PointF PointF
        {
            get
            {
                return new PointF((float)X, (float)Y);
            }
            set
            {
                this.X = value.X;
                this.Y = value.Y;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return (X == 0) && (Y == 0);
            }
        }

        public static PointF ToPointF(PointD point)
        {
            return point.PointF;
        }

        public static PointF[] ToPointF(PointD[] points)
        {
            PointF[] result = new PointF[points.Length];
            for (int i = 0; i < result.Length; i++)
                result[i] = points[i].PointF;
            return result;
        }
    }

    internal class LatLonParser
    {
        public enum FFormat : byte
        {
            None    = 0,
            DDDDDD  = 1,
            DDMMMM  = 2,
            DDMMSS  = 3,
            APRSLAT = 4,
            APRSLON = 5
        }

        public enum DFormat : byte
        {
            ENG_NS  = 0,
            ENG_EW  = 1,
            RUS_NS  = 2,
            RUS_EW  = 3,
            MINUS   = 4,
            DEFAULT = 4,
            NONE    = 4
        }

        public static double ToLat(string line_in) => Parse(line_in, true);

        public static double ToLon(string line_in) => Parse(line_in, false);

        public static double Parse(string line_in, bool true_lat_false_lon)
        {
            int nn = 1;
            string full = GetCorrectString(line_in, true_lat_false_lon, out nn);
            if (String.IsNullOrEmpty(full)) return 0f;
            string mm = "0";
            string ss = "0";
            string dd = "0";
            if (full.IndexOf("°") > 0)
            {
                int dms = 0;
                int from = 0;
                int next = 0;
                dd = full.Substring(from, (next = full.IndexOf("°", from)) - from);
                from = next + 1;
                if (full.IndexOf("'") > 0)
                {
                    dms = 1;
                    mm = full.Substring(from, (next = full.IndexOf("'", from)) - from);
                    from = next + 1;
                };
                if (full.IndexOf("\"") > 0)
                {
                    dms = 2;
                    ss = full.Substring(from, (next = full.IndexOf("\"", from)) - from);
                    from = next + 1;
                };
                if (from < full.Length)
                {
                    if (dms == 1)
                        ss = full.Substring(from);
                    else if (dms == 0)
                        mm = full.Substring(from);
                };
            }
            else
            {
                bool loop = true;
                double num3 = 0.0;
                int num4 = 1;
                if (full[0] == '-') num4++;
                while (loop)
                {
                    try
                    {
                        num3 = Convert.ToDouble(full.Substring(0, num4++), System.Globalization.CultureInfo.InvariantCulture);
                    }
                    catch
                    {
                        loop = false;
                    };
                    if (num4 > full.Length)
                    {
                        loop = false;
                    };
                }
                dd = num3.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }
            double d = ((Convert.ToDouble(dd, System.Globalization.CultureInfo.InvariantCulture) + Convert.ToDouble(mm, System.Globalization.CultureInfo.InvariantCulture) / 60.0 + Convert.ToDouble(ss, System.Globalization.CultureInfo.InvariantCulture) / 60.0 / 60.0) * (double)nn);
            return d;
        }

        public static PointD Parse(string line_in) => new PointD(ToLon(line_in), ToLat(line_in));

        private static string GetCorrectString(string str, bool lat, out int digit)
        {
            digit = 1;
            if (String.IsNullOrEmpty(str)) return null;

            string text = str.Trim();
            if (String.IsNullOrEmpty(text)) return null;

            text = text.ToLower().Replace("``", "\"").Replace("`", "'").Replace("%20", " ").Trim();
            while (text.IndexOf("  ") >= 0) text = text.Replace("  ", " ");
            text = text.Replace("° ", "°").Replace("' ", "'").Replace("\" ", "\"");
            if (String.IsNullOrEmpty(text)) return null;

            bool hasDigits = false;
            bool noletters = true;
            for (int i = 0; i < text.Length; i++)
            {
                if (char.IsDigit(text[i]))
                    hasDigits = true;
                if (char.IsLetter(text[i]))
                    noletters = false;
            };
            if (!hasDigits) return null;

            if (noletters)
            {
                string[] lalo = text.Split(new char[] { '+', ' ', '=', ';', ',', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (lalo.Length == 0)
                    return null;
                if (lalo.Length == 2)
                {
                    if (lat)
                        text = lalo[0];
                    else
                        text = lalo[1];
                };
            };

            text = text.Replace("+", "").Replace(" ", "").Replace("=", "").Replace(";", "").Replace("\r", "").Replace("\n", "").Replace("\t", "").Trim();
            if (String.IsNullOrEmpty(text)) return null;

            double d;
            if (double.TryParse(text, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out d))
            {
                if (d < 0) digit = -1;
                return text.Replace("-", "");
            };

            int copyl = text.Length;
            int find = 0;
            int start = 0;
            bool endsWithLetter = (char.IsLetter(text[text.Length - 1]));

            if (lat)
            {
                if ((find = text.IndexOf("lat")) >= 0) start = find + (endsWithLetter ? 0 : 3);
                if ((find = text.IndexOf("latitude")) >= 0) start = find + (endsWithLetter ? 0 : 8);
                if ((find = text.IndexOf("ш")) >= 0) start = find + (endsWithLetter ? 0 : 1);
                if ((find = text.IndexOf("шир")) >= 0) start = find + (endsWithLetter ? 0 : 3);
                if ((find = text.IndexOf("широта")) >= 0) start = find + (endsWithLetter ? 0 : 6);
                if ((find = text.IndexOf("n")) >= 0) start = find + (endsWithLetter ? 0 : 1);
                if ((find = text.IndexOf("с")) >= 0) start = find + (endsWithLetter ? 0 : 1);
                if ((find = text.IndexOf("сш")) >= 0) start = find + (endsWithLetter ? 0 : 2);
                if ((find = text.IndexOf("с.ш")) >= 0) start = find + (endsWithLetter ? 0 : 3);
                if ((find = text.IndexOf("с.ш.")) >= 0) start = find + (endsWithLetter ? 0 : 4);
                if ((find = text.IndexOf("s")) >= 0) { start = find + (endsWithLetter ? 0 : 1); digit = -1; };
                if ((find = text.IndexOf("ю")) >= 0) { start = find + (endsWithLetter ? 0 : 1); digit = -1; };
                if ((find = text.IndexOf("юш")) >= 0) { start = find + (endsWithLetter ? 0 : 2); digit = -1; };
                if ((find = text.IndexOf("ю.ш")) >= 0) { start = find + (endsWithLetter ? 0 : 3); digit = -1; };
                if ((find = text.IndexOf("ю.ш.")) >= 0) { start = find + (endsWithLetter ? 0 : 4); digit = -1; };
            }
            else
            {
                if ((find = text.IndexOf("lon")) >= 0) start = find + (endsWithLetter ? 0 : 3);
                if ((find = text.IndexOf("longitude")) >= 0) start = find + (endsWithLetter ? 0 : 9);
                if ((find = text.IndexOf("д")) >= 0) start = find + (endsWithLetter ? 0 : 1);
                if ((find = text.IndexOf("дол")) >= 0) start = find + (endsWithLetter ? 0 : 3);
                if ((find = text.IndexOf("долгота")) >= 0) start = find + (endsWithLetter ? 0 : 7);
                if ((find = text.IndexOf("e")) >= 0) start = find + (endsWithLetter ? 0 : 1);
                if ((find = text.IndexOf("в")) >= 0) start = find + (endsWithLetter ? 0 : 1);
                if ((find = text.IndexOf("вд")) >= 0) start = find + (endsWithLetter ? 0 : 2);
                if ((find = text.IndexOf("в.д")) >= 0) start = find + (endsWithLetter ? 0 : 3);
                if ((find = text.IndexOf("в.д.")) >= 0) start = find + (endsWithLetter ? 0 : 4);
                if ((find = text.IndexOf("w")) >= 0) { start = find + (endsWithLetter ? 0 : 1); digit = -1; };
                if ((find = text.IndexOf("з")) >= 0) { start = find + (endsWithLetter ? 0 : 1); digit = -1; };
                if ((find = text.IndexOf("зд")) >= 0) { start = find + (endsWithLetter ? 0 : 2); digit = -1; };
                if ((find = text.IndexOf("з.д")) >= 0) { start = find + (endsWithLetter ? 0 : 3); digit = -1; };
                if ((find = text.IndexOf("з.д.")) >= 0) { start = find + (endsWithLetter ? 0 : 4); digit = -1; };
            };

            if (endsWithLetter)
            {
                copyl = start;
                start = 0;
                for (int i = copyl - 1; i >= start; i--)
                    if (char.IsLetter(text[i]))
                        copyl = copyl - (start = i + 1);
            }
            else
            {
                for (int i = start; i < copyl; i++)
                    if (char.IsLetter(text[i]))
                        copyl = i - start;
            };

            if (copyl > (text.Length - start)) copyl -= start;

            text = text.Substring(start, copyl);
            text = text.Replace(",", ".");
            return text;
        }

        public static string ToString(double fvalue) => DoubleToString(fvalue, -1);

        public static string ToString(double fvalue, int digitsAfterDelimiter) => DoubleToString(fvalue, digitsAfterDelimiter);

        public static string ToString(double lat, double lon) => String.Format("{0},{1}", ToString(lat), ToString(lon));

        public static string ToString(double lat, double lon, int digitsAfterDelimiter) => String.Format("{0},{1}", DoubleToString(lat, digitsAfterDelimiter), DoubleToString(lon, digitsAfterDelimiter));

        public static string ToString(double lat, double lon, FFormat fformat)
        {
            if (fformat == FFormat.None)
                return String.Format("{0},{1}", ToString(lat, fformat), ToString(lon, fformat));
            else
                return String.Format("{0} {1} {2} {3}", new string[] { GetLinePrefix(lat, DFormat.ENG_NS), ToString(lat, fformat), GetLinePrefix(lat, DFormat.ENG_EW), ToString(lon, fformat) });
        }

        public static string ToString(double lat, double lon, FFormat fformat, int digitsAfterDelimiter)
        {
            if (fformat == FFormat.None)
                return String.Format("{0},{1}", ToString(lat, fformat, digitsAfterDelimiter), ToString(lon, fformat, digitsAfterDelimiter));
            else
                return String.Format("{0} {1} {2} {3}", new string[] { GetLinePrefix(lat, DFormat.ENG_NS), ToString(lat, fformat, digitsAfterDelimiter), GetLinePrefix(lat, DFormat.ENG_EW), ToString(lon, fformat, digitsAfterDelimiter) });
        }

        public static string ToString(PointD latlon) => String.Format("{0},{1}", ToString(latlon.Y), ToString(latlon.X));

        public static string ToString(PointD latlon, int digitsAfterDelimiter) => String.Format("{0},{1}", DoubleToString(latlon.Y, digitsAfterDelimiter), DoubleToString(latlon.X, digitsAfterDelimiter));        

        public static string ToString(PointD latlon, FFormat fformat)
        {
            if (fformat == FFormat.None)
                return String.Format("{0},{1}", ToString(latlon.Y, fformat), ToString(latlon.X, fformat));
            else
                return String.Format("{0} {1} {2} {3}", new string[] { GetLinePrefix(latlon.Y, DFormat.ENG_NS), ToString(latlon.Y, fformat), GetLinePrefix(latlon.X, DFormat.ENG_EW), ToString(latlon.X, fformat) });
        }

        public static string ToString(PointD latlon, FFormat fformat, int digitsAfterDelimiter)
        {
            if (fformat == FFormat.None)
                return String.Format("{0},{1}", ToString(latlon.Y, fformat, digitsAfterDelimiter), ToString(latlon.X, fformat, digitsAfterDelimiter));
            else
                return String.Format("{0} {1} {2} {3}", new string[] { GetLinePrefix(latlon.Y, DFormat.ENG_NS), ToString(latlon.Y, fformat, digitsAfterDelimiter), GetLinePrefix(latlon.X, DFormat.ENG_EW), ToString(latlon.X, fformat, digitsAfterDelimiter) });
        }

        public static string ToString(double fvalue, FFormat format) => ToString(fvalue, format, 6);

        public static string ToString(double fvalue, FFormat format, int digitsAfterDelimiter)
        {
            double num = Math.Abs(fvalue);
            string result;
            if (format == FFormat.None)
            {
                result = DoubleToString(num, digitsAfterDelimiter);
            }
            else if (format == FFormat.DDDDDD)
            {
                result = DoubleToString(num, digitsAfterDelimiter) + "°";
            }
            else if(format == FFormat.APRSLAT)
            {
                string text = DoubleToString(Math.Truncate(num), 0);
                while (text.Length < 2) text = "0" + text;
                double num2 = (num - Math.Truncate(num)) * 60.0;
                text = text + DoubleToString(num2, 2, 2);
                text += fvalue >= 0 ? "N" : "S";
                result = text;
            }
            else if (format == FFormat.APRSLON)
            {
                string text = DoubleToString(Math.Truncate(num), 0);
                while (text.Length < 3) text = "0" + text;
                double num2 = (num - Math.Truncate(num)) * 60.0;
                text = text + DoubleToString(num2, 2, 2);
                text += fvalue >= 0 ? "E" : "W";
                result = text;
            }
            else
            {
                string text = "";
                text = text + DoubleToString(Math.Truncate(num), 0) + "° ";
                double num2 = (num - Math.Truncate(num)) * 60.0;
                if (format == FFormat.DDMMMM)
                {
                    text = text + DoubleToString(num2, 4) + "'";
                }
                else
                {
                    text = text + string.Format("{0}", (int)Math.Truncate(num2)) + "' ";
                    num2 = (num2 - Math.Truncate(num2)) * 60.0;
                    text = text + DoubleToString(num2, 3) + "\"";
                }
                result = text;
            }
            return result;
        }

        public static string GetLinePrefix(double fvalue, DFormat format)
        {
            string result;
            switch ((byte)format)
            {
                case 0:
                    result = ((fvalue >= 0.0) ? "N" : "S");
                    break;
                case 1:
                    result = ((fvalue >= 0.0) ? "E" : "W");
                    break;
                case 2:
                    result = ((fvalue >= 0.0) ? "С" : "Ю");
                    break;
                case 3:
                    result = ((fvalue >= 0.0) ? "В" : "З");
                    break;
                default:
                    result = ((fvalue >= 0.0) ? "" : "-");
                    break;
            }
            return result;
        }

        public static string DoubleToString(double val, int digitsAfterDelimiter, int digitsBeforeDelimiter = 0)
        {
            if (digitsAfterDelimiter < 0)
                return val.ToString(System.Globalization.CultureInfo.InvariantCulture);

            string daf = "";
            for (int i = 0; i < digitsAfterDelimiter; i++) daf += "0";
            daf = val.ToString("0." + daf, System.Globalization.CultureInfo.InvariantCulture);
            if (digitsBeforeDelimiter == 0 || daf.IndexOf(".") < 0) return daf;
            while (daf.IndexOf(".") < digitsBeforeDelimiter) daf = "0" + daf;
            return daf;
        }

        public static string DoubleToStringMax(double val, int maxDigitsAfterDelimiter)
        {
            string res = val.ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (maxDigitsAfterDelimiter < 0) return res;
            if (res.IndexOf(".") < 0) return res;
            if ((res.Length - res.IndexOf(".")) <= maxDigitsAfterDelimiter) return res;

            string daf = "";
            for (int i = 0; i < maxDigitsAfterDelimiter; i++) daf += "0";
            return val.ToString("0." + daf, System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}