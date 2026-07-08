# CactusOSC

a simple, powerful , easy to use c# Open Sound Control sending and receiving library, built entirely in c#, from the ground up, for .NET and c#.



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



just create and start an OSCServer object and you are good to go! messages will automatically be processed and queued, and building messages is easy, just assemble them out of the provided classes.



there is no real documentation or example code as of yet(sorry! its on my todo list!), but it should be understandable with just the types file and some trial and error.



i will say that Incoming messages are buffered internally in an ordered queue. Applications should poll frequently enough to prevent unbounded backlog growth. also Timetags are preserved but not scheduled.

enjoy!

