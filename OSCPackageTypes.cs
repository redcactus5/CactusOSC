using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{


    public enum OSCPackageType
    {
        OSCMessage,
        OSCBundle
    }

    /// <summary>
    /// the base class for OSC messages and bundles. do not extend.
    /// </summary>
    public abstract class OSCPackage
    {
        private OSCPackageType type;
        protected int size;
        private bool sizeSet;

        /// <summary>
        /// get the OSCPackage Type
        /// </summary>
        /// <returns>OSCPackageType</returns>
        public OSCPackageType GetPackageType()
        {
            return this.type;
        }

        public OSCPackage(OSCPackageType type)
        {
            this.type = type;
        }




        protected int CalculateOSCStringSize(int textLength)
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

        /// <summary>
        /// get the size of the OSCPackage
        /// </summary>
        /// <returns>int</returns>
        public int GetSize()
        {
            return this.size;
        }

        protected void SetSize(int size)
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

    /// <summary>
    /// an immutible osc message
    /// </summary>
    public sealed class OSCMessage : OSCPackage
    {
        private string address;
        private OSCValue[] values;



        /// <summary>
        /// create an OSCMessage
        /// </summary>
        /// <param name="address"></param>
        /// <param name="values"></param>
        public OSCMessage(string address, OSCValue[] values) : base(OSCPackageType.OSCMessage)
        {

            this.address = address;
            this.values = values;
            int adressSize = this.CalculateOSCStringSize(Encoding.UTF8.GetByteCount(address) + 1);
            //account for the comma that denotes its start and the null terminator
            int typeStringSize = 2;
            //calculate the data size and typestring size
            int dataSize = 0;
            for (int index = 0; index < this.values.Length; index++)
            {
                dataSize += values[index].GetByteSize();
                typeStringSize += values[index].GetTypeStringSize();
            }
            //calculate the final size

            typeStringSize = this.CalculateOSCStringSize(typeStringSize);
            this.SetSize(adressSize + typeStringSize + dataSize);

        }
        /// <summary>
        /// create an OSCMessage
        /// </summary>
        /// <param name="address"></param>
        public OSCMessage(string address) : base(OSCPackageType.OSCMessage)
        {
            this.address = address;
            this.values = Array.Empty<OSCValue>();
            //account for the type string
            this.SetSize(this.CalculateOSCStringSize(Encoding.UTF8.GetByteCount(address) + 1) + CalculateOSCStringSize(2));
        }
        /// <summary>
        /// get the adress string of the OSCMessage
        /// </summary>
        /// <returns>string</returns>
        public string GetAddress()
        {
            return this.address;
        }
        /// <summary>
        /// get the Values of the OSCMessage
        /// </summary>
        /// <returns></returns>
        public OSCValue[] GetValues()
        {
            OSCValue[] Clone = new OSCValue[this.values.Length];
            for (int index = 0; index < Clone.Length; index++)
            {
                Clone[index] = this.values[index].Clone();
            }
            return Clone;
        }
        internal OSCValue[] GetRawValues()
        {
            return this.values;
        }

        /// <summary>
        /// get a value from an osc array at an index
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        /// <exception cref="IndexOutOfRangeException"></exception>
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

        /// <summary>
        /// get the string representation of an oscMessage
        /// </summary>
        /// <returns>string</returns>
        public override string ToString()
        {
            StringBuilder stringedVersion = new StringBuilder();
            stringedVersion.Append(this.address);
            stringedVersion.Append(" ");
            for (int index = 0; index < this.values.Length - 1; index++)
            {
                stringedVersion.Append(this.values[index].ToString());
                stringedVersion.Append(", ");

            }
            stringedVersion.Append(this.values[this.values.Length - 1].ToString());
            return stringedVersion.ToString();
        }
        /// <summary>
        /// create a deep copy of an OSCMessage
        /// </summary>
        /// <returns>OSCMessage</returns>
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


    /// <summary>
    /// an immutible osc bundle
    /// </summary>
    public sealed class OSCBundle : OSCPackage
    {
        private OSCBundleElement[] elements;
        private ulong timeTag;
        private const int identSize = 8;
        private const int timeTagSize = 8;

        /// <summary>
        /// get a string rperesentation of an OSCBundle
        /// </summary>
        /// <returns>string </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();

            Stack<int> subIndexes = new Stack<int>();
            int index = 0;
            Stack<OSCBundle> childBundles = new Stack<OSCBundle>();
            OSCBundle currentSubBundle = this;
            List<HashSet<OSCPackage>> seenPackages = new List<HashSet<OSCPackage>>();
            seenPackages.Add(new HashSet<OSCPackage>());
            seenPackages[0].Add(this);
            seenPackages.Add(new HashSet<OSCPackage>());
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

                if ((index >= currentSubBundle.GetRawElements().Length) && (depth > 0))
                {
                    depth--;
                    index = subIndexes.Pop() + 1;
                    currentSubBundle = childBundles.Pop();
                }
                else
                {
                    if (currentSubBundle.elements[index].GetRawContents().GetPackageType() == OSCPackageType.OSCMessage)
                    {
                        for (int i = 0; i < depth + 1; i++)
                        {
                            sb.Append("    ");
                        }
                        sb.Append(((OSCMessage)currentSubBundle.elements[index].GetRawContents()).ToString());
                        sb.Append('\n');
                        index++;
                    }
                    else
                    {
                        if (depth > 0)
                        {
                            for(int search=0; search < depth; search++)
                            {
                                if (seenPackages[search].Contains((OSCBundle)currentSubBundle.elements[index].GetRawContents()))
                                {

                                    throw new RecursiveBundleException();
                                }
                                
                                
                            }
                        }
                        seenPackages[depth].Add(currentSubBundle.elements[index].GetRawContents());
                        seenPackages.Add(new HashSet<OSCPackage>());
                        depth++;
                        subIndexes.Push(index);
                        index = 0;

                        childBundles.Push(currentSubBundle);


                        currentSubBundle = ((OSCBundle)currentSubBundle.elements[index].GetRawContents());
                            
                        
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
            return sb.ToString();
        }

        private int GetElementsSize()
        {
            int tempSize = 0;

            for (int elementIndex = 0; elementIndex < this.elements.Length; elementIndex++)
            {
                tempSize += this.elements[elementIndex].GetSize();

            }



            return tempSize;
        }
        /// <summary>
        /// create a new OSCBundle
        /// </summary>
        /// <param name="elements"></param>
        /// <param name="timeTag"></param>
        public OSCBundle(OSCBundleElement[] elements, ulong timeTag) : base(OSCPackageType.OSCBundle)
        {
            this.elements = (OSCBundleElement[])elements.Clone();
            this.timeTag = timeTag;
            this.SetSize(this.GetElementsSize() + identSize + timeTagSize);
        }
        /// <summary>
        /// create a new OSCBundle with a default timeTag
        /// </summary>
        /// <param name="elements"></param>
        public OSCBundle(OSCBundleElement[] elements) : base(OSCPackageType.OSCBundle)
        {
            this.elements = (OSCBundleElement[])elements.Clone();
            this.timeTag = 1;
            this.SetSize(this.GetElementsSize() + identSize + timeTagSize);
        }

        /// <summary>
        /// get an array of all the elements of the bundle
        /// </summary>
        /// <returns>OSCBundleElement[]</returns>
        /// <exception cref="RecursiveBundleException"></exception>
        public OSCBundleElement[] GetElements()
        {



            Stack<OSCBundleElement[]> templateArrayStack = new Stack<OSCBundleElement[]>();
            OSCBundleElement[] currentTemplate = this.elements;

            Stack<OSCBundleElement[]> copyStack = new Stack<OSCBundleElement[]>();
            OSCBundleElement[] copyArray = new OSCBundleElement[currentTemplate.Length];

            List<HashSet<OSCBundle>> seenBundles = new List<HashSet<OSCBundle>>();
            seenBundles.Add(new HashSet<OSCBundle>());
            seenBundles[0].Add(this);
            seenBundles.Add(new HashSet<OSCBundle>());

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
                    seenBundles.RemoveAt(depth);
                    depth--;
                }
                else
                {
                    if (currentTemplate[currentIndex].GetRawContents().GetPackageType() == OSCPackageType.OSCBundle)
                    {
                        if (depth > 0)
                        {
                            for (int search = 0; search < depth; search++)
                            {
                                if (seenBundles[search].Contains((OSCBundle)currentTemplate[currentIndex].GetRawContents()))
                                {
                                    throw new RecursiveBundleException();
                                }
                            }
                        }

                        seenBundles[depth].Add((OSCBundle)currentTemplate[currentIndex].GetRawContents());
                        seenBundles.Add(new HashSet<OSCBundle>());
                        templateArrayStack.Push(currentTemplate);
                        copyStack.Push(copyArray);
                        currentTemplate = ((OSCBundle)currentTemplate[currentIndex].GetRawContents()).GetRawElements();
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
        internal OSCBundleElement[] GetRawElements()
        {
            return this.elements;
        }
        /// <summary>
        /// create a deep copy of the OSCBundle
        /// </summary>
        /// <returns>OSCBundle</returns>
        public OSCBundle Clone()
        {

            return new OSCBundle(this.GetElements(), this.timeTag);
        }
        /// <summary>
        /// get the raw timetag value of the bundle
        /// </summary>
        /// <returns>ulong</returns>
        public ulong GetTimeTag()
        {
            return this.timeTag;
        }
        /// <summary>
        /// get the parsed timeTagValue of the Bundle
        /// </summary>
        /// <returns>OSCTimeTagValue</returns>
        public OSCTimeTagValue getParsedTimeTag()
        {
            return new OSCTimeTagValue(this.timeTag);
        }
    }


    /// <summary>
    /// an immutible wrapper for an osc bundle element
    /// </summary>
    public sealed class OSCBundleElement
    {
        private OSCPackage contents;
        private int dataSize;
        private int size;
        public OSCBundleElement(OSCPackage contents)
        {
            this.contents = contents;


            this.dataSize = this.contents.GetSize();
            //account for the size integer
            this.size = 4 + this.dataSize;
        }

        public OSCBundleElement Clone()
        {
            return new OSCBundleElement(this.GetContents());
        }
        public OSCPackage GetContents()
        {
            switch (this.contents.GetPackageType())
            {
                case OSCPackageType.OSCMessage:
                    OSCMessage tempMessage = (OSCMessage)this.contents;
                    return tempMessage.Clone();
                case OSCPackageType.OSCBundle:
                    OSCBundle tempBundle = (OSCBundle)this.contents;
                    return (tempBundle.Clone());
                default:
                    throw new InvalidBundleElementException();
            }

        }

        internal OSCPackage GetRawContents()
        {
            return this.contents;
        }

        public string ToString()
        {
            switch (this.contents.GetPackageType())
            {
                case OSCPackageType.OSCMessage:
                    OSCMessage tempMessage = (OSCMessage)this.contents;
                    return tempMessage.ToString();
                case OSCPackageType.OSCBundle:
                    OSCBundle tempBundle = (OSCBundle)this.contents;
                    return tempBundle.ToString();
                default:
                    throw new InvalidBundleElementException();
            }
        }
        public int GetSize()
        {
            return this.size;
        }

        public int GetDataSize()
        {
            return this.dataSize;
        }
    }
}
