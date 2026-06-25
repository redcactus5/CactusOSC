using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{
    class invalidBundleException: invalidPackageException
    {

    }
    


    class indexAlreadyPopulatedException : Exception
    {

    }
    class unfinishedDataSegmentException:invalidPackageException { }

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
