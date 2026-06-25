

using System.Buffers.Binary;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;

namespace CactusOSC
{

    
    internal class OSCPackageParser
    {
        private bool[] possibleOSCTypes; 
        private byte[] bundleTag;

        public OSCPackageParser()
        {
            this.bundleTag=this.generateOSCString("#bundle");
            //generate a fast lookup table for typestring valudation
            this.possibleOSCTypes = new bool[256];
            for (int i = 0; i < possibleOSCTypes.Length; i++) {
                possibleOSCTypes[i] = false;
            }
            this.possibleOSCTypes[(byte)'s'] = true;
            this.possibleOSCTypes[(byte)'i'] = true;
            this.possibleOSCTypes[(byte)'f'] = true;
            this.possibleOSCTypes[(byte)'b'] = true;
            this.possibleOSCTypes[(byte)'h'] = true;
            this.possibleOSCTypes[(byte)'t'] = true;
            this.possibleOSCTypes[(byte)'d'] = true;
            this.possibleOSCTypes[(byte)'S'] = true;
            this.possibleOSCTypes[(byte)'c'] = true;
            this.possibleOSCTypes[(byte)'r'] = true;
            this.possibleOSCTypes[(byte)'m'] = true;
            this.possibleOSCTypes[(byte)'T'] = true;
            this.possibleOSCTypes[(byte)'F'] = true;
            this.possibleOSCTypes[(byte)'N'] = true;
            this.possibleOSCTypes[(byte)'I'] = true;

        }

       
        private bool isTypeStringCharValid(char character)
        {
            return this.possibleOSCTypes[(byte)character];
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

        
        

        struct listTreeBuilderNode {
            
            
            public int childLists;
            public int values;
            public int parentIndex;
            public int index;
            public bool isLeaf;
            public int[] childrenIndexes;
            public int childrenIndexesIndex;
            public listTreeBuilderNode( int parentIndex,int childLists,int values,int index)
            {
                
                this.parentIndex = parentIndex;
                this.values = values;
                this.childLists = childLists;
                this.isLeaf = true;
                this.index = index;
                this.childrenIndexesIndex = 0;
            }
        }

        private struct findListStructureReturn
        {
            public listTreeBuilderNode[] nodes;
            public int maxDepth;
            public findListStructureReturn(listTreeBuilderNode[] nodes, int maxDepth)
            {
                this.maxDepth = maxDepth;
                this.nodes = nodes;
            }
        }

        private findListStructureReturn findListStructure(ReadOnlySpan<char> typeString)
        {

            int listCount = 0;
            for (int i = 0; i < typeString.Length; i++)
            {
                if (typeString[i] == '[')
                {
                    listCount++;
                }
            }

            listTreeBuilderNode[] nodes = new listTreeBuilderNode[listCount+1];
            Stack<int> lastNodeIndex = new Stack<int>();
            Stack<int> indexInParent = new Stack<int>();
            int maxDepth = 0;
            int depth = 0;
            
            nodes[0] = new listTreeBuilderNode(-1, 0, 0,-1);
            int TopOfNodeList = 1;
            int currentIndex = 0;
            int currentParentIndex = 0;
            
            
            listTreeBuilderNode currentNode;


            int leafcount = listCount;
            for (int i = 0; i<typeString.Length; i++)
            {
                if (typeString[i] == '[')
                {
                    depth++;
                    if(depth > maxDepth)
                    {
                        maxDepth = depth;
                    }
                    nodes[TopOfNodeList]=new listTreeBuilderNode(currentIndex,0,0,currentParentIndex);
                    indexInParent.Push(currentParentIndex+1);
                    currentParentIndex = 0;
                    nodes[currentIndex].isLeaf = false;
                    leafcount--;
                    
                    lastNodeIndex.Push(currentIndex);

                    nodes[currentIndex].childLists = nodes[currentIndex].childLists + 1;
                    currentIndex = TopOfNodeList;
                    TopOfNodeList++;
                }
                else if (typeString[i] == ']')
                {
                    depth--;
                    nodes[currentIndex].childrenIndexes = new int[nodes[currentIndex].childLists];
                    currentIndex = lastNodeIndex.Pop();
                    currentParentIndex=indexInParent.Pop();

                }
                else
                {
                    nodes[currentIndex].values= nodes[currentIndex].values + 1;
                    currentParentIndex++;
                }
            }
            
            for(int list=0; list<nodes.Length; list++)
            {
                if (nodes[list].parentIndex!=-1)
                {
                    nodes[nodes[list].parentIndex].childrenIndexes[nodes[nodes[list].parentIndex].childrenIndexesIndex] = list;
                    nodes[nodes[list].parentIndex].childrenIndexesIndex = nodes[nodes[list].parentIndex].childrenIndexesIndex + 1;
                }
            }

            

            return new findListStructureReturn(nodes,maxDepth);
        }
        
        private OSCValue[] buildOSCMessageValuesList(ReadOnlySpan<char> typestring,Span<byte> argumentData)
        {
            findListStructureReturn structureData= this.findListStructure(typestring);
            int stackHeightMax = structureData.maxDepth;
            listTreeBuilderNode[] listStructure = structureData.nodes;

            OSCValue[][] unfinishedArrays = new OSCValue[stackHeightMax][];
            int unfinishedArraysPointer = 0;
            int[] unfinishedArraysIndex= new int[stackHeightMax];
            int unfinishedArraysIndexPointer = 0;
            listTreeBuilderNode[] framingStack = new listTreeBuilderNode[stackHeightMax];
            int framingStackPointer = 0;
            int[] framingStackIndexes = new int[stackHeightMax];
            int framingStackIndexesPointer = 0;
            listTreeBuilderNode currentNode = listStructure[0];
            OSCValue[] baseList= new OSCValue[currentNode.childLists+currentNode.values];
            OSCValue[] currentList=Array.Empty<OSCValue>();
            int baseListIndex = 0;
            int currentArrayIndex = 0;
            int depth = 0;
            int byteIndex = 0;
            int currentFramingStackIndex = 0;
            OSCvalueConversionReturn dataReturn;
            for (int character = 0; character < typestring.Length; character++)
            {
                if (depth > 0)
                {
                    if (this.isTypeStringCharValid(typestring[character]))
                    {

                        dataReturn = this.getOSCValueFromBytes(typestring[character], argumentData.Slice(byteIndex, argumentData.Length));
                        byteIndex += dataReturn.bytesConsumed;
                        currentList[currentArrayIndex] = dataReturn.returnValue;
                        currentArrayIndex++;
                    }
                    else if (typestring[character] == '[')
                    {
                        depth++;
                        framingStack[framingStackPointer]=currentNode;
                        framingStackPointer++;
                        currentNode = listStructure[currentNode.childrenIndexes[currentFramingStackIndex]];
                        currentFramingStackIndex++;
                        framingStackIndexes[framingStackIndexesPointer]=currentFramingStackIndex;
                        framingStackIndexesPointer++;
                        currentFramingStackIndex = 0;
                        unfinishedArrays[unfinishedArraysPointer]=currentList;
                        unfinishedArraysPointer++;
                        currentList = new OSCValue[currentNode.childLists + currentNode.values];
                        unfinishedArraysIndex[unfinishedArraysIndexPointer] = currentArrayIndex;
                        unfinishedArraysIndexPointer++;
                        currentArrayIndex = 0;
                    }
                    else if (typestring[character] == ']')
                    {
                        depth--;
                        currentNode = framingStack[framingStackPointer];
                        framingStackPointer--;
                        currentFramingStackIndex = framingStackIndexes[framingStackIndexesPointer];
                        framingStackIndexesPointer--;
                        OSCArray tempVlaue = new OSCArray(currentList);
                        currentList = unfinishedArrays[unfinishedArraysPointer];
                        unfinishedArraysIndexPointer--;
                        currentArrayIndex = unfinishedArraysIndex[unfinishedArraysIndexPointer];
                        unfinishedArraysIndexPointer--;
                        if (depth > 0)
                        {
                            currentList[currentArrayIndex] = tempVlaue;
                            currentArrayIndex++;
                        }
                        else
                        {
                            baseList[baseListIndex] = tempVlaue;
                            baseListIndex++;
                        }

                    }
                    else
                    {
                        if (this.isTypeStringCharValid(typestring[character]))
                        {
                            dataReturn = this.getOSCValueFromBytes(typestring[character], argumentData.Slice(byteIndex, argumentData.Length));
                            byteIndex += dataReturn.bytesConsumed;
                            baseList[baseListIndex] = dataReturn.returnValue;
                            baseListIndex++;
                        }
                        else if (typestring[character] == '[')
                        {
                            depth++;
                            framingStack[framingStackPointer]=currentNode;
                            framingStackPointer++;
                            currentNode = listStructure[currentNode.childrenIndexes[currentFramingStackIndex]];
                            currentFramingStackIndex++;
                            framingStackIndexes[framingStackIndexesPointer]=currentFramingStackIndex;
                            framingStackIndexesPointer++;
                            currentFramingStackIndex = 0;
                            currentList = new OSCValue[currentNode.childLists + currentNode.values];

                        }
                        else
                        {
                            throw new invalidTypestringException();
                        }

                    }
                }
            }
            return baseList;

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
