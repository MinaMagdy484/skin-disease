using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.IO;

namespace WebApplication1.Controllers
{
    public class PredictionController : Controller
    {
        private readonly ILogger<PredictionController> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly IConfiguration _configuration;

        public PredictionController(ILogger<PredictionController> logger, IWebHostEnvironment environment, IConfiguration configuration)
        {
            _logger = logger;
            _environment = environment;
            _configuration = configuration;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PredictImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return Json(new { success = false, error = "Please select an image file." });
            }

            try
            {
                // Validate image format
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".bmp", ".gif" };
                var fileExtension = Path.GetExtension(imageFile.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(fileExtension))
                {
                    return Json(new { success = false, error = "Invalid file format. Please upload JPG, PNG, BMP, or GIF images." });
                }

                // Convert to byte array instead of saving to disk
                byte[] imageData;
                using (var memoryStream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(memoryStream);
                    imageData = memoryStream.ToArray();
                }

                // Call prediction service
                var result = await CallPredictionService(imageData);
                return Json(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during image prediction");
                return Json(new { success = false, error = "An error occurred during prediction. Please try again." });
            }
        }

        private async Task<object> CallPredictionService(byte[] imageData)
        {
            try
            {
                var apiUrl = _configuration["PredictionService:Url"] ?? "http://127.0.0.1:5000";
                
                using var httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromSeconds(30);
                
                using var form = new MultipartFormDataContent();
                using var streamContent = new ByteArrayContent(imageData);
                
                form.Add(streamContent, "file", "image.jpg");

                var response = await httpClient.PostAsync($"{apiUrl}/predict", form);
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync();
                    var prediction = JsonSerializer.Deserialize<JsonElement>(responseContent);
                    
                    // Get original predicted class
                    var originalClass = prediction.GetProperty("predicted_class").GetString();
                    
                    // Translate all predictions
                    var allPredictions = prediction.GetProperty("all_predictions");
                    var translatedPredictions = new Dictionary<string, double>();
                    
                    foreach (var pred in allPredictions.EnumerateObject())
                    {
                        var translatedName = TranslateDiseaseName(pred.Name);
                        translatedPredictions[translatedName] = pred.Value.GetDouble();
                    }
                    
                    return new { 
                        success = true,
                        predicted_class = TranslateDiseaseName(originalClass),
                        confidence = prediction.GetProperty("confidence").GetDouble(),
                        all_predictions = translatedPredictions
                    };
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"Prediction service returned error: {response.StatusCode} - {errorContent}");
                    return new { success = false, error = "Prediction service unavailable" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling prediction service");
                return new { success = false, error = "Unable to process image prediction" };
            }
        }

        private string TranslateDiseaseName(string turkishName)
        {
            var translations = new Dictionary<string, string>
            {
                { "1. Enfeksiyonel", "1. Infectious" },
                { "2. Ekzama", "2. Eczema" },
                { "3. Akne", "3. Acne" },
                { "4. Pigment", "4. Pigmentation" },
                { "5. Benign", "5. Benign Tumor" },
                { "6. Malign", "6. Malignant Tumor" }
            };
            
            return translations.ContainsKey(turkishName) ? translations[turkishName] : turkishName;
        }
    }
}