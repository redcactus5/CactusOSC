
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/


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
        OSCInfinitum,
        OSCArray
    }



    public abstract class OSCValue
    {

        private OSCValueType oscType;
        private int size;
        private bool sizeSet;
        private int typeStringSize;
        private bool typeStringSizeSet;
        public abstract override string ToString();

        public abstract OSCValue Clone();

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

    public sealed class OSCString : OSCValue
    {
        private string value;

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

        public string getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCString Clone()
        {
            return new OSCString(this.value);
        }
    }

    public sealed class OSCInt : OSCValue
    {
        private int value;
        public OSCInt(int value) : base(OSCValueType.OSCInt)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }

        public int getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCInt Clone()
        {
            return new OSCInt(this.value);
        }
    }


    public sealed class OSCFloat : OSCValue
    {
        private float value;
        public OSCFloat(float value) : base(OSCValueType.OSCFloat)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }

        public float getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCFloat Clone()
        {
            return new OSCFloat(this.value);
        }

    }


    public sealed class OSCBlob : OSCValue
    {

        private byte[] value;
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

        public byte[] getValue()
        {
            return (byte[])this.value.Clone();
        }

        internal byte[] getRawValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return Convert.ToHexString(this.value);
        }
        public override OSCBlob Clone()
        {
            return new OSCBlob((byte[])this.value.Clone());
        }

    }


    public sealed class OSCLong : OSCValue
    {
        private long value;
        public OSCLong(long value) : base(OSCValueType.OSCLong)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);

        }
        public long getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }

        public override OSCLong Clone()
        {
            return new OSCLong(this.value);
        }

    }

    public sealed class OSCTimeTag : OSCValue
    {
        private long value;
        public OSCTimeTag(long value) : base(OSCValueType.OSCTimeTag)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);
        }
        public long getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCTimeTag Clone()
        {
            return new OSCTimeTag(this.value);
        }
    }

    public sealed class OSCDouble : OSCValue
    {
        private double value;
        public OSCDouble(double value) : base(OSCValueType.OSCDouble)
        {
            this.value = value;
            this.setByteSize(8);
            this.setTypeStringSize(1);
        }
        public double getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCDouble Clone()
        {
            return new OSCDouble(this.value);
        }

    }


    public sealed class OSCNonstandardString : OSCValue
    {
        private string value;
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
        public string getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCNonstandardString Clone()
        {
            return new OSCNonstandardString(this.value);
        }

    }



    public sealed class OSCChar : OSCValue
    {
        private char value;
        public OSCChar(char value) : base(OSCValueType.OSCChar)
        {
            this.value = value;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        public char getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCChar Clone()
        {
            return new OSCChar(this.value);
        }
    }


    public sealed class OSCColor : OSCValue
    {
        private byte r;
        private byte g;
        private byte b;
        private byte a;

        public OSCColor(byte r, byte g, byte b, byte a) : base(OSCValueType.OSCRGBA)
        {
            this.r = r;
            this.g = g;
            this.b = b;
            this.a = a;
            this.setByteSize(4);
            this.setTypeStringSize(1);

        }
        public OSCColor(int rgba) : base(OSCValueType.OSCRGBA)
        {
            this.r = (byte)((rgba >> 24) & 0xff);
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
        public override string ToString()
        {
            return "#" + Convert.ToHexString(new byte[] { this.r, this.g, this.b, this.a });
        }
        public override OSCColor Clone()
        {
            return new OSCColor(this.r, this.g, this.b, this.a);
        }
    }


    public sealed class OSCMIDI : OSCValue
    {
        private byte port;
        private byte status;
        private byte data1;
        private byte data2;

        public OSCMIDI(byte port, byte status, byte data1, byte data2) : base(OSCValueType.OSCMIDI)
        {
            this.port = port;
            this.status = status;
            this.data1 = data1;
            this.data2 = data2;
            this.setByteSize(4);
            this.setTypeStringSize(1);
        }
        public OSCMIDI(int midiMessage) : base(OSCValueType.OSCMIDI)
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
            return (((this.port << 24) & (0xff << 24)) | ((this.status << 16) & (0xff << 16)) | ((this.data1 << 8) & (0xff << 8)) | ((this.data2) & 0xff)); ;
        }
        public override string ToString()
        {
            return "#" + Convert.ToHexString(new byte[] { this.port, this.status, this.data1, this.data2 });
        }
        public override OSCMIDI Clone()
        {
            return new OSCMIDI(this.port, this.status, this.data1, this.data2);
        }
    }


    public sealed class OSCBool : OSCValue
    {
        private bool value;
        public OSCBool(bool value) : base(OSCValueType.OSCBool)
        {

            this.value = value;
            this.setByteSize(0);
            this.setTypeStringSize(1);
        }
        public bool getValue()
        {
            return this.value;
        }
        public override string ToString()
        {
            return this.value.ToString();
        }
        public override OSCBool Clone()
        {
            return new OSCBool(this.value);
        }
    }

    public sealed class OSCNil : OSCValue
    {
        public OSCNil() : base(OSCValueType.OSCNil)
        {
            this.setByteSize(0);
        }


        public override string ToString()
        {
            return "nil";
        }

        public override OSCNil Clone()
        {
            return new OSCNil();
        }

    }


    public sealed class OSCInfinitum : OSCValue
    {
        public OSCInfinitum() : base(OSCValueType.OSCInfinitum)
        {
            this.setByteSize(0);
            this.setTypeStringSize(1);
        }


        public override string ToString()
        {
            return "infinitum";
        }

        public override OSCInfinitum Clone()
        {
            return new OSCInfinitum();
        }

    }


    public sealed class OSCArray : OSCValue
    {
        private OSCValue[] data;

        public OSCArray(OSCValue[] data) : base(OSCValueType.OSCArray)
        {
            this.data = (OSCValue[])data.Clone();
            int tempsize = 0;
            int tempTypeStringSize = 0;
            for (int index = 0; index < data.Length; index++)
            {
                tempsize += data[index].getByteSize();
                tempTypeStringSize += data[index].getTypeStringSize();
            }
            this.setByteSize(tempsize);
            this.setTypeStringSize(2 + tempTypeStringSize);
        }

        public OSCValue[] getValue()
        {
            Stack<OSCValue[]> templateArrayStack = new Stack<OSCValue[]>();
            OSCValue[] currentTemplate = this.data;

            HashSet<OSCArray> seenLists = new HashSet<OSCArray>();

            seenLists.Add(this);
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
                    depth--;
                }
                else
                {
                    if (currentTemplate[currentIndex].getOSCType() == OSCValueType.OSCArray)
                    {
                        if (seenLists.Contains((OSCArray)currentTemplate[currentIndex]))
                        {
                            throw new RecursiveListException();
                        }
                        seenLists.Add((OSCArray)currentTemplate[currentIndex]);
                        templateArrayStack.Push(currentTemplate);
                        copyStack.Push(copyArray);
                        currentTemplate = ((OSCArray)currentTemplate[currentIndex]).getRawValue();
                        IndexStack.Push(currentIndex);
                        currentIndex = 0;
                        depth++;
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

        internal OSCValue[] getRawValue()
        {
            return this.data;
        }

        public override string ToString()
        {
            //need to redo this to be manual recusion so its safe for deep nesting
            StringBuilder stringEdition = new StringBuilder();
            stringEdition.Append("[");

            OSCValue[] currentArray = this.data;
            Stack<OSCValue[]> subListStack = new Stack<OSCValue[]>();

            int currentIndex = 0;
            Stack<int> indexStack = new Stack<int>();

            int depth = 0;

            while ((currentIndex < currentArray.Length) || (depth > 0))
            {
                if (currentArray.Length <= currentIndex)
                {
                    stringEdition.Append("]");
                    currentArray = subListStack.Pop();
                    currentIndex = indexStack.Pop() + 1;
                    depth--;
                }
                else
                {
                    if (currentArray[currentIndex].getOSCType() == OSCValueType.OSCArray)
                    {
                        subListStack.Push(currentArray);
                        indexStack.Push(currentIndex);

                        currentArray = ((OSCArray)currentArray[currentIndex]).getRawValue();
                        currentIndex = 0;
                        depth++;
                        stringEdition.Append("[");

                    }
                    else
                    {
                        if (data.Length > 0)
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

            }


            return stringEdition.ToString();
        }


        public override OSCArray Clone()
        {
            return new OSCArray(this.getValue());
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
    public sealed class OSCMessage : OSCPackage
    {
        private string address;
        private OSCValue[] values;




        public OSCMessage(string address, OSCValue[] values) : base(OSCPackageType.OSCMessage)
        {

            this.address = address;
            this.values = values;
            int adressSize = this.calculateOSCStringSize(Encoding.UTF8.GetByteCount(address) + 1);
            //account for the comma that denotes its start and the null terminator
            int typeStringSize = 2;
            //calculate the data size and typestring size
            int dataSize = 0;
            for (int index = 0; index < this.values.Length; index++)
            {
                dataSize += values[index].getByteSize();
                typeStringSize += values[index].getTypeStringSize();
            }
            //calculate the final size

            typeStringSize = this.calculateOSCStringSize(typeStringSize);
            this.setSize(adressSize + typeStringSize + dataSize);

        }
        public OSCMessage(string address) : base(OSCPackageType.OSCMessage)
        {
            this.address = address;
            this.values = Array.Empty<OSCValue>();
            //account for the type string
            this.setSize(this.calculateOSCStringSize(Encoding.UTF8.GetByteCount(address) + 1) + calculateOSCStringSize(2));
        }
        public string getAddress()
        {
            return this.address;
        }

        public OSCValue[] getValues()
        {
            OSCValue[] Clone = new OSCValue[this.values.Length];
            for (int index = 0; index < Clone.Length; index++)
            {
                Clone[index] = this.values[index].Clone();
            }
            return Clone;
        }
        internal OSCValue[] getRawValues()
        {
            return this.values;
        }

        public OSCValue GetValue(int index)
        {
            if ((index < this.values.Length) && (index >= 0))
            {
                return this.values[index].Clone();
            }
            else
            {
                throw new IndexOutOfRangeException();
            }
        }


        public override string ToString()
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

        public OSCMessage Clone()
        {
            OSCValue[] valueListCopy = new OSCValue[this.values.Length];
            for (int index = 0; index < this.values.Length; index++)
            {
                valueListCopy[index] = this.values[index].Clone();
            }

            return new OSCMessage(this.address, valueListCopy);
        }
    }



    public sealed class OSCBundle : OSCPackage
    {
        private OSCBundleElement[] elements;
        private long timeTag;
        private const int identSize = 8;
        private const int timeTagSize = 8;


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            Stack<int> subIndexes = new Stack<int>();
            int index = 0;
            Stack<OSCBundle> childBundles = new Stack<OSCBundle>();
            OSCBundle currentSubBundle = this;
            HashSet<OSCPackage> packages = new HashSet<OSCPackage>();
            packages.Add(this);
            int depth = 0;

            sb.Append("Bundle(");
            uint seconds = (uint)(timeTag >> 32);
            uint fraction = (uint)(timeTag & 0xffffffff);
            sb.Append(seconds);
            sb.Append(':');
            sb.Append(fraction);
            sb.Append("):");
            sb.Append("\n");

            while ((index < currentSubBundle.elements.Length) || (depth > 0))
            {

                if ((index >= currentSubBundle.getRawElements().Length) && (depth > 0))
                {
                    depth--;
                    index = subIndexes.Pop() + 1;
                    currentSubBundle = childBundles.Pop();
                }
                else
                {
                    if (currentSubBundle.elements[index].getRawContents().getPackageType() == OSCPackageType.OSCMessage)
                    {
                        for (int i = 0; i < depth + 1; i++)
                        {
                            sb.Append("    ");
                        }
                        sb.Append(((OSCMessage)currentSubBundle.elements[index].getRawContents()).ToString());
                        sb.Append('\n');
                        index++;
                    }
                    else
                    {
                        if (packages.Contains((OSCBundle)currentSubBundle.elements[index].getRawContents()))
                        {
                            for (int i = 0; i < depth; i++)
                            {
                                sb.Append("    ");

                            }
                            sb.Append("<recursive bundle reference>");
                            sb.Append('\n');
                        }
                        else
                        {
                            depth++;
                            subIndexes.Push(index);
                            index = 0;

                            childBundles.Push(currentSubBundle);


                            currentSubBundle = ((OSCBundle)currentSubBundle.elements[index].getRawContents());
                            packages.Add(currentSubBundle);
                            for (int i = 0; i < depth; i++)
                            {
                                sb.Append("    ");
                            }
                            sb.Append("Bundle(");
                            seconds = (uint)(currentSubBundle.timeTag >> 32);
                            fraction = (uint)(currentSubBundle.timeTag & 0xffffffff);
                            sb.Append(seconds);
                            sb.Append(':');
                            sb.Append(fraction);
                            sb.Append("):");
                            sb.Append("\n");
                        }


                    }
                }

            }
            return sb.ToString();
        }

        private int getElementsSize()
        {
            int tempSize = 0;

            for (int elementIndex = 0; elementIndex < this.elements.Length; elementIndex++)
            {
                tempSize += this.elements[elementIndex].getSize();

            }



            return tempSize;
        }
        public OSCBundle(OSCBundleElement[] elements, long timeTag) : base(OSCPackageType.OSCBundle)
        {
            this.elements = elements;
            this.timeTag = timeTag;
            this.setSize(this.getElementsSize() + identSize + timeTagSize);
        }
        public OSCBundle(OSCBundleElement[] elements) : base(OSCPackageType.OSCBundle)
        {
            this.elements = elements;
            this.timeTag = 1;
            this.setSize(this.getElementsSize() + identSize + timeTagSize);
        }

        public OSCBundleElement[] getElements()
        {

            

            Stack<OSCBundleElement[]> templateArrayStack = new Stack<OSCBundleElement[]>();
            OSCBundleElement[] currentTemplate = this.elements;

            Stack<OSCBundleElement[]> copyStack = new Stack<OSCBundleElement[]>();
            OSCBundleElement[] copyArray = new OSCBundleElement[currentTemplate.Length];

            HashSet<OSCBundle> seenBundles = new HashSet<OSCBundle>();
            seenBundles.Add(this);

            Stack<int> IndexStack = new Stack<int>();
            int currentIndex = 0;

            int depth = 0;

            OSCBundleElement tempArray;
            while ((currentIndex < currentTemplate.Length) || (depth > 0))
            {
                if (currentIndex >= currentTemplate.Length)
                {
                    tempArray = new OSCBundleElement(new OSCBundle(copyArray));
                    currentIndex = IndexStack.Pop();
                    currentTemplate = templateArrayStack.Pop();
                    copyArray = copyStack.Pop();
                    copyArray[currentIndex] = tempArray;
                    currentIndex++;
                    depth--;
                }
                else
                {
                    if (currentTemplate[currentIndex].getRawContents().getPackageType() == OSCPackageType.OSCBundle)
                    {
                        if (seenBundles.Contains((OSCBundle)currentTemplate[currentIndex].getRawContents()))
                        {
                            throw new RecursiveBundleException();
                        }
                        seenBundles.Add((OSCBundle)currentTemplate[currentIndex].getRawContents());
                        templateArrayStack.Push(currentTemplate);
                        copyStack.Push(copyArray);
                        currentTemplate = ((OSCBundle)currentTemplate[currentIndex].getRawContents()).getRawElements();
                        IndexStack.Push(currentIndex);
                        currentIndex = 0;
                        depth++;
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
        internal OSCBundleElement[] getRawElements()
        {
            return this.elements;
        }
        public OSCBundle Clone()
        {

            return new OSCBundle(this.getElements(), this.timeTag);
        }
        public long getTimeTag()
        {
            return this.timeTag;
        }
    }

    public sealed class OSCBundleElement
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

        public OSCBundleElement Clone()
        {
            return new OSCBundleElement(this.getContents());
        }
        public OSCPackage getContents()
        {
            if (this.contents.getPackageType() == OSCPackageType.OSCMessage)
            {
                OSCMessage tempMessage = (OSCMessage)this.contents;
                return tempMessage.Clone();
            }
            else
            {
                OSCBundle tempBundle = (OSCBundle)this.contents;
                return (tempBundle.Clone());
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

        public string ToString()
        {
            return this.contents.ToString();
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
}