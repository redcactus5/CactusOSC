using CactusOSC;
using System.Buffers.Binary;
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

    public class SizeAlreadySetException : Exception;

    public abstract class OSCValue
    {
   
        private OSCValueType oscType;
        private int size;
        private bool sizeSet;
        private int typeStringSize;
        private bool typeStringSizeSet;
        public  abstract string toString();

        public abstract OSCValue clone();
        
        public OSCValueType getOSCType()
        {
            return this.oscType;
        }

        public OSCValue(OSCValueType type)
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

        public int getByteSize()
        {
            return this.size;
        }
        public int getTypeStringSize()
        {
            return this.typeStringSize;
        }
        

    }

    public class OSCString : OSCValue {
        private string value;
        
        public OSCString(string value):base(OSCValueType.OSCString)
        {
            this.value = value;
            //calculate the byte block size
            int tempsize=Encoding.UTF8.GetByteCount(value)+1;
            if (tempsize > 0)
            {
                int overflow = tempsize % 4;
                if (overflow != 0)
                {
                    tempsize += 4-overflow;
                }
            }
            else {
                tempsize = 4;
            }
            this.setByteSize(tempsize);
            this.setTypeStringSize(1);
        }
        
        public string getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCString clone()
        {
            return new OSCString(this.value);
        }
    }

    public class OSCInt : OSCValue
    {
        private int value;
        public OSCInt(int value):base(OSCValueType.OSCInt)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }

        public  int getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCInt clone()
        {
            return new OSCInt(this.value);
        }
    }


    public class OSCFloat : OSCValue
    {
        private float value;
        public OSCFloat(float value):base(OSCValueType.OSCFloat)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }

        public  float getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCFloat clone()
        {
            return new OSCFloat(this.value);
        }

    }


    public class OSCBlob : OSCValue
    {
        
        private byte[] value;
        public OSCBlob(byte[] value):base(OSCValueType.OSCBlob) {
        
            this.value = value;
            //calculate the byte block size
            int tempsize = this.value.Length;
            if (tempsize > 0)
            {
                int overflow = tempsize % 4;
                if (overflow != 0)
                {
                    tempsize += 4- overflow;
                    
                }
                    
                
            }
            
            this.setByteSize(tempsize+4);
            this.setTypeStringSize(1);

        }
        
        public byte[] getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return Convert.ToHexString(this.value);
        }
        public override OSCBlob clone()
        {
            return new OSCBlob((byte[])this.value.Clone());
        }

    }


    public class OSCLong : OSCValue
    {
        private long value;
        public  OSCLong(long value):base(OSCValueType.OSCLong)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);

        }
        public long getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }

        public override OSCLong clone()
        {
            return new OSCLong(this.value);
        }

    }

    public class OSCTimeTag : OSCValue
    {
        private long value;
        public OSCTimeTag(long value):base(OSCValueType.OSCTimeTag)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);
        }
        public long getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCTimeTag clone()
        {
            return new OSCTimeTag(this.value);
        }
    }

    public class OSCDouble : OSCValue
    {
        private double value;
        public OSCDouble(double value):base(OSCValueType.OSCDouble)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);
        }
        public double getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCDouble clone()
        {
            return new OSCDouble(this.value);
        }

    }


    public class OSCNonstandardString : OSCValue
    {
        private string value;
        public OSCNonstandardString(string value):base(OSCValueType.OSCNonstandardString)
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
        public string getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCNonstandardString clone()
        {
            return new OSCNonstandardString(this.value);
        }

    }



    public class OSCChar : OSCValue
    {
        private char value;
        public OSCChar(char value):base(OSCValueType.OSCChar)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        public char getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCChar clone()
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

        public OSCColor(byte r, byte g, byte b, byte a):base(OSCValueType.OSCRGBA)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
            this.setByteSize(4);
            this.setTypeStringSize(1);

        }
        public OSCColor(int rgba):base(OSCValueType.OSCRGBA)
        {
            this.r = (byte)((rgba>>24) & 0xff);
            this.g = (byte)((rgba >> 16) & 0xff);
            this.b = (byte)((rgba >> 8) & 0xff);
            this.a = (byte)(rgba & 0xff);
            this.setByteSize(4);
            this.setTypeStringSize(1);

        }
        public int getValue()
        {
            return (((this.r << 24) & (0xff << 24)) | ((this.g << 16) & (0xff << 16)) | ((this.b << 8) & (0xff << 8)) | ((this.a) & 0xff));
        }
        public override string toString()
        {
            return "#"+Convert.ToHexString(new byte[] { this.r, this.g, this.b, this.a });
        }
        public override OSCColor clone()
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

        public OSCMIDI(byte port, byte status, byte data1, byte data2):base(OSCValueType.OSCMIDI)
        {
            this.port = port;
            this.status = status;
            this.data1 = data1;
            this.data2 = data2;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        public OSCMIDI(int midiMessage):base(OSCValueType.OSCMIDI)
        {
            this.port = (byte)((midiMessage >> 24) & 0xff);
            this.status = (byte)((midiMessage >> 16) & 0xff);
            this.data1 = (byte)((midiMessage >> 8) & 0xff);
            this.data2 = (byte)(midiMessage & 0xff);
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        public int getValue()
        {
            return (((this.port << 24)&(0xff<<24)) | ((this.status << 16) & (0xff << 16)) | ((this.data1 << 8) & (0xff << 8)) | ((this.data2)&0xff)); ;
        }
        public override string toString()
        {
            return "#" + Convert.ToHexString(new byte[] { this.port, this.status, this.data1, this.data2 });
        }
        public override OSCMIDI clone()
        {
            return new OSCMIDI(this.port, this.status, this.data1, this.data2);
        }
    }


    public class OSCBool : OSCValue
    {
        private bool value;
        public OSCBool(bool value):base(OSCValueType.OSCBool) {
        
            this.value = value;
            this.setByteSize(0);
            this.setTypeStringSize(1);
        }
        public bool getValue()
        {
            return this.value;
        }
        public override string toString()
        {
            return this.value.ToString();
        }
        public override OSCBool clone()
        {
            return new OSCBool(this.value);
        }
    }

    public class OSCNil : OSCValue
    {
        public OSCNil():base(OSCValueType.OSCNil)
        {
            this.setByteSize(0);
        }
        

        public override string toString()
        {
            return "nil";
        }

        public override OSCNil clone()
        {
            return new OSCNil();
        }

    }


    public class OSCInfinitum : OSCValue
    {
        public OSCInfinitum():base(OSCValueType.OSCInfinum)
        {
            this.setByteSize(0);
            this.setTypeStringSize(1);
        }
        

        public override string toString()
        {
            return "infinitum";
        }

        public override OSCInfinitum clone()
        {
            return new OSCInfinitum();
        }

    }


    public class OSCArray : OSCValue
    {
        public OSCValue[] data;

        public OSCArray(OSCValue[] data):base(OSCValueType.OSCArray)
        {
            this.data = data;
            int tempsize = 0;
            int tempTypeStringSize = 0;
            for (int index = 0; index < data.Length; index++) 
            {
                tempsize += data[index].getByteSize();
                tempTypeStringSize += data[index].getTypeStringSize();
            }
            this.setByteSize(tempsize);
            this.setTypeStringSize(2+tempTypeStringSize);
        }

        public OSCValue[] getValue()
        {
            OSCValue[] dataclone = new OSCValue[data.Length];
            for (int index = 0; index < dataclone.Length; index++)
            {
                dataclone[index] = this.data[index].clone();
            }
            return dataclone;
        }

         internal  OSCValue[] getRawValue()
        {
            return this.data;
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

        public override OSCArray clone()
        {
            OSCValue[] dataclone = new OSCValue[data.Length];
            for (int index = 0; index < dataclone.Length; index++)
            {
                dataclone[index] = this.data[index].clone();
            }
            return new OSCArray(dataclone);
        }
    }


    public enum OSCPackageType
    {
        OSCMessage,
        OSCBundle
    }

    
    public abstract class OSCPackage
    {
        private OSCPackageType type;
        protected int size;
        private bool sizeSet;

        public OSCPackageType getPackageType()
        {
            return this.type;
        }

        public OSCPackage(OSCPackageType type)
        {
            this.type = type;
        }

        

        protected int calculateOSCStringSize(int textLength)
        {
            int tempsize = textLength;
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
            return tempsize;
        }


        public int getSize()
        {
            return this.size;
        }

        protected void setSize(int size)
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
    }
    public class OSCMessage: OSCPackage
    {
        private string address;
        private OSCValue[] values;
        

        
        
        public OSCMessage(string address, OSCValue[] values):base(OSCPackageType.OSCMessage)
        {
            
            this.address = address;
            this.values= values;
            int adressSize=this.calculateOSCStringSize(Encoding.UTF8.GetByteCount(address)+1);
            //account for the comma that denotes its start and the null terminator
            int typeStringSize = 2;
            //calculate the data size and typestring size
            int dataSize = 0;
            for(int index=0; index<this.values.Length; index++)
            {
                dataSize += values[index].getByteSize();
                typeStringSize += values[index].getTypeStringSize();
            }
            //calculate the final size

            typeStringSize = this.calculateOSCStringSize(typeStringSize);
            this.setSize(adressSize+typeStringSize+dataSize);
            
        }
        public OSCMessage(string address): base(OSCPackageType.OSCMessage)
        {
            this.address= address;
            this.values = Array.Empty<OSCValue>();
            //account for the type string
            this.setSize(this.calculateOSCStringSize(Encoding.UTF8.GetByteCount(address) + 1) +calculateOSCStringSize(2));
        }
        public string getAddress()
        {
            return this.address;
        }
        
        public OSCValue[] getValues()
        {
            OSCValue[] clone = new OSCValue[this.values.Length];
            for (int index = 0; index < clone.Length; index++) 
            {
                clone[index] = this.values[index].clone();
            }
            return clone;
        }
        internal OSCValue[] getRawValues()
        {
            return this.values;
        }

        public OSCValue GetValue(int index)
        {
            if ((index < this.values.Length)&&(index>=0))
            {
                return this.values[index].clone();
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

        public OSCMessage clone()
        {
            OSCValue[] valueListCopy= new OSCValue[this.values.Length];
            for(int index = 0; index<this.values.Length; index++)
            {
                valueListCopy[index]= this.values[index].clone();
            }

            return new OSCMessage(this.address, valueListCopy);
        }
    }
}


public class OSCBundle : OSCPackage
{
    private OSCBundleElement[] elements;
    private long timeTag;
    private const int identSize = 8;
    private const int timeTagSize = 8;
    
    internal List<OSCMessage> RawUnpackBundle(OSCBundle bundle)
    {
        
        List<OSCMessage> unpackedValues = new List<OSCMessage>();
        Queue<OSCBundle> toUnpack = new Queue<OSCBundle>();
        toUnpack.Enqueue(bundle);
        while(toUnpack.Count > 0)
        {
            OSCBundle currentBundle= toUnpack.Dequeue();
            OSCBundleElement[] currentElements= currentBundle.getRawElements();
            for(int bundleIndex=0; bundleIndex<currentElements.Length; bundleIndex++)
            {
                OSCPackage contents = currentElements[bundleIndex].getRawContents();
                if (contents.getPackageType() == OSCPackageType.OSCBundle)
                {
                    toUnpack.Enqueue((OSCBundle)contents);
                    
                }
                else
                {
                    unpackedValues.Add((OSCMessage)contents);
                }
            }
        }
        return unpackedValues;
    }
    public List<OSCMessage> UnpackBundle(OSCBundle bundle)
    {
        
        List<OSCMessage> unpackedMessages = new List<OSCMessage>();
        Queue<OSCBundle> toUnpack = new Queue<OSCBundle>();
        toUnpack.Enqueue(bundle);
        while (toUnpack.Count > 0)
        {
            OSCBundle currentBundle = toUnpack.Dequeue();
            OSCBundleElement[] currentElements = currentBundle.getRawElements();
            for (int bundleIndex = 0; bundleIndex < currentElements.Length; bundleIndex++)
            {
                OSCPackage contents = currentElements[bundleIndex].getRawContents();
                if (contents.getPackageType() == OSCPackageType.OSCBundle)
                {
                    
                    toUnpack.Enqueue((OSCBundle)contents);
                }
                else
                {
                    unpackedMessages.Add(((OSCMessage)contents).clone());
                }
            }
        }
        return unpackedMessages;
    }
    private int getElementsSize()
    {
        int tempSize = 0;
        
        for(int elementIndex =0; elementIndex < this.elements.Length; elementIndex++)
        {
            tempSize += this.elements[elementIndex].getSize();
            
        }

        
        
        return tempSize;
    }
    public OSCBundle(OSCBundleElement[] elements,long timeTag):base(OSCPackageType.OSCBundle) 
    {
        this.elements=elements;
        this.timeTag=timeTag;
        this.setSize(this.getElementsSize()+identSize+timeTagSize);
    }
    public OSCBundle(OSCBundleElement[] elements) : base(OSCPackageType.OSCBundle)
    {
        this.elements = elements;
        this.timeTag = 1;
        this.setSize(this.getElementsSize() + identSize+timeTagSize);
    }

    public OSCBundleElement[] getElements()
    {
        OSCBundleElement[] elementsCopy = new OSCBundleElement[this.elements.Length];
        for (int index = 0; index < this.elements.Length; index++)
        {
            elementsCopy[index] = this.elements[index].clone();
        }
        return elementsCopy;
    }
    internal OSCBundleElement[] getRawElements()
    {
        return this.elements;
    }
    public OSCBundle clone()
    {
        OSCBundleElement[] elementsCopy=new OSCBundleElement[this.elements.Length];
        for (int index = 0; index<this.elements.Length; index++)
        {
            elementsCopy[index]= this.elements[index].clone();
        }
        return new OSCBundle(elementsCopy,this.timeTag);
    }
    public long getTimeTag()
    {
        return this.timeTag;
    }
}

public class OSCBundleElement
{
    private OSCPackage contents;
    private int dataSize;
    private int size;
    public OSCBundleElement(OSCPackage contents) 
    {
        this.contents = contents;
        
        
        this.dataSize = this.contents.getSize();
        //account for the size integer
        this.size = 4 + this.dataSize;
    }

    public OSCBundleElement clone()
    {
        return new OSCBundleElement(this.getContents());
    }
    public OSCPackage getContents()
    {
        if (this.contents.getPackageType() == OSCPackageType.OSCMessage)
        {
            OSCMessage tempMessage = (OSCMessage)this.contents;
            return tempMessage.clone();
        }
        else
        {
            OSCBundle tempBundle= (OSCBundle)this.contents;
            return (tempBundle.clone());
        }
    }

    internal OSCPackage getRawContents()
    {
        if (this.contents.getPackageType() == OSCPackageType.OSCMessage)
        {
            OSCMessage tempMessage = (OSCMessage)this.contents;
            return tempMessage;
        }
        else
        {
            OSCBundle tempBundle = (OSCBundle)this.contents;
            return tempBundle;
        }
    }

    public int getSize()
    {
        return this.size;
    }

    public int getDataSize()
    {
        return this.dataSize;
    }
}