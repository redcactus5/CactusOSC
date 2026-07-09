
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/

using System.Buffers.Binary;

using System.Text;



namespace CactusOSC
{
    internal class RawOSCPackage
    {
        private byte[] data;
        private int head;
        
        internal void updateHead(int offset)
        {
            this.head += offset;
        }
        internal void setHead(int newPos)
        {
            this.head = newPos;
        }
        internal byte[] getRawData()
        {
            return this.data;
        }
        
        public int getHead()
        {
            return this.head;
        }

        

        public Span<byte> getWriteSpan4()
        {
            this.head += 4;
            return new Span<byte>(this.data, this.head-4, 4);
            
        }

        public Span<byte> getWriteSpan8()
        {
            this.head += 8;
            return new Span<byte>(this.data, this.head-8, 8);
        }

        public int writeData(byte[] dataToWrite)
        {
            if (dataToWrite.Length > 0)
            {
                Buffer.BlockCopy(dataToWrite, 0, this.data, this.head, dataToWrite.Length);
                this.head += dataToWrite.Length;
            }
            return dataToWrite.Length;
        }

        public int writeByte(byte dataToWrite)
        {
            this.data[this.head] = dataToWrite;
            this.head++;
            return 1;
        }

        public RawOSCPackage(int size)
        {
            this.head = 0;
            this.data= new byte[size];
        }
    }
    internal class OSCPackageCompiler
    {
        private byte[] openBracketBytes;
        private byte[] closeBracketBytes;
        private byte[] commaBytes;
        byte[] bundleIdent;
        private byte[][] typeStrings;
        private byte[] nullByte;
        private byte[][] paddingBytes;
        private bool[] shouldOSCValueDataBytesLookup;
        
        public OSCPackageCompiler()
        {


            //init the constants and caches
            typeStrings = new byte[15][] { Encoding.UTF8.GetBytes("s"), Encoding.UTF8.GetBytes("i"), Encoding.UTF8.GetBytes("f"), Encoding.UTF8.GetBytes("b"), Encoding.UTF8.GetBytes("h"), Encoding.UTF8.GetBytes("t"), Encoding.UTF8.GetBytes("d"), Encoding.UTF8.GetBytes("S"), Encoding.UTF8.GetBytes("c"), Encoding.UTF8.GetBytes("r"), Encoding.UTF8.GetBytes("m"), Encoding.UTF8.GetBytes("T"), Encoding.UTF8.GetBytes("F"), Encoding.UTF8.GetBytes("N"), Encoding.UTF8.GetBytes("I") };
             shouldOSCValueDataBytesLookup= new bool[14] { true, true, true, true, true, true, true, true, true, true, true, false, false, false };
            nullByte = new byte[] { 0 };
            paddingBytes = new byte[4][] { Array.Empty<byte>(), new byte[] { 0 }, new byte[] { 0, 0 }, new byte[] { 0, 0, 0 } };
            


            openBracketBytes = Encoding.UTF8.GetBytes("[");
            closeBracketBytes = Encoding.UTF8.GetBytes("]");
            commaBytes = Encoding.UTF8.GetBytes(",");
            bundleIdent= this.generateOSCString("#bundle");

        }

