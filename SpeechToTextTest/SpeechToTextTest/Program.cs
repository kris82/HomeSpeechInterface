namespace SpeechToTextTest
{
    using System;
    using System.Speech.Recognition;
    using System.Threading;
    using System.Linq;
    using SpeechToTextTest.VoiceRecognition;
    using System.Collections.Generic;
    public class Program
    {
        

        public static void Main(string[] args)
        {
            var commandHandler = new HouseVoiceCommandHandler();
            var speechTask = commandHandler.InitiateSpeechRecognition();
            speechTask.Wait();
        }

       
    }
}
