using System;
using System.Collections.Generic;

using System.Text;

namespace CactusOSC
{
    /// <summary>
    /// a simple class for converting between OSC package byte data and osc object trees
    /// </summary>
    public sealed class RawOSCConverter
    {
        private OSCPackageCompiler compiler;
        private OSCPackageInterpreter interpreter;
        public RawOSCConverter() {
            this.interpreter = new OSCPackageInterpreter();
            this.compiler = new OSCPackageCompiler();
        }

        /// <summary>
        /// a function to convert an OSC package into a byte[] format for transmision
        /// </summary>
        /// <param name="PackageToCompile"></param>
        /// <returns>byte[]</returns>
        /// <exception cref="InvalidPackageException"></exception>
        public byte[] EncodeOSCPackage(OSCPackage PackageToCompile)
        {
            
            RawOSCPackage tempRaw = new RawOSCPackage(PackageToCompile.GetSize());
            switch (PackageToCompile.GetPackageType())
            {
                case OSCPackageType.OSCBundle:

                    compiler.convertOSCBundleToByteArray(((OSCBundle)PackageToCompile), tempRaw);

                    return tempRaw.getRawData();

                case OSCPackageType.OSCMessage:

                    compiler.convertOSCMessageToByteArray(((OSCMessage)PackageToCompile), tempRaw);
                    return tempRaw.getRawData();

                default:
                    throw new InvalidPackageException();
            }
            
        }
        /// <summary>
        /// a function to convert a byte[] of osc package data into an OSC package for processing
        /// </summary>
        /// <param name="PackageToDecode"></param>
        /// <returns>OSCPackage</returns>
        public OSCPackage DecodeOSCPackage(byte[] PackageToDecode)
        {
            return this.interpreter.convertOSCByteArrayToPackage(PackageToDecode);
        }
    }
}
