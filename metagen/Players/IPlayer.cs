using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace metagen
{
    public interface IPlayer
    {
        bool isPlaying { get; set; }
        void StartPlaying();
        void StopPlaying();
        void PlayStreams();

        //void WaitForFinish();
    }
}
