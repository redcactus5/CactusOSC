
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/


using System.Text;
using System;
using System.Collections.Generic;
namespace CactusOSC
{
    public enum OSCValueType
    {
        OSCString,
        OSCInt,
        OSCFloat,
        OSCBlob,
        OSCLong,
        OSCTimeTag,
        OSCDouble,
        OSCNonstandardString,
        OSCChar,
        OSCRGBA,
        OSCMIDI,
        OSCBool,
        OSCNil,
        OSCInfinitum,
        OSCArray
    }


    /// <summary>
    /// the base class for all osc values.
    /// do not extend, all OSC types are hard coded in the serializer/deserializer, and new osc value classes will not be recognized.
    /// </summary>
    public abstract class OSCValue
    {

        private OSCValueType oscType;
        private int size;
        private bool sizeSet;
        private int typeStringSize;
        private bool typeStringSizeSet;
        /// <summary>
        /// get a string representation of the internal value of an OSCValue Object
        /// </summary>
        /// <returns>string</returns>
        public abstract override string ToString();

        /// <summary>
        /// returns a copy of an OSCValue Object
        /// </summary>
        /// <returns>OSCValue</returns>
        public abstract OSCValue Clone();

        /// <summary>
        /// get the type enum value of an OSCValue
        /// </summary>
        /// <returns>OSCValueType</returns>
        public OSCValueType GetOSCType()
        {
            return this.oscType;
        }

        /// <summary>
        /// the constructor of abstract class OSCValue
        /// </summary>
        /// <param name="type"></param>
        internal OSCValue(OSCValueType type)
        {
            this.size = -1;
            this.oscType = type;
        }

        protected void setByteSize(int size)
        {
            if (!this.sizeSet)
            {
                this.size = size;
                this.sizeSet = true;
            }
            else
            {
                throw new SizeAlreadySetException();
            }
        }

        protected void setTypeStringSize(int size)
        {
            if (!this.typeStringSizeSet)
            {
                this.typeStringSize = size;
                this.typeStringSizeSet = true;
            }
            else
            {
                throw new SizeAlreadySetException();
            }
        }

        /// <summary>
        /// get the size of an OSCValue in bytes
        /// </summary>
        /// <returns>int</returns>
        public int GetByteSize()
        {
            return this.size;
        }
        /// <summary>
        /// get the Number of characters in typeString of an OSCValue
        /// </summary>
        /// <returns>int</returns>
        public int GetTypeStringSize()
        {
            return this.typeStringSize;
        }


    }

    /// <summary>
    /// an immutable OSC string
    /// </summary>
    public sealed class OSCString : OSCValue
    {
        private string value;

        /// <summary>
        /// creates a new OSCString with a string internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCString(string value) : base(OSCValueType.OSCString)
        {
            this.value = value;
            //calculate the byte block size
            int tempsize = Encoding.UTF8.GetByteCount(value) + 1;
            if (tempsize > 0)
            {
                int overflow = tempsize % 4;
                if (overflow != 0)
                {
                    tempsize += 4 - overflow;
                }
            }
            else
            {
                tempsize = 4;
            }
            this.setByteSize(tempsize);
            this.setTypeStringSize(1);
        }

