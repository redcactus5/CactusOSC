

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
        public startEndPair(int start, int end)
        {
            this.start = start;
            this.end = end;
            this.length = start - end;
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
        private oscListNodeSize calculateOSCListNodeSize(Span<char> typeString)
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
            }
            return new oscListNodeSize(subLists, OSCValues);
        }

        private startEndPair findFirstSublist(Span<char> typeStringSegment)
        {
            int mode = 0;
            int depth = 0;
            int start = 0; 
            int end=0;
            for(int character = 0;character < typeStringSegment.Length;character++ )
            {
                switch (mode)
                {
                    case 0:
                        if (typeStringSegment[character] == '[')
                        {
                            mode = 1;
                            start=character;
                        }
                        break;
                    case 1:
                        if(depth > 0)
                        {
                            if (typeStringSegment[character] == '[')
                            {
                                depth++;
                            }else if (typeStringSegment[character] == ']')
                            {
                                depth--;
                            }

                        }else if (depth == 0)
                        {
                            if (typeStringSegment[character] == '[')
                            {
                                depth++;
                            }else if(typeStringSegment[character] == ']')
                            {
                                end = character;
                            }
                        }
                        break;
                    default:
                        throw new IndexOutOfRangeException();
                            
                }
            }
            return new startEndPair(start, end);
        }

        public static bool IsValidUtf8(Span<byte> bytes)
        {
            try
            {
                UTF8Encoding utf8 = new UTF8Encoding(false, true); 
                utf8.GetString(bytes);
                return true;
            }
            catch (DecoderFallbackException)
            {
                return false;
            }
        }


        private bool validateOSCStringData(Span<byte> stringData)
        {
            int end = 0;
            int mode = 0;
            int padding = 0;
            bool shouldBreak = false;
            for(int index=0; index<stringData.Length;index++ )
            {
                switch (mode)
                {
                    case 0:
                        if (stringData[index] == 0)
                        {
                            end = index;
                            mode = 1;
                        }
                        break;
                    case 1:
                        if(stringData[index] == 0)
                        {
                            padding++;
                        }
                        else
                        {
                            shouldBreak = true;
                        }
                        break;

                    
                }
                if (shouldBreak)
                {
                    break;
                }
                
            }
            if (((end + padding) % 4) == 0)
            {
                return (IsValidUtf8(stringData.Slice(0, end)));
                
            }
            return false;
        }
        private string extractOSCString(Span<byte> stringData)
        {
            int end = 0;
            for(int index=0;index<stringData.Length;index++)
            {

            }

        }


        private oscListNode extractOSCMessageArgumentData(string typeString, byte[] data)
        {

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