        private int generateArrayTypeString(OSCArray array,RawOSCPackage target)
        {
  


            Stack<OSCArray> arrayStack = new Stack<OSCArray>();
            Stack<int> restoreIndex= new Stack<int>();

            int bytesWritten = 0;
            bytesWritten=target.writeData(this.openBracketBytes);

            HashSet<OSCArray> seenArrays = new HashSet<OSCArray>();

            OSCArray currentArray = array;
            OSCValue[] currentContents = currentArray.GetRawValue();
            int currentIndex = 0;
            

            bool solving = true;
            while (solving)
            {
                
                while (currentIndex<currentContents.Length)
                {
                    if (currentContents[currentIndex].GetOSCType() == OSCValueType.OSCArray)
                    {
                        if (seenArrays.Contains((OSCArray)currentContents[currentIndex]))
                        {
                            throw new RecursiveListException();
                        }
                        seenArrays.Add((OSCArray)currentContents[currentIndex]);
                        arrayStack.Push(currentArray);
                        restoreIndex.Push(currentIndex+1);

                        bytesWritten+=target.writeData(this.openBracketBytes);
                        currentArray = ((OSCArray)currentContents[currentIndex]);
                        currentIndex = 0;
                        currentContents = currentArray.GetRawValue();
                    }
                    else
                    {
                        bytesWritten += this.getOSCTypeString(currentContents[currentIndex], target);
                        currentIndex++;
                    }
                    
                }
                if(arrayStack.Count > 0)
                {
                    currentArray=arrayStack.Pop();
                    currentContents = currentArray.GetRawValue();
                    currentIndex = restoreIndex.Pop();


                }
                else
                {
                    solving = false;
                }
                bytesWritten= target.writeData(this.closeBracketBytes);
            }
            

            return bytesWritten;

            
        }

        private int getOSCTypeString(OSCValue value, RawOSCPackage target)
        {
            //i know it looks weird, but this is actually the fastest way to do this
            switch (value.GetOSCType())
            {
                case OSCValueType.OSCString:
                    return target.writeData(this.typeStrings[0]);
                   
                case OSCValueType.OSCInt:
                    return target.writeData(this.typeStrings[1]);
                  
                case OSCValueType.OSCFloat:
                    return target.writeData(this.typeStrings[2]);
                  
                case OSCValueType.OSCBlob:
                    return target.writeData(this.typeStrings[3]);
                   
                case OSCValueType.OSCLong:
                    return target.writeData(this.typeStrings[4]);
                 
                case OSCValueType.OSCTimeTag:
                    return target.writeData(this.typeStrings[5]);
                  
                case OSCValueType.OSCDouble:
                    return target.writeData(this.typeStrings[6]);
                   
                case OSCValueType.OSCNonstandardString:
                    return target.writeData(this.typeStrings[7]);
                    
                case OSCValueType.OSCChar:
                    return target.writeData(this.typeStrings[8]);
                    
                case OSCValueType.OSCRGBA:
                    return target.writeData(this.typeStrings[9]);
                    
                case OSCValueType.OSCMIDI:
                    return target.writeData(this.typeStrings[10]);
                    
                case OSCValueType.OSCBool:
                    if (((OSCBool)value).GetValue())
                    {
                        return target.writeData(this.typeStrings[11]);
                    }
                    else
                    {
                        return target.writeData(this.typeStrings[12]);
                    }
                    
                case OSCValueType.OSCNil:
                    return target.writeData(this.typeStrings[13]);
                    
                case OSCValueType.OSCInfinitum:
                    return target.writeData(this.typeStrings[14]);
                    
                default:
                        throw new InvalidOSCValueTypeException();
                    
                    
            }
            
            
        }

        private RawOSCPackage getOSCValueBytes(OSCValue value, RawOSCPackage target)
        {
            

            switch (value.GetOSCType())
            {
                case OSCValueType.OSCString:
                    GenerateAndWriteOSCString(((OSCString)value).GetValue(),target);
                    break;
                case OSCValueType.OSCInt:
                    BinaryPrimitives.WriteInt32BigEndian(target.getWriteSpan4(), ((OSCInt)value).GetValue());
                    
                    break;
                case OSCValueType.OSCFloat:
                    BinaryPrimitives.WriteSingleBigEndian(target.getWriteSpan4(), ((OSCFloat)value).GetValue());
                    
                    break;
                case OSCValueType.OSCBlob:
                    BinaryPrimitives.WriteInt32BigEndian(target.getWriteSpan4(), ((OSCBlob)value).GetRawValue().Length);
                    
                    target.writeData(((OSCBlob)value).GetRawValue());
                    break;
                case OSCValueType.OSCLong:
                    BinaryPrimitives.WriteInt64BigEndian(target.getWriteSpan8(), ((OSCLong)value).GetValue());
                    
                    break;
                case OSCValueType.OSCTimeTag:
                    BinaryPrimitives.WriteUInt64BigEndian(target.getWriteSpan8(), ((OSCTimeTag)value).getValue());
                    
                    break;
                case OSCValueType.OSCDouble:
                    BinaryPrimitives.WriteDoubleBigEndian(target.getWriteSpan8(), ((OSCDouble)value).GetValue());
                    
                    break;
                case OSCValueType.OSCNonstandardString:
                    GenerateAndWriteOSCString(((OSCNonstandardString)value).GetValue(), target);
                    break;
                case OSCValueType.OSCChar:
                    target.writeByte((byte)((OSCChar)value).GetValue());
                    target.writeData(this.paddingBytes[3]);
                    break;
                case OSCValueType.OSCRGBA:
                    BinaryPrimitives.WriteInt32BigEndian(target.getWriteSpan4(),((OSCColor)value).GetValue());
                    
                    break;
                case OSCValueType.OSCMIDI:
                    BinaryPrimitives.WriteInt32BigEndian(target.getWriteSpan4(), ((OSCMIDI)value).GetValue());
                    
                    break;
                default:
                    throw new InvalidOSCValueTypeException();
                    
            }
            return target;
        }

