using Microsoft.AspNetCore.Mvc;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebApplication1.Controllers
{
    public class ChatController : Controller
    {
        private readonly ILogger<ChatController> _logger;
        private readonly HttpClient _httpClient;

        public ChatController(ILogger<ChatController> logger, IHttpClientFactory httpClientFactory)
        {
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient();
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
        {
            try
            {
                var response = await ProcessChatMessage(message.Message);
                return Json(new { success = true, response = response });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing chat message");
                return Json(new { success = false, error = "Failed to process message" });
            }
        }

        private async Task<string> ProcessChatMessage(string message)
        {
            // Add delay for realistic typing effect
            await Task.Delay(Random.Shared.Next(800, 1500));
            
            try
            {
                // Try Hugging Face API first
                var apiUrl = "https://api-inference.huggingface.co/models/microsoft/DialoGPT-medium";
                
                var requestBody = new
                {
                    inputs = $"Medical assistant specializing in dermatology: I help with skin conditions and health. User: {message}",
                    parameters = new
                    {
                        max_length = 150,
                        temperature = 0.6,
                        return_full_text = false
                    }
                };

                var json = JsonSerializer.Serialize(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(apiUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var result = JsonSerializer.Deserialize<HuggingFaceResponse[]>(responseContent);
                    
                    if (result != null && result.Length > 0 && !string.IsNullOrEmpty(result[0].generated_text))
                    {
                        var aiResponse = result[0].generated_text;
                        return ProcessMedicalResponse(aiResponse, message);
                    }
                }
                
                // Fallback to enhanced rule-based responses
                return GetAdvancedRuleBasedResponse(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Hugging Face API");
                return GetAdvancedRuleBasedResponse(message);
            }
        }

        private string ProcessMedicalResponse(string aiResponse, string originalMessage)
        {
            var processedResponse = aiResponse.Trim();
            
            // Add context-specific tips
            if (originalMessage.ToLower().Contains("skin") || originalMessage.ToLower().Contains("rash"))
            {
                processedResponse += "\n\n💡 **Tip**: For accurate skin diagnosis, try our AI Image Analysis feature!";
            }
            
            if (originalMessage.ToLower().Contains("urgent") || originalMessage.ToLower().Contains("emergency"))
            {
                processedResponse += "\n\n🚨 **Important**: For urgent symptoms, please contact emergency services or visit your nearest hospital.";
            }
            
            processedResponse += "\n\n⚠️ **Medical Disclaimer**: This information is for educational purposes only. Always consult a healthcare professional for proper diagnosis and treatment.";
            
            return processedResponse;
        }

        private string GetAdvancedRuleBasedResponse(string message)
        {
            message = message.ToLower().Trim();
            
            // Handle greetings and basic interactions
            if (IsGreeting(message))
                return GetGreetingResponse();
            
            // Handle thanks and appreciation
            if (IsThanking(message))
                return GetThankingResponse();
            
            // Handle emergencies
            if (IsEmergency(message))
                return GetEmergencyResponse();
            
            // Handle symptom combinations
            var symptomResponse = GetSymptomBasedResponse(message);
            if (!string.IsNullOrEmpty(symptomResponse))
                return symptomResponse;
            
            // Handle specific conditions
            var conditionResponse = GetConditionBasedResponse(message);
            if (!string.IsNullOrEmpty(conditionResponse))
                return conditionResponse;
            
            // Handle treatment and care questions
            var treatmentResponse = GetTreatmentBasedResponse(message);
            if (!string.IsNullOrEmpty(treatmentResponse))
                return treatmentResponse;
            
            // Handle age and demographic specific questions
            var demographicResponse = GetDemographicBasedResponse(message);
            if (!string.IsNullOrEmpty(demographicResponse))
                return demographicResponse;
            
            // Handle prevention questions
            var preventionResponse = GetPreventionBasedResponse(message);
            if (!string.IsNullOrEmpty(preventionResponse))
                return preventionResponse;
            
            // Default helpful response
            return GetDefaultHelpfulResponse();
        }

        private bool IsGreeting(string message)
        {
            string[] greetings = { "hello", "hi", "hey", "good morning", "good afternoon", "good evening", "greetings" };
            return greetings.Any(g => message.Contains(g));
        }

        private bool IsThanking(string message)
        {
            string[] thanks = { "thank", "thanks", "appreciate", "helpful", "great", "awesome" };
            return thanks.Any(t => message.Contains(t));
        }

        private bool IsEmergency(string message)
        {
            string[] emergencyWords = { "emergency", "urgent", "severe pain", "bleeding", "can't breathe", "allergic reaction", "swelling face" };
            return emergencyWords.Any(e => message.Contains(e));
        }

        private string GetGreetingResponse()
        {
            var greetings = new[]
            {
                "Hello! 👋 I'm your AI dermatology assistant. I'm here to help with skin conditions, symptoms, and general skin health questions. What can I help you with today?",
                "Hi there! 😊 Welcome to our AI medical assistant. I specialize in skin health and can provide information about various skin conditions. How may I assist you?",
                "Greetings! 🌟 I'm your virtual dermatology consultant. Feel free to ask me about skin concerns, symptoms, or general skincare advice. What's on your mind?"
            };
            return greetings[Random.Shared.Next(greetings.Length)];
        }

        private string GetThankingResponse()
        {
            var responses = new[]
            {
                "You're very welcome! 😊 I'm glad I could help. Feel free to ask if you have any other skin health questions!",
                "Happy to help! 🌟 Remember, I'm here whenever you need information about skin conditions or general dermatology questions.",
                "My pleasure! 👍 Don't hesitate to reach out if you have more questions about skin health or need guidance on using our diagnosis features."
            };
            return responses[Random.Shared.Next(responses.Length)];
        }

        private string GetEmergencyResponse()
        {
            return "🚨 **EMERGENCY NOTICE** 🚨\n\n" +
                   "If you're experiencing a medical emergency, please:\n" +
                   "• **Call emergency services immediately (911/112)**\n" +
                   "• **Go to your nearest emergency room**\n" +
                   "• **Contact your doctor right away**\n\n" +
                   "For severe allergic reactions, difficulty breathing, or serious injuries, do NOT wait - seek immediate medical attention!\n\n" +
                   "⚠️ This AI assistant cannot replace emergency medical care.";
        }

        private string GetSymptomBasedResponse(string message)
        {
            // Complex symptom combinations
            if (message.Contains("red") && message.Contains("itchy"))
            {
                return "**Red and itchy skin** can indicate several conditions:\n\n" +
                       "🔹 **Eczema/Dermatitis**: Chronic inflammatory condition\n" +
                       "🔹 **Contact dermatitis**: Reaction to irritants or allergens\n" +
                       "🔹 **Allergic reaction**: Response to food, medication, or environmental factors\n" +
                       "🔹 **Fungal infection**: Especially in warm, moist areas\n\n" +
                       "**Immediate care**: Cool compresses, fragrance-free moisturizer, avoid scratching\n" +
                       "**See a doctor if**: Symptoms worsen, spread, or don't improve in 2-3 days\n\n" +
                       "💡 Use our AI image analysis for visual assessment!";
            }

            if (message.Contains("painful") && (message.Contains("bump") || message.Contains("lump")))
            {
                return "**Painful bumps or lumps** require attention:\n\n" +
                       "🔹 **Cyst**: Fluid-filled sac that can become infected\n" +
                       "🔹 **Boil/Abscess**: Bacterial infection requiring treatment\n" +
                       "🔹 **Ingrown hair**: Common in shaved areas\n" +
                       "🔹 **Lipoma**: Usually painless unless pressing on nerves\n\n" +
                       "**Red flags**: Increasing pain, fever, red streaking, rapid growth\n" +
                       "**Action needed**: See healthcare provider for painful or growing lumps\n\n" +
                       "🚨 Don't try to pop or squeeze - this can worsen infection!";
            }

            if (message.Contains("scaly") && message.Contains("patches"))
            {
                return "**Scaly patches** can indicate several conditions:\n\n" +
                       "🔹 **Psoriasis**: Thick, silvery scales on elbows, knees, scalp\n" +
                       "🔹 **Seborrheic dermatitis**: Yellowish, greasy scales\n" +
                       "🔹 **Eczema**: Dry, flaky patches that may be itchy\n" +
                       "🔹 **Fungal infection**: Especially ring-shaped patches\n\n" +
                       "**Management**: Regular moisturizing, gentle cleansers\n" +
                       "**Professional care**: Needed for proper diagnosis and treatment\n\n" +
                       "📸 Our AI diagnosis can help identify specific patterns!";
            }

            return string.Empty;
        }

        private string GetConditionBasedResponse(string message)
        {
            var conditions = new Dictionary<string, string>
            {
                { "acne", "**ACNE MANAGEMENT** 🎯\n\n" +
                         "**Causes**: Clogged pores, bacteria (P. acnes), hormones, genetics\n\n" +
                         "**Treatment steps**:\n" +
                         "1️⃣ Gentle cleansing (twice daily)\n" +
                         "2️⃣ Non-comedogenic products only\n" +
                         "3️⃣ Start with salicylic acid or benzoyl peroxide\n" +
                         "4️⃣ Moisturize (oil-free formulas)\n" +
                         "5️⃣ Sunscreen daily\n\n" +
                         "**Avoid**: Over-washing, picking, oil-based products\n" +
                         "**See dermatologist if**: Cystic acne, scarring, or no improvement in 6-8 weeks" },

                { "eczema", "**ECZEMA (ATOPIC DERMATITIS)** 🌿\n\n" +
                           "**Symptoms**: Dry, itchy, inflamed patches of skin\n" +
                           "**Common triggers**: Stress, allergens, weather changes, rough fabrics\n\n" +
                           "**Management strategy**:\n" +
                           "✅ Moisturize within 3 minutes of bathing\n" +
                           "✅ Use fragrance-free, hypoallergenic products\n" +
                           "✅ Take lukewarm (not hot) showers\n" +
                           "✅ Identify and avoid personal triggers\n" +
                           "✅ Consider wet wrap therapy for severe flares\n\n" +
                           "**Prescription options**: Topical corticosteroids, calcineurin inhibitors, dupilumab" },

                { "psoriasis", "**PSORIASIS** 🔬\n\n" +
                              "**Characteristics**: Thick, red patches with silvery scales\n" +
                              "**Autoimmune condition**: Immune system attacks healthy skin cells\n\n" +
                              "**Treatment options**:\n" +
                              "🔸 **Topical**: Corticosteroids, vitamin D analogs, retinoids\n" +
                              "🔸 **Light therapy**: UVB phototherapy, PUVA treatments\n" +
                              "🔸 **Systemic**: For moderate-severe cases (biologics, immunosuppressants)\n\n" +
                              "**Lifestyle factors**: Stress management, healthy diet, avoid triggers\n" +
                              "**Common triggers**: Stress, infections, medications, skin injury (Koebner phenomenon)" },

                { "rosacea", "**ROSACEA** 🌹\n\n" +
                            "**Signs**: Facial redness, visible blood vessels, bumps, eye irritation\n" +
                            "**Common areas**: Cheeks, nose, forehead, chin\n\n" +
                            "**Management**:\n" +
                            "🌞 Daily broad-spectrum sunscreen (SPF 30+)\n" +
                            "🌶️ Identify food triggers (spicy foods, hot beverages, alcohol)\n" +
                            "❄️ Avoid extreme temperatures\n" +
                            "🧴 Use gentle, fragrance-free skincare\n" +
                            "💆‍♀️ Manage stress levels\n\n" +
                            "**Treatment**: Topical metronidazole, azelaic acid, oral antibiotics available" },

                { "pigmentation", "**PIGMENTATION DISORDERS** 🎨\n\n" +
                         "**Types**:\n" +
                         "🔸 **Hyperpigmentation**: Dark patches (melasma, age spots, post-inflammatory)\n" +
                         "🔸 **Hypopigmentation**: Light patches (vitiligo, pityriasis alba)\n\n" +
                         "**Common causes**: Sun exposure, hormones, inflammation, genetics\n\n" +
                         "**Treatment options**:\n" +
                         "☀️ Strict sun protection (most important!)\n" +
                         "🧴 Topical lightening agents (hydroquinone, kojic acid, vitamin C)\n" +
                         "⚡ Professional treatments: chemical peels, laser therapy, microneedling\n\n" +
                         "**Prevention**: Daily SPF 50+, protective clothing, avoid picking at skin" },

                { "melasma", "**MELASMA** ☀️\n\n" +
                    "**Description**: Brown/gray patches, usually on face\n" +
                    "**Triggers**: Hormones (pregnancy, birth control), sun exposure\n\n" +
                    "**Prevention & treatment**:\n" +
                    "🛡️ **Strict sun protection** (most important!)\n" +
                    "🧴 Broad-spectrum SPF 50+ daily\n" +
                    "👒 Wide-brimmed hats, UV-protective clothing\n" +
                    "💊 Consider hormone evaluation\n" +
                    "🎨 Topical lightening agents (hydroquinone, tretinoin, azelaic acid)\n" +
                    "⚡ Professional treatments: chemical peels, laser therapy (with caution)" },

                { "infectious", "**INFECTIOUS SKIN CONDITIONS** 🦠\n\n" +
                       "**Types**:\n" +
                       "🔹 **Bacterial**: Impetigo, cellulitis, folliculitis\n" +
                       "🔹 **Viral**: Warts, cold sores, shingles\n" +
                       "🔹 **Fungal**: Ringworm, athlete's foot, candidiasis\n" +
                       "🔹 **Parasitic**: Scabies, lice\n\n" +
                       "**Treatment**: Requires proper diagnosis - may need antibiotics, antivirals, or antifungals\n" +
                       "**Prevention**: Good hygiene, avoid sharing personal items, keep skin clean and dry\n\n" +
                       "🚨 **See a doctor if**: Spreading rapidly, fever, severe pain, or not improving" },

                { "benign tumor", "**BENIGN SKIN GROWTHS** 🔍\n\n" +
                         "**Common types**:\n" +
                         "🔸 **Moles (Nevi)**: Usually harmless, monitor for changes\n" +
                         "🔸 **Skin tags**: Small, soft growths in friction areas\n" +
                         "🔸 **Seborrheic keratoses**: Waxy, stuck-on appearance\n" +
                         "🔸 **Lipomas**: Soft, movable lumps of fat tissue\n" +
                         "🔸 **Cherry angiomas**: Bright red, dome-shaped spots\n\n" +
                         "**When to see a doctor**: Changes in size, shape, color, bleeding, or itching\n" +
                         "**Removal**: Usually optional unless causing discomfort or cosmetic concerns" },

                { "malignant tumor", "**MALIGNANT SKIN TUMORS** ⚠️🚨\n\n" +
                            "**CRITICAL - REQUIRES IMMEDIATE MEDICAL ATTENTION**\n\n" +
                            "**Types of skin cancer**:\n" +
                            "🔺 **Melanoma**: Most dangerous, can spread quickly\n" +
                            "🔺 **Basal Cell Carcinoma**: Most common, slow-growing\n" +
                            "🔺 **Squamous Cell Carcinoma**: Can spread if untreated\n\n" +
                            "**Warning signs (ABCDE)**:\n" +
                            "A - Asymmetry\n" +
                            "B - Border irregularity\n" +
                            "C - Color variation\n" +
                            "D - Diameter > 6mm\n" +
                            "E - Evolving/changing\n\n" +
                            "🚨 **ACTION REQUIRED**: Schedule appointment with dermatologist or oncologist IMMEDIATELY\n" +
                            "**Early detection saves lives!**" }
            };

            foreach (var condition in conditions)
            {
                if (message.Contains(condition.Key))
                {
                    return condition.Value + "\n\n⚠️ **Always consult a dermatologist for proper diagnosis and treatment plan.**";
                }
            }

            return string.Empty;
        }

        private string GetTreatmentBasedResponse(string message)
        {
            if (message.Contains("moisturizer") || message.Contains("cream"))
            {
                return "**CHOOSING THE RIGHT MOISTURIZER** 🧴\n\n" +
                       "**For dry skin**: Look for ceramides, hyaluronic acid, glycerin\n" +
                       "**For sensitive skin**: Fragrance-free, hypoallergenic formulas\n" +
                       "**For acne-prone skin**: Non-comedogenic, oil-free options\n" +
                       "**For eczema**: Thick, occlusive creams or ointments\n\n" +
                       "**Application tips**:\n" +
                       "• Apply to slightly damp skin\n" +
                       "• Use within 3 minutes of bathing\n" +
                       "• Reapply throughout the day as needed\n\n" +
                       "**Ingredients to avoid**: Fragrances, dyes, harsh preservatives";
            }

            if (message.Contains("sunscreen") || message.Contains("sun protection"))
            {
                return "**SUN PROTECTION ESSENTIALS** ☀️\n\n" +
                       "**SPF recommendations**:\n" +
                       "• Daily use: SPF 30 minimum\n" +
                       "• Extended outdoor time: SPF 50+\n" +
                       "• Reapply every 2 hours\n\n" +
                       "**Types**:\n" +
                       "🔸 **Physical/Mineral**: Zinc oxide, titanium dioxide (gentle)\n" +
                       "🔸 **Chemical**: Absorbs UV rays (may irritate sensitive skin)\n\n" +
                       "**Pro tips**:\n" +
                       "• Apply 15-30 minutes before sun exposure\n" +
                       "• Don't forget ears, neck, feet!\n" +
                       "• Seek shade between 10 AM - 4 PM";
            }

            return string.Empty;
        }

        private string GetDemographicBasedResponse(string message)
        {
            if (message.Contains("baby") || message.Contains("infant"))
            {
                return "**INFANT SKIN CARE** 👶\n\n" +
                       "**Common concerns**: Diaper rash, cradle cap, baby acne\n" +
                       "**Gentle care**: Fragrance-free products, minimal bathing\n" +
                       "**When to worry**: Fever, unusual rashes, persistent crying\n\n" +
                       "🚨 **Always consult your pediatrician for infant skin concerns**";
            }

            if (message.Contains("elderly") || message.Contains("aging"))
            {
                return "**MATURE SKIN CARE** 👵👴\n\n" +
                       "**Common changes**: Thinner skin, slower healing, dryness\n" +
                       "**Special care**: Extra gentle products, increased moisturizing\n" +
                       "**Watch for**: New growths, changes in moles, slow-healing wounds\n\n" +
                       "**Prevention**: Daily sunscreen, regular skin checks";
            }

            return string.Empty;
        }

        private string GetPreventionBasedResponse(string message)
        {
            if (message.Contains("prevent") || message.Contains("avoid"))
            {
                return "**SKIN HEALTH PREVENTION** 🛡️\n\n" +
                       "**Daily essentials**:\n" +
                       "☀️ Broad-spectrum sunscreen (SPF 30+)\n" +
                       "💧 Adequate hydration (8+ glasses water)\n" +
                       "🧴 Gentle, consistent skincare routine\n" +
                       "🥗 Balanced diet rich in antioxidants\n" +
                       "😴 Quality sleep (7-9 hours)\n" +
                       "🚭 Avoid smoking and excessive alcohol\n\n" +
                       "**Monthly**: Self-examine skin for changes\n" +
                       "**Annually**: Professional skin check with dermatologist";
            }

            return string.Empty;
        }

        private string GetDefaultHelpfulResponse()
        {
            var responses = new[]
            {
                "I'm here to help with skin health questions! 🌟\n\n" +
                "**I can assist with**:\n" +
                "• Skin condition information (acne, eczema, psoriasis, etc.)\n" +
                "• Symptom guidance and when to see a doctor\n" +
                "• Skincare routine recommendations\n" +
                "• Treatment options and prevention tips\n" +
                "• Using our AI diagnosis features\n\n" +
                "**What specific skin concern would you like to discuss?**",

                "Let me help you with your skin health concerns! 💫\n\n" +
                "**Popular topics I can help with**:\n" +
                "🔹 Identifying skin conditions and symptoms\n" +
                "🔹 Skincare product recommendations\n" +
                "🔹 Treatment and prevention strategies\n" +
                "🔹 When to seek professional medical care\n" +
                "🔹 Understanding skin changes and aging\n\n" +
                "**Feel free to describe your symptoms or ask specific questions!**"
            };
            
            return responses[Random.Shared.Next(responses.Length)] + 
                   "\n\n⚠️ **Remember**: This information is educational. Always consult healthcare professionals for diagnosis and treatment.";
        }
    }

    public class ChatMessage
    {
        public string Message { get; set; } = string.Empty;
    }

    public class HuggingFaceResponse
    {
        public string generated_text { get; set; } = string.Empty;
    }
}