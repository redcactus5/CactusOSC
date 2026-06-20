using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{
    internal partial class messageConverter
    {

        private string generateArrayTypeString(OSCArray array)
        {
            
            StringBuilder typeString = new StringBuilder();


            Stack<OSCArray> arrayStack = new Stack<OSCArray>();
            Stack<int> arrayIndex= new Stack<int>();
            arrayStack.Push(array);
            typeString.Append("[");
            while (arrayStack.Count > 0)
            {
                OSCArray currentArray = arrayStack.Pop();
                OSCValue[] currentContents=currentArray.getRawValue();
                int currentIndex = 0;
                while (currentIndex<currentContents.Length)
                {
                    if (currentContents[currentIndex].getOSCType() == OSCValueType.OSCArray)
                    {
                        arrayStack.Push(currentArray);
                        arrayIndex.Push(currentIndex+1);
                        
                        typeString.Append('[');
                        currentArray = ((OSCArray)currentContents[currentIndex]);
                        currentIndex = 0;
                        currentContents = currentArray.getRawValue();
                    }
                    else
                    {
                        typeString.Append(this.getOSCTypeString(currentContents[currentIndex]));
                        currentIndex++;
                    }
                    
                }
                if(arrayStack.Count > 0)
                {
                    currentArray=arrayStack.Pop();
                    currentContents = currentArray.getRawValue();
                    currentIndex = arrayIndex.Pop();
                    
                    
                }
                typeString.Append("]");
            }
            return typeString.ToString();
        }

        private string getOSCTypeString(OSCValue value)
        {
            switch (value.getOSCType())
            {
                case OSCValueType.OSCString:
                    return("s");
                    break;
                case OSCValueType.OSCInt:
                    return("i");
                    break;
                case OSCValueType.OSCFloat:
                    return("f");
                    break;
                case OSCValueType.OSCBlob:
                    return("b");
                    break;
                case OSCValueType.OSCLong:
                    return("h");
                    break;
                case OSCValueType.OSCTimeTag:
                    return("t");
                    break;
                case OSCValueType.OSCDouble:
                    return("d");
                    break;
                case OSCValueType.OSCNonstandardString:
                    return("S");
                    break;
                case OSCValueType.OSCChar:
                    return("c");
                    break;
                case OSCValueType.OSCRGBA:
                    return("r");
                    break;
                case OSCValueType.OSCMIDI:
                    return("m");
                    break;
                case OSCValueType.OSCBool:
                    if (((OSCBool)value).getValue())
                    {
                        return("T");
                    }
                    else
                    {
                        return("F");
                    }
                    break;
                case OSCValueType.OSCNil:
                    return("N");
                    break;
                case OSCValueType.OSCInfinum: 
                    return("I");
                    break;
                default:
                        throw new Exception("invalid OSCValueType!");
                    break;
                    
            }
            
            
        }

        private byte[] getOSCValueBytes(OSCValue value)
        {
            byte[] result;
            switch (value.getOSCType())
            {
                case OSCValueType.OSCString:
                    return generateOSCString(((OSCString)value).getValue());
                    break;
                case OSCValueType.OSCInt:
                    result = BitConverter.GetBytes(((OSCInt)value).getValue());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(result);
                    }
                    return result;
                    break;
                case OSCValueType.OSCFloat:
                    result = BitConverter.GetBytes(((OSCFloat)value).getValue());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(result);
                    }
                    return result;
                    break;
                case OSCValueType.OSCBlob:
                    byte[] data = ((OSCBlob)value).getValue();
                    result = new byte[data.Length];
                    byte[] size=BitConverter.GetBytes(value.getByteSize());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(size);
                    }
                    int index=writeToByteArrayAtIndex(result, size, 0);
                    writeToByteArrayAtIndex(result, data, index);
                    return result;
                    break;
                case OSCValueType.OSCLong:
                    result = BitConverter.GetBytes(((OSCLong)value).getValue());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(result);
                    }
                    return result;
                    break;
                case OSCValueType.OSCTimeTag:
                    result = BitConverter.GetBytes(((OSCTimeTag)value).getValue());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(result);
                    }
                    return result;
                    break;
                case OSCValueType.OSCDouble:
                    result = BitConverter.GetBytes(((OSCDouble)value).getValue());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(result);
                    }
                    return result;
                    break;
                case OSCValueType.OSCNonstandardString:
                    result = generateOSCString(((OSCNonstandardString)value).getValue());
                    return result;
                    break;
                case OSCValueType.OSCChar:
                    return new byte[] { (byte)((OSCChar)value).getValue(), 0, 0, 0 };
                    break;
                case OSCValueType.OSCRGBA:
                    result = BitConverter.GetBytes(((OSCColor)value).getValue());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(result);
                    }
                    return result;

                    break;
                case OSCValueType.OSCMIDI:
                    result = BitConverter.GetBytes(((OSCMIDI)value).getValue());
                    if (BitConverter.IsLittleEndian)
                    {
                        Array.Reverse(result);
                    }
                    return result;
                    break;
                default:
                    throw new Exception("invalid OSCValueType!");
                    break;
            }
            
        }
       
        private bool shouldGetOSCValueBytes(OSCValue value)
        {
            switch (value.getOSCType())
            {
                case OSCValueType.OSCString:
                    return true;
                    break;
                case OSCValueType.OSCInt:
                    return true;
                    break;
                case OSCValueType.OSCFloat:
                    return true;
                    break;
                case OSCValueType.OSCBlob:
                    return true;
                    break;
                case OSCValueType.OSCLong:
                    return true;
                    break;
                case OSCValueType.OSCTimeTag:
                    return true;
                    break;
                case OSCValueType.OSCDouble:
                    return true;
                    break;
                case OSCValueType.OSCNonstandardString:
                    return true;
                    break;
                case OSCValueType.OSCChar:
                    return true;
                    break;
                case OSCValueType.OSCRGBA:
                    return true;
                    break;
                case OSCValueType.OSCMIDI:
                    return true;
                    break;
                case OSCValueType.OSCBool:
                    return false;
                    break;
                case OSCValueType.OSCNil:
                    return false;
                    break;
                case OSCValueType.OSCInfinum:
                    return false;
                    break;
                default:
                    throw new Exception("invalid OSCValueType!");
                    break;

            }
        }

        private byte[] generateArrayValueBytes(OSCArray array)
        {

            byte[] data=new byte[array.getByteSize()];
            int targetIndex = 0;

            Stack<OSCArray> arrayStack = new Stack<OSCArray>();
            Stack<int> arrayIndex = new Stack<int>();

            arrayStack.Push(array);
            while (arrayStack.Count > 0)
            {
                OSCArray currentArray = arrayStack.Pop();
                OSCValue[] currentContents = currentArray.getRawValue();
                int currentIndex = 0;
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
                            targetIndex= this.writeToByteArrayAtIndex(data,this.getOSCValueBytes(currentContents[currentIndex]),targetIndex);
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
                
            }
            return data;
        }

        private byte[] generateOSCValueBytes(OSCValue[] values)
        {
            int dataSize = 0;
            for(int index = 0; index< values.Length; index++)
            {
                dataSize += values[index].getByteSize();
            }
            byte[] dataBytes = new byte[dataSize];
            int writeHead = 0;
            
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].getOSCType() == OSCValueType.OSCArray)
                {
                    writeHead=writeToByteArrayAtIndex(dataBytes, generateArrayValueBytes((OSCArray)values[i]),writeHead);
                }
                else
                {
                    if (this.shouldGetOSCValueBytes(values[i]))
                    {
                        writeHead = writeToByteArrayAtIndex(dataBytes, getOSCValueBytes(values[i]), writeHead);
                    }
                }
            }
            return dataBytes;
        }

        private string generateOSCValueTypeString(OSCValue[] values)
        {
            StringBuilder typeString = new StringBuilder();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i].getOSCType() == OSCValueType.OSCArray)
                {
                    typeString.Append(this.generateArrayTypeString((OSCArray)values[i]));
                }
                else
                {
                    typeString.Append(this.getOSCTypeString(values[i]));
                }
            }
            return typeString.ToString();
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
            byte[] bytes = new byte[this.calculateOSCStringSize(text.Length)];
            int index= 0;
            ReadOnlySpan<char> textSpan=text.AsSpan(0, text.Length);
            for(int character=0; character<text.Length; character++ )
            {
                bytes[index] = (byte)textSpan[index];
                index++;
            }
            for (int padding = index;padding<bytes.Length; padding++)
            {
                bytes[padding] = 0;
            }
            return bytes;
        }
        //returns new current index
        private int writeToByteArrayAtIndex(byte[] target, byte[] source, int startIndex)
        {
            int writeHead = startIndex;
            for(int readHead=0; readHead<source.Length; readHead++)
            {
                target[writeHead] = source[readHead];
                writeHead++;
            }
            return writeHead;
        }

        public byte[] convertOSCMessageToByteArray(OSCMessage message)
        {
            byte[] byteVersion = new byte[message.getSize()];

            int byteVersionIndex = 0;
            byteVersionIndex = writeToByteArrayAtIndex(byteVersion, generateOSCString(message.getAddress()), byteVersionIndex);
            byteVersionIndex = writeToByteArrayAtIndex(byteVersion, generateOSCString(generateOSCValueTypeString(message.getValues())), byteVersionIndex);
            byteVersionIndex = writeToByteArrayAtIndex(byteVersion, generateOSCValueBytes(message.getValues()), byteVersionIndex);
            return byteVersion;
        }
    }
}
