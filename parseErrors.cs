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

    class invalidPackageException: Exception
    {

    }

    class OSCListNodeReturnTypeMismatchException: Exception
    {

    }
    class invalidOSCStringException : invalidPackageException
    {

    }
    class invalidTypestringException: invalidOSCStringException
    {

    }
}
