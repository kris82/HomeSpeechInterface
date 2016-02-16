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
        private DateTime LastSpeechTime { get; set; }

        private LightActionSpec LightActionSpec { get; set; }

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
            LightActionSpec = new LightActionSpec();
        }

        public Task InitiateSpeechRecognition()
        {
            speechCancellationTokenSource = new CancellationTokenSource();

            var initiateCommandGrammerBuilder = new GrammarBuilder(InitiateCommandsPhrase);
            var cancelOverrideGrammarBuilder = new GrammarBuilder(CancelProgramCommand);
            var lightGrammerBuilder = CreateLightCommandGrammar(LightActionSpec);
            var finalGrammarBuilder = GrammarHelper.CombineGrammarBuilders(initiateCommandGrammerBuilder, cancelOverrideGrammarBuilder, lightGrammerBuilder);

            var recognizerInfo = SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault(ri => ri.Culture.TwoLetterISOLanguageName.Equals("en"));
            return Task.Run(() => RunSpeechRecognizer(finalGrammarBuilder, recognizerInfo)); ;
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
            // Handle special Action Start.
            if (string.Equals(e.Result.Text, InitiateCommandsPhrase, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Acknowledged...");
                ComputerFeedbackPlayer.PlayComputerInit();
                CommandInitiated = true;
            }
            // Handle special action Cancel Override.
            else if(string.Equals(e.Result.Text, CancelProgramCommand, StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Program Quit Detected.");
                speechCancellationTokenSource.Cancel();
            }
            else
            {
                if(!CommandInitiated)
                {
                    // Ignore matching commands if Commands are not initiated by start command.
                    return;
                }

                var semantics = e.Result.Semantics;

                if (!semantics.ContainsKey(CommandSubjectSemanticKey))
                {
                    Console.WriteLine("Grammar recognized but no subject was found on which to take an action.");
                    //throw new Exception("Grammar recognized but no subject was found on which to take an action.");
                }

                var subject = e.Result.Semantics[CommandSubjectSemanticKey];

                // TODO this should be configured not hardcoded for when there are more than just light actions.
                if (Equals(subject.Value, LightActionSpec.LightSubjectSemanticValue))
                {
                    ComputerFeedbackPlayer.PlayComputerAck();

                    if (!semantics.ContainsKey(LightIdentifierSemanticKey) || !semantics.ContainsKey(LightActionSemanticKey))
                    {
                        Console.WriteLine("A command with the light subject must contain a light identifier and action semantic.");
                        //throw new Exception("A command with the light subject must contain a light identifier and action semantic.");
                    }

                    // TODO consider also making this configured by the action spec itself.  Something like expected Semantic Keys.
                    var identifier = e.Result.Semantics[LightIdentifierSemanticKey];
                    var action = e.Result.Semantics[LightActionSemanticKey];

                    LightActionSpec.ExecuteAction((string)action.Value, (string)identifier.Value);

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

        private static GrammarBuilder CreateLightCommandGrammar(LightActionSpec lightActionSpec)
        {
            var allLightsIdentifierChoices = GrammarHelper.ConvertIdentyListToGrammarBuilder(lightActionSpec.LightIdentifiers.ToList());
            var lightActions = lightActionSpec.GetAllActions();
            var lightsLables = lightActionSpec.GetLightSubjectChoices();

            var actionGrammarBuilders = new List<GrammarBuilder>();
            foreach(var action in lightActions)
            {
                var actionChoices = action.ToChoices();

                var actionGrammer1 = new GrammarBuilder();
                actionGrammer1.AppendWildcard();
                actionGrammer1.Append(new SemanticResultKey(LightActionSemanticKey, actionChoices));
                actionGrammer1.AppendWildcard();
                actionGrammer1.Append(new SemanticResultKey(LightIdentifierSemanticKey, allLightsIdentifierChoices));
                actionGrammer1.AppendWildcard();
                actionGrammer1.Append(new SemanticResultKey(CommandSubjectSemanticKey, lightsLables));

                var actionGrammar2 = new GrammarBuilder();
                actionGrammar2.AppendWildcard();
                actionGrammar2.Append(new SemanticResultKey(LightActionSemanticKey, actionChoices));
                actionGrammar2.AppendWildcard();
                actionGrammar2.Append(new SemanticResultKey(CommandSubjectSemanticKey, lightsLables));
                actionGrammar2.AppendWildcard();
                actionGrammar2.Append(new SemanticResultKey(LightIdentifierSemanticKey, allLightsIdentifierChoices));

                actionGrammarBuilders.Add(actionGrammer1);
                actionGrammarBuilders.Add(actionGrammar2);
            }

            return new GrammarBuilder(new Choices(actionGrammarBuilders.ToArray()));
        }
    }
}
