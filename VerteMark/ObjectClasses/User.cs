namespace VerteMark.ObjectClasses
{
    /// <summary>
    /// Reprezentuje přihlášeného uživatele aplikace.
    /// </summary>
    class User {
        /// <summary>Identifikátor uživatele</summary>
        public string UserID {get; private set;}
        /// <summary>True, pokud je uživatel validátor, false pokud anotátor</summary>
        public bool Validator {get; private set;}

        /// <summary>
        /// Vytvoří novou instanci uživatele.
        /// </summary>
        /// <param name="id">Identifikátor uživatele</param>
        /// <param name="valid">True pro validátora, false pro anotátora</param>
        public User(string id, bool valid) {
            UserID = id; //vychozi hodnota, dle ktere se muze odehravat 
            Validator = valid; // True = validator, False = anotator
        }
    }
}
