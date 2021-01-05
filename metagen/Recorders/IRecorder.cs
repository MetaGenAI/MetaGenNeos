using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metagen
{
    public interface IRecorder
    {
        void StartRecording();
        void StopRecording();

        void WaitForFinish();
    }
}
