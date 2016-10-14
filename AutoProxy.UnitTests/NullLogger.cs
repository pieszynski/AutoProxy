using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AutoProxy.UnitTests
{
    class NullLogger : IUnitLogger
    {
        public void Info(string text)
        {
            Console.WriteLine(text);
        }
    }
}
