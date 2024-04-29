using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;


namespace VerteMark.ObjectClasses
{
    class JsonManipulator
    {

        public string? ValidatorID { get; private set; }
        public string? AnnotatorID { get; private set; }
        public string? LastEditDate { get; private set; } // automaticky se dopln� ve chv�li kdy export
        public string? ValidationDate { get; private set; }

        public Tuple<int, int> ImageDimension { get; private set; } // jeden tuple List<int, int> (75,99)

        public List<Dictionary<string, List<Tuple<int, int>>>> Annotations { get; private set; }


        public JsonManipulator()
        {
            ImageDimension = new Tuple<int, int>(1920, 1080);
            // rozmery obrazku, prijme od projektu posledni zmeny velikosti
        }

        // naimportovat atributy usera a vsechny anotace
        public void ImportAttributes(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations)
        {
            // na�ten� z�kladn�ch atribut�

            DateTime theTime = DateTime.Now;

            if (user.Validator == true)
            {
                ValidatorID = user.UserID;
                ValidationDate = theTime.ToString("dd. MM. yyyy");
            }
            else
            {
                AnnotatorID = user.UserID;
                LastEditDate = theTime.ToString("dd. MM. yyyy");
            }

            Annotations = programAnnotations;
        }

        // vytvoreni jsonu na zaklade metody ImportAttributes
        public string CreateJson()
        {
            string stringJson = JsonConvert.SerializeObject(this);
            return stringJson;
        }

        
        // pro jednoduchost muze Project vyuzit jen metodu ExportJson, ktera kombinuje predchozi metody
        public string ExportJson(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations)
        {
            ImportAttributes(user, programAnnotations);
            string createdJson = CreateJson();

            return createdJson;
        }

        // UnpackJson - lepsi funkce pro rozbaleni json stringu
        public List<object> UnpackJson(string createdJson)
        {
            JsonManipulator loadedJson = JsonConvert.DeserializeObject<JsonManipulator>(createdJson);
            List<object> returnValues = new List<object>();

            returnValues.Add(loadedJson.AnnotatorID);
            returnValues.Add(loadedJson.ValidatorID);
            returnValues.Add(loadedJson.Annotations);

            return returnValues;
        }
    }
}
