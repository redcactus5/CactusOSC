

using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace CactusOSC
{

    
    internal class readerRawOSCPackage
    {
        private byte[] data;
        private int readHead;

        public readerRawOSCPackage(byte[] data){
            this.readHead = 0;
            this.data = data;
            
        }
        public int getLength()
        {
            return data.Length;
        }
        public int getreadHead()
        {
            return this.readHead;
        }
        public void setReadHead(int readHead)
        {
            this.readHead = readHead;
        }

        public void updateReadHead(int offset)
        {
            this.readHead += offset;
        }
        public byte[] getRawData()
        {
            return this.data;
        }

        public void addData(byte[] newData)
        {
            byte[] temp = new byte[newData.Length+this.data.Length];
            Buffer.BlockCopy(this.data, 0, temp, 0, this.data.Length);
            Buffer.BlockCopy(newData, 0, temp, this.data.Length, newData.Length);
            this.data = temp;
        }

        public byte[] getData()
        {
            return this.data;
        }

        public ReadOnlySpan<byte> getSection(int start, int length)
        {
            return new ReadOnlySpan<byte>(this.data, start, length);
        }

        public ReadOnlySpan<byte> read(int length)
        {

           ReadOnlySpan<byte> temp= new ReadOnlySpan<byte>(this.data,this.readHead, length);
            this.readHead += length;
            return temp;

        }

        public ReadOnlySpan<byte> getRemaining()
        {
            return new ReadOnlySpan<byte>(this.data, this.readHead, this.data.Length);
        }
    }
    

    internal struct startEndPair
    {
        public int start;
        public int end;
        public int length;
        public bool empty;
        public startEndPair(int start, int end)
        {
            this.start = start;
            this.end = end;
            this.length =  end-start;
            if ((start == -1) && (end == -1){
                empty = true;
            }
            else {
                empty = false;
            }
        } 
    }
    internal class OSCPackageParser
    {
        private HashSet<char> possibleOSCTypes; 
        private byte[] bundleTag;

        public OSCPackageParser()
        {
            this.bundleTag=this.generateOSCString("#bundle");
            this.possibleOSCTypes = new HashSet<char> { 's', 'i', 'f', 'b', 'h', 't', 'd', 'S', 'c', 'r', 'm', 'T', 'F', 'N', 'I' };

        }

       
        private struct oscListIndexIdentifyer
        {
            public NodeEntryType type;
            public int index;
            public oscListIndexIdentifyer(int index, NodeEntryType type){
                this.type=type;
                this.index =index;

            }
        }

        private enum NodeEntryType
        {
            oscValue,
            subList
        }
        private class oscListNode
        {
            private oscListIndexIdentifyer[] types;
            private oscListNode[] subLists;
            private OSCValue[] oscValues;
            private int typesIndex;
            private int subListIndex;
            private int oscValuesIndex;

            public oscListNode(int sublists,int oscValues)
            {
                this.types=new oscListIndexIdentifyer[sublists+oscValues];
                this.subLists = new oscListNode[sublists];
                this.oscValues=new OSCValue[oscValues];
                this.typesIndex=0;
                this.oscValuesIndex=0;
                this.subListIndex = 0;

            }

            public void addOSCValue(OSCValue value)
            {
                if (typesIndex < types.Length && oscValuesIndex < this.oscValues.Length)
                {
                    this.oscValues[this.oscValuesIndex] = value;
                    this.types[this.typesIndex] = new oscListIndexIdentifyer(this.oscValuesIndex, NodeEntryType.oscValue);
                    this.oscValuesIndex++;
                    this.typesIndex++;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }

            }

            public void addSubList(int subLists,int oscValues)
            {
                if(typesIndex < types.Length && subListIndex < this.subLists.Length)
                {
                    this.subLists[this.subListIndex]=new oscListNode(subLists, oscValues);
                    this.types[this.typesIndex]=new oscListIndexIdentifyer(this.subListIndex, NodeEntryType.subList);
                    this.subListIndex++;
                    this.typesIndex++;
                }
                else
                {
                    throw new IndexOutOfRangeException();
                }
                
            }

            public NodeEntryType getType(int index)
            {
                if ((index < 0) || (index > this.types.Length - 1))
                {
                    throw new IndexOutOfRangeException();
                }
                return this.types[index].type;
            }

            public oscListNode getSubList(int index)
            {
                if ((index < 0) || (index > this.types.Length - 1))
                {
                    throw new IndexOutOfRangeException();
                }
                if (this.types[index].type != NodeEntryType.subList)
                {
                    throw new OSCListNodeReturnTypeMismatchException();
                }
                return this.subLists[this.types[index].index];
                
            }

            public OSCValue GetOSCValue(int index)
            {
                if ((index < 0) || (index > this.types.Length - 1))
                {
                    throw new IndexOutOfRangeException();
                }
                if (this.types[index].type != NodeEntryType.oscValue)
                {
                    throw new OSCListNodeReturnTypeMismatchException();
                }
                return this.oscValues[this.types[index].index];
            }
                
        }
        
        private struct oscListNodeSize
        {
            public int subLists;
            public int OSCValues;
            public oscListNodeSize(int subLists, int OSCValues)
            {
                this.subLists = subLists;
                this.OSCValues = OSCValues;
            }
        }
        private oscListNodeSize calculateOSCListNodeSize(ReadOnlySpan<char> typeString)
        {
            
            int depth = 0;
            int subLists = 0;
            int OSCValues = 0;


            for (int character = 0; character < typeString.Length; character++)
            {
                if (depth > 0)
                {
                    if (typeString[character] == '[')
                    {
                        depth++;
                    }
                    else if (typeString[character] == ']')
                    {
                        depth--;
                    }
                }
                else if (depth == 0)
                {
                    if (typeString[character] == '[')
                    {
                        depth++;
                        subLists++;
                    }
                    else if (this.possibleOSCTypes.Contains(typeString[character]))
                    {
                        OSCValues++;
                    }
                }
                else
                {
                    break;
                }
            }
            return new oscListNodeSize(subLists, OSCValues);
        }

        private startEndPair findFirstSublist(ReadOnlySpan<char> typeStringSegment)
        {
            //init our vars
            int mode = 0;
            int depth = 0;
            int start = 0; 
            int end=0;
            //loop throug the type string
            for(int character = 0;character < typeStringSegment.Length;character++ )
            {

                switch (mode)
                {
                    //search for the first start of a list
                    case 0:
                        if (typeStringSegment[character] == '[')
                        {
                            //go to list contents processing mode
                            mode = 1;
                            //mark down where the start is
                            start=character;
                        }
                        break;
                    case 1:
                        //if we are in a sublist
                        if(depth > 0)
                        {
                            //check if there is yet another sublist insside the sublist
                            if (typeStringSegment[character] == '[')
                            {
                                //mark down that we are going deeper
                                depth++;
                            //check if we are exiting a sublist
                            }else if (typeStringSegment[character] == ']')
                            {
                                //mark down that we are surfacing
                                depth--;
                            }
                        //if we arent in a sublist
                        }else if (depth == 0)
                        {
                            //check if there is a sublist inside this list
                            if (typeStringSegment[character] == '[')
                            {
                                //mark down that we are entering a sublist to ignore
                                depth++;
                            //check if we have hit the end of the list
                            }else if(typeStringSegment[character] == ']')
                            {
                                //mark down the end of the sublist
                                end = character;
                            }
                        }
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                            
                }
            }
            //if we found a sublist
            if ((start > 0) || (end > 0))
            {
                //return a start end pair for it
                return new startEndPair(start, end);
            }
            //otherwise return an empty start end pair
            return new startEndPair(-1,-1);
        }

        //self explanitory, just checks if the array of bytes is a valid utf8 string
        public static bool IsValidUtf8(ReadOnlySpan<byte> bytes)
        {
            try
            {   //set up a canary decoder
                UTF8Encoding utf8 = new UTF8Encoding(false, true); 
                //try to decode
                utf8.GetString(bytes);
                //if not erros occured its valid
                return true;
            }
            catch (DecoderFallbackException)
            {
                //if the canary threw then its invalid
                return false;
            }
        }


        private int[] validateOSCStringDataAndGetLengths(ReadOnlySpan<byte> stringData)
        {
            //init our vars
            int end = 0;
            int mode = 0;
            int padding = 0;
            int expectedPadding = 0; 
            bool shouldBreak = false;
            bool hitEnd=false;
            int paddingNeed = 0;
            //determine the length and padding of the string and if that all is valid
            for (int index=0; index<stringData.Length;index++ )
            {
                switch (mode)
                {
                    //waiting for the end
                    case 0:
                        //if we hit the end
                        if (stringData[index] == 0)
                        {
                            //save where the end is
                            end = index;
                            //switch to padding validation mode
                            mode = 1;
                            //save that we hit the end
                            hitEnd = true;
                            //determine our expected padding
                            paddingNeed= ((end + 1) % 4);
                            if (paddingNeed != 0)
                            {
                                //fence post bug fixed by the +1
                                expectedPadding= 4 - paddingNeed;
                            }
                            
                        }
                        break;
                    case 1:
                        //if we havent finished verifying our padding
                        if (padding < expectedPadding)
                        {
                            //if we see a padding byte count it, if not, break
                            if (stringData[index] == 0)
                            {
                                padding++;
                            }
                            else
                            {
                                shouldBreak = true;
                            }

                        }
                        else//if we have then break
                        {
                            shouldBreak = true;
                        }
                        break;
                        

                    
                }
                //if we have previosuly determined we should break, then break;
                if (shouldBreak)
                {
                    break;
                }
                
            }
            if (!hitEnd)
            {
                throw new OSCStringNotNullTerminatedException();
            }
            if (padding != expectedPadding)
            {
                throw new invalidOSCStringException();
            }
            //fence post bug fixed by the +1
            
            if(!IsValidUtf8(stringData.Slice(0, end))){
                throw new invalidOSCStringException();
            }

            return new int[] { end, padding };
            
            
        }

        private struct OSCStringConversionReturn
        {
            public string value;
            public int bytesRead;
            public OSCStringConversionReturn(string value, int bytesRead)
            {
                this.value = value;
                this.bytesRead= bytesRead;
            }
        }
        private OSCStringConversionReturn extractOSCString(ReadOnlySpan<byte> stringData)
        {
            //encoding is [0]==length, [1]==padding
            int[] lengths = this.validateOSCStringDataAndGetLengths(stringData);
            
            return new OSCStringConversionReturn(Encoding.UTF8.GetString(stringData.Slice(0, lengths[0])), lengths[0] + lengths[1]);

        }

        private struct OSCvalueConversionReturn
        {
            public int bytesConsumed;
            public OSCValue returnValue;
            public OSCvalueConversionReturn(int bytesConsumed, OSCValue returnValue) {
                this.bytesConsumed = bytesConsumed;
                this.returnValue = returnValue;
            }
        }
        private OSCvalueConversionReturn getOSCValueFromBytes(char typeChar, ReadOnlySpan<byte> data)
        {
            OSCStringConversionReturn temp;
            switch (typeChar) {
                case 's':
                    temp = this.extractOSCString(data);
                    return new OSCvalueConversionReturn(temp.bytesRead, new OSCString(temp.value));
                case 'i':
                    return new OSCvalueConversionReturn(4, new OSCInt(BinaryPrimitives.ReadInt32BigEndian(data.Slice(0, 4))));
                case 'f':
                    return new OSCvalueConversionReturn(4, new OSCFloat(BinaryPrimitives.ReadSingleBigEndian(data.Slice(0, 4))));
                case 'b':
                    int length = BinaryPrimitives.ReadInt32BigEndian(data.Slice(0, 4));
                    return new OSCvalueConversionReturn(length + 4, new OSCBlob(data.Slice(4, length).ToArray()));
                case 'h':
                    return new OSCvalueConversionReturn(8, new OSCLong(BinaryPrimitives.ReadInt64BigEndian(data.Slice(0, 8))));
                case 't':
                    return new OSCvalueConversionReturn(8, new OSCTimeTag(BinaryPrimitives.ReadInt64BigEndian(data.Slice(0, 8))));
                case 'd':
                    return new OSCvalueConversionReturn(8, new OSCDouble(BinaryPrimitives.ReadDoubleBigEndian(data.Slice(0, 8))));
                case 'S':
                    temp = this.extractOSCString(data);
                    return new OSCvalueConversionReturn(temp.bytesRead, new OSCNonstandardString(temp.value));
                case 'c':
                    if ((data[1] == 0) && (data[2] == 0) && (data[3] == 0))
                    {
                        return new OSCvalueConversionReturn(4, new OSCChar((char)data[0]));
                    }
                    else
                    {
                        throw new invalidPackageException();
                    }
                case 'r':
                    //because of how i encoded colors and midi, they are natively big endian even on little endian systems, and as such must be treated differently depending on system
                    if (BitConverter.IsLittleEndian)
                    {
                        return new OSCvalueConversionReturn(4, new OSCColor(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(0, 4))));
                    }
                    else
                    {
                        return new OSCvalueConversionReturn(4, new OSCColor(BinaryPrimitives.ReadInt32BigEndian((data.Slice(0, 4)))));
                    }
                case 'm':
                    //because of how i encoded colors and midi, they are natively big endian even on little endian systems, and as such must be treated differently depending on system
                    if (BitConverter.IsLittleEndian)
                    {
                        return new OSCvalueConversionReturn(4, new OSCMIDI(BinaryPrimitives.ReadInt32LittleEndian((data.Slice(0, 4)))));
                    }
                    else
                    {
                        return new OSCvalueConversionReturn(4, new OSCMIDI(BinaryPrimitives.ReadInt32BigEndian(data.Slice(0, 4))));
                    }
                case 'T':
                    return new OSCvalueConversionReturn(0, new OSCBool(true));
                case 'F':
                    return new OSCvalueConversionReturn(0, new OSCBool(false));
                case 'N':
                    return new OSCvalueConversionReturn(0, new OSCNil());
                case 'I':
                    return new OSCvalueConversionReturn(0, new OSCInfinitum());
                default:
                    throw new invalidTypestringException();
            }
        }
        private oscListNode extractOSCMessageArgumentData(string typeString, ReadOnlySpan<byte> data)
        {
            int typeStringIndex = 0;
            ReadOnlySpan<char> typeStringSpan = typeString.AsSpan();

            Stack<oscListNode> Lists = new Stack<oscListNode>();
            oscListNodeSize currentNodeSize = this.calculateOSCListNodeSize(typeStringSpan);
            oscListNode currentNode = new oscListNode(currentNodeSize.subLists,currentNodeSize.OSCValues);

            while (typeStringIndex < typeString.Length)
            {
                
            }
            
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




        private byte[] generateOSCString(string text)
        {
            int textSize = Encoding.UTF8.GetByteCount(text);
            byte[] bytes = new byte[this.calculateOSCStringSize(textSize)];
            //ensure the padding is cleared
            Array.Clear(bytes, textSize, bytes.Length - textSize);
            //fast copy
            Encoding.UTF8.GetBytes(text, 0, text.Length, bytes, 0);

            return bytes;
        }
    }
}
