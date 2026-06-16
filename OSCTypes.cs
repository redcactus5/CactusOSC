using System.Buffers.Binary;
using System.Text;

namespace CactusOSC
{
    
    public class OSCValue
    {
        //base class, nothing to see here 
        private object value;

        public OSCValue(object value)
        {
            this.value = value;
        }
        public OSCValue()
        {
            this.value = null;
        }
        public object getValue()
        {
            return value;
        }

        public void setValue(object value)
        {
            this.value = value;
        }

        public string toString()
        {
            return value.ToString();
        }

        public OSCValue copy()
        {
            return new OSCValue(this.value);
        }
    }

    public class OSCString : OSCValue {
        private string value;
        public OSCString(string value)
        {
            this.value = value;
        }
        public OSCString()
        {
            this.value = "";
        }
        public string getValue()
        {
            return this.value;
        }
        public void setValue(string value)
        {
            this.value = value;
        }

        public string toString()
        {
            return this.value.ToString();
        }

        public OSCString copy()
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
        }
        public OSCInt()
        {
            this.value = 0;
        }
        public int getValue()
        {
            return this.value;
        }
        public void setValue(int value)
        {
            this.value = value;
        }

        public string toString()
        {
            return this.value.ToString();
        }

        public OSCInt copy()
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
        }
        public OSCFloat()
        {
            this.value = 0f;
        }
        public float getValue()
        {
            return this.value;
        }
        public void setValue(float value)
        {
            this.value = value;
        }

        public string toString()
        {
            return this.value.ToString();
        }

        public OSCFloat copy()
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
        }
        public OSCBlob()
        {
            this.value = null;
        }
        public byte[] getValue()
        {
            return this.value;
        }
        public void setValue(byte[] value)
        {
            this.value = value;
        }

        public string toString()
        {
            
            return Convert.ToHexString(this.value);
        }

        public OSCBlob copy()
        {
            return new OSCBlob(this.value);
        }

    }


    public class OSCLong : OSCValue
    {
        private long value;
        public OSCLong(long value)
        {
            this.value = value;
        }
        public OSCLong()
        {
            this.value = 0l;
        }
        public long getValue()
        {
            return this.value;
        }
        public void setValue(long value)
        {
            this.value = value;
        }

        public string toString()
        {
            return this.value.ToString();
        }

        public OSCLong copy()
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
        }
        public OSCTimeTag()
        {
            this.value = 0;
        }
        public long getValue()
        {
            return this.value;
        }
        public void setValue(long value)
        {
            this.value = value;
        }

        public string toString()
        {
            return this.value.ToString();
        }

        public OSCInt copy()
        {
            return new OSCInt(this.value);
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

        public void setAddress(string address)
        {
            this.address = address;
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

        public void setValues(OSCValue[] values)
        {
            this.values = values;
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

        public void setValue(uint index, OSCValue value)
        {
            if (index < this.values.Length)
            {
                this.values[index]= value;
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
