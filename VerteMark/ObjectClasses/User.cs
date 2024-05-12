namespace VerteMark.ObjectClasses
{


    class User {
        public string UserID {get; private set;}
        public bool Validator {get; private set;}

        public User(string id, bool valid) {
            UserID = id; //vychozi hodnota, dle ktere se muze odehravat 
            Validator = valid; // True = validator, False = anotator
        }
    }
}
