using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Digi.Shared.Helper
{
    public static class GlobalFunction
    {
        public static List<string> ExtractAllInnerMessages(Exception ex)
        {
            var errors = new List<string>();
            while (ex != null)
            {
                errors.Add(ex.Message);
                ex = ex.InnerException;
            }
            return errors;
        }

    }
}
