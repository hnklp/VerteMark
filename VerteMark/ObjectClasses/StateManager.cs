namespace VerteMark.ObjectClasses
{
    public enum AppState
    {
        Drawing,
        Cropping
    }

    public class StateManager
    {
        public event EventHandler<AppState> StateChanged;

        private AppState currentState;

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