        private bool shouldGetOSCValueBytes(OSCValue value)
        {
            int typeCache = (int)value.GetOSCType();
            if ((typeCache < 0) || (typeCache > this.shouldOSCValueDataBytesLookup.Length))
            {
                throw new InvalidOSCValueTypeException();
            }
            return this.shouldOSCValueDataBytesLookup[typeCache];

        }

        private RawOSCPackage generateArrayValueBytes(OSCArray array,RawOSCPackage target)
        {

            Stack<OSCArray> arrayStack = new Stack<OSCArray>();
            Stack<int> arrayIndex = new Stack<int>();



            OSCArray currentArray = array;
            OSCValue[] currentContents = currentArray.GetRawValue();
            int currentIndex = 0;

            bool solving = true;

            while (solving)
            {
                
                while (currentIndex < currentContents.Length)
                {
                    if (currentContents[currentIndex].GetOSCType() == OSCValueType.OSCArray)
                    {
                        arrayStack.Push(currentArray);
                        arrayIndex.Push(currentIndex+1);
                        
                        
                        currentArray = ((OSCArray)currentContents[currentIndex]);
                        currentIndex = 0;
                        currentContents = currentArray.GetRawValue();
                    }
                    else
                    {
                        if (this.shouldGetOSCValueBytes(currentContents[currentIndex]))
                        {
                            this.getOSCValueBytes(currentContents[currentIndex],target);
                            currentIndex++;
                        }
                    }

                }
                if (arrayStack.Count > 0)
                {
                    currentArray = arrayStack.Pop();
                    currentContents = currentArray.GetRawValue();
                    currentIndex = arrayIndex.Pop();


                }
                else
                {
                    solving = false;
                }
                
            }
            return target;
        }

