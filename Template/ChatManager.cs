using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace yandereMod
{
    public static class ChatManager
    {
        static HttpClient client;
        public static bool init_success = false;
        private static string gpt_model;
        public static bool using_gemini = false;
        public static void Init(string api_key, string modelToUse, bool gemini = false)
        {
            try
            {
                if (api_key.Length == 0)
                {
                    throw new ArgumentException("No ChatGPT/Gemini API key!");
                    return;
                }
                client = new HttpClient();
                using_gemini = gemini;
                if (!gemini)
                    client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"Bearer {api_key}");
                else
                    client.DefaultRequestHeaders.TryAddWithoutValidation("x-goog-api-key", api_key);
                gpt_model = modelToUse;
                Console.WriteLine("ChatManager INIT SUCCESS");
                init_success = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("ChatManager INIT FAILED");
                Console.WriteLine(ex.Message);
            }
        }

        public static string SendPromptToGemini(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    contents = new[]
                    {
                    new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                }
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var task = client.PostAsync("https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent", content);
                task.Wait();
                var response = task.Result;
                response.EnsureSuccessStatusCode();

                var task2 = response.Content.ReadAsStringAsync();
                task2.Wait();
                var responseContent = task2.Result;
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

                Console.WriteLine("MESSAGE RECEIVED");
                if (jsonResponse.candidates != null && jsonResponse.candidates.Count > 0 && jsonResponse.candidates[0].content != null && jsonResponse.candidates[0].content.parts != null && jsonResponse.candidates[0].content.parts.Count > 0)
                {
                    return jsonResponse.candidates[0].content.parts[0].text;
                }
                else
                {
                    Console.WriteLine("Error: Could not extract text from response.");
                    return "";
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine("CHAT BROKE");
                Console.WriteLine(ex.ToString());
                return "";
            }
        }

        public static string SendPromptToChatGPT(string prompt)
        {
            try
            {
                var requestBody = new
                {
                    model = gpt_model,
                    messages = new[]
                    {
                        new { role = "user", content = prompt }
                    },
                    max_tokens = 200
                };

                var content = new StringContent(JsonConvert.SerializeObject(requestBody), Encoding.UTF8, "application/json");

                var task = client.PostAsync("https://api.openai.com/v1/chat/completions", content);
                task.Wait();
                var response = task.Result;
                response.EnsureSuccessStatusCode();

                var task2 = response.Content.ReadAsStringAsync();
                task2.Wait();
                var responseContent = task2.Result;
                dynamic jsonResponse = JsonConvert.DeserializeObject(responseContent);

                Console.WriteLine("MESSAGE RECIEVED");
                return jsonResponse.choices[0].message.content;
            }
            catch (Exception ex)
            {
                Console.WriteLine("CHAT BROKE");
                Console.WriteLine(ex.ToString());
                return "";
            }
        }
    }
}
