namespace SpeechToTextTest
{
    using SpeechToTextTest.VoiceRecognition;
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
