namespace SpeechToTextTest.VoiceRecognition
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Speech.Recognition;

    public class LightVoiceIdentifier : IVoiceIdentifier
    {
        public const string LightLabelSemanticValueAllLights = "LIGHT_IDENTIFIER_ALL";

        public List<string> LightVoiceLabels { get; set; }

        public string LightLabelSemanticValue { get; set; }

        // TODO if I need to map this to actual ids or something later
        public List<string> LightBulbIds = new List<string>();

        public LightVoiceIdentifier()
        {
        }

        public LightVoiceIdentifier(string semanticValue, List<string> voiceLabels)
        {
            LightLabelSemanticValue = semanticValue;
            LightVoiceLabels = voiceLabels;
            LightBulbIds = null;
        }

        public LightVoiceIdentifier(string semanticValue, List<string> voiceLabels, List<string> ids)
        {
            LightLabelSemanticValue = semanticValue;
            LightVoiceLabels = voiceLabels;
            LightBulbIds = ids;
        }

        public static LightVoiceIdentifier GetAllLightsIdentifier()
        {
            return new LightVoiceIdentifier
            {
                LightLabelSemanticValue = LightLabelSemanticValueAllLights,
                LightVoiceLabels = LightsGrammars.LightIdentifiersAll.ToList(),
                LightBulbIds = null
            };
        }

        public Choices ToChoices()
        {
            if(LightVoiceLabels == null || !LightVoiceLabels.Any())
            {
                throw new Exception("Could not convert light voice identifier to choices because no labels were present.");
            }

            var choices = new Choices();
            choices.Add(LightVoiceLabels.ToArray());
            return choices;
        }
    }
}
