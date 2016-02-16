namespace SpeechToTextTest
{
    using System;
    using System.Collections.Generic;
    using System.Speech.Recognition;
    using VoiceRecognition;

    public static class GrammarHelper
    {
        public static GrammarBuilder ConvertIdentyListToGrammarBuilder(IList<LightVoiceIdentifier> identifiers)
        {
            List<GrammarBuilder> grammarBuilders = new List<GrammarBuilder>();
            foreach (var identifier in identifiers)
            {
                var labels = identifier.LightVoiceLabels;
                var choices = new Choices();
                foreach (var label in labels)
                {
                    choices.Add(new SemanticResultValue(label, identifier.LightLabelSemanticValue));
                }

                grammarBuilders.Add(new GrammarBuilder(choices));
            }

            return new GrammarBuilder(new Choices(grammarBuilders.ToArray()));
        }

        public static GrammarBuilder CombineGrammarBuilders(params GrammarBuilder[] grammars)
        {
            if (grammars.Length < 1)
            {
                throw new ArgumentException("Must pass in at least one grammar to combine.");
            }

            var choices = new Choices(grammars);
            return new GrammarBuilder(choices);
        }
    }
}
