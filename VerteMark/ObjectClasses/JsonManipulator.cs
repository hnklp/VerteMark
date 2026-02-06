using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace VerteMark.ObjectClasses
{
    /// <summary>
    /// Třída pro serializaci a deserializaci anotací do/z JSON formátu.
    /// </summary>
    internal class JsonManipulator {
            /// <summary>ID validátora, který validoval anotace</summary>
            public string? ValidatorID { get; private set; }
            /// <summary>ID anotátora, který vytvořil anotace</summary>
            public string? AnnotatorID { get; private set; }
            /// <summary>Datum a čas poslední úpravy anotací</summary>
            public string? LastEditDate { get; private set; } // automaticky se doplní ve chvíli kdy export
            /// <summary>Datum a čas validace anotací</summary>
            public string? ValidationDate { get; private set; }

            /// <summary>Seznam anotací ve formátu slovníku</summary>
            public List<Dictionary<string, List<Tuple<int, int>>>>? Annotations { get; private set; }
            /// <summary>Seznam ID validovaných anotací</summary>
            public List<string>? ValidatedAnnotations { get; private set; }


            /// <summary>
            /// Naimportuje atributy uživatele a všechny anotace do instance třídy.
            /// </summary>
            /// <param name="user">Uživatel (anotátor nebo validátor)</param>
            /// <param name="programAnnotations">Seznam anotací ve formátu slovníku</param>
            /// <param name="programValidatedAnnotations">Seznam ID validovaných anotací</param>
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

            /// <summary>
            /// Vytvoří JSON řetězec z aktuální instance třídy.
            /// </summary>
            /// <returns>JSON řetězec reprezentující anotace a metadata</returns>
            string CreateJson() {
                string stringJson = JsonConvert.SerializeObject(this);
                return stringJson;
            }


            /// <summary>
            /// Exportuje anotace do JSON formátu.
            /// Kombinuje ImportAttributes a CreateJson pro jednoduché použití.
            /// </summary>
            /// <param name="user">Uživatel (anotátor nebo validátor)</param>
            /// <param name="programAnnotations">Seznam anotací ve formátu slovníku</param>
            /// <param name="programValidatedAnnotations">Seznam ID validovaných anotací</param>
            /// <returns>JSON řetězec obsahující anotace a metadata</returns>
            public string ExportJson(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations, List<string> programValidatedAnnotations) {
                ImportAttributes(user, programAnnotations, programValidatedAnnotations);
                string createdJson = CreateJson();

                return createdJson;
            }


            /// <summary>
            /// Rozbalí JSON řetězec a vrátí seznam anotací a validovaných anotací.
            /// </summary>
            /// <param name="createdJson">JSON řetězec k rozbalení</param>
            /// <returns>Seznam obsahující JArray anotací a JArray validovaných anotací, nebo null při chybě</returns>
            public List<JArray>? UnpackJson(string createdJson) {
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
    }
}
