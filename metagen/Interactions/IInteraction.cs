using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metagen
{
    public interface IInteraction
    {
        void StartInteracting();
        void StopInteracting();

        //void WaitForFinish();
    }
}