        private RawOSCPackage generateOSCValueBytes(OSCValue[] values, RawOSCPackage target)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].GetOSCType() == OSCValueType.OSCArray)
                {
                    generateArrayValueBytes((OSCArray)values[i], target);
                }
                else
                {
                    if (this.shouldGetOSCValueBytes(values[i]))
                    {
                        getOSCValueBytes(values[i], target);
                    }
                }
            }
            return target;
        }

        private RawOSCPackage generateOSCValueTypeString(OSCValue[] values,RawOSCPackage target)
        {
            target.writeData(this.commaBytes);
            int typeStringSize = 1;
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].GetOSCType() == OSCValueType.OSCArray)
                {
                    typeStringSize+=this.generateArrayTypeString(((OSCArray)values[i]),target);
                }
                else
                {
                    this.getOSCTypeString(values[i],target);
                    typeStringSize++;
                    
                }
            }
            typeStringSize+=target.writeData(this.nullByte);
            int padding = this.calculateOSCStringOverflowSize(typeStringSize);
            target.writeData(this.paddingBytes[padding]);
            return target;
        }

        private int calculateOSCStringSize(int textLength)
        {
            int tempsize = textLength + 1;
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

        private int calculateOSCStringOverflowSize(int textByteCount)
        {
            int tempsize = 0;
            int overflow = textByteCount % 4;


            if (overflow != 0)
            {
                tempsize = 4 - overflow;
            }
            
            return tempsize;
        }

        private byte[] generateOSCString(string text)
        {
            int textSize = Encoding.UTF8.GetByteCount(text);
            byte[] bytes = new byte[this.calculateOSCStringSize(textSize)];
            //ensure the padding is cleared
            Array.Clear(bytes,textSize,bytes.Length-textSize);
            //fast copy
            Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);
            
            return bytes;
        }
        private int GenerateAndWriteOSCString(string text, RawOSCPackage target)
        {
            int byteCount = Encoding.UTF8.GetByteCount(text);
            int totalSize = this.calculateOSCStringSize(byteCount);

            
            int written = Encoding.UTF8.GetBytes(text,0,Encoding.UTF8.GetByteCount(text),target.getRawData(),target.getHead());
            target.updateHead(written);
            target.writeData(this.nullByte);
            
            int paddingBytes = calculateOSCStringOverflowSize(written+1);
            target.writeData(this.paddingBytes[paddingBytes]);

            return written+paddingBytes;
        }
        
        

        public RawOSCPackage convertOSCMessageToByteArray(OSCMessage message,RawOSCPackage target)
        {
            this.GenerateAndWriteOSCString(message.GetAddress(), target);
            generateOSCValueTypeString(message.GetValues(),target);
            generateOSCValueBytes(message.GetValues(),target);
            return target;
        }

        
        public RawOSCPackage convertOSCBundleToByteArray(OSCBundle bundle,RawOSCPackage target)
        {
            
            

            Stack<OSCBundle> subBundleStack = new Stack<OSCBundle>();
            
            Stack<int> indexStack = new Stack<int>();


            OSCBundle currentBundle = bundle;
            OSCBundleElement[] currentContents = currentBundle.GetRawElements();
            int currentIndex = 0;

            
            HashSet<OSCBundle> seenBundles = new HashSet<OSCBundle>();
            
            //write bundle ident
            target.writeData(this.bundleIdent);
            //writeTimeTag
            BinaryPrimitives.WriteUInt64BigEndian(target.getWriteSpan8(),bundle.GetTimeTag());
            

            bool solving = true;
            while (solving)
            {

                while (currentIndex < currentContents.Length)
                {
                    if (currentContents[currentIndex].GetRawContents().GetPackageType() == OSCPackageType.OSCBundle)
                    {
                        if (seenBundles.Contains((OSCBundle)currentContents[currentIndex].GetRawContents()))
                        {
                            throw new RecursiveBundleException();

                        }
                        seenBundles.Add((OSCBundle)currentContents[currentIndex].GetRawContents());

                        subBundleStack.Push(currentBundle);
                        indexStack.Push(currentIndex + 1);


                        currentBundle = ((OSCBundle)currentContents[currentIndex].GetRawContents());
                        currentIndex = 0;
                        currentContents = currentBundle.GetRawElements();
                        BinaryPrimitives.WriteInt32BigEndian(target.getWriteSpan4(), currentBundle.GetSize());
                        
                        target.writeData(this.bundleIdent);
                        BinaryPrimitives.WriteUInt64BigEndian(target.getWriteSpan8(),currentBundle.GetTimeTag());
                        

                    }
                    else
                    {
                        OSCBundleElement elementCache = currentContents[currentIndex];
                        BinaryPrimitives.WriteInt32BigEndian(target.getWriteSpan4(), elementCache.GetDataSize());
                        
                        this.convertOSCMessageToByteArray((OSCMessage)elementCache.GetRawContents(),target);
                        currentIndex++;

                    }

                }
                if (subBundleStack.Count > 0)
                {
                    currentBundle = subBundleStack.Pop();
                    currentContents = currentBundle.GetRawElements();
                    currentIndex = indexStack.Pop();


                }
                else
                {
                    solving = false;
                }

            }
            return target;
        
        }
        

        
    }
}