        /// <summary>
        /// get the string internal value of a OSCString
        /// </summary>
        /// <returns>string</returns>
        public string GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of a OSCString's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value;
        }
        /// <summary>
        /// creates a copy of an OSCString Object
        /// </summary>
        /// <returns>OSCString</returns>
        public override OSCString Clone()
        {
            return new OSCString(this.value);
        }
    }

    /// <summary>
    /// an immuatable OSC integer
    /// </summary>
    public sealed class OSCInt : OSCValue
    {
        private int value;
        /// <summary>
        /// creates a new OSCOInt with an integer internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCInt(int value) : base(OSCValueType.OSCInt)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the integer internal value of an OSCInt
        /// </summary>
        /// <returns>int</returns>
        public int GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCInt's internal Value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
        /// <summary>
        /// creates a copy of an OSCInt object
        /// </summary>
        /// <returns>OSCInt</returns>
        public override OSCInt Clone()
        {
            return new OSCInt(this.value);
        }
    }

    /// <summary>
    /// an immutable OSCF loat
    /// </summary>
    public sealed class OSCFloat : OSCValue
    {
        private float value;
        /// <summary>
        /// creates a new OSCFloat with a float internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCFloat(float value) : base(OSCValueType.OSCFloat)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the float internal value of an OSCFloat
        /// </summary>
        /// <returns>float</returns>
        public float GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCFloat's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
        /// <summary>
        /// creates a copy of an OSCFloat object
        /// </summary>
        /// <returns>OSCFloat</returns>
        public override OSCFloat Clone()
        {
            return new OSCFloat(this.value);
        }

    }

    /// <summary>
    /// an immutable OSC blob
    /// </summary>
    public sealed class OSCBlob : OSCValue
    {

        private byte[] value;
        /// <summary>
        /// creates a new OSCBlob with a byte[] internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCBlob(byte[] value) : base(OSCValueType.OSCBlob)
        {

            this.value = (byte[])value.Clone();
            //calculate the byte block size
            int tempsize = this.value.Length;
            if (tempsize > 0)
            {
                int overflow = tempsize % 4;
                if (overflow != 0)
                {
                    tempsize += 4 - overflow;

                }


            }

            this.setByteSize(tempsize + 4);
            this.setTypeStringSize(1);

        }

        /// <summary>
        /// gets the byte[] internal Value of an OSCBlob
        /// </summary>
        /// <returns>byte[]</returns>
        public byte[] GetValue()
        {
            return (byte[])this.value.Clone();
        }

        internal byte[] GetRawValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCBlob's internal value
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Convert.ToHexString(this.value);
        }
        /// <summary>
        /// creates a deep copy of an OSCBlob
        /// </summary>
        /// <returns>OSCBlob</returns>
        public override OSCBlob Clone()
        {
            return new OSCBlob((byte[])this.value.Clone());
        }

    }

    /// <summary>
    /// an immutable OSC long
    /// </summary>
    public sealed class OSCLong : OSCValue
    {
        private long value;
        /// <summary>
        /// creates a new OSCLong with a long internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCLong(long value) : base(OSCValueType.OSCLong)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);

        }
        /// <summary>
        /// gets the long internal Value of an OSCLong
        /// </summary>
        /// <returns>long</returns>
        public long GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCLong's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
        /// <summary>
        /// creates a copy of an OSCLong
        /// </summary>
        /// <returns>OSCLong</returns>
        public override OSCLong Clone()
        {
            return new OSCLong(this.value);
        }

    }

    public struct OSCTimeTagValue
    {
        public uint networkTimeWhole;
        public uint networkTimeFraction;

        
        public OSCTimeTagValue(uint networkTimeWhole, uint networkTimeFraction)
        {
            this.networkTimeWhole = networkTimeWhole;
            this.networkTimeFraction = networkTimeFraction;
            
        }

        public OSCTimeTagValue(ulong rawNetworkTimeValue)
        {
            
            this.networkTimeWhole = (uint)(rawNetworkTimeValue >> 32);
            this.networkTimeFraction = (uint)(rawNetworkTimeValue & ((ulong)0xFFFFFFFF));
        }

        public ulong GetRawNetworkTimeValue()
        {
            return ((ulong)(((ulong)networkTimeWhole)<<32)|((ulong)networkTimeFraction));
        }
        
        
    }
    /// <summary>
    /// an immutable OSC timetag
    /// </summary>
    public sealed class OSCTimeTag : OSCValue
    {
        private ulong value;
        /// <summary>
        /// creates a new OSCTimeTag with a ulong internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCTimeTag(ulong value) : base(OSCValueType.OSCTimeTag)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// creates a new OSCTimeTag with a ulong internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCTimeTag(OSCTimeTagValue value) : base(OSCValueType.OSCTimeTag)
        {
            this.value = value.GetRawNetworkTimeValue();
            this.setByteSize(8);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the ulong internal Value of an OSCTimeTag
        /// </summary>
        /// <returns>Long</returns>
        public ulong GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the OSCTimeTagValue struct internal Value of an OSCTimeTag
        /// </summary>
        /// <returns>Long</returns>
        public OSCTimeTagValue GetParsedValue()
        {
            return new OSCTimeTagValue(this.value);
        }
        /// <summary>
        /// gets the string representation of an OSCTimeTag's internal value
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
        /// <summary>
        /// creates a copy of an OSC TimeTag
        /// </summary>
        /// <returns>OSCTimeTag</returns>
        public override OSCTimeTag Clone()
        {
            return new OSCTimeTag(this.value);
        }
    }

    /// <summary>
    /// an immutible OSC double
    /// </summary>
    public sealed class OSCDouble : OSCValue
    {
        private double value;
        /// <summary>
        /// creates a new OSCDouble with a double internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCDouble(double value) : base(OSCValueType.OSCDouble)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the double internal Value of an OSCTDouble
        /// </summary>
        /// <returns>double</returns>
        public double GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCDouble's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
        /// <summary>
        /// creates a copy of an OSCDouble
        /// </summary>
        /// <returns>OSCDouble</returns>
        public override OSCDouble Clone()
        {
            return new OSCDouble(this.value);
        }

    }

    /// <summary>
    /// an immutible OSC string for when the standard oscString has been reassigned
    /// </summary>
    public sealed class OSCNonstandardString : OSCValue
    {
        private string value;
        /// <summary>
        /// creates a new OSCNonstandardString with a string internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCNonstandardString(string value) : base(OSCValueType.OSCNonstandardString)
        {
            this.value = value;
            //calculate the byte block size
            int tempsize = Encoding.UTF8.GetByteCount(value) + 1;
            if (tempsize > 0)
            {
                int overflow = tempsize % 4;


                if (overflow != 0)
                {
                    tempsize += 4 - overflow;
                }

            }
            else
            {
                tempsize = 4;
            }
            this.setByteSize(tempsize);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the string internal Value of an OSCNonstandardString
        /// </summary>
        /// <returns>string</returns>
        public string GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCNonstandardString's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value;
        }
        /// <summary>
        /// creates a copy of an OSCNonstandardString
        /// </summary>
        /// <returns>OSCNonstandardString</returns>
        public override OSCNonstandardString Clone()
        {
            return new OSCNonstandardString(this.value);
        }

    }


    /// <summary>
    /// an immutable osc character
    /// </summary>
    public sealed class OSCChar : OSCValue
    {
        private char value;

        /// <summary>
        /// creates a new OSCChar with a char internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCChar(char value) : base(OSCValueType.OSCChar)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the char internal Value of an OSCChar
        /// </summary>
        /// <returns>char</returns>
        public char GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCChar's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
        /// <summary>
        /// creates a copy of an OSCChar
        /// </summary>
        /// <returns>OSCChar</returns>
        public override OSCChar Clone()
        {
            return new OSCChar(this.value);
        }
    }


    public struct OSCColorValue
    {
        public byte r;
        public byte g;
        public byte b;
        public byte a;
        

        public OSCColorValue(byte r, byte g, byte b, byte a){
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
        }

        public OSCColorValue(int rgba)
        {
            this.SetColor(rgba);
        }

        public void SetColor(int rgba)
        {
            this.r = (byte)((rgba >> 24) & 0xff);
            this.g = (byte)((rgba >> 16) & 0xff);
            this.b = (byte)((rgba >> 8) & 0xff);
            this.a = (byte)(rgba & 0xff);
        }

        public int GetColor()
        {
            return (((this.r << 24) & (0xff << 24)) | ((this.g << 16) & (0xff << 16)) | ((this.b << 8) & (0xff << 8)) | ((this.a) & 0xff));
        }
            


    }


    /// <summary>
    /// an immutible osc rgba color
    /// </summary>
    public sealed class OSCColor : OSCValue
    {
        private byte r;
        private byte g;
        private byte b;
        private byte a;

        /// <summary>
        /// creates a new OSCColor with an rgba internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCColor(byte r, byte g, byte b, byte a) : base(OSCValueType.OSCRGBA)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
            this.setByteSize(4);
            this.setTypeStringSize(1);

        }
        /// <summary>
        /// creates a new OSCColor with an rgba internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCColor(OSCColorValue value) : base(OSCValueType.OSCRGBA)
        {
            this.r = value.r;
            this.g = value.g;
            this.b = value.b;
            this.a = value.a;
            this.setByteSize(4);
            this.setTypeStringSize(1);

        }
        /// <summary>
        /// creates a new OSCColor with an rgba internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCColor(int rgba) : base(OSCValueType.OSCRGBA)
        {
            this.r = (byte)((rgba >> 24) & 0xff);
            this.g = (byte)((rgba >> 16) & 0xff);
            this.b = (byte)((rgba >> 8) & 0xff);
            this.a = (byte)(rgba & 0xff);
            this.setByteSize(4);
            this.setTypeStringSize(1);

        }
        /// <summary>
        /// gets the int internal Value of an OSCColor
        /// </summary>
        /// <returns>int</returns>
        public int GetValue()
        {
            return (((this.r << 24) & (0xff << 24)) | ((this.g << 16) & (0xff << 16)) | ((this.b << 8) & (0xff << 8)) | ((this.a) & 0xff));
        }
        /// <summary>
        /// gets the OSCColorValue internal Value of an OSCColor
        /// </summary>
        /// <returns>OSCColorValue</returns>
        public OSCColorValue GetDecodedValue()
        {
            return new OSCColorValue(this.r, this.g, this.b, this.a);
        }
        /// <summary>
        /// gets the string representation of an OSCColor's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return "#" + Convert.ToHexString(new byte[] { this.r, this.g, this.b, this.a });
        }
        /// <summary>
        /// creates a copy of an OSCColor
        /// </summary>
        /// <returns>OSCColor</returns>
        public override OSCColor Clone()
        {
            return new OSCColor(this.r, this.g, this.b, this.a);
        }
    }


    public struct OSCMidiValue
    {
        public byte port;
        public byte status;
        public byte data1;
        public byte data2;

        public OSCMidiValue(byte port, byte status, byte data1, byte data2)
        {
            this.port = port;
            this.status = status;
            this.data1 = data1;
            this.data2 = data2;
        }

        public OSCMidiValue(int midiMessage)
        {
            this.port = (byte)((midiMessage >> 24) & 0xff);
            this.status = (byte)((midiMessage >> 16) & 0xff);
            this.data1 = (byte)((midiMessage >> 8) & 0xff);
            this.data2 = (byte)(midiMessage & 0xff);
        }

        public int GetMessage()
        {
            return (((this.port << 24) & (0xff << 24)) | ((this.status << 16) & (0xff << 16)) | ((this.data1 << 8) & (0xff << 8)) | ((this.data2) & 0xff));
        }
    }



    /// <summary>
    /// an immutible osc midi message
    /// </summary>
    public sealed class OSCMIDI : OSCValue
    {
        private byte port;
        private byte status;
        private byte data1;
        private byte data2;

        /// <summary>
        /// creates a new OSCMidi with an midi message internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCMIDI(byte port, byte status, byte data1, byte data2) : base(OSCValueType.OSCMIDI)
        {
            this.port = port;
            this.status = status;
            this.data1 = data1;
            this.data2 = data2;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// creates a new OSCMidi with an midi message internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCMIDI(OSCMidiValue midiMessage) : base(OSCValueType.OSCMIDI)
        {
            this.port = midiMessage.port;
            this.status = midiMessage.status;
            this.data1 = midiMessage.data1;
            this.data2 = midiMessage.data2;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// creates a new OSCMidi with an midi message internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCMIDI(int midiMessage) : base(OSCValueType.OSCMIDI)
        {
            this.port = (byte)((midiMessage >> 24) & 0xff);
            this.status = (byte)((midiMessage >> 16) & 0xff);
            this.data1 = (byte)((midiMessage >> 8) & 0xff);
            this.data2 = (byte)(midiMessage & 0xff);
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the int internal Value of an OSCMidi
        /// </summary>
        /// <returns>int</returns>
        public int GetValue()
        {
            return (((this.port << 24) & (0xff << 24)) | ((this.status << 16) & (0xff << 16)) | ((this.data1 << 8) & (0xff << 8)) | ((this.data2) & 0xff));
        }
        /// <summary>
        /// gets the OSCMidiValue internal Value of an OSCColor
        /// </summary>
        /// <returns>OSCMidiValue</returns>
        public OSCMidiValue GetDecodedValue()
        {
            return new OSCMidiValue(this.port,this.status,this.data1,this.data2);
        }
        /// <summary>
        /// gets the string representation of an OSCMIDI's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return "#" + Convert.ToHexString(new byte[] { this.port, this.status, this.data1, this.data2 });
        }
        /// <summary>
        /// creates a copy of an OSCMidi
        /// </summary>
        /// <returns>OSCMidi</returns>
        public override OSCMIDI Clone()
        {
            return new OSCMIDI(this.port, this.status, this.data1, this.data2);
        }
    }

    /// <summary>
    /// an immutible osc boolean value (can be either true or false, maps onto the true or false typeChars in the osc spec)
    /// </summary>
    public sealed class OSCBool : OSCValue
    {
        private bool value;
        /// <summary>
        /// creates a new OSCBool with an bool internal value
        /// </summary>
        /// <param name="value"></param>
        public OSCBool(bool value) : base(OSCValueType.OSCBool)
        {

            this.value = value;
            this.setByteSize(0);
            this.setTypeStringSize(1);
        }
        /// <summary>
        /// gets the bool internal Value of an OSCBool
        /// </summary>
        /// <returns>bool</returns>
        public bool GetValue()
        {
            return this.value;
        }
        /// <summary>
        /// gets the string representation of an OSCBool's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return this.value.ToString();
        }
        /// <summary>
        /// creates a copy of an OSCBool
        /// </summary>
        /// <returns>OSCBool</returns>
        public override OSCBool Clone()
        {
            return new OSCBool(this.value);
        }
    }

    /// <summary>
    /// an immutible osc value of nil
    /// </summary>
    public sealed class OSCNil : OSCValue
    {
        /// <summary>
        /// creates a new OSCNil
        /// </summary>
        public OSCNil() : base(OSCValueType.OSCNil)
        {
            this.setByteSize(0);
            this.setTypeStringSize(1);
        }

        /// <summary>
        /// gets the string representation of an OSCNil's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return "nil";
        }

        public override OSCNil Clone()
        {
            return new OSCNil();
        }

    }

    /// <summary>
    /// an immmutible oscValue of bang
    /// </summary>
    public sealed class OSCInfinitum : OSCValue
    {
        /// <summary>
        /// creates a new OSCInfinitum
        /// </summary>
        public OSCInfinitum() : base(OSCValueType.OSCInfinitum)
        {
            this.setByteSize(0);
            this.setTypeStringSize(1);
        }

        /// <summary>
        /// gets the string representation of an OSCInfinitim's internal value
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            return "infinitum";
        }
        /// <summary>
        /// creates a copy of an OSCInfinitum
        /// </summary>
        /// <returns>OSCInfinitum</returns>
        public override OSCInfinitum Clone()
        {
            return new OSCInfinitum();
        }

    }

    /// <summary>
    /// an immutible container for mutliple osc values, that itself is an osc value. it represents an array in the typestring. for more details see the official osc docs
    /// </summary>
    public sealed class OSCArray : OSCValue
    {
        private OSCValue[] data;

        /// <summary>
        /// creates a new osc OSCArray with an OSCValue[] internal value
        /// </summary>
        /// <param name="data"></param>
        public OSCArray(OSCValue[] data) : base(OSCValueType.OSCArray)
        {
            this.data = (OSCValue[])data.Clone();
            int tempsize = 0;
            int tempTypeStringSize = 0;
            for (int index = 0; index < data.Length; index++)
            {
                tempsize += data[index].GetByteSize();
                tempTypeStringSize += data[index].GetTypeStringSize();
            }
            this.setByteSize(tempsize);
            this.setTypeStringSize(2 + tempTypeStringSize);
        }
        /// <summary>
        /// get a deep copy of the values in an OSCArray
        /// </summary>
        /// <returns>OSCValue[]</returns>
        /// <exception cref="RecursiveListException"></exception>
        public OSCValue[] GetValue()
        {
            Stack<OSCValue[]> templateArrayStack = new Stack<OSCValue[]>();
            OSCValue[] currentTemplate = this.data;

            List<HashSet<OSCArray>> seenLists = new List<HashSet<OSCArray>>();

            seenLists.Add(new HashSet<OSCArray>());
            seenLists[0].Add(this);
            seenLists.Add(new HashSet<OSCArray>());
            Stack<OSCValue[]> copyStack = new Stack<OSCValue[]>();
            OSCValue[] copyArray = new OSCValue[currentTemplate.Length];

            Stack<int> IndexStack = new Stack<int>();
            int currentIndex = 0;

            int depth = 0;

            OSCArray tempArray;
            while ((currentIndex < currentTemplate.Length) || (depth > 0))
            {
                if (currentIndex >= currentTemplate.Length)
                {
                    tempArray = new OSCArray(copyArray);
                    currentIndex = IndexStack.Pop();
                    currentTemplate = templateArrayStack.Pop();
                    copyArray = copyStack.Pop();
                    copyArray[currentIndex] = tempArray;
                    currentIndex++;
                    seenLists.RemoveAt(depth);
                    depth--;
                    
                }
                else
                {
                    if (currentTemplate[currentIndex].GetOSCType() == OSCValueType.OSCArray)
                    {
                        if (depth > 0)
                        {
                            for(int search=0; search<depth; search++)
                            {
                                if (seenLists[search].Contains((OSCArray)currentTemplate[currentIndex]))
                                {
                                    throw new RecursiveListException();
                                }
                            }
                        }

                        seenLists[depth].Add((OSCArray)currentTemplate[currentIndex]);
                        templateArrayStack.Push(currentTemplate);
                        copyStack.Push(copyArray);
                        currentTemplate = ((OSCArray)currentTemplate[currentIndex]).GetRawValue();
                        IndexStack.Push(currentIndex);
                        currentIndex = 0;
                        depth++;
                        seenLists.Add(new HashSet<OSCArray>());
                    }
                    else
                    {
                        copyArray[currentIndex] = currentTemplate[currentIndex].Clone();
                        currentIndex++;
                    }
                }
            }
            return copyArray;

        }

        internal OSCValue[] GetRawValue()
        {
            return this.data;
        }
        /// <summary>
        /// get the string representation of an OSCArray
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            List<HashSet<OSCArray>> seenLists = new List<HashSet<OSCArray>>();

            seenLists.Add(new HashSet<OSCArray>());
            seenLists[0].Add(this);
            seenLists.Add(new HashSet<OSCArray>());
            StringBuilder stringEdition = new StringBuilder();
            stringEdition.Append("[");

            OSCValue[] currentArray = this.data;
            Stack<OSCValue[]> subListStack = new Stack<OSCValue[]>();

            int currentIndex = 0;
            Stack<int> indexStack = new Stack<int>();

            int depth = 0;
            bool setEnd=false;
            while ((currentIndex < currentArray.Length) || (depth > 0))
            {
                if (currentArray.Length <= currentIndex)
                {
                    
                    stringEdition.Append("]");
                    currentArray = subListStack.Pop();
                    currentIndex = indexStack.Pop() + 1;
                    seenLists.RemoveAt(depth);
                    depth--;
                    if (currentArray.Length > currentIndex)
                    {
                        stringEdition.Append(", ");
                    }
                }
                else
                {
                    if (currentArray[currentIndex].GetOSCType() == OSCValueType.OSCArray)
                    {
                        if (depth > 0)
                        {
                            for(int search=0; search < depth; search++)
                            {
                                if (seenLists[search].Contains((OSCArray)currentArray[currentIndex]))
                                {
                                    throw new RecursiveListException();
                                }
                            }
                        }
                        seenLists[depth].Add((OSCArray)currentArray[currentIndex]);
                        seenLists.Add(new HashSet<OSCArray>());
                        subListStack.Push(currentArray);
                        indexStack.Push(currentIndex);

                        currentArray = ((OSCArray)currentArray[currentIndex]).GetRawValue();
                        currentIndex = 0;
                        depth++;
                        
                        stringEdition.Append("[");

                    }
                    else
                    {
                        
                        stringEdition.Append(currentArray[currentIndex].ToString());
                        if (currentIndex + 1 < currentArray.Length)
                        {
                            stringEdition.Append(", ");
                        }
                        currentIndex++;
                        


                    }
                }

            }
            stringEdition.Append("]");

            return stringEdition.ToString();
        }

        /// <summary>
        /// create a deep copy of an OSC Array
        /// </summary>
        /// <returns>OSCArray</returns>
        public override OSCArray Clone()
        {
            return new OSCArray(this.GetValue());
        }
    }

}