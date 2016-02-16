namespace SpeechToTextTest.VoiceRecognition
{
    public interface IVoiceAction : IGrammarEntity
    {
        void ExecuteAction(LightVoiceIdentifier lightIdentifier);
    }
}
