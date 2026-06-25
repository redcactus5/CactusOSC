using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{
    class invalidBundleException: invalidPackageException
    {

    }
    class unfinshedPackageException : Exception
    {

    }


    class indexAlreadyPopulatedException : Exception
    {

    }
    class unfinishedDataSegmentException:unfinshedPackageException { }

    class invalidPackageException: Exception
    {

    }

    class OSCListNodeReturnTypeMismatchException: Exception
    {

    }
    class invalidOSCStringException : Exception
    {

    }
    class invalidTypestringException: invalidOSCStringException
    {

    }

    class OSCStringNotNullTerminatedException : invalidOSCStringException
    {
    }
}
