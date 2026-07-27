using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public enum ReturnCodes
    {
        Success = 0,
        NotFound = 404,
        AlreadyExists = 409,
        ValidationError = 400,
        Unauthorized = 401,
        InternalError = 500
    }
}
