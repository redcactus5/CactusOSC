# CactusOSC

a simple, powerful, easy to use c# Open Sound Control sending and receiving library, built entirely in c#, from the ground up, for .NET and c#.



it comes fully featured with:

* array support
* nesting support
* bundle support
* Strong .NET object model
* a complete set of osc object classes
* a built in osc send and receive udp server
* multithreading
* Automatic type-tag generation
* automatic sizing
* guaranteed send and receive ordering
* polling model
* Messages are immutable after creation
* Bundle hierarchy is never flattened
* full OSC 1.1 bundle hierarchy preservation
* OSC 1.1 compliance



just create and start an OSCServer object and you are good to go! messages will automatically be processed and queued, and building messages is easy, just assemble them out of the provided classes. or, if you arent using UDP, or just want to get your hands dirty use the RawOSCConverter class to manually convert your object trees into sendable data and back!



there is no real documentation, but it should be understandable with just the types file and some trial and error.
in leu of documentation, have a complete example program.
//example program:
using CactusOSC;
namespace demo
{
    public class OSCMaster
    {
        const string OSCServerAddress = "127.0.0.1";
        const string receiveAddress = "127.0.0.1";
        const UInt16 sendPort = 8000;
        const UInt16 receivePort = 9000;
        public OSCMaster()
        {

        }
        
        public void testOSCReceive()
        {
            OSCServer testServer=new OSCServer();
            testServer.StartOSCServer(receivePort, receiveAddress, receivePort, OSCServerAddress);
            RawOSCConverter testConverter= new RawOSCConverter();
            OSCMessage demoMessage = new OSCMessage("/demo");

            OSCInt testInt = new OSCInt(43110);
            OSCString testString = new OSCString("test");
            OSCFloat testFloat = new OSCFloat(3.14f);
            OSCBlob testBlob = new OSCBlob(testConverter.EncodeOSCPackage(demoMessage));
            OSCLong testLong = new OSCLong(43110L);
            OSCTimeTag testTimeTag = new OSCTimeTag(new OSCTimeTagValue(0, 1));
            OSCDouble testDouble = new OSCDouble(3.14d);
            OSCNonstandardString testNonstandardString= new OSCNonstandardString("test");
            OSCChar testChar = new OSCChar('c');
            OSCColor testColor = new OSCColor(255, 0, 0, 255);
            OSCMIDI testMidi = new OSCMIDI(0, 0x9c, 0x0, 0x7f);
            OSCBool testBool = new OSCBool(true);
            OSCNil testNil = new OSCNil();
            OSCInfinitum testInf = new OSCInfinitum();

            OSCValue[] templateOSCArray = new OSCValue[14] { testInt,testString,testFloat,testBlob,testLong,testTimeTag,testDouble,testNonstandardString,testChar,testColor,testMidi,testBool,testNil,testInf};


            OSCArray testArray= new OSCArray(templateOSCArray);

            OSCValue[] messageArguments= new OSCValue[15] { testArray, testInt, testString, testFloat, testBlob, testLong, testTimeTag, testDouble, testNonstandardString, testChar, testColor, testMidi, testBool, testNil, testInf };

            OSCMessage testMessage=new OSCMessage("/test",messageArguments);

            OSCBundleElement testElement = new OSCBundleElement(testMessage);

            OSCBundleElement[] bundlePayloads= new OSCBundleElement[2] {testElement,testElement};

            OSCBundle packageToSend= new OSCBundle(bundlePayloads);

            while (true)
            {

                long startTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                testServer.SendOSCPackage(packageToSend);
                testServer.WaitForSendCompletion();
                testServer.WaitForOSCPackageReception();
                long endTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                long elapsed=endTime- startTime;
                Console.WriteLine(elapsed.ToString());
                OSCPackage received;
                
                if(testServer.TryReceiveOSCPackage(out received))
                {
                    Console.WriteLine(received.ToString());
                }
                else
                {
                    Console.WriteLine("none received!");
                }

                Thread.Sleep(100);
            }
        }
    }
}
//end example program

i will say that Incoming messages are buffered internally in an ordered queue. Applications should poll frequently enough to prevent unbounded backlog growth. also Timetags are preserved but not scheduled.

enjoy!



copyright 2026 RedCactus5

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 

