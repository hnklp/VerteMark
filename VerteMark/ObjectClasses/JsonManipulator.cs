﻿using System;
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

            public Tuple<int, int> ImageDimension { get; private set; } // jeden tuple List<int, int> (75,99)

            public List<Dictionary<string, List<Tuple<int, int>>>> Annotations { get; private set; }


            public JsonManipulator() {
                ImageDimension = new Tuple<int, int>(1920, 1080);
                // rozmery obrazku, prijme od projektu posledni zmeny velikosti
            }

            // naimportovat atributy usera a vsechny anotace
            void ImportAttributes(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations) {
                // načtení základních atributů

                DateTime theTime = DateTime.Now;

                if (user.Validator == true) {
                    ValidatorID = user.UserID;
                    ValidationDate = theTime.ToString("dd. MM. yyyy");
                } else {
                    AnnotatorID = user.UserID;
                    LastEditDate = theTime.ToString("dd. MM. yyyy");
                }

                Annotations = programAnnotations;
            }

            // vytvoreni jsonu na zaklade metody ImportAttributes
            public string CreateJson() {
                string stringJson = JsonConvert.SerializeObject(this);
                return stringJson;
            }


            // pro jednoduchost muze Project vyuzit jen metodu ExportJson, ktera kombinuje predchozi metody
            public string ExportJson(User user, List<Dictionary<string, List<Tuple<int, int>>>> programAnnotations) {
                ImportAttributes(user, programAnnotations);
                string createdJson = CreateJson();

                return createdJson;
            }

            // InvertedJson
            /*
            public List<T> InvertedJson(string createdJson)
            {
                JsonManipulator loadedJson = JsonManipulator.InvertedJson(createdJson);
                List<T> returnValues = new List<T>;

                returnValues.add(loadedJson.AnnotatorID);
                returnValues.add(loadedJson.ValidatorID);
                returnValues.add(loadedJson.Annotations);

                return returnValues;
            }
            */

    }
}