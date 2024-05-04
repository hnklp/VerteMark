using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace VerteMark.ObjectClasses {
    internal class JsonManipulator {
            public string? ValidatorID { get; private set; }
            public string? AnnotatorID { get; private set; }
            public string? LastEditDate { get; private set; } // automaticky se doplní ve chvíli kdy export
            public string? ValidationDate { get; private set; }

            public List<Dictionary<string, List<Tuple<int, int>>>> Annotations { get; private set; }


            // naimportovat atributy usera a vsechny anotace
            void ImportAttributes(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations) {
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
            }

            // vytvoreni jsonu na zaklade metody ImportAttributes
            string CreateJson() {
                string stringJson = JsonConvert.SerializeObject(this);
                return stringJson;
            }


            // pro jednoduchost muze Project vyuzit jen metodu ExportJson, ktera kombinuje predchozi metody
            public string ExportJson(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations) {
                ImportAttributes(user, programAnnotations);
                string createdJson = CreateJson();

                return createdJson;
            }


            // UnpackJson - lepsi funkce pro rozbaleni json stringu
            public object UnpackJson(string createdJson)
            {
                JsonManipulator loadedJson = JsonConvert.DeserializeObject<JsonManipulator>(createdJson);
                return loadedJson.Annotations;
            }
    }
}
