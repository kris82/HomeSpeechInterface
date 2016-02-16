namespace SpeechToTextTest.VoiceRecognition
{
    using System.Collections.Generic;
    using System.Speech.Recognition;

    public class LightVoiceSubject : IGrammarEntity
    {
        private readonly List<string> LightSubjectLabels = new List<string> { "lights", "light", "lamp", "lamps", "lightswitch", "lightswitches" };

        public const string LightSubjectSemanticValue = "SUBJECT_LIGHTS";

        public Choices ToChoices()
        {
            var choices = new Choices();
            foreach (var commandText in LightSubjectLabels)
            {
                choices.Add(new SemanticResultValue(commandText, LightSubjectSemanticValue));
            }
            return choices;
        }
    }
}
