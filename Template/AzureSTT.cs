using System;
using System.Collections.Generic;
using System.Text;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Data;
using NAudio.CoreAudioApi;
using Unity.Netcode;
using NAudio.Wave;

namespace yandereMod
{
    class AzureSTT
    {
        public static int num_gens = 0;
        public static bool is_init = false;
        public static string System_Prompt = "You are a monster in the game Lethal Company that looks like a highschool girl who is narcissistic and quirky. " +
            "Your goal is to provide an engaging gameplay experience by determining if a player should be let go or kept by the monster you are playing. " +
            "When the player you are judging speaks to you, reply simply with a numeric score from 0 to 100 that represents your current judgement based on what they said, " +
            "with 0 being keep and 100 being let go. Appealing to your beauty or attractiveness should result in a high score (even if it is distasteful), " +
            "and simple requests like asking to let go should result in a low score.";
        public static SpeechRecognizer speechRecognizer;
        public static TaskCompletionSource<int> stopRecognition = new TaskCompletionSource<int>();
        async static Task FromMic()
        {
            try
            {
                Console.WriteLine("Yandere: Start transcribing audio...");


                //var stopRecognition = new TaskCompletionSource<int>();

                speechRecognizer.Recognizing += (s, e) =>
                {
                    //Console.WriteLine($"RECOGNIZING: Text={e.Result.Text}");
                };

                speechRecognizer.Recognized += (s, e) =>
                {
                    if (e.Result.Reason == ResultReason.RecognizedSpeech)
                    {
                        if (e.Result.Text.Length > 1)
                        {
                            Console.WriteLine($"RECOGNIZED: Text={e.Result.Text}");

                            if (!ChatManager.init_success) return;

                            string response;
                            if (!ChatManager.using_gemini)
                                response = ChatManager.SendPromptToChatGPT(System_Prompt + $"" +
                                    $"\n\nYou have already heard {num_gens} statements from the player. " +
                                    $"Your judgements should get exponentially further from 50 (higher or lower) the more statements the player has made. " +
                                    $"A final judgement is made when your score is lower than 20 or higher than 80. " +
                                    $"If this value: {num_gens} < 2, your score CANNOT be decisive (lower than 20 or higher than 80). " +
                                    $"Only respond with your numeric score, nothing else.\n\n" +
                                    $"Player: " + e.Result.Text);
                            else
                                response = ChatManager.SendPromptToGemini(System_Prompt + $"" +
                                    $"\n\nYou have already heard {num_gens} statements from the player. " +
                                    $"Your judgements should get exponentially further from 50 (higher or lower) the more statements the player has made. " +
                                    $"If this value: {num_gens} < 2, your score CANNOT be decisive (lower than 20 or higher than 80). " +
                                    $"A final judgement is made when your score is lower than 20 or higher than 80. " +
                                    $"Only respond with your numeric score, nothing else.\n\n" +
                                    $"Player: " + e.Result.Text);

                            Console.WriteLine("RESPONSE: " + response);
                            num_gens++;

                            if (TiedPlayerManager.instance != null)
                            {
                                int response_int = Int32.Parse(response);
                                if (response_int <= 40)
                                {
                                    TiedPlayerManager.instance.ShowText("not... Senpai...");
                                    TiedPlayerManager.instance.heartbeat.volume = 1f;
                                    TiedPlayerManager.instance.heartbeat.pitch = 1.3f;
                                }

                                if (response_int > 40 && response_int < 60)
                                {
                                    TiedPlayerManager.instance.ShowText("...");
                                    TiedPlayerManager.instance.heartbeat.volume = 0.9f;
                                    TiedPlayerManager.instance.heartbeat.pitch = 1f;
                                }

                                if (response_int >= 60)
                                {
                                    TiedPlayerManager.instance.ShowText("... good...");
                                    TiedPlayerManager.instance.heartbeat.volume = 0.8f;
                                    TiedPlayerManager.instance.heartbeat.pitch = 0.75f;
                                }

                                if (response_int < 20 && num_gens >= 2)
                                    TiedPlayerManager.instance.Kill(true);

                                if (response_int > 80 && num_gens >= 2)
                                    TiedPlayerManager.instance.Kill(false);
                            }
                        }
                    }
                    else if (e.Result.Reason == ResultReason.NoMatch)
                    {
                        Console.WriteLine($"NOMATCH: Speech could not be recognized.");
                    }
                };

                speechRecognizer.Canceled += (s, e) =>
                {
                    Console.WriteLine($"CANCELED: Reason={e.Reason}");

                    if (e.Reason == CancellationReason.Error)
                    {
                        Console.WriteLine($"CANCELED: ErrorCode={e.ErrorCode}");
                        Console.WriteLine($"CANCELED: ErrorDetails={e.ErrorDetails}");
                        Console.WriteLine($"CANCELED: Did you set the speech resource key and region values?");
                    }

                    stopRecognition.TrySetResult(0);
                };

                speechRecognizer.SessionStopped += (s, e) =>
                {
                    Console.WriteLine("\n    Session stopped event.");
                    stopRecognition.TrySetResult(0);
                };

                await speechRecognizer.StartContinuousRecognitionAsync();

                Task.WaitAny(new[] { stopRecognition.Task });

                //await speechRecognizer.StopContinuousRecognitionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine("STT BROKE");
                Console.WriteLine(ex.ToString());
            }
        }

        public async static Task Main(string prompt = "")
        {



            //Console.WriteLine("IN MAIN");
            try
            {
                if (prompt.Length > 0)
                    System_Prompt = prompt;
                await FromMic();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static void Init(string api_key, string region, string language)
        {
            if (api_key.Length == 0)
            {
                Console.WriteLine("No azure API key. STT disabled.");
                return;
            }

            if (is_init)
                return;

            yandereAI.WriteToConsole(IngamePlayerSettings.Instance.settings.micDevice);

            //using var audioConfig = AudioConfig.FromDefaultMicrophoneInput();
            using var audioConfig = AudioConfig.FromMicrophoneInput(GetAudioDevices(IngamePlayerSettings.Instance.settings.micDevice));
            var speechConfig = SpeechConfig.FromSubscription(api_key, region);
            speechConfig.SpeechRecognitionLanguage = language;
            speechRecognizer = new SpeechRecognizer(speechConfig, audioConfig);
            is_init = true;
            
        }


        static string GetAudioDevices(string deviceName = "")
        {
            var enumerator = new MMDeviceEnumerator();
            foreach (var endpoint in
            enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active))
            {
                if (deviceName.Length != 0 && endpoint.FriendlyName == deviceName)
                    return endpoint.ID;
            }

            return "";
        }
    }
}
