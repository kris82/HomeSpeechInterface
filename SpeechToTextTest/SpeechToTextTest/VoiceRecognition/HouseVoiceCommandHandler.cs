using SpeechToTextTest.SoundFeedback;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Speech.Recognition;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeechToTextTest.VoiceRecognition
{
    public class HouseVoiceCommandHandler
    {
        public const string LightIdentifierSemanticKey = "LIGHT_IDENTIFIER";

        public const string LightActionSemanticKey = "LIGHT_ACTION";

        public const string CommandSubjectSemanticKey = "ACTION_SUBJECT";

        public const string InitiateCommandsPhrase = "computer";

        public const string CancelProgramCommand = "cancel override";

        private const int CommandSilenceTimeout = 5000;

        private object checkLastSpeechLock = new object();
        
        // Do not access this variable directly, it should be synchronized access through methods.
        public DateTime LastSpeechTime { get; set; }


        private object stateLock = new object();
        private bool _commandInitiated;
        public bool CommandInitiated
        {
            get
            {
                lock (stateLock)
                {
                    return _commandInitiated;
                }
            }
            set
            {
                lock (stateLock)
                {
                    _commandInitiated = value;
                }
            }
        }

        public CancellationTokenSource speechCancellationTokenSource { get; set; }

        public HouseVoiceCommandHandler()
        {
            CommandInitiated = false;
            LastSpeechTime = DateTime.MinValue;
        }

        public Task InitiateSpeechRecognition()
        {
            speechCancellationTokenSource = new CancellationTokenSource();

            var initiateCommandGrammerBuilder = new GrammarBuilder(InitiateCommandsPhrase);
            var cancelOverrideGrammarBuilder = new GrammarBuilder(CancelProgramCommand);
            var lightGrammerBuilder = CreateLightCommandGrammar();
            var finalGrammarBuilder = CombineGrammarBuilders(initiateCommandGrammerBuilder, cancelOverrideGrammarBuilder, lightGrammerBuilder);

            var recognizerInfo = SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault(ri => ri.Culture.TwoLetterISOLanguageName.Equals("en"));

            var speechRecognitionTask = Task.Run(() => RunSpeechRecognizer(finalGrammarBuilder, recognizerInfo));
            return speechRecognitionTask;
        }

        private void RunSpeechRecognizer(GrammarBuilder finalGrammarBuilder, RecognizerInfo recognizerInfo)
        {
            using (SpeechRecognitionEngine recognizer = new SpeechRecognitionEngine(recognizerInfo))
            {
                //recognizer.LoadGrammar(grammar);
                recognizer.LoadGrammar(new Grammar(finalGrammarBuilder));
                recognizer.SpeechRecognized += Engine_SpeechRecognized;

                // Configure input to the speech recognizer.
                recognizer.SetInputToDefaultAudioDevice();

                // Start asynchronous, continuous speech recognition.
                recognizer.RecognizeAsync(RecognizeMode.Multiple);

                while(!speechCancellationTokenSource.Token.IsCancellationRequested)
                {
                    CheckCommandTimeout();
                }

                Console.WriteLine("Cancelling Speech Recognizer Task.");
            }
        }

        private void CheckCommandTimeout()
        {
            if (CommandInitiated && IsSpeechTimeout(DateTime.UtcNow))
            {
                Console.WriteLine("Command timeout has occurred since the last command, resetting command initiated to false.");
                CommandInitiated = false;
                ComputerFeedbackPlayer.PlayComputerTimeout();
            }
        }

        private void Engine_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            if (string.Equals(e.Result.Text, InitiateCommandsPhrase, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Acknowledged...");
                ComputerFeedbackPlayer.PlayComputerInit();
                CommandInitiated = true;
            }
            else if(string.Equals(e.Result.Text, CancelProgramCommand, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Program Quit Detected.");
                speechCancellationTokenSource.Cancel();
            }
            else
            {
                if(!CommandInitiated)
                {
                    return;
                }

                var semantics = e.Result.Semantics;

                if (!semantics.ContainsKey(CommandSubjectSemanticKey))
                {
                    Console.WriteLine("Grammar recognized but no subject was found on which to take an action.");
                    //throw new Exception("Grammar recognized but no subject was found on which to take an action.");
                }

                var subject = e.Result.Semantics[CommandSubjectSemanticKey];

                if (Equals(subject.Value, LightVoiceSubject.LightSubjectSemanticValue))
                {
                    ComputerFeedbackPlayer.PlayComputerAck();

                    if (!semantics.ContainsKey(LightIdentifierSemanticKey) || !semantics.ContainsKey(LightActionSemanticKey))
                    {
                        Console.WriteLine("A command with the light subject must contain a light identifier and action semantic.");
                        //throw new Exception("A command with the light subject must contain a light identifier and action semantic.");
                    }

                    var identifier = e.Result.Semantics[LightIdentifierSemanticKey];
                    var action = e.Result.Semantics[LightActionSemanticKey];

                    Console.WriteLine("Grammer match: {0}", e.Result.Text);
                    Console.WriteLine("subject:{0}, action:{1}, identifier:{2}", subject.Value, action.Value, identifier.Value);
                    Console.WriteLine("With Confidence {0}", e.Result.Confidence);
                }
                else
                {
                    Console.WriteLine("Subject value {0} was found but can not be bound to an action", subject.Value);
                    //throw new Exception(string.Format("Subject value {0} was found but can not be bound to an action", subject.Value));
                }
            }

            // Do not share a variable with the above code, this is to protect against long processing times.
            SetNewSpeechTime(DateTime.UtcNow);
        }

        private void SetNewSpeechTime(DateTime curDateTime)
        {
            lock(checkLastSpeechLock)
            {
                LastSpeechTime = curDateTime;
            }
        }

        private bool IsSpeechTimeout(DateTime curDateTime)
        {
            lock (checkLastSpeechLock)
            {
                var elapsedTicks = curDateTime.Ticks - LastSpeechTime.Ticks;
                var elapsedSpan = new TimeSpan(elapsedTicks);

                return elapsedSpan.TotalMilliseconds >= CommandSilenceTimeout;
            }
        }

        public static GrammarBuilder CombineGrammarBuilders(params GrammarBuilder[] grammars)
        {
            if(grammars.Length < 1)
            {
                throw new ArgumentException("Must pass in at least one grammar to combine.");
            }

            var choices = new Choices(grammars);
            return new GrammarBuilder(choices);
        }

        private static GrammarBuilder ConvertIdentyListToGrammarBuilder(List<LightVoiceIdentifier> identifiers)
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

        private static GrammarBuilder CreateLightCommandGrammar()
        {
            var houseLightingModel = new List<LightVoiceIdentifier>();
            houseLightingModel.Add(new LightVoiceIdentifier("MASTER_BEDROOM", new List<string> { "master bedroom", "big bedroom", "suite" }));
            houseLightingModel.Add(new LightVoiceIdentifier("SMALL_BATHROOM", new List<string> { "half bath" }));
            houseLightingModel.Add(new LightVoiceIdentifier("BIG_BATHROOM", new List<string> { "big bathroom", "main bathroom" }));
            houseLightingModel.Add(new LightVoiceIdentifier("KITCHEN", new List<string> { "kitchen", "dining room" }));
            houseLightingModel.Add(new LightVoiceIdentifier("LIVING_ROOM", new List<string> { "living room", "tv room", "entrance" }));
            houseLightingModel.Add(new LightVoiceIdentifier("GARAGE", new List<string> { "garage", "car port" }));
            houseLightingModel.Add(new LightVoiceIdentifier("OFFICE", new List<string> { "office", "computer room" }));
            houseLightingModel.Add(new LightVoiceIdentifier("HALLWAY", new List<string> { "hallway" }));
            houseLightingModel.Add(new LightVoiceIdentifier("GUEST_BEDROOM", new List<string> { "guest bedroom" }));
            houseLightingModel.Add(new LightVoiceIdentifier("HEDGEHOG_ROOM", new List<string> { "hedgehog room" }));
            houseLightingModel.Add(LightVoiceIdentifier.GetAllLightsIdentifier());

            var lightSubject = new LightVoiceSubject();
            var turnOnAction = new TurnOnVoiceAction();
            var turnOffAction = new TurnOffVoiceAction();

            var allLightsIdentifierChoices = ConvertIdentyListToGrammarBuilder(houseLightingModel);

            var turnOnActionChoices = turnOnAction.ToChoices();
            var turnOffActionChoices = turnOffAction.ToChoices();
            var lightsLables = lightSubject.ToChoices();

            // Action 1: Turn on lights, all
            var turnOnAllLightsGrammar = new GrammarBuilder();
            turnOnAllLightsGrammar.AppendWildcard();
            turnOnAllLightsGrammar.Append(new SemanticResultKey(LightActionSemanticKey, turnOnActionChoices));
            turnOnAllLightsGrammar.AppendWildcard();
            turnOnAllLightsGrammar.Append(new SemanticResultKey(LightIdentifierSemanticKey, allLightsIdentifierChoices));
            turnOnAllLightsGrammar.AppendWildcard();
            turnOnAllLightsGrammar.Append(new SemanticResultKey(CommandSubjectSemanticKey, lightsLables));

            var turnOnAllLightsGrammar2 = new GrammarBuilder();
            turnOnAllLightsGrammar2.AppendWildcard();
            turnOnAllLightsGrammar2.Append(new SemanticResultKey(LightActionSemanticKey, turnOnActionChoices));
            turnOnAllLightsGrammar2.AppendWildcard();
            turnOnAllLightsGrammar2.Append(new SemanticResultKey(CommandSubjectSemanticKey, lightsLables));
            turnOnAllLightsGrammar2.AppendWildcard();
            turnOnAllLightsGrammar2.Append(new SemanticResultKey(LightIdentifierSemanticKey, allLightsIdentifierChoices));

            // Action 2: Turn off lights, all
            var turnOffAllLightsGrammer = new GrammarBuilder();
            turnOffAllLightsGrammer.AppendWildcard();
            turnOffAllLightsGrammer.Append(new SemanticResultKey(LightActionSemanticKey, turnOffActionChoices));
            turnOffAllLightsGrammer.AppendWildcard();
            turnOffAllLightsGrammer.Append(new SemanticResultKey(LightIdentifierSemanticKey, allLightsIdentifierChoices));
            turnOffAllLightsGrammer.AppendWildcard();
            turnOffAllLightsGrammer.Append(new SemanticResultKey(CommandSubjectSemanticKey, lightsLables));

            var turnOffAllLightsGrammer2 = new GrammarBuilder();
            turnOffAllLightsGrammer2.AppendWildcard();
            turnOffAllLightsGrammer2.Append(new SemanticResultKey(LightActionSemanticKey, turnOffActionChoices));
            turnOffAllLightsGrammer2.AppendWildcard();
            turnOffAllLightsGrammer2.Append(new SemanticResultKey(CommandSubjectSemanticKey, lightsLables));
            turnOffAllLightsGrammer2.AppendWildcard();
            turnOffAllLightsGrammer2.Append(new SemanticResultKey(LightIdentifierSemanticKey, allLightsIdentifierChoices));

            var allLightsChoices = new Choices();
            allLightsChoices.Add(
                turnOnAllLightsGrammar,
                turnOnAllLightsGrammar2,
                turnOffAllLightsGrammer,
                turnOffAllLightsGrammer2);
            var finalLightsGrammerBuilder = new GrammarBuilder();
            finalLightsGrammerBuilder.Append(allLightsChoices);

            return finalLightsGrammerBuilder;
        }

    }
}
