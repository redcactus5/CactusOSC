using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{
    class parserStuckException: InvalidBundleException
    {

    }
    class InvalidBundleException: InvalidPackageException
    {

    }
    
    class IncompleteBundleException: InvalidBundleException
    {

    }

    class IndexAlreadyPopulatedException : Exception
    {

    }
    class UnfinishedDataSegmentException:InvalidPackageException { }

    class InvalidPackageException: Exception
    {

    }

    class IncompleteOSCDataException: InvalidPackageException
    {

    }
    class InvalidOSCDataException : InvalidPackageException
    {

    }
    class InvalidOSCAddressException : InvalidPackageException
    {
    }
    class OSCListNodeReturnTypeMismatchException: Exception
    {

    }
    class InvalidOSCStringException : InvalidPackageException
    {

    }
    class InvalidTypestringException: InvalidOSCStringException
    {

    }

    class OSCStringNotNullTerminatedException : InvalidOSCStringException
    {
    }
}
