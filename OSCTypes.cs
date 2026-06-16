
using System.Text;

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
        OSCInfinum,
        OSCArray
    }
    public abstract class OSCValue
    {
   
        protected OSCValueType oscType;
        public  abstract string toString();

        public abstract OSCValue copy();
        
        public OSCValueType getOSCType()
        {
            return this.oscType;
        }

    }

    public class OSCString : OSCValue {
        private string value;
        public OSCString(string value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCString;
        }
        
        public string getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCString copy()
        {
            return new OSCString(this.value);
        }
    }

    public class OSCInt : OSCValue
    {
        private int value;
        public OSCInt(int value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCInt;
        }

        public  int getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCInt copy()
        {
            return new OSCInt(this.value);
        }
    }


    public class OSCFloat : OSCValue
    {
        private float value;
        public OSCFloat(float value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCFloat;
        }

        public  float getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCFloat copy()
        {
            return new OSCFloat(this.value);
        }

    }


    public class OSCBlob : OSCValue
    {
        private byte[] value;
        public OSCBlob(byte[] value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCBlob;
        }
        public byte[] getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return Convert.ToHexString(this.value);
        }
        public override OSCBlob copy()
        {
            return new OSCBlob((byte[])this.value.Clone());
        }

    }


    public class OSCLong : OSCValue
    {
        private long value;
        public  OSCLong(long value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCLong;
        }
        public long getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }

        public override OSCLong copy()
        {
            return new OSCLong(this.value);
        }

    }

    public class OSCTimeTag : OSCValue
    {
        private long value;
        public OSCTimeTag(long value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCTimeTag;
        }
        public long getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCTimeTag copy()
        {
            return new OSCTimeTag(this.value);
        }
    }

    public class OSCDouble : OSCValue
    {
        private double value;
        public OSCDouble(double value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCDouble;
        }
        public double getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCDouble copy()
        {
            return new OSCDouble(this.value);
        }

    }


    public class OSCNonstandardString : OSCValue
    {
        private string value;
        public OSCNonstandardString(string value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCNonstandardString;
        }
        public string getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCNonstandardString copy()
        {
            return new OSCNonstandardString(this.value);
        }

    }



    public class OSCChar : OSCValue
    {
        private char value;
        public OSCChar(char value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCChar;
        }
        public char getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCChar copy()
        {
            return new OSCChar(this.value);
        }
    }


    public class OSCColor : OSCValue
    {
        private byte r;
        private byte g;
        private byte b;
        private byte a;

        public OSCColor(byte r, byte g, byte b, byte a)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
            this.oscType = OSCValueType.OSCRGBA;
        }
        public OSCColor(UInt32 rgba)
        {
            this.r = (byte)((rgba>>24) & 0xff);
            this.g = (byte)((rgba >> 16) & 0xff);
            this.b = (byte)((rgba >> 8) & 0xff);
            this.a = (byte)(rgba & 0xff);
            this.oscType = OSCValueType.OSCRGBA;
        }
        public UInt32 getValue()
        {
            return BitConverter.ToUInt32(new byte[] {this.r,this.g,this.b, this.a});
        }
        public override string toString()
        {
            return "#"+Convert.ToHexString(new byte[] { this.r, this.g, this.b, this.a });
        }
        public override OSCColor copy()
        {
            return new OSCColor(this.r,this.g,this.b,this.a);
        }
    }


    public class OSCMIDI : OSCValue
    {
        private byte port;
        private byte status;
        private byte data1;
        private byte data2;

        public OSCMIDI(byte r, byte g, byte b, byte a)
        {
            this.port = r;
            this.status = g;
            this.data1 = b;
            this.data2 = a;
            this.oscType = OSCValueType.OSCMIDI;
        }
        public OSCMIDI(UInt32 rgba)
        {
            this.port = (byte)((rgba >> 24) & 0xff);
            this.status = (byte)((rgba >> 16) & 0xff);
            this.data1 = (byte)((rgba >> 8) & 0xff);
            this.data2 = (byte)(rgba & 0xff);
        }
        public UInt32 getValue()
        {
            return BitConverter.ToUInt32(new byte[] { this.port, this.status, this.data1, this.data2 });
        }
        public override string toString()
        {
            return "#" + Convert.ToHexString(new byte[] { this.port, this.status, this.data1, this.data2 });
        }
        public override OSCMIDI copy()
        {
            return new OSCMIDI(this.port, this.status, this.data1, this.data2);
        }
    }


    public class OSCBool : OSCValue
    {
        private bool value;
        public OSCBool(bool value)
        {
            this.value = value;
            this.oscType = OSCValueType.OSCBool;
        }
        public bool getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCBool copy()
        {
            return new OSCBool(this.value);
        }
    }

    public class OSCNil : OSCValue
    {
        public OSCNil()
        {
            this.oscType = OSCValueType.OSCNil;
        }
        

        public override string toString()
        {
            return "nil";
        }

        public override OSCNil copy()
        {
            return new OSCNil();
        }

    }


    public class OSCInfinitum : OSCValue
    {
        public OSCInfinitum()
        {
            this.oscType = OSCValueType.OSCInfinum;
        }
        

        public override string toString()
        {
            return "infinitum";
        }

        public override OSCNil copy()
        {
            return new OSCNil();
        }

    }


    public class OSCArray : OSCValue
    {
        public OSCValue[] data;

        public OSCArray(OSCValue[] data)
        {
            this.data = data;
            this.oscType = OSCValueType.OSCArray;
        }

        public OSCValue[] getValue()
        {
            OSCValue[] dataCopy = new OSCValue[data.Length];
            for (int index = 0; index < dataCopy.Length; index++)
            {
                dataCopy[index] = this.data[index].copy();
            }
            return dataCopy;
        }

        public override string toString()
        {
            StringBuilder stringEdition = new StringBuilder();
            stringEdition.Append("[");
            for (int index = 0; index < data.Length - 1; index++)
            {
                stringEdition.Append(this.data[index].ToString() + ", ");
            }
            stringEdition.Append(this.data[data.Length - 1].ToString());
            stringEdition.Append("]");
            return stringEdition.ToString();
        }

        public override OSCArray copy()
        {
            OSCValue[] dataCopy = new OSCValue[data.Length];
            for (int index = 0; index < dataCopy.Length; index++)
            {
                dataCopy[index] = this.data[index].copy();
            }
            return new OSCArray(dataCopy);
        }
    }

    public class OSCMessage
    {
        private string address;
        private OSCValue[] values;
        public OSCMessage(string address, OSCValue[] values)
        {
            this.address = address;
            this.values= values;
        }
        public OSCMessage(string address)
        {
            this.address= address;
            this.values = null;
        }
        public string getAddress()
        {
            return this.address;
        }
        public OSCValue[] getValues()
        {
            OSCValue[] copy = new OSCValue[this.values.Length];
            for (int index = 0; index < copy.Length; index++) 
            {
                copy[index] = this.values[index].copy();
            }
            return copy;
        }

        public OSCValue GetValue(uint index)
        {
            if (index < this.values.Length)
            {
                return this.values[index].copy();
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }


        public string toString()
        {
            StringBuilder stringedVersion = new StringBuilder();
            stringedVersion.Append(this.address);
            stringedVersion.Append(" ");
            for (int index = 0; index < this.values.Length; index++)
            {
                stringedVersion.Append(this.values[index].ToString());
            }
            return stringedVersion.ToString();
        }

        
    }
}
