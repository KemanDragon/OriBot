using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;

namespace OriBot.Framework
{
    public class Attributes
    {
        public abstract class PreconditionAttribute : Attribute {
            public abstract bool CheckCondition();
        }

        public class RequireCorrectServerAttribute : PreconditionAttribute
        {
            public override bool CheckCondition()
            {
                return true;
            }
        
        }
    }
}
