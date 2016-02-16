namespace SpeechToTextTest.VoiceRecognition
{
    using System;
    using System.Speech.Recognition;

    public class TurnOffVoiceAction : IVoiceAction
    {
        public const string TurnOffLightsSemanticValue = "TURN_OFF";

        public void ExecuteAction(LightVoiceIdentifier lightIdentifier)
        {
            Console.WriteLine("EXECUTING THE TURN OFF ACTION ON LIGHT ID {0}", lightIdentifier.LightLabelSemanticValue);
        }

        public Choices ToChoices()
        {
            var choices = new Choices();

            foreach (var commandText in LightsGrammars.TurnOffLightCommands)
            {
                choices.Add(new SemanticResultValue(commandText, TurnOffLightsSemanticValue));
            }

            return choices;
        }
    }
}
