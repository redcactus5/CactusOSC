
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
        public int writeData(byte[] dataToWrite)
        {
            if (dataToWrite.Length > 0)
            {
                Buffer.BlockCopy(dataToWrite, 0, this.data, this.head, dataToWrite.Length);
                this.head += dataToWrite.Length;
            }
            return dataToWrite.Length;
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
        private byte[] writeCache8;
        private byte[] writeCache4;
        private byte[] writecache1;
        
        public OSCPackageCompiler()
        {
            //init the constants and caches
            typeStrings = new byte[15][] { Encoding.UTF8.GetBytes("s"), Encoding.UTF8.GetBytes("i"), Encoding.UTF8.GetBytes("f"), Encoding.UTF8.GetBytes("b"), Encoding.UTF8.GetBytes("h"), Encoding.UTF8.GetBytes("t"), Encoding.UTF8.GetBytes("d"), Encoding.UTF8.GetBytes("S"), Encoding.UTF8.GetBytes("c"), Encoding.UTF8.GetBytes("r"), Encoding.UTF8.GetBytes("m"), Encoding.UTF8.GetBytes("T"), Encoding.UTF8.GetBytes("F"), Encoding.UTF8.GetBytes("N"), Encoding.UTF8.GetBytes("I") };
            nullByte = new byte[] { 0 };
            paddingBytes = new byte[4][] { Array.Empty<byte>(), new byte[] { 0 }, new byte[] { 0, 0 }, new byte[] { 0, 0, 0 } };
            writeCache8 = new byte[8];
            writeCache4 = new byte[4];
            writecache1 = new byte[1];


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
            OSCValue[] currentContents = currentArray.getRawValue();
            int currentIndex = 0;
            

            bool solving = true;
            while (solving)
            {
                
                while (currentIndex<currentContents.Length)
                {
                    if (currentContents[currentIndex].getOSCType() == OSCValueType.OSCArray)
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
                        currentContents = currentArray.getRawValue();
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
                    currentContents = currentArray.getRawValue();
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
            switch (value.getOSCType())
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
                    if (((OSCBool)value).getValue())
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
            

            switch (value.getOSCType())
            {
                case OSCValueType.OSCString:
                    GenerateAndWriteOSCString(((OSCString)value).getValue(),target);
                    break;
                case OSCValueType.OSCInt:
                    BinaryPrimitives.WriteInt32BigEndian(this.writeCache4, ((OSCInt)value).getValue());
                    target.writeData(this.writeCache4);
                    break;
                case OSCValueType.OSCFloat:
                    BinaryPrimitives.WriteSingleBigEndian(this.writeCache4, ((OSCFloat)value).getValue());
                    target.writeData(this.writeCache4);
                    break;
                case OSCValueType.OSCBlob:
                    BinaryPrimitives.WriteInt32BigEndian(this.writeCache4, ((OSCBlob)value).getRawValue().Length);
                    target.writeData(this.writeCache4);
                    target.writeData(((OSCBlob)value).getRawValue());
                    break;
                case OSCValueType.OSCLong:
                    BinaryPrimitives.WriteInt64BigEndian(this.writeCache8, ((OSCLong)value).getValue());
                    target.writeData(this.writeCache8);
                    break;
                case OSCValueType.OSCTimeTag:
                    BinaryPrimitives.WriteInt64BigEndian(this.writeCache8, ((OSCTimeTag)value).getValue());
                    target.writeData(this.writeCache8);
                    break;
                case OSCValueType.OSCDouble:
                    BinaryPrimitives.WriteDoubleBigEndian(this.writeCache8, ((OSCDouble)value).getValue());
                    target.writeData(this.writeCache8);
                    break;
                case OSCValueType.OSCNonstandardString:
                    GenerateAndWriteOSCString(((OSCNonstandardString)value).getValue(), target);
                    break;
                case OSCValueType.OSCChar:
                    this.writecache1[0] = (byte)((OSCChar)value).getValue();
                    target.writeData(writecache1);
                    target.writeData(this.paddingBytes[3]);
                    break;
                case OSCValueType.OSCRGBA:
                    BinaryPrimitives.WriteInt32BigEndian(this.writeCache4,((OSCColor)value).getValue());
                    target.writeData(writeCache4);
                    break;
                case OSCValueType.OSCMIDI:
                    BinaryPrimitives.WriteInt32BigEndian(this.writeCache4, ((OSCMIDI)value).getValue());
                    target.writeData(writeCache4);
                    break;
                default:
                    throw new InvalidOSCValueTypeException();
                    
            }
            return target;
        }
       
        private bool shouldGetOSCValueBytes(OSCValue value)
        {
            switch (value.getOSCType())
            {
                case OSCValueType.OSCString:
                    return true;
                    
                case OSCValueType.OSCInt:
                    return true;
                    
                case OSCValueType.OSCFloat:
                    return true;
                    
                case OSCValueType.OSCBlob:
                    return true;
                    
                case OSCValueType.OSCLong:
                    return true;
                    
                case OSCValueType.OSCTimeTag:
                    return true;
                    
                case OSCValueType.OSCDouble:
                    return true;
                    
                case OSCValueType.OSCNonstandardString:
                    return true;
                    
                case OSCValueType.OSCChar:
                    return true;
                    
                case OSCValueType.OSCRGBA:
                    return true;
                    
                case OSCValueType.OSCMIDI:
                    return true;
                    
                case OSCValueType.OSCBool:
                    return false;
                    
                case OSCValueType.OSCNil:
                    return false;
                    
                case OSCValueType.OSCInfinitum:
                    return false;
                    
                default:
                    throw new Exception("invalid OSCValueType!");
                    

            }
        }

        private RawOSCPackage generateArrayValueBytes(OSCArray array,RawOSCPackage target)
        {

            Stack<OSCArray> arrayStack = new Stack<OSCArray>();
            Stack<int> arrayIndex = new Stack<int>();



            OSCArray currentArray = array;
            OSCValue[] currentContents = currentArray.getRawValue();
            int currentIndex = 0;

            bool solving = true;

            while (solving)
            {
                
                while (currentIndex < currentContents.Length)
                {
                    if (currentContents[currentIndex].getOSCType() == OSCValueType.OSCArray)
                    {
                        arrayStack.Push(currentArray);
                        arrayIndex.Push(currentIndex+1);
                        
                        
                        currentArray = ((OSCArray)currentContents[currentIndex]);
                        currentIndex = 0;
                        currentContents = currentArray.getRawValue();
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
                    currentContents = currentArray.getRawValue();
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
                if (values[i].getOSCType() == OSCValueType.OSCArray)
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
                if (values[i].getOSCType() == OSCValueType.OSCArray)
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
        
        //returns new current index
        private int writeToByteArrayAtIndex(byte[] target, byte[] source, int startIndex)
        {
            //dma copy
            Buffer.BlockCopy(source, 0, target, startIndex, source.Length);
            return startIndex+source.Length;
        }

        public RawOSCPackage convertOSCMessageToByteArray(OSCMessage message,RawOSCPackage target)
        {
            this.GenerateAndWriteOSCString(message.getAddress(), target);
            generateOSCValueTypeString(message.getValues(),target);
            generateOSCValueBytes(message.getValues(),target);
            return target;
        }

        
        public RawOSCPackage convertOSCBundleToByteArray(OSCBundle bundle,RawOSCPackage target)
        {
            
            

            Stack<OSCBundle> subBundleStack = new Stack<OSCBundle>();
            
            Stack<int> indexStack = new Stack<int>();


            OSCBundle currentBundle = bundle;
            OSCBundleElement[] currentContents = currentBundle.getRawElements();
            int currentIndex = 0;

            
            HashSet<OSCBundle> seenBundles = new HashSet<OSCBundle>();
            
            //write bundle ident
            target.writeData(this.bundleIdent);
            //writeTimeTag
            BinaryPrimitives.WriteInt64BigEndian(this.writeCache8,bundle.getTimeTag());
            target.writeData(this.writeCache8);

            bool solving = true;
            while (solving)
            {

                while (currentIndex < currentContents.Length)
                {
                    if (currentContents[currentIndex].getRawContents().getPackageType() == OSCPackageType.OSCBundle)
                    {
                        if (seenBundles.Contains((OSCBundle)currentContents[currentIndex].getRawContents()))
                        {
                            throw new RecursiveBundleException();

                        }
                        seenBundles.Add((OSCBundle)currentContents[currentIndex].getRawContents());

                        subBundleStack.Push(currentBundle);
                        indexStack.Push(currentIndex + 1);


                        currentBundle = ((OSCBundle)currentContents[currentIndex].getRawContents());
                        currentIndex = 0;
                        currentContents = currentBundle.getRawElements();
                        BinaryPrimitives.WriteInt32BigEndian(this.writeCache4, currentBundle.getSize());
                        target.writeData(this.writeCache4);
                        target.writeData(this.bundleIdent);
                        BinaryPrimitives.WriteInt64BigEndian(this.writeCache8,currentBundle.getTimeTag());
                        target.writeData(this.writeCache8);

                    }
                    else
                    {
                        OSCBundleElement elementCache = currentContents[currentIndex];
                        BinaryPrimitives.WriteInt32BigEndian(this.writeCache4, elementCache.getDataSize());
                        target.writeData(this.writeCache4);
                        this.convertOSCMessageToByteArray((OSCMessage)elementCache.getRawContents(),target);
                        currentIndex++;

                    }

                }
                if (subBundleStack.Count > 0)
                {
                    currentBundle = subBundleStack.Pop();
                    currentContents = currentBundle.getRawElements();
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
