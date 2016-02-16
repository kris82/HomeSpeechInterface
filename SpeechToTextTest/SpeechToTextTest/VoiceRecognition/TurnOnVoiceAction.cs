namespace SpeechToTextTest.VoiceRecognition
{
    using System;
    using System.Speech.Recognition;

    public class TurnOnVoiceAction : IVoiceAction
    {
        public const string TurnOnLightsSemanticValue = "TURN_ON";

        public void ExecuteAction(LightVoiceIdentifier lightIdentifier)
        {
            Console.WriteLine("EXECUTING THE TURN ON ACTION ON LIGHT ID {0}", lightIdentifier.LightLabelSemanticValue);
        }

        public Choices ToChoices()
        {
            var choices = new Choices();

            foreach(var commandText in LightsGrammars.TurnOnLightCommands)
            {
                choices.Add(new SemanticResultValue(commandText, TurnOnLightsSemanticValue));
            }

            return choices;
        }
    }
}
