using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading.Tasks;

namespace SpeechToTextTest
{
    public interface IGrammarEntity
    {
        Choices ToChoices();
    }
}
