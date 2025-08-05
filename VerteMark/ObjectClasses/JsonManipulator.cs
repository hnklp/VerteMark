using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VerteMark.ObjectClasses
{
    internal class JsonManipulator {
            public string? ValidatorID { get; private set; }
            public string? AnnotatorID { get; private set; }
            public string? LastEditDate { get; private set; } // automaticky se doplní ve chvíli kdy export
            public string? ValidationDate { get; private set; }

            public List<Dictionary<string, List<Tuple<int, int>>>>? Annotations { get; private set; }
            public List<string>? ValidatedAnnotations { get; private set; }
            
            // Pacientovo metadata
            public JObject? PatientMetadata { get; private set; }

            // naimportovat atributy usera a vsechny anotace
            void ImportAttributes(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations, List<string> programValidatedAnnotations, JObject patientMetadata = null) {
                // načtení základních atributů

                DateTime theTime = DateTime.Now;

                if (user.Validator) {
                    ValidatorID = user.UserID;
                    ValidationDate = theTime.ToString("dd. MM. yyyy HH:mm:ss");
                } else {
                    AnnotatorID = user.UserID;
                    LastEditDate = theTime.ToString("dd. MM. yyyy HH:mm:ss");
                }

                Annotations = programAnnotations;
                ValidatedAnnotations = programValidatedAnnotations;
                PatientMetadata = patientMetadata;
        }

            // vytvoreni jsonu na zaklade metody ImportAttributes
            string CreateJson() {
                string stringJson = JsonConvert.SerializeObject(this);
                return stringJson;
            }


            // pro jednoduchost muze Project vyuzit jen metodu ExportJson, ktera kombinuje predchozi metody
            public string ExportJson(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations, List<string> programValidatedAnnotations, JObject patientMetadata = null)
            {
                ImportAttributes(user, programAnnotations, programValidatedAnnotations, patientMetadata);
                string createdJson = CreateJson();

                return createdJson;
            }


        // UnpackJson - lepsi funkce pro rozbaleni json stringu
        public List<JArray>? UnpackJson(string createdJson)
            {
                if (string.IsNullOrWhiteSpace(createdJson))
                {
                    return null;
                }

                try
                {
                    JObject jsonObject = JObject.Parse(createdJson);
                    // Získání seznamu anotací ze zpracovaného JObject
                    JArray? annotationsArray = (JArray?)jsonObject["Annotations"];
                    JArray? validatedAnnotationsArray = (JArray?)jsonObject["ValidatedAnnotations"];

                    List<JArray>? GatheredAnnotations = new List<JArray>
                    {
                        annotationsArray,
                        validatedAnnotationsArray
                    };

                    return GatheredAnnotations;
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing errors
                    System.Diagnostics.Debug.WriteLine($"Error parsing JSON: {ex.Message}");
                    return null;
                }
            }

            // Add method to extract patient metadata from JSON
            public JObject? GetPatientMetadataFromJson(string createdJson)
            {
                if (string.IsNullOrWhiteSpace(createdJson))
                {
                    return null;
                }

                try
                {
                    JObject jsonObject = JObject.Parse(createdJson);
                    // Check if PatientMetadata exists (for backward compatibility)
                    if (jsonObject.ContainsKey("PatientMetadata"))
                    {
                        return (JObject?)jsonObject["PatientMetadata"];
                    }
                    return null;
                }
                catch (JsonException ex)
                {
                    // Handle JSON parsing errors
                    System.Diagnostics.Debug.WriteLine($"Error parsing JSON for patient metadata: {ex.Message}");
                    return null;
                }
            }
    }
}
