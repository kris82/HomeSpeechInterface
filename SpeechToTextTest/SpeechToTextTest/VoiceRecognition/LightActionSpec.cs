namespace SpeechToTextTest.VoiceRecognition
{
    using System.Collections.Generic;
    using System.Speech.Recognition;
    using System.Linq;
    using System;
    public class LightActionSpec
    {
        private static readonly List<string> LightSubjectLabels = new List<string> { "lights", "light", "lamp", "lamps", "lightswitch", "lightswitches" };

        public const string LightSubjectSemanticValue = "SUBJECT_LIGHTS";

        public IReadOnlyDictionary<string, IVoiceAction> SemanticValueToActionMap { get; private set; }

        public IReadOnlyList<LightVoiceIdentifier> LightIdentifiers { get; private set; }

        private object executeActionLock = new object();

        // TODO consider making these actions configurable.
        public LightActionSpec()
        {
            var semanticValueToActionMap = new Dictionary<string, IVoiceAction>();
            semanticValueToActionMap.Add(TurnOffVoiceAction.TurnOffLightsSemanticValue, new TurnOffVoiceAction());
            semanticValueToActionMap.Add(TurnOnVoiceAction.TurnOnLightsSemanticValue, new TurnOnVoiceAction());
            SemanticValueToActionMap = semanticValueToActionMap;

            var lightIdentifiers = new List<LightVoiceIdentifier>();
            lightIdentifiers.Add(new LightVoiceIdentifier("MASTER_BEDROOM", new List<string> { "master bedroom", "big bedroom", "suite" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("SMALL_BATHROOM", new List<string> { "half bath" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("BIG_BATHROOM", new List<string> { "big bathroom", "main bathroom" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("KITCHEN", new List<string> { "kitchen", "dining room" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("LIVING_ROOM", new List<string> { "living room", "tv room", "entrance" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("GARAGE", new List<string> { "garage", "car port" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("OFFICE", new List<string> { "office", "computer room" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("HALLWAY", new List<string> { "hallway" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("GUEST_BEDROOM", new List<string> { "guest bedroom" }));
            lightIdentifiers.Add(new LightVoiceIdentifier("HEDGEHOG_ROOM", new List<string> { "hedgehog room" }));
            lightIdentifiers.Add(LightVoiceIdentifier.GetAllLightsIdentifier());
            LightIdentifiers = lightIdentifiers;
        }

        public void ExecuteAction(string actionSemanticValue, string identifierSemanticValue)
        {
            if(!SemanticValueToActionMap.ContainsKey(actionSemanticValue))
            {
                throw new ArgumentException(string.Format("Invalid action semantic value {0} found.", actionSemanticValue), "actionSemanticValue");
            }

            if (!LightIdentifiers.Any(id => string.Equals(id.LightLabelSemanticValue, identifierSemanticValue)))
            {
                throw new ArgumentException(string.Format("Invalid light label semantic value {0} found.", identifierSemanticValue), "identifierSemanticValue");
            }

            var action = SemanticValueToActionMap[actionSemanticValue];
            var lightIdentifier = LightIdentifiers.FirstOrDefault(id => string.Equals(id.LightLabelSemanticValue, identifierSemanticValue));

            // lock to make sure that action execution doesn't overlap.
            lock(executeActionLock)
            {
                action.ExecuteAction(lightIdentifier);
            }
        }

        public List<IVoiceAction> GetAllActions()
        {
            return SemanticValueToActionMap.Values.ToList();
        }

        public Choices GetLightSubjectChoices()
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
