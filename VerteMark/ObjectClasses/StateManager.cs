namespace VerteMark.ObjectClasses
{
    /// <summary>
    /// Stavy aplikace.
    /// </summary>
    public enum AppState
    {
        /// <summary>Režim kreslení anotací</summary>
        Drawing,
        /// <summary>Režim ořezávání obrázku</summary>
        Cropping,
        /// <summary>Režim pouze pro čtení</summary>
        ReadOnly
    }

    /// <summary>
    /// Správce stavu aplikace.
    /// Spravuje přechody mezi stavy aplikace a publikuje události při změně stavu.
    /// </summary>
    public class StateManager
    {
        /// <summary>
        /// Událost vyvolaná při změně stavu aplikace.
        /// </summary>
        public event EventHandler<AppState> StateChanged;

        private AppState currentState;

        /// <summary>
        /// Získá nebo nastaví aktuální stav aplikace.
        /// Při nastavení vyvolá událost StateChanged.
        /// </summary>
        public AppState CurrentState
        {
            get { return currentState; }
            set
            {
                currentState = value;
                StateChanged?.Invoke(this, currentState);
            }
        }
    }
}
