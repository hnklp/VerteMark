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


            // naimportovat atributy usera a vsechny anotace
            void ImportAttributes(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations, List<string> programValidatedAnnotations) {
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
            }

            // vytvoreni jsonu na zaklade metody ImportAttributes
            string CreateJson() {
                string stringJson = JsonConvert.SerializeObject(this);
                return stringJson;
            }


            // pro jednoduchost muze Project vyuzit jen metodu ExportJson, ktera kombinuje predchozi metody
            public string ExportJson(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations, List<string> programValidatedAnnotations) {
                ImportAttributes(user, programAnnotations, programValidatedAnnotations);
                string createdJson = CreateJson();

                return createdJson;
            }


        // UnpackJson - lepsi funkce pro rozbaleni json stringu
            public List<JArray?> UnpackJson(string createdJson) {
            JObject jsonObject = JObject.Parse(createdJson);
            // Získání seznamu anotací ze zpracovaného JObject
            JArray? annotationsArray = (JArray?)jsonObject["Annotations"];
            JArray? validatedAnnotationsArray = (JArray?)jsonObject["ValidatedAnnotations"];

            List<JArray?> GatheredAnnotations = new List<JArray?>
            {
                annotationsArray,
                validatedAnnotationsArray
            };

            return GatheredAnnotations;
            }
    }
}
