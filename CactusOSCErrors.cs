
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
        public serverAlreadyStartedException()
        {
        }

        public serverAlreadyStartedException(string message)
            : base(message)
        {
        }

        public serverAlreadyStartedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    public class serverNotStartedException: Exception
    {
        public serverNotStartedException()
        {
        }

        public serverNotStartedException(string message)
            : base(message)
        {
        }

        public serverNotStartedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    class parserStuckException: InvalidBundleException
    {
        public parserStuckException()
        {
        }

        public parserStuckException(string message)
            : base(message)
        {
        }

        public parserStuckException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    class InvalidBundleException: InvalidPackageException
    {
        public InvalidBundleException()
        {
        }

        public InvalidBundleException(string message)
            : base(message)
        {
        }

        public InvalidBundleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    
    class IncompleteBundleException: InvalidBundleException
    {
        public IncompleteBundleException()
        {
        }

        public IncompleteBundleException(string message)
            : base(message)
        {
        }

        public IncompleteBundleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    class IndexAlreadyPopulatedException : Exception
    {
        public IndexAlreadyPopulatedException()
        {
        }

        public IndexAlreadyPopulatedException(string message)
            : base(message)
        {
        }

        public IndexAlreadyPopulatedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    

    class InvalidPackageException: Exception
    {
        public InvalidPackageException()
        {
        }

        public InvalidPackageException(string message)
            : base(message)
        {
        }

        public InvalidPackageException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    class IncompleteOSCDataException: InvalidPackageException
    {
        public IncompleteOSCDataException()
        {
        }

        public IncompleteOSCDataException(string message)
            : base(message)
        {
        }

        public IncompleteOSCDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    class InvalidOSCDataException : InvalidPackageException
    {
        public InvalidOSCDataException()
        {
        }

        public InvalidOSCDataException(string message)
            : base(message)
        {
        }

        public InvalidOSCDataException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    class InvalidOSCAddressException : InvalidPackageException
    {
        public InvalidOSCAddressException()
        {
        }

        public InvalidOSCAddressException(string message)
            : base(message)
        {
        }

        public InvalidOSCAddressException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    class OSCListNodeReturnTypeMismatchException: Exception
    {
        public OSCListNodeReturnTypeMismatchException()
        {
        }

        public OSCListNodeReturnTypeMismatchException(string message)
            : base(message)
        {
        }

        public OSCListNodeReturnTypeMismatchException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    class InvalidOSCStringException : InvalidPackageException
    {
        public InvalidOSCStringException()
        {
        }

        public InvalidOSCStringException(string message)
            : base(message)
        {
        }

        public InvalidOSCStringException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    class InvalidTypestringException: InvalidOSCStringException
    {
        public InvalidTypestringException()
        {
        }

        public InvalidTypestringException(string message)
            : base(message)
        {
        }

        public InvalidTypestringException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }

    class OSCStringNotNullTerminatedException : InvalidOSCStringException
    {
        public OSCStringNotNullTerminatedException()
        {
            
        }

        public OSCStringNotNullTerminatedException(string message)
            : base(message)
        {
        }

        public OSCStringNotNullTerminatedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
    public class SizeAlreadySetException : Exception
    {
        public SizeAlreadySetException()
        {
        }

        public SizeAlreadySetException(string message)
            : base(message)
        {
        }

        public SizeAlreadySetException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    public class InvalidOSCValueTypeException : Exception
    {
        public InvalidOSCValueTypeException()
        {
        }

        public InvalidOSCValueTypeException(string message)
            : base(message)
        {
        }

        public InvalidOSCValueTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

    class RecursiveListException : InvalidPackageException
    {
        public RecursiveListException()
        {
        }

        public RecursiveListException(string message)
            : base(message)
        {
        }

        public RecursiveListException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }
    class RecursiveBundleException : InvalidPackageException
    {
        public RecursiveBundleException()
        {
        }

        public RecursiveBundleException(string message)
            : base(message)
        {
        }

        public RecursiveBundleException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

    }

}
