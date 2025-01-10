using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightNet
{


    public class IllegalDataException: Exception
    {
        public IllegalDataException(String message):base(message)
        {
            
        }
    }
        
       
}
