

using System.Buffers.Binary;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.Design;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace CactusOSC
{

    
    internal class OSCPackageInterpreter
    {
        private bool[] possibleOSCTypes; 
        private byte[] bundleTag;
        private UTF8Encoding utf8;
        private const int timeStampSize = 0;
        public OSCPackageInterpreter()
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
            this.utf8 = new UTF8Encoding(false, true);
        }

       
        private bool isTypeStringCharValid(char character)
        {
            return this.possibleOSCTypes[(byte)character];
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
                throw new InvalidOSCStringException();
            }
            //fence post bug fixed by the +1
            
            

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
            try
            {
                return new OSCStringConversionReturn(this.utf8.GetString(stringData.Slice(0, lengths[0])), lengths[0] + lengths[1]);
            }
            catch (DecoderFallbackException){
                throw new InvalidOSCStringException();
            }
            

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
        private OSCvalueConversionReturn getOSCValueFromBytes(char typeChar, ReadOnlySpan<byte> data, int startIndex)
        {
            OSCStringConversionReturn temp;
            switch (typeChar) {
                case 's':
                    temp = this.extractOSCString(data.Slice(startIndex));
                    return new OSCvalueConversionReturn(temp.bytesRead, new OSCString(temp.value));
                case 'i':
                    if ((startIndex + 4) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    return new OSCvalueConversionReturn(4, new OSCInt(BinaryPrimitives.ReadInt32BigEndian(data.Slice(startIndex, 4))));
                case 'f':
                    if ((startIndex + 4) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    return new OSCvalueConversionReturn(4, new OSCFloat(BinaryPrimitives.ReadSingleBigEndian(data.Slice(startIndex, 4))));
                case 'b':
                    if ((startIndex + 4) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    int length = BinaryPrimitives.ReadInt32BigEndian(data.Slice(startIndex, 4));
                    if (length < 0)
                    {
                        throw new InvalidOSCDataException ();
                    }
                    if ((startIndex + 4 + length) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    return new OSCvalueConversionReturn(length + 4, new OSCBlob(data.Slice(startIndex+4, length).ToArray()));
                case 'h':
                    if ((startIndex + 8) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    return new OSCvalueConversionReturn(8, new OSCLong(BinaryPrimitives.ReadInt64BigEndian(data.Slice(startIndex, 8))));
                case 't':
                    if ((startIndex + 8) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    return new OSCvalueConversionReturn(8, new OSCTimeTag(BinaryPrimitives.ReadInt64BigEndian(data.Slice(startIndex, 8))));
                case 'd':
                    if ((startIndex + 8) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    return new OSCvalueConversionReturn(8, new OSCDouble(BinaryPrimitives.ReadDoubleBigEndian(data.Slice(startIndex, 8))));
                case 'S':
                    temp = this.extractOSCString(data.Slice(startIndex));
                    return new OSCvalueConversionReturn(temp.bytesRead, new OSCNonstandardString(temp.value));
                case 'c':
                    if ((startIndex + 4) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    if ((data[startIndex+1] == 0) && (data[startIndex + 2] == 0) && (data[startIndex + 3] == 0))
                    {
                        return new OSCvalueConversionReturn(4, new OSCChar((char)data[startIndex]));
                    }
                    else
                    {
                        throw new InvalidPackageException();
                    }
                case 'r':
                    if ((startIndex + 4) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    //because of how i encoded colors and midi, they are natively big endian even on little endian systems, and as such must be treated differently depending on system
                    if (BitConverter.IsLittleEndian)
                    {
                        return new OSCvalueConversionReturn(4, new OSCColor(BinaryPrimitives.ReadInt32LittleEndian(data.Slice(startIndex, 4))));
                    }
                    else
                    {
                        return new OSCvalueConversionReturn(4, new OSCColor(BinaryPrimitives.ReadInt32BigEndian((data.Slice(startIndex, 4)))));
                    }
                case 'm':
                    if ((startIndex + 4) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    //because of how i encoded colors and midi, they are natively big endian even on little endian systems, and as such must be treated differently depending on system
                    if (BitConverter.IsLittleEndian)
                    {
                        return new OSCvalueConversionReturn(4, new OSCMIDI(BinaryPrimitives.ReadInt32LittleEndian((data.Slice(startIndex, 4)))));
                    }
                    else
                    {
                        return new OSCvalueConversionReturn(4, new OSCMIDI(BinaryPrimitives.ReadInt32BigEndian(data.Slice(startIndex, 4))));
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
                    throw new InvalidTypestringException();
            }
        }

        
        

        class listTreeBuilderNode {
            
            
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
            int[] lastNodeIndex = new int[listCount];
            int lastNodeIndexPointer = 0;
            int[] indexInParent = new int[listCount];
            int indexInParentPointer = 0;
            int maxDepth = 0;
            int depth = 0;
            
            nodes[0] = new listTreeBuilderNode(-1, 0, 0,-1);
            int TopOfNodeList = 1;
            int currentIndex = 0;
            int currentParentIndex = 0;
            
            
            


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
                    indexInParent[indexInParentPointer]=(currentParentIndex+1);
                    indexInParentPointer++;
                    currentParentIndex = 0;
                    nodes[currentIndex].isLeaf = false;
                    leafcount--;
                    
                    lastNodeIndex[lastNodeIndexPointer]=currentIndex;
                    lastNodeIndexPointer++;

                    nodes[currentIndex].childLists = nodes[currentIndex].childLists + 1;
                    currentIndex = TopOfNodeList;
                    TopOfNodeList++;
                }
                else if (typeString[i] == ']')
                {
                    depth--;
                    nodes[currentIndex].childrenIndexes = new int[nodes[currentIndex].childLists];
                    currentIndex = lastNodeIndex[lastNodeIndexPointer];
                    lastNodeIndexPointer--;
                    currentParentIndex=indexInParent[indexInParentPointer];
                    indexInParentPointer--;

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
                    listTreeBuilderNode cache0 = nodes[list];
                    listTreeBuilderNode cache1 = nodes[cache0.parentIndex];
                    cache1.childrenIndexes[cache1.childrenIndexesIndex] = list;
                    cache1.childrenIndexesIndex = cache1.childrenIndexesIndex + 1;
                }
            }

            

            return new findListStructureReturn(nodes,maxDepth);
        }
        
        private OSCValue[] buildOSCMessageValuesList(ReadOnlySpan<char> typestring,ReadOnlySpan<byte> argumentData)
        {
            this.validateTypeString(typestring);
            
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
            //start at one to avoid the comma
            for (int character = 1; character < typestring.Length; character++)
            {
                if (depth > 0)
                {
                    if (this.isTypeStringCharValid(typestring[character]))
                    {

                        dataReturn = this.getOSCValueFromBytes(typestring[character], argumentData,byteIndex);
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
                            dataReturn = this.getOSCValueFromBytes(typestring[character], argumentData,byteIndex);
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
                            throw new InvalidTypestringException();
                        }

                    }
                }
            }
            return baseList;

        }



        private void validateTypeString(ReadOnlySpan<char> rawTypeString)
        {
            bool valid = true;
            Stack<bool> levelClosed = new Stack<bool>();
            int depth = 0;
            bool closed = true;
            int openCount = 0;
            int closeCount = 0;
            int dataLength = 0;
            if (rawTypeString[0] != ',')
            {
                throw new InvalidTypestringException();
            }
            for (int i = 1; i < rawTypeString.Length; i++)
            {
                if (rawTypeString[i] == '[')
                {
                    closed = false;
                    depth++;
                    levelClosed.Push(closed);
                    openCount++;
                    closed = true;
                }
                else if (rawTypeString[i] == ']')
                {
                    closed = true;
                    if (depth > 1)
                    {
                        throw new InvalidTypestringException();
                    }
                    depth--;
                    closed = levelClosed.Pop();
                    closeCount++;
                }
                else if (!this.isTypeStringCharValid(rawTypeString[i]))
                {
                    throw new InvalidTypestringException();
                }
            }
            if ((depth > 0) || (openCount != closeCount) || (!closed)) {
                throw new InvalidTypestringException();
            }
        }
        

        private OSCMessage convertOSCByteArrayToMessage(ReadOnlySpan<byte> rawData)
        {
            int byteIndex = 0;
            string address = "";
            string typeString = "";
            OSCValue[] arguments= Array.Empty<OSCValue>();
            OSCStringConversionReturn stringReturn = this.extractOSCString(rawData);
            byteIndex += stringReturn.bytesRead;
            address = stringReturn.value;
            if (address[0] != '/')
            {
                throw new InvalidOSCAddressException();
            }
            stringReturn = this.extractOSCString(rawData.Slice(byteIndex));
            byteIndex+= stringReturn.bytesRead;
            typeString = stringReturn.value;
            arguments = this.buildOSCMessageValuesList(typeString, rawData.Slice(byteIndex));

            return new OSCMessage(address, arguments);
        }




        private int findBundleCountAndCoarseValidate(ReadOnlySpan<byte> rawBundle)
        {
            int readHead = 0;
            int mode = 1;//used to be zero but i removed a case so now its 1
            int elementSize = 0;
            int bundleCount = 0;
            bool shouldBreak=false;
            OSCStringConversionReturn stringReturn;
            while (readHead < rawBundle.Length) {
                switch (mode)
                {
                   
                    case 1:
                        //look for a bundle start and if found skip to the contents
                        if (readHead >= rawBundle.Length)
                        {
                            throw new InvalidBundleException();
                        }
                        stringReturn = this.extractOSCString(rawBundle.Slice(readHead));
                        if (stringReturn.value.Length <= 0)
                        {
                            throw new InvalidBundleException();
                        }
                        if (stringReturn.value == "#bundle")
                        {
                            readHead += stringReturn.bytesRead + 8;
                            bundleCount++;
                            mode = 2;
                        }
                        break;
                    case 2:
                        if (readHead + 4 >= rawBundle.Length)
                        {
                            shouldBreak = true;
                            break;
                        }
                        elementSize = BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead, 4));
                        if (elementSize < 0)
                        {
                            throw new InvalidBundleException();
                        }
                            
                        readHead += 4;
                        
                        stringReturn = this.extractOSCString(rawBundle.Slice(readHead));
                        if (stringReturn.value.Length <= 0)
                        {
                            throw new InvalidBundleException();
                        }
                        if (stringReturn.value == "#bundle")
                        {
                            bundleCount++;
                            readHead += 8;
                        }
                        else if (stringReturn.value[0]=='/')
                        {
                            if(readHead+elementSize > rawBundle.Length)
                            {
                                throw new InvalidBundleException();
                            }
                            readHead += elementSize;
                        }
                        else
                        {
                            throw new InvalidBundleException();
                        }
                        break;
                }
                if (shouldBreak)
                {
                    break;
                }      

            }
            return bundleCount; 

        }

        

        class BundleTreeBuilderNode
        {
            public int childBundles;
            public int messages;
            public int parentIndex;
            public int index;

            public int[] childrenIndexes;
            public int childrenIndexesIndex;
            public long timeStamp;

            public BundleTreeBuilderNode(int parentIndex,  int childBundles, int messages, int index, long timeStamp)
            {

                this.parentIndex = parentIndex;
                this.messages = messages;
                this.childBundles = childBundles;
                
                this.index = index;
                this.childrenIndexes = Array.Empty<int>();
                this.childrenIndexesIndex = 0;
                this.timeStamp = timeStamp;
                
            }
        }

        private BundleTreeBuilderNode[] findBundleStructure(ReadOnlySpan<byte> rawBundle)
        {
            int readHead = 0;
            int mode = 1;
            int bundleCount = this.findBundleCountAndCoarseValidate(rawBundle);
            long timeStamp = 0;

            BundleTreeBuilderNode[] nodes = new BundleTreeBuilderNode[bundleCount];
            

            int currentBundle = 0;
            int newestBundle = 1;
            int currentItemSize = 0;

            int[] bundleStack= new int[bundleCount];
            int[] bundleEnd= new int[bundleCount];
            
            int bundleStackIndex = 0;

            OSCStringConversionReturn stringReturn;

            while (readHead < rawBundle.Length)
            {
                if ((nodes[currentBundle] != null) && (bundleEnd[bundleStackIndex]==readHead))
                {
                    nodes[currentBundle].childrenIndexes = new int[nodes[currentBundle].childBundles];
                    currentBundle = bundleStack[bundleStackIndex];
                    bundleStackIndex--;
                }
                else
                {
                    switch (mode)
                    {
                        
                        case 1:
                            stringReturn = this.extractOSCString(rawBundle.Slice(readHead));
                            if (stringReturn.value == "#bundle")
                            {
                                readHead += stringReturn.bytesRead;
                                timeStamp = BinaryPrimitives.ReadInt64BigEndian(rawBundle.Slice(readHead, 8));
                                readHead += 8;


                                //create the base node
                                nodes[0] = new BundleTreeBuilderNode(-1, 0, 0, 0, timeStamp);

                                mode = 2;
                            }
                            else
                            {
                                throw new InvalidBundleException();
                            }
                            break;
                        case 2:
                            if (readHead + 4 >= rawBundle.Length)
                            {
                                throw new InvalidBundleException();
                            }
                            currentItemSize = BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead, 4));
                            readHead += 4;
                            if (readHead + currentItemSize >= rawBundle.Length)
                            {
                                throw new InvalidBundleException();
                            }
                            stringReturn = this.extractOSCString(rawBundle.Slice(readHead));
                            if (stringReturn.value[0] == '/')
                            {
                                nodes[currentBundle].messages += 1;
                                readHead += currentItemSize;
                            }
                            else if (stringReturn.value == "#bundle")
                            {

                                timeStamp = BinaryPrimitives.ReadInt64BigEndian(rawBundle.Slice(readHead, 8));

                                nodes[currentBundle].childBundles +=  1;
                                bundleStack[bundleStackIndex] = currentBundle;
                                bundleEnd[bundleStackIndex] = readHead + currentItemSize;
                                bundleStackIndex++;
                                nodes[newestBundle] = new BundleTreeBuilderNode(currentBundle, 0, 0, newestBundle, timeStamp);
                                readHead += currentItemSize + 8;

                                currentBundle = newestBundle;
                                newestBundle++;
                            }
                            else
                            {
                                throw new InvalidBundleException();
                            }

                            break;

                    }
                }
            }
            for (int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].parentIndex != -1)
                {
                    BundleTreeBuilderNode nodeCache0 = nodes[i];
                    BundleTreeBuilderNode nodeCache1 = nodes[nodeCache0.parentIndex];

                    nodeCache1.childrenIndexes[nodeCache0.childrenIndexesIndex] = i;
                    nodeCache1.childrenIndexesIndex+= 1;
                }
                
            }
            return nodes;

        }






        private OSCBundle ConvertOSCByteArrayToBundle(ReadOnlySpan<byte> rawBundle)
        {
            BundleTreeBuilderNode[] treeStructure=this.findBundleStructure(rawBundle);
            


            BundleTreeBuilderNode currentNode = treeStructure[0]; 
            int currentNodeChildIndex = 0;
            int[] currentNodeChildIndexStack=new int[treeStructure.Length];
            int currentNodeChildIndexStackPointer = 0;
            
            OSCBundleElement[] currentNodeData=new OSCBundleElement[currentNode.messages+currentNode.childBundles];
            int currentNodeDataIndex = 0;
            int[] currentNodeDataIndexStack=new int[treeStructure.Length];
            int currentNodeDataIndexStackPointer = 0;
            int currentNodeDataStackPointer = 0;
            OSCBundleElement[][] currentNodeDataStack = new OSCBundleElement[treeStructure.Length][];
            
            int readHead = 0;
            int currentSize = 0;
            int mode = 1;
            OSCBundle tempBundle=null;
            
            OSCStringConversionReturn stringReturn;

            

            
            while (readHead < rawBundle.Length)
            {
                if (currentNodeDataIndex == currentNodeData.Length)
                {
                    tempBundle = new OSCBundle(currentNodeData, currentNode.timeStamp);
                    if (currentNode.parentIndex == -1)
                    {
                        break;
                    }
                    currentNode = treeStructure[currentNode.parentIndex];

                    currentNodeDataStackPointer--;
                     currentNodeData=currentNodeDataStack[currentNodeDataStackPointer];


                    currentNodeDataIndexStackPointer--;
                    currentNodeDataIndex=currentNodeDataIndexStack[currentNodeDataIndexStackPointer];
                    
                    currentNodeData[currentNodeDataIndex] = new OSCBundleElement(tempBundle);
                    currentNodeDataIndex++;

                    currentNodeChildIndexStackPointer--;
                    currentNodeChildIndex =currentNodeChildIndexStack[currentNodeChildIndexStackPointer];
                    
                    
                    

                }
                else
                {
                    switch (mode)
                    {
                       
                        case 1:
                            stringReturn = this.extractOSCString(rawBundle.Slice(readHead));
                            readHead += 8 + stringReturn.bytesRead;
                            mode = 2;
                            break;
                        case 2:
                            currentSize = BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead, 4));
                            readHead += 4;
                            stringReturn = this.extractOSCString(rawBundle.Slice(readHead));
                            if (stringReturn.value[0] == '/')
                            {
                                currentNodeData[currentNodeDataIndex] = new OSCBundleElement(this.convertOSCByteArrayToMessage(rawBundle.Slice(readHead, currentSize)));
                                currentNodeDataIndex++;
                                readHead += currentSize;
                            }
                            else
                            {
                                readHead += stringReturn.bytesRead+8;
                                
                                currentNodeDataStack[currentNodeDataStackPointer] = currentNodeData;
                                currentNodeDataStackPointer++;

                                currentNodeDataIndexStack[currentNodeDataIndexStackPointer] = currentNodeDataIndex;
                                currentNodeDataIndexStackPointer++;

                                currentNode = treeStructure[currentNode.childrenIndexes[currentNodeChildIndex]];
                                currentNodeChildIndex++;
                                currentNodeChildIndexStack[currentNodeChildIndexStackPointer] = currentNodeChildIndex;
                                currentNodeChildIndexStackPointer++;

                                currentNodeData = new OSCBundleElement[currentNode.messages+currentNode.childBundles];

                                
                                
                            }
                            break;
                    }
                }

                
                  
            }

            return new OSCBundle(currentNodeData, currentNode.timeStamp);

            
        }






        public OSCPackage convertOSCByteArrayToPackage(ReadOnlySpan<byte> rawPackage)
        {
            int readHead = 0;
            while (readHead < rawPackage.Length)
            {
                if (rawPackage[readHead] == '\0'){
                    readHead++;
                }
                else {
                    break;
                }
            }
            OSCStringConversionReturn stringReturn=this.extractOSCString(rawPackage.Slice(readHead));
            if (stringReturn.value.Length <= 0)
            {
                throw new InvalidPackageException();
            }
            if (stringReturn.value[0] == '/')
            {
                return this.convertOSCByteArrayToMessage(rawPackage.Slice(readHead));
            }else if (stringReturn.value == "#bundle")
            {
                return this.ConvertOSCByteArrayToBundle(rawPackage.Slice(readHead));
            }
            else
            {
                throw new InvalidPackageException();
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
