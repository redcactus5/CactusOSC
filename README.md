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



i will say that Incoming messages are buffered internally in an ordered queue. Applications should poll frequently enough to prevent unbounded backlog growth. also Timetags are preserved but not scheduled.

enjoy!



copyright 2026 RedCactus5

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 

