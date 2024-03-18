using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Linq;


namespace VerteMark.ObjectClasses {
    
    interface IUser
    {
        void logIn();

        void logOut();

        void unselectID();

        void selectID(string selectedID);

        void unselectMod();

        void selectMod(bool? validatorChoice);
    }
    
    class User : IUser 
    {
        public string userID {get; private set;}
        public bool? Validator {get; private set;}

        // (KROK 1) vytváří si usera pri zapnuti session (zapnuti programu)
        public User()
        {
            userID = null; //vychozi hodnota, dle ktere se muze odehravat 
            Validator = null; // True = validator, False = anotator
        }

        public void unselectID()
        {
            userID = null;
        }
        
        // (KROK 2) bere si string hodnotu z fill boxu
        public void selectID(string selectedID)
        {
            userID = selectedID;
        }

        public void unselectMod()
        {
            Validator = null;
        }

        // (KROK 3) bere si bool hodnotu z radio boxu
        public void selectMod(bool? validatorChoice)
        {
            Validator = validatorChoice;
        }

        // (KROK 4) do jsonu při načtení canvasu hodí údaj o ID a módu příhlašenýho
        //          dle hodnoty this.Validator načte příslušný canvas
        public void logIn()
        {
            // program.openCanvas(this.Validator) - vstup do canvas prostredi (True - validator, False - anotator)
            // program.registerUserInfo(this.userID, this.Validator) - vyplivnuti hodnot do json souboru
        }

        public void logOut()
        {
            // vykona vsechny funkce, ktere si musi vykonat pri ukonceni prace jako treba:
            // program.OpenWindowAreYouSureYouWantToQuit()
            // program.SaveProgress()
            unselectID();
            unselectMod();
            // program.getBackToLoginWindow zavolá funkci, která hodí program do jiného okna -> vybrání validátor/anotátor
        }


    }

    class Program 
    {
        static void Main (string [] args)
        {
            // na zacatku session se nacte json soubor (mam list pro jednoduchost)
            // na zacatku session se vytvori uzivatel
            List<string> provizorniJsonJakoList = new List<string>();
            User sessionUser = new User();

            // uživatel kouká do okna, kde má zadávat ID a mód (validator, anotátor)
            // vyplní okna

            string importedZadavaciBox = "1"; 
            sessionUser.selectID(importedZadavaciBox);

            bool importedRadioBox = true;
            sessionUser.selectMod(importedRadioBox);
            
            // předpokládám že tento loginStatusButton/logoutStatusButton je objekt a budu u něj později moct využít enum a case
            // loginStatusButton;
            // logoutStatusButton;

            // switch
            // case loginStatusButton.clicked:
            //           sessionUser.logIn(); // vstup do canvas prostoru
            // case loginStatusButton.clicked:
            //           sessionUser.logOut();
        }
    }
}
