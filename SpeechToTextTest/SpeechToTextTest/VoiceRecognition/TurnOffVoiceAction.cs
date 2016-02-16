using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextTest.VoiceRecognition
{
    public class TurnOffVoiceAction : IVoiceAction
    {
        public Choices ToChoices()
        {
            return new Choices(LightsGrammars.TurnOffLightCommands.ToArray());
        }
    }
}
