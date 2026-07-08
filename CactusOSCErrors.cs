
/*
copyright 2026 Redcactus5
This file is part of CactusOSC.

CactusOSC is free software: you can redistribute it and/or modify it under the terms of the GNU Lesser General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.

CactusOSC is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.

You should have received a copy of the GNU Lesser General Public License along with CactusOSC. If not, see <https://www.gnu.org/licenses/>. 
*/
using System;
using System.Collections.Generic;
using System.Text;

namespace CactusOSC
{
    public class serverAlreadyStartedException: Exception
    {

    }
    public class serverNotStartedException: Exception
    {

    }
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
    public class SizeAlreadySetException : Exception
    {

    }
}
