

using System.Buffers.Binary;

using System.Text;


namespace CactusOSC
{

    
    internal class OSCPackageInterpreter
    {
        private bool[] possibleOSCTypes; 
        private byte[] bundleTag;
        private UTF8Encoding utf8;
        private const int timeStampSize = 8;
        private const int sizeTagSize = 4;
        private byte[] messageTag;
        
        public OSCPackageInterpreter()
        {
            this.bundleTag=Encoding.UTF8.GetBytes("#bundle");
            this.messageTag =Encoding.UTF8.GetBytes("/");

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

        private bool isBundle(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length >= this.bundleTag.Length) {
                bool match = true;
                for (int i = 0; i < this.bundleTag.Length; i++)
                {
                    if (bytes[i] != this.bundleTag[i])
                    {
                        match = false;
                        break;
                    }
                }
                return match;
            }
            
            
            return false;
            
            
        }

        private bool isMessage(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length >= this.messageTag.Length)
            {
                bool match = true;
                for (int i = 0; i < this.messageTag.Length; i++)
                {
                    if (bytes[i] != this.messageTag[i])
                    {
                        match = false;
                        break;
                    }
                }
                return match;
            }
            return false;
        }


        private struct validateOSCStringDataAndGetLengthsReturn
        {
            public int end;
            public int padding;
            public validateOSCStringDataAndGetLengthsReturn(int end, int padding)
            {
                this.end = end;
                this.padding = padding;
            }
        }
        private validateOSCStringDataAndGetLengthsReturn validateOSCStringDataAndGetLengths(ReadOnlySpan<byte> stringData)
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


            
            return new validateOSCStringDataAndGetLengthsReturn(end,padding);
            
            
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
            validateOSCStringDataAndGetLengthsReturn lengths = this.validateOSCStringDataAndGetLengths(stringData);
            try
            {
                return new OSCStringConversionReturn(this.utf8.GetString(stringData.Slice(0, lengths.end)), lengths.end + lengths.padding);
            }
            catch (DecoderFallbackException){
                throw new InvalidOSCStringException();
            }
            

        }


        //just a simple struct to carry two values in a return instead of the usual one
        private struct OSCvalueConversionReturn
        {
            public int bytesConsumed;
            public OSCValue returnValue;
            public OSCvalueConversionReturn(int bytesConsumed, OSCValue returnValue) {
                this.bytesConsumed = bytesConsumed;
                this.returnValue = returnValue;
            }
        }

        //pretty self explanitory, it takes a typestring, data span, and start index, and returns either an error if something is incorrect or the
        //data span is too small, or if everything is correct it will return to osc vaoue encoded at startindex through whatever the length of the
        //value is, and said length, with the value being of type typechar
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
                    
                    return new OSCvalueConversionReturn(length + 4,new OSCBlob(data.Slice(startIndex + 4, length).ToArray()));

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
                        return new OSCvalueConversionReturn(4,new OSCMIDI(BinaryPrimitives.ReadInt32BigEndian(data.Slice(startIndex, 4))));
                        
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

        
        
        
        //this us just used by the data segment interpreter's structure finding stage
        //to denote the structure of the data segment. think nested lists and their sizes and all that
        struct listTreeBuilderNode {
            
            
            public int childLists;
            public int values;
            public int parentIndex;
            public int index;
            public bool isLeaf;
            public int[] childrenIndexes;
            public int childrenIndexesIndex;
            public listTreeBuilderNode(int parentIndex,int childLists,int values,int index)
            {
                //the index in the holding array of this node's parent
                this.parentIndex = parentIndex;
                //the number of normal values in this array
                this.values = values;
                //the number of sublists in this array
                this.childLists = childLists;
                //whether this node is a leaf or not
                this.isLeaf = true;
                //i forget what this is for
                this.index = index;
                //the index of the children indexes array we are currently pointing to as the next open index(used for later)
                this.childrenIndexesIndex = 0;
            }
        }

        //just a simple struct to allow the return of two values instead of just one
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

        //a simple function that uses a clever algorithm to determine if the list structure is valid
        private void validateListCloses(ReadOnlySpan<byte> typeString)
        {
            //how deep we are
            int counter = 0;
            //loop through the entire string
            for (int character = 0; character < typeString.Length; character++)
            {
                //if we find an open bracket, this is the start of a new list, so incrememnt the counter
                if (typeString[character] == '[')
                {
                    counter++;
                }
                //otherwise if we find a close bracket, thiws is the end of a list so decrement the counter
                else if (typeString[character] == ']')
                {
                    counter--;
                }
                //if the counter is ever zero that means we closed a list that doesnt exist, so the typestring is automatically invalid
                if (counter < 0)
                {
                    throw new InvalidTypestringException();
                }
            }
            //if at the end we arent back where we started that means that we didnt close a list, so that means the typestring is invalid
            if (counter != 0)
            {
                throw new InvalidTypestringException();
            }
        }

        private findListStructureReturn findListStructure(ReadOnlySpan<byte> typeString)
        {
            //validate that the typestring list structure is even valid in the first place
            validateListCloses(typeString);
            //find the number of lists in the typestring so we can use a fixed size array instead of a list, to speed up executuion
            int listCount = 0;
            for (int i = 0; i < typeString.Length; i++)
            {
                if (typeString[i] == '[')
                {
                    listCount++;
                }
            }
            //this is the preallocated list where we store all the lists we find, including the base typestirng which we treat as a list here for simpler code
            listTreeBuilderNode[] nodes = new listTreeBuilderNode[listCount+1];
            //a fixed length stack we use to speed up execution and to store the index of the last node wee were on
            int[] lastNodeIndex = new int[listCount];
            int lastNodeIndexPointer = 0;
            //a fixed length stack we use to speed up execution and to store the index in our parent list that this node is in
            int[] indexInParent = new int[listCount];
            int indexInParentPointer = 0;
            //the deepest we ever go, its useful for later so we can make yet more data structures fixed size
            int maxDepth = 0;
            //our current depth, its useful so we can find the max depth
            int depth = 0;
            //create our base node, that represents the typestring itself
            nodes[0] = new listTreeBuilderNode(-1, 0, 0,-1);
            //the positon wee put any new nodes we make
            int TopOfNodeList = 1;
            //the index in the nodes list of the node we are currently on
            int currentIndex = 0;
            //i forget what this does
            int currentParentIndex = 0;

            listTreeBuilderNode currentNode;
            

            //init our leafcount  to max, so we can decrement it later as fe find nodes that arent leaves
            int leafcount = listCount;
            //loop through the entire typestring
            for (int i = 0; i<typeString.Length; i++)
            {
                //if we find the start of a list
                if (typeString[i] == '[')
                {
                    //keep track of our max depth
                    depth++;
                    if(depth > maxDepth)
                    {
                        maxDepth = depth;
                    }
                    //create a new node for this list
                    nodes[TopOfNodeList]=new listTreeBuilderNode(currentIndex,0,0,currentParentIndex);
                    //i forget why we do this
                    indexInParent[indexInParentPointer]=(currentParentIndex+1);
                    indexInParentPointer++;
                    //reset the current parent index to zero
                    currentParentIndex = 0;
                    //set that the current node is not a leaf becuase it has a sublist and update leaftcount accordingly
                    currentNode = nodes[currentIndex];
                    currentNode.isLeaf = false;
                    leafcount--;
                    //put our current node index onto the last node index stack as we are diving deeper into data structure, and need to remember where we were for when we surface
                    lastNodeIndex[lastNodeIndexPointer]=currentIndex;
                    lastNodeIndexPointer++;
                    //give the current node a child list as we just found one
                    currentNode.childLists += 1;
                    nodes[currentIndex] = currentNode;
                    //set our current node to the node we just created in order to dive to that new level
                    currentIndex = TopOfNodeList;
                    //increment the top of node index to the next open space
                    TopOfNodeList++;
                }
                //if we find the end of a list
                else if (typeString[i] == ']')
                {
                    //surface by one level
                    depth--;
                    //give the current node a child indexes lsit ofr later, as we finally have found the number of child nodes it has and as such can use a preallocated array for it, we will find these indexes later
                    currentNode = nodes[currentIndex];
                    currentNode.childrenIndexes = new int[currentNode.childLists];
                    nodes[currentIndex] = currentNode;
                    //get the index of the parent node off the stack
                    currentIndex = lastNodeIndex[lastNodeIndexPointer];
                    lastNodeIndexPointer--;
                    //and get its parent node index off the stack too
                    currentParentIndex=indexInParent[indexInParentPointer];
                    indexInParentPointer--;

                }
                //otherwise we have found a value and need to add one to the number of values the current node has
                else
                {
                    currentNode = nodes[currentIndex];
                    currentNode.values= currentNode.values + 1;
                    nodes[currentIndex] = currentNode;
                    currentParentIndex++;
                }
            }
            
            for(int list=0; list<nodes.Length; list++)
            {
                if (nodes[list].parentIndex!=-1)
                {
                    currentNode = nodes[nodes[list].parentIndex];
                    currentNode.childrenIndexes[currentNode.childrenIndexesIndex] = list;
                    currentNode.childrenIndexesIndex = currentNode.childrenIndexesIndex + 1;
                    nodes[nodes[list].parentIndex] = currentNode;
                }
            }

            
            
            return new findListStructureReturn(nodes,maxDepth);
        }
        
        private OSCValue[] buildOSCMessageValuesList(ReadOnlySpan<byte> typestring,ReadOnlySpan<byte> argumentData)
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
          
            //start at one to avoid the comma
            for (int character = 1; character < typestring.Length; character++)
            {
                if (depth > 0)
                {
                    if (this.isTypeStringCharValid((char)typestring[character]))
                    {

                        dataReturn = this.getOSCValueFromBytes((char)typestring[character], argumentData,byteIndex);
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
                        if (this.isTypeStringCharValid((char)typestring[character]))
                        {
                            dataReturn = this.getOSCValueFromBytes((char)typestring[character], argumentData,byteIndex);
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



       
        

        private OSCMessage convertOSCByteArrayToMessage(ReadOnlySpan<byte> rawData)
        {
            int byteIndex = 0;
            
            
            OSCValue[] arguments= Array.Empty<OSCValue>();
            validateOSCStringDataAndGetLengthsReturn stringReturn0;
            OSCStringConversionReturn stringReturn1 = this.extractOSCString(rawData);
            byteIndex += stringReturn1.bytesRead;
            
            if (stringReturn1.value[0]=='/')
            {
                throw new InvalidOSCAddressException();
            }
            stringReturn0 = this.validateOSCStringDataAndGetLengths(rawData.Slice(byteIndex));
            byteIndex+= stringReturn0.end+stringReturn0.padding;
            ReadOnlySpan<byte> typeString = rawData.Slice(byteIndex,stringReturn0.end);
            arguments = this.buildOSCMessageValuesList(typeString, rawData.Slice(byteIndex));

            return new OSCMessage(stringReturn1.value, arguments);
        }




        private int findBundleCountAndCoarseValidate(ReadOnlySpan<byte> rawBundle)
        {
            int readHead = 0;
            int mode = 1;//used to be zero but i removed a case so now its 1
            int elementSize = 0;
            int bundleCount = 0;
            bool shouldBreak=false;
            validateOSCStringDataAndGetLengthsReturn stringReturn;
            while (readHead < rawBundle.Length) {
                switch (mode)
                {
                   
                    case 1:
                        //look for a bundle start and if found skip to the contents
                        if (readHead > rawBundle.Length)
                        {
                            throw new InvalidBundleException();
                        }
                        stringReturn = this.validateOSCStringDataAndGetLengths(rawBundle.Slice(readHead));
                        if (stringReturn.end <= 0)
                        {
                            throw new InvalidBundleException();
                        }
                        if (isBundle(rawBundle.Slice(readHead)))
                        {
                            readHead += stringReturn.end+stringReturn.padding + 8;
                            bundleCount++;
                            mode = 2;
                        }
                        break;
                    case 2:
                        if (readHead + 4 > rawBundle.Length)
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
                        
                        stringReturn = this.validateOSCStringDataAndGetLengths(rawBundle.Slice(readHead));
                        if (stringReturn.end <= 0)
                        {
                            throw new InvalidBundleException();
                        }
                        if (isBundle(rawBundle.Slice(readHead)))
                        {
                            bundleCount++;
                            readHead += 8;
                        }
                        else if (isMessage(rawBundle.Slice(readHead)))
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

        

        struct BundleTreeBuilderNode
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
            bool starting = true;
            validateOSCStringDataAndGetLengthsReturn stringReturn;

            while ((readHead < rawBundle.Length))
            {
                if ((!starting) && ((bundleEnd[bundleStackIndex]==readHead)))
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
                            stringReturn = this.validateOSCStringDataAndGetLengths(rawBundle.Slice(readHead));
                            if (isBundle(rawBundle.Slice(readHead)))
                            {
                                readHead += stringReturn.end+stringReturn.padding;
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
                            if (readHead + 4 > rawBundle.Length)
                            {
                                throw new InvalidBundleException();
                            }
                            currentItemSize = BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead, 4));
                            readHead += 4;
                            if (readHead + currentItemSize > rawBundle.Length)
                            {
                                throw new InvalidBundleException();
                            }
                            stringReturn = this.validateOSCStringDataAndGetLengths(rawBundle.Slice(readHead));
                            if (isMessage(rawBundle.Slice(readHead)))
                            {
                                nodes[currentBundle].messages += 1;
                                readHead += currentItemSize;
                            }else
                            {
                                if (starting)
                                {
                                    starting = false;
                                }
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

                    nodeCache1.childrenIndexes[nodeCache1.childrenIndexesIndex] = i;
                    nodeCache1.childrenIndexesIndex+= 1;
                    nodes[nodeCache0.parentIndex]=nodeCache1;

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
            
            validateOSCStringDataAndGetLengthsReturn stringReturn;

            

            
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
                            stringReturn = this.validateOSCStringDataAndGetLengths(rawBundle.Slice(readHead));
                            readHead += 8 + stringReturn.padding+stringReturn.end;
                            mode = 2;
                            break;
                        case 2:
                            currentSize = BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead, 4));
                            readHead += 4;
                            stringReturn = this.validateOSCStringDataAndGetLengths(rawBundle.Slice(readHead));
                            if (isMessage(rawBundle.Slice(readHead)))
                            {
                                currentNodeData[currentNodeDataIndex] = new OSCBundleElement(this.convertOSCByteArrayToMessage(rawBundle.Slice(readHead, currentSize)));
                                currentNodeDataIndex++;
                                readHead += currentSize;
                            }
                            else
                            {
                                readHead += stringReturn.end+stringReturn.end+8;
                                
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
            validateOSCStringDataAndGetLengthsReturn stringReturn=this.validateOSCStringDataAndGetLengths(rawPackage.Slice(readHead));
            if (stringReturn.end <= 0)
            {
                throw new InvalidPackageException();
            }
            if (isMessage(rawPackage.Slice(readHead, stringReturn.end)))
            {
                return this.convertOSCByteArrayToMessage(rawPackage.Slice(readHead));
            }else if (isBundle(rawPackage.Slice(readHead,stringReturn.end)))
            {
                return this.ConvertOSCByteArrayToBundle(rawPackage.Slice(readHead));
            }
            else
            {
                throw new InvalidPackageException();
            }
        }


        
    }
}
