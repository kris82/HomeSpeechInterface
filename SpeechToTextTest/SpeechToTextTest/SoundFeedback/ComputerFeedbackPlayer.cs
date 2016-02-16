namespace SpeechToTextTest.SoundFeedback
{
    using System.Media;

    public class ComputerFeedbackPlayer
    {
        // Lock to ensure I don't play multiple sounds at once.
        public static readonly object soundPlayLock = new object();

        public static void PlayComputerAck()
        {
            PlaySound(@"..\..\Sounds\StarTrekComputerBeepAck.wav");
        }

        public static void PlayComputerInit()
        {
            PlaySound(@"..\..\Sounds\StarTrekComputerBeepInit.wav");
        }

        public static void PlayComputerTimeout()
        {
            PlaySound(@"..\..\Sounds\StarTrekVoiceTimeout.wav");
        }

        public static void PlaySound(string soundLocation)
        {
            lock(soundPlayLock)
            {
                var soundPlayer = new SoundPlayer();
                soundPlayer.SoundLocation = soundLocation;
                soundPlayer.Play();
            }
        }
    }
}
