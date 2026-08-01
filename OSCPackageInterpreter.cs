/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
using System.Buffers.Binary;

using System.Text;


namespace CactusOSC
{

    
    internal class OSCPackageInterpreter
    {
        private bool[] possibleOSCTypes;
        private bool[] invalidMethodChars;
        private byte[] bundleTag;
        private const int bundleTagLength = 8;
        private UTF8Encoding utf8;
        private const int timeStampSize = 8;
        private const int sizeTagSize = 4;
        private const int OSCWordSize=4;
        private const int OSCLongSize=8;
        private byte[] messageTag;
        
        public OSCPackageInterpreter()
        {
            
            this.bundleTag= Encoding.UTF8.GetBytes("#bundle\0");
            
            this.messageTag =Encoding.UTF8.GetBytes("/");

            //generate a fast lookup table for typestring valudation and method validation
            this.possibleOSCTypes = new bool[256];
            this.invalidMethodChars= new bool[256];
            for (int i = 0; i < possibleOSCTypes.Length; i++) {
                possibleOSCTypes[i] = false;
                invalidMethodChars[i] = true;
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
            //make a canary enabled utf8 encoder
            this.utf8 = new UTF8Encoding(false, true);

            this.invalidMethodChars[(byte)' '] = false;
            this.invalidMethodChars[(byte)'*'] = false;
            this.invalidMethodChars[(byte)','] = false;
            this.invalidMethodChars[(byte)'?'] = false;
            this.invalidMethodChars[(byte)'['] = false;
            this.invalidMethodChars[(byte)']'] = false;
            this.invalidMethodChars[(byte)'{'] = false;
            this.invalidMethodChars[(byte)'}'] = false;
        }

       
        private bool isTypeStringCharValid(byte character)
        {
            return this.possibleOSCTypes[character];
        }

        //self explanitory, checks if a readonly span of bytes has a starting bundle tag
        private bool isBundle(ReadOnlySpan<byte> bytes)
        {
            if (bytes.Length >= bundleTagLength) {
                bool match = true;
                for (int i = 0; i < bundleTagLength; i++)
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

        //self explanitory checks if a read only span of bytes ahas a starting message tag
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
            //find the null terminator
            int end = stringData.IndexOf((byte)0);
            //if there is none, scream
            if (end < 0)
            {
                throw new InvalidOSCStringException();
            }
            //calcualte the total length of the string
            int total = end + 1;
            //find out the expected ammount of padding with division magic
            int padding = (OSCWordSize - (total % OSCWordSize)) % OSCWordSize;

            //check if the bytestream padding matches what is expected
            for (int i = 1; i <= padding; i++)
            {
                if (stringData[end + i] != 0)
                {
                    throw new InvalidOSCStringException();
                }
                    
            }
            //if everything checks out return the bytes consumed
            return new validateOSCStringDataAndGetLengthsReturn(total, padding);


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
                //minus one to account for the null terminator that gets included
                return new OSCStringConversionReturn(this.utf8.GetString(stringData.Slice(0, lengths.end-1)), lengths.end + lengths.padding);
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
                    if ((startIndex + OSCWordSize) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    
                    return new OSCvalueConversionReturn(4, new OSCInt(BinaryPrimitives.ReadInt32BigEndian(data.Slice(startIndex, 4))));

                case 'f':
                    if ((startIndex + OSCWordSize) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    
                    return new OSCvalueConversionReturn(4, new OSCFloat(BinaryPrimitives.ReadSingleBigEndian(data.Slice(startIndex, 4))));

                case 'b':
                    if ((startIndex + OSCWordSize) > data.Length)
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
                    int padding = (OSCWordSize - (length % OSCWordSize)) % OSCWordSize;
                    return new OSCvalueConversionReturn(length +padding+4,new OSCBlob(data.Slice(startIndex + 4, length).ToArray()));

                case 'h':
                    if ((startIndex + OSCLongSize) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    
                    return new OSCvalueConversionReturn(8, new OSCLong(BinaryPrimitives.ReadInt64BigEndian(data.Slice(startIndex, 8))));

                case 't':
                    if ((startIndex + OSCLongSize) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    return new OSCvalueConversionReturn(8, new OSCTimeTag(BinaryPrimitives.ReadUInt64BigEndian(data.Slice(startIndex, 8))));

                case 'd':
                    if ((startIndex + OSCLongSize) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    
                    return new OSCvalueConversionReturn(8, new OSCDouble(BinaryPrimitives.ReadDoubleBigEndian(data.Slice(startIndex, 8))));

                case 'S':
                    temp = this.extractOSCString(data.Slice(startIndex));
                    return new OSCvalueConversionReturn(temp.bytesRead, new OSCNonstandardString(temp.value));

                case 'c':
                    if ((startIndex + OSCWordSize) > data.Length)
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
                    if ((startIndex + OSCWordSize) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    
                    
                    
                    return new OSCvalueConversionReturn(4, new OSCColor(BinaryPrimitives.ReadInt32BigEndian((data.Slice(startIndex, 4)))));
                        
                    
                    

                case 'm':
                    if ((startIndex + OSCWordSize) > data.Length)
                    {
                        throw new IncompleteOSCDataException();
                    }
                    
                    return new OSCvalueConversionReturn(4,new OSCMIDI(BinaryPrimitives.ReadInt32BigEndian(data.Slice(startIndex, 4))));
                        
                    
                    

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


       
        //this us just used by the data segment interpreter's structure finding stage
        //to denote the structure of the data segment. think nested lists and their sizes and all that
        struct listTreeBuilderNode
        {


            public int childLists;
            public int values;
            public int parentIndex;
            public int index;

            public int[] childrenIndexes;
            public int nextOpenChildPosition;
            public listTreeBuilderNode(int parentIndex, int childLists, int values, int index)
            {
                //the index in the holding array of this node's parent
                this.parentIndex = parentIndex;
                //the number of normal values in this array
                this.values = values;
                //the number of sublists in this array
                this.childLists = childLists;
                //where in the master list the node is
                this.index = index;
                //the index of the children indexes array we are currently pointing to as the next open index(used for later)
                this.nextOpenChildPosition = 0;
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
            listTreeBuilderNode[] nodes = new listTreeBuilderNode[listCount + 1];
            //the deepest we ever go, its useful for later so we can make yet more data structures fixed size
            int maxDepth = 0;
            //our current depth, its useful so we can find the max depth
            int depth = 0;
            //create our base node, that represents the typestring itself
            nodes[0] = new listTreeBuilderNode(-1, 0, 0,0);
            //the positon wee put any new nodes we make
            int TopOfNodeList = 1;
            //the index in the nodes list of the node we are currently on
            int currentIndex = 0;
            

            listTreeBuilderNode currentNode=nodes[0];
            

            
            //loop through the entire typestring
            for (int i = 0; i<typeString.Length; i++)
            {
                if (typeString[i] == '[')
                {
                    currentNode.childLists++;
                    nodes[currentIndex] = currentNode;
                    
                    currentNode = new listTreeBuilderNode(currentIndex, 0, 0, TopOfNodeList);
                    nodes[TopOfNodeList] = currentNode;

                    currentIndex = TopOfNodeList;
                    TopOfNodeList++;

                    depth++;
                    if (depth > maxDepth)
                    {
                        maxDepth = depth;
                    }
                }else if (typeString[i] == ']')
                {
                    
                    currentNode.childrenIndexes = new int[currentNode.childLists];
                    nodes[currentIndex] = currentNode;
                    currentIndex = nodes[currentIndex].parentIndex;
                    currentNode = nodes[currentIndex];
                    
                    depth--;
                }
                else
                {

                    
                    currentNode.values++;
                }
            }
            currentNode.childrenIndexes = new int[currentNode.childLists];
            nodes[currentIndex] = currentNode;
            listTreeBuilderNode nodecache0;
            listTreeBuilderNode nodeCache1;
            for(int i = 0; i < nodes.Length; i++)
            {
                if (nodes[i].parentIndex != -1)
                {
                    nodecache0 = nodes[i];
                    nodeCache1 = nodes[nodecache0.parentIndex];
                    nodeCache1.childrenIndexes[nodeCache1.nextOpenChildPosition] = i;
                    nodeCache1.nextOpenChildPosition++;
                    nodes[nodecache0.parentIndex] = nodeCache1;
                }
            }

            
            
            return new findListStructureReturn(nodes,maxDepth);
        }
        

        //need a struct here to represent the stack frames for the below function
        private struct buildOSCMessageValuesFrame
        {
            public int listStructureIndex;
            public listTreeBuilderNode currentNode;
            public OSCValue[] currentList;
            public int currentListIndex;
            public int childListIndex;

            public buildOSCMessageValuesFrame(int listStructureIndex, listTreeBuilderNode currentNode, OSCValue[] currentList,int currentListIndex,int childListIndex)
            {
                this.listStructureIndex = listStructureIndex;
                this.currentNode = currentNode;
                this.currentList = currentList;
                this.currentListIndex = currentListIndex;
                this.childListIndex = childListIndex;
            }
        }
        private struct buildOSCMessageValuesStack
        {
            private int stackIndex;
            private buildOSCMessageValuesFrame[] frames;

            public buildOSCMessageValuesStack(int size)
            {
                this.frames=new buildOSCMessageValuesFrame[size];
                this.stackIndex = 0;
            }

            public void push(buildOSCMessageValuesFrame newFrame)
            {
                if (this.stackIndex >= this.frames.Length)
                {
                    throw new IndexOutOfRangeException();
                }
                this.frames[this.stackIndex] = newFrame;
                this.stackIndex++;
            }

            public buildOSCMessageValuesFrame pop()
            {
                this.stackIndex--;
                if (this.stackIndex < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                
                return this.frames[this.stackIndex];
            }
        }
        
        //give the raw typestirng data, dont trim it first
        private OSCValue[] buildOSCMessageValuesList(ReadOnlySpan<byte> typestring,ReadOnlySpan<byte> argumentData)
        {
            //TODO:

            //check for front comma in typestring
            if (typestring[0] != ',')
            {
                throw new InvalidTypestringException();
            }
            //remove end null and front comma from typestring
            ReadOnlySpan<byte> localTypeString=typestring.Slice(1,typestring.Length - 2);
            //find the list structure and save the array to a local variable
            findListStructureReturn listStructureReturn =this.findListStructure(localTypeString);
            listTreeBuilderNode[] listStrucure = listStructureReturn.nodes;

            //make an array of the same size of the list structure for the stack frames
            buildOSCMessageValuesStack stack = new buildOSCMessageValuesStack(listStructureReturn.maxDepth);
            
            //variable for the index of the current list strucre element
            int listStructureIndex = 0;
            //the current list structure element
            listTreeBuilderNode currentNode=listStrucure[listStructureIndex];
            //variable for the current childlist of currentlist we are on
            int childListIndex = 0;

            //varaible for the current list, take size for the current element in the list structure
            OSCValue[] currentList = new OSCValue[currentNode.childLists+currentNode.values];
            //variable for the position in the current list
            int currentListIndex = 0;

            //variable for the current position in the data segment
            int readHead = 0;

            //just some temporary storeage while we transiton stack frames
            OSCArray oscListTemp;
            buildOSCMessageValuesFrame oscTempFrame;

            //just some temporary value storage
            OSCvalueConversionReturn tempValue;

            //loop through the cut out typestring from 0 to length
            for (int index = 0; index < localTypeString.Length; index++)
            {

                //if the typestring is "[":
                if (localTypeString[index] == '[')
                {
                    //push the current variables to the stack
                    //get the next sublist from the list structure
                    //reset our position variables for the new sublist
                    //create our new current list form its data
                    stack.push(new buildOSCMessageValuesFrame(listStructureIndex, currentNode, currentList, currentListIndex, childListIndex));
                    listStructureIndex = currentNode.childrenIndexes[childListIndex];
                    currentNode = listStrucure[listStructureIndex];
                    childListIndex = 0;
                    currentListIndex = 0;
                    currentList = new OSCValue[currentNode.childLists + currentNode.values];
                }
                //else if the typestring is "]":
                else if (localTypeString[index] == ']')
                {
                    //take the current list and convert it to an osc list
                    //pop the values for the last list off the stack
                    //store the list we just made in the list we just got off the stack and increment our positon in it
                    //increment the childListIndex as we just finished a childlist
                    oscListTemp = new OSCArray(currentList);
                    oscTempFrame = stack.pop();
                    listStructureIndex = oscTempFrame.listStructureIndex;
                    currentNode = oscTempFrame.currentNode;
                    childListIndex = oscTempFrame.childListIndex;
                    childListIndex++;
                    currentList = oscTempFrame.currentList;
                    currentListIndex = oscTempFrame.currentListIndex;
                    currentList[currentListIndex] = oscListTemp;
                    currentListIndex++;

                }
                //else if the typestring is a valid type:
                else if (this.isTypeStringCharValid(localTypeString[index]))
                {
                    //create an item for the data at the readhead of that type and get how many bytes we consumed
                    //put that item in the current list
                    //increment the positon in the current list
                    //update the readhead by how many bytes we consumed
                    tempValue = this.getOSCValueFromBytes((char)localTypeString[index], argumentData, readHead);
                    readHead += tempValue.bytesConsumed;
                    currentList[currentListIndex] = tempValue.returnValue;
                    currentListIndex++;
                }
                //else:
                else
                {
                    //throw a typestyring error
                    throw new InvalidTypestringException();
                }
                
            }
            //after the loop is done return the current array
            return currentList;
            

        }



       
        

        private OSCMessage convertOSCByteArrayToMessage(ReadOnlySpan<byte> rawData)
        {
            int byteIndex = 0;
            
            
            OSCValue[] arguments= Array.Empty<OSCValue>();
            validateOSCStringDataAndGetLengthsReturn stringReturn0;
            OSCStringConversionReturn stringReturn1 = this.extractOSCString(rawData);
            byteIndex += stringReturn1.bytesRead;
            
            if (stringReturn1.value[0]!='/')
            {
                throw new InvalidOSCAddressException();
            }
            for(int i = 0; i < stringReturn1.value.Length; i++)
            {
                if (this.invalidMethodChars[stringReturn1.value[i]] == false)
                {
                    throw new InvalidOSCAddressException();
                }
            }
            stringReturn0 = this.validateOSCStringDataAndGetLengths(rawData.Slice(byteIndex));
            
            
            ReadOnlySpan<byte> typeString = rawData.Slice(byteIndex,stringReturn0.end);
            byteIndex += stringReturn0.end + stringReturn0.padding;
            
            arguments = this.buildOSCMessageValuesList(typeString, rawData.Slice(byteIndex));

            return new OSCMessage(stringReturn1.value, arguments);
        }




        private int findBundleCountAndCoarseValidate(ReadOnlySpan<byte> rawBundle)
        {

            
            //variable for the count
            int bundleCount = 0;
            //variable for the current item's size
            int currentItemSize = 0;
            //vairable for the progress through the bundle data
            int readHead = 0;
            //just a control flow variable for the algorithm
            bool bundleCheckIsBundle = false;
            //verify that the bundle data is indeed a bundle and move past its header, and incriment the bundle count
            if (rawBundle.Length < bundleTagLength + timeStampSize)
            {
                throw new InvalidBundleException();
            }
            if (this.isBundle(rawBundle))
            {
                readHead += bundleTagLength + timeStampSize;
                bundleCount++;
            }
            else
            {
                throw new InvalidBundleException();
            }


            //loop while the readhead is less than or equal to the surrounding raw bundle's size
            while(readHead< rawBundle.Length)
            {
                bundleCheckIsBundle = false;
                //test if reading the item size is too big for the current rawbundle size, and if so throw
                if (readHead + sizeTagSize > rawBundle.Length)
                {
                    throw new InvalidBundleException();
                }
                //read in the item size and incrmeent the readhead by it
                currentItemSize=BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead,sizeTagSize));
                readHead += sizeTagSize;
                //acount for the sizeTag
                int remainingElementSize = currentItemSize;
                //if the item size is negitive or greater than the remaining length of rawbundle throw
                if ((currentItemSize < 0) || (rawBundle.Length-readHead  < remainingElementSize))
                {
                    
                    throw new InvalidBundleException();
                }
                //otherwise test if remaining size is big enough for a bundle header
                if (rawBundle.Length >= bundleTagLength + timeStampSize+readHead)
                {
                    //if it is big enough, test the header for the bundle tag, and if it passes move past the header and increment bundle count
                    if (isBundle(rawBundle.Slice(readHead)))
                    {
                        readHead += bundleTagLength + timeStampSize;
                        bundleCount++;
                        bundleCheckIsBundle=true;
                    }
                    
                }
                if (bundleCheckIsBundle == false)
                {
                    //otherwise test if the remaining size is big enough for a message, and if not, throw
                    if (rawBundle.Length < messageTag.Length + readHead)
                    {
                        throw new InvalidBundleException();
                    }
                    //if it is big enough, test if it is a message, if not throw
                    if (isMessage(rawBundle.Slice(readHead)))
                    {
                        //if it is a message increment the readhead by the item size
                        readHead += currentItemSize;
                    }
                    else
                    {
                        throw new InvalidBundleException();
                    }

                }
                
                
                
            }


            //return the bundle count


            return bundleCount;
        }

        
        struct BundleTreeBuilderNode
        {
            public int parentIndex;
            public int childNodeCount;
            public int messageCount;
            public int childNodesArrayCurrentIndex;
            public int endPosition;
            public int[] childNodesIndexes;

            public BundleTreeBuilderNode(int parentIndex,int endPosition)
            {
                this.parentIndex = parentIndex;
                this.endPosition = endPosition;
                this.childNodeCount = 0;
                this.messageCount = 0;
                this.childNodesArrayCurrentIndex = 0;
                
            }
        }

        private struct findBundleStructureReturn
        {
            public BundleTreeBuilderNode[] nodeArray;
            public int maxDepth;
            public int totalPayloads;

            public findBundleStructureReturn(BundleTreeBuilderNode[] nodeArray, int maxDepth, int totalPayloads)
            {
                this.nodeArray = nodeArray;
                this.maxDepth = maxDepth;
                this.totalPayloads = totalPayloads;
            }
        }
        private findBundleStructureReturn findBundleStructure(ReadOnlySpan<byte> rawBundle)
        {
            
            //a varaible to store the total number of bundles in the array, retreived with a function
            int bundleCount=this.findBundleCountAndCoarseValidate(rawBundle);
            //an array to store all the bundle nodes
            BundleTreeBuilderNode[] masterArray=new BundleTreeBuilderNode[bundleCount];
            //a varaible to point ot the next open slot in the above array
            int nextOpen = 0;
            //a varaible to point ot the current active array index
            int currentIndex = 0;
            //a varaible to store the current active node struct
            BundleTreeBuilderNode currentNode;
            //a variable to store the current position in the raw bundle
            int readHead = 0 ;
            //a varaible to store the current item size
            int currentSize = 0 ;
            //a varaible to store the current depth
            int depth = 0;
            //a varaible to store the max depth
            int maxDepth = 0;
            //a variable to store the total number of payloads that are in this bundle
            int totalPayloads = 0;
            //get past the root bundle header and create the root node
            readHead += bundleTagLength + timeStampSize;
            currentNode = new BundleTreeBuilderNode(-1,rawBundle.Length);
            masterArray[currentIndex]=currentNode;
            nextOpen++;


            //loop while the read head is less than size of the raw bundle
            while ((readHead < rawBundle.Length)||(depth>0))
            {
                //if the readhead is greater than or equal to the end position of the current node:
                if (readHead >= currentNode.endPosition)
                {
                    if (currentNode.parentIndex == -1) {
                        throw new InvalidBundleException();
                    }
                    //create the final childnodesIndexes array with the know size of its childcount, set the current node to its postion in the array,
                    currentNode.childNodesIndexes = new int[currentNode.childNodeCount];
                    masterArray[currentIndex] = currentNode;
                    //then load its parent node from the array and set currentindex node to that node index and set current node to the node at that index
                    currentIndex = currentNode.parentIndex;
                    currentNode=masterArray[currentIndex];
                    //decrement depth
                    depth--;
                }
                else
                {
                    //get the size tag of the current payload and advance the read head past it
                    currentSize=BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead,sizeTagSize));
                    readHead +=sizeTagSize;
                    //check if the payload at the current positon is a bundle and if it is:
                    if (isBundle(rawBundle.Slice(readHead)))
                    {
                        
                        //increment the number of children of the current node
                        currentNode.childNodeCount=currentNode.childNodeCount+1;
                        //store the current bundle to its position in the array
                        masterArray[currentIndex] = currentNode;
                        //create a new bundle at the next open array slot, with its parent node set to the current node index and increment the pointer to that slot,
                        //set the current index to the index of this new bundle, and set the current bundle to this new bundle
                        currentNode = new BundleTreeBuilderNode(currentIndex,readHead+currentSize);
                        currentIndex = nextOpen;
                        masterArray[currentIndex] = currentNode;
                        //update the next open slot
                        nextOpen++;
                        //increment depth
                        depth++;
                        //if current depth is greater than max depth, update it
                        if(depth> maxDepth)
                        {
                            maxDepth = depth;
                        }
                        //update the total payloads count
                        totalPayloads++;
                        //update the read head
                        readHead += bundleTagLength + timeStampSize;
                    }
                    //otherwise:
                    else
                    {
                        // increment message count of the current node and move the read head past it by its size
                        currentNode.messageCount=currentNode.messageCount+1;
                        readHead += currentSize;
                        //update the total payloads count
                        totalPayloads++;
                    }
                }

            }

            //populate the final node
            currentNode.childNodesIndexes = new int[currentNode.childNodeCount];
            masterArray[currentIndex] = currentNode;

            BundleTreeBuilderNode nodeCache0;
            BundleTreeBuilderNode nodeCache1;
            //loop through all the nodes in the list
            for (int index = 0; index < masterArray.Length; index++)
            {
                nodeCache0 = masterArray[index];
                //if the node's parent index isnt -1 (the root)
                if (nodeCache0.parentIndex != -1)
                {
                    //goto its parent node, and add its index in the master arry to its parent's children's indexes array at the first open position
                    nodeCache1 = masterArray[nodeCache0.parentIndex];
                    
                    nodeCache1.childNodesIndexes[nodeCache1.childNodesArrayCurrentIndex] = index;
                    nodeCache1.childNodesArrayCurrentIndex++;
                    masterArray[nodeCache0.parentIndex] = nodeCache1;
                }

            }



            //return the max depth and nodeArray as a struct
            return new findBundleStructureReturn(masterArray, maxDepth,totalPayloads);
        }



        private struct convertByteArrayToBundleFrame
        {
            public OSCBundleElement[] currentBundleArray;
            public int currentBundleArrayLength;
            public int bundleArrayPosition;
            public BundleTreeBuilderNode currentNode;
            public int currentChildNode;
            public ulong TimeStamp;
            

            public convertByteArrayToBundleFrame(int bundleArraySize,BundleTreeBuilderNode currentNode,ulong bundleTimeStamp)
            {
                this.currentBundleArray = new OSCBundleElement[bundleArraySize];
                this.currentBundleArrayLength = bundleArraySize;
                this.bundleArrayPosition = 0;
                this.currentNode = currentNode;
                this.currentChildNode = 0;
                this.TimeStamp = bundleTimeStamp;
            }
        }

        private struct convertByteArrayToBundleStack{
            private int nextOpenPosition;
            private convertByteArrayToBundleFrame[] stack;
            public convertByteArrayToBundleStack(int height)
            {
                this.stack= new convertByteArrayToBundleFrame[height];
                this.nextOpenPosition= 0;
            }

            public void push(convertByteArrayToBundleFrame frame)
            {
                if (this.nextOpenPosition >= stack.Length)
                {
                    throw new IndexOutOfRangeException();
                }
                this.stack[nextOpenPosition] = frame;
                this.nextOpenPosition++;
            }

            public convertByteArrayToBundleFrame pop()
            {
                this.nextOpenPosition--;
                if (this.nextOpenPosition < 0)
                {
                    throw new IndexOutOfRangeException();
                }
                return this.stack[this.nextOpenPosition];
            }

            public bool isEmpty()
            {
                if((this.nextOpenPosition <= -1))
                {
                    throw new IndexOutOfRangeException();
                }
                return (this.nextOpenPosition == 0);
            }
        }

        private OSCBundle ConvertOSCByteArrayToBundle(ReadOnlySpan<byte> rawBundle)
        {

            //variable to store the return of finding the bundle structure
            findBundleStructureReturn structureData = this.findBundleStructure(rawBundle);
            //variable to store the max depth that we use to size the stack
            int maxDepth=structureData.maxDepth;
            //vairable used to store the total number of payloads
            int totalPayloads=structureData.totalPayloads;
            //vairable to store the current ammount of payloads processed
            int processedPayloads = 0;
            //vairable used to store the master structure array
            BundleTreeBuilderNode[] masterStructureArray= structureData.nodeArray;
            //variable to store the stack struct
            convertByteArrayToBundleStack decodeStack;
            //varaible to store the current node
            BundleTreeBuilderNode currentNode; 
            //variable to store the current stack frame
            convertByteArrayToBundleFrame currentFrame;
            //variable to store the current position in the raw bundle
            int readHead = 0;
            //variable to store the size of the current item
            int currentSize = 0;
            //variable to temporairily store new bundles
            OSCBundle bundleTemp;
            //variable to temporailily store timestamps
            ulong timeStampTemp;
            
            //get the root bundle header read in
            readHead += bundleTagLength;
            timeStampTemp=BinaryPrimitives.ReadUInt64BigEndian(rawBundle.Slice(readHead,timeStampSize));
            readHead += timeStampSize;
            //init starting values
            decodeStack = new convertByteArrayToBundleStack(maxDepth);
            currentNode = masterStructureArray[0];
            currentFrame = new convertByteArrayToBundleFrame(currentNode.childNodeCount + currentNode.messageCount, currentNode, timeStampTemp);

            //loop while the current total number of payloads processed is less than the total number of payloads
            while (processedPayloads < totalPayloads)
            {
                //if the current frame's bundle array position is greater than or equal to its length:
                if (currentFrame.bundleArrayPosition >= currentFrame.currentBundleArrayLength)
                {
                    //this sub bundle is finished, so 
                    //convert the currentframe into a new bundle
                    bundleTemp = new OSCBundle(currentFrame.currentBundleArray, currentFrame.TimeStamp);
                    if (decodeStack.isEmpty())
                    {
                        throw new InvalidBundleException();
                    }
                    //pop the last frame off the stack and set current frame to it
                    currentFrame=decodeStack.pop();
                    //get the its current node too
                    currentNode = currentFrame.currentNode;
                    //store the new bundle in the current frame
                    currentFrame.currentBundleArray[currentFrame.bundleArrayPosition]=new OSCBundleElement(bundleTemp);
                    //update the current position of the current bundle
                    currentFrame.bundleArrayPosition += 1;
                    //increment the current amounts of payloads processed
                    processedPayloads++;
                }
                //otherwise:
                else
                {
                    //read in the size of the payload at readhead
                    currentSize = BinaryPrimitives.ReadInt32BigEndian(rawBundle.Slice(readHead, sizeTagSize));
                    //advance the readhead past it
                    readHead += sizeTagSize;
                    //if the data at readhead is a bundle header:
                    if (this.isBundle(rawBundle.Slice(readHead)))
                    {
                        //move the readhead past the bundle header tag
                        readHead += bundleTagLength;
                        //grab its timestamp for later
                        timeStampTemp = BinaryPrimitives.ReadUInt64BigEndian(rawBundle.Slice(readHead, timeStampSize));
                        //move the readhead past its timestamp
                        readHead += timeStampSize;

                        //incrment the current current child node of the current frame
                        currentFrame.currentChildNode += 1;
                        //push  the current frame to the stack
                        decodeStack.push(currentFrame);
                        //make a new stack frame for the  with node of the current frame's next child node pulled from the master structure array, with a size of this new node's children and messages, and with the timestamp we just read
                        //set the current frame to this new frame
                        currentNode = masterStructureArray[currentFrame.currentNode.childNodesIndexes[currentFrame.currentChildNode-1]];//-1 here because we incrimetned it for the next use but want the old index for this use
                        currentFrame = new convertByteArrayToBundleFrame(currentNode.childNodeCount + currentNode.messageCount, currentNode, timeStampTemp);
                    }
                    //otherwise:
                    else
                    {
                        //convert the data from the readhead to readHead+payload size into a message
                        //store the message to the current frame
                        currentFrame.currentBundleArray[currentFrame.bundleArrayPosition] = new OSCBundleElement(this.convertOSCByteArrayToMessage(rawBundle.Slice(readHead, currentSize)));
                        //update the current position of the current bundle
                        currentFrame.bundleArrayPosition += 1;
                        //increment the current amounts of payloads processed
                        processedPayloads++;
                        //move the readhead past the payload
                        readHead+= currentSize;
                    }

                }

            }


            //convert the current frame to a bundle and return it
            return new OSCBundle(currentFrame.currentBundleArray, currentFrame.TimeStamp);


        }






        public OSCPackage convertOSCByteArrayToPackage(ReadOnlySpan<byte> rawPackage)
        {
            if (rawPackage == null)
            {
                throw new InvalidPackageException();
            }
            int startingPos = 0;
            bool foundStart = false;
            while (startingPos < rawPackage.Length)
            {
                if (rawPackage[startingPos] == '\0')
                {
                    startingPos++;
                }
                else
                {
                    foundStart = true;
                    break;
                }
            }

            if (!foundStart)
            {
                throw new InvalidPackageException();
            }
            bool wasBundle = false;
            
            if (rawPackage.Length-startingPos >= bundleTagLength)
            {
                if (this.isBundle(rawPackage.Slice(startingPos)))
                {
                    wasBundle = true;
                    
                    return this.ConvertOSCByteArrayToBundle(rawPackage.Slice(startingPos));
                }
            }
            if (!wasBundle)
            {
                if(rawPackage.Length-startingPos >=messageTag.Length)
                {
                    if (this.isMessage(rawPackage.Slice(startingPos)))
                    {
                        
                        return this.convertOSCByteArrayToMessage(rawPackage.Slice(startingPos));
                    }
                }
            }
            throw new InvalidPackageException();

        }


        
    }
}
